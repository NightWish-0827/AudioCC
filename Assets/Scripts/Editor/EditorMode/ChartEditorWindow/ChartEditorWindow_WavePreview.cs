using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Linq;
using MyRhythmEditor;

public partial class ChartEditorWindow
{
    // ─────────────────────────────────────
    // Wave Preview
    // ─────────────────────────────────────
    private void DrawWavePreview()
    {
        if (!waveClip) return;

        float totalTime = waveClip.length;
        float baseWidth = 1024f;
        float baseHeight = 256f;

        float contentWidth = baseWidth * waveZoomX;
        float contentHeight = baseHeight * waveZoomY;

        // 뷰포트 영역
        Rect viewRect = EditorGUILayout.GetControlRect(GUILayout.Height(600)); // 뷰포트 넓이
        Event e = Event.current;

        // 마우스 위치
        Vector2 mousePos = e.mousePosition;
        bool isMouseInView = viewRect.Contains(mousePos);

        // 스크롤뷰 시작
        Rect contentRect = new Rect(0, 0, contentWidth, contentHeight);
        Vector2 oldScrollPos = waveScrollPos;
        waveScrollPos = GUI.BeginScrollView(viewRect, waveScrollPos, contentRect);

        // 파형 그리기 영역
        Rect waveRect = new Rect(0, 0, contentWidth, contentHeight);

        // 배경
        Handles.BeginGUI();
        Handles.DrawSolidRectangleWithOutline(waveRect, new Color(0, 0, 0, 0.3f), Color.black);
        Handles.EndGUI();

        // 파형
        if (useLineWave)
        {
            Handles.BeginGUI();
            DrawWaveSegmentAsLinePartial(waveRect, waveClip, 0f, totalTime, waveAmplitudeFactor, Color.white, waveScrollPos);
            Handles.EndGUI();
        }

        // 타임라인, 노트, 플레이백 커서
        Handles.BeginGUI();
        if (useBpmTimeline)
            DrawBpmTimelineGrid(waveRect, totalTime);
        else
            DrawTimeAxis(waveRect, totalTime);

        DrawNotes(waveRect, totalTime);
        DrawPlaybackCursor(waveRect, totalTime);
        Handles.EndGUI();

        // 마우스 입력 처리
        if (isMouseInView)
        {
            Vector2 localMousePos = mousePos - viewRect.position;
            float scrolledX = localMousePos.x + waveScrollPos.x;
            float normalizedX = scrolledX / contentWidth;
            float timePosition = normalizedX * totalTime;
            timePosition = Mathf.Clamp(timePosition, 0f, totalTime);

            if (e.type != EventType.Layout && e.type != EventType.Repaint)
            {
                // 상세 보기 구역 드래그 처리
                HandleDetailZoneDrag(waveRect, normalizedX, e);
                // 노트 입력 처리
                HandleWaveInput(waveRect, timePosition, totalTime);
            }
        }

        GUI.EndScrollView();
        /*
        enableNotePlacement = GUILayout.Toggle(enableNotePlacement, "Enable Note Placement?");
        EditorGUILayout.LabelField("(Left= create, Drag=move, Shift+Drag=LN, Right=delete)");
        */
    }

    // ─────────────────────────────────────
    // WaveformData / Cache
    // ─────────────────────────────────────
    private class WaveformData
    {
        public float[] samples;
        public float[] minMaxData;
        public int channels;
        public int sampleRate;
    }

    private Dictionary<AudioClip, WaveformData> waveformCache = new Dictionary<AudioClip, WaveformData>();

    private WaveformData GetOrCreateWaveformData(AudioClip clip)
    {
        if (waveformCache.TryGetValue(clip, out var data))
            return data;

        data = new WaveformData();
        data.channels = clip.channels;
        data.sampleRate = clip.frequency;
        data.samples = new float[clip.samples * clip.channels];
        clip.GetData(data.samples, 0);

        // 미리 min/max 데이터 계산
        PreprocessWaveformData(data);

        waveformCache[clip] = data;
        return data;
    }

    private void PreprocessWaveformData(WaveformData data)
    {
        // 화면 해상도에 맞춘 청크 단위로 미리 계산
        int chunksCount = 1024;
        data.minMaxData = new float[chunksCount * 2];

        int samplesPerChunk = data.samples.Length / data.channels / chunksCount;

        for (int i = 0; i < chunksCount; i++)
        {
            float min = float.MaxValue;
            float max = float.MinValue;

            int startSample = i * samplesPerChunk * data.channels;
            int endSample = Mathf.Min((i + 1) * samplesPerChunk * data.channels, data.samples.Length);

            for (int j = startSample; j < endSample; j += data.channels)
            {
                float sum = 0f;
                for (int c = 0; c < data.channels; c++)
                    sum += data.samples[j + c];
                float avg = sum / data.channels;

                min = Mathf.Min(min, avg);
                max = Mathf.Max(max, avg);
            }

            data.minMaxData[i * 2] = min;
            data.minMaxData[i * 2 + 1] = max;
        }
    }

    private void DrawWaveSegmentAsLinePartial(
        Rect rect,
        AudioClip clip,
        float startSec,
        float endSec,
        float amplitudeFactor,
        Color lineColor,
        Vector2 scrollPos
    )
    {
        if (!clip) return;
        var waveData = GetOrCreateWaveformData(clip);
        if (waveData == null) return;

        float visibleWidth = position.width - 25f;
        if (visibleWidth <= 0f) visibleWidth = rect.width;

        float pixelStart = scrollPos.x;
        float pixelEnd = pixelStart + visibleWidth;
        if (pixelEnd > rect.width) pixelEnd = rect.width;

        // 상세 보기 구역 계산
        float zoneWidth = visibleWidth * 0.2f;
        float detailZoneStart = rect.xMin + (rect.width * (detailZoneCenter - 0.1f));
        float detailZoneEnd = rect.xMin + (rect.width * (detailZoneCenter + 0.1f));

        int drawWidth = (int)(pixelEnd - pixelStart);
        if (drawWidth < 1) return;

        float centerY = rect.yMin + rect.height / 2f;
        float halfH = rect.height / 2f;

        Vector3[] linePoints = new Vector3[drawWidth * 2];
        int pointIndex = 0;

        float totalWaveDuration = endSec - startSec;

        for (int x = 0; x < drawWidth; x++)
        {
            float px = rect.xMin + (pixelStart + x);
            float normalizedPos = (pixelStart + x) / rect.width;

            // 상세 구역인지 확인
            bool isDetailZone = (px >= detailZoneStart && px <= detailZoneEnd);

            if (isDetailZone)
            {
                // 상세 구간: 실제 샘플 데이터 사용
                int sampleIndex = Mathf.FloorToInt(normalizedPos * waveData.samples.Length);
                if (sampleIndex < waveData.samples.Length)
                {
                    float sample = waveData.samples[sampleIndex];
                    float y = centerY - (sample * amplitudeFactor * halfH);
                    linePoints[pointIndex++] = new Vector3(px, y, 0f);
                    linePoints[pointIndex++] = new Vector3(px, centerY, 0f);
                }
            }
            else
            {
                // 비상세 구간: 최적화된 min/max 데이터 사용
                int dataIndex = Mathf.FloorToInt(normalizedPos * waveData.minMaxData.Length / 2) * 2;
                if (dataIndex < waveData.minMaxData.Length - 1)
                {
                    float minVal = waveData.minMaxData[dataIndex];
                    float maxVal = waveData.minMaxData[dataIndex + 1];
                    float yMin = centerY - (maxVal * amplitudeFactor * halfH);
                    float yMax = centerY - (minVal * amplitudeFactor * halfH);
                    linePoints[pointIndex++] = new Vector3(px, yMin, 0f);
                    linePoints[pointIndex++] = new Vector3(px, yMax, 0f);
                }
            }
        }

        Handles.color = lineColor;
        Handles.DrawLines(linePoints);

        // 상세 보기 구역 표시
        Handles.color = new Color(1, 1, 0, isDraggingDetailZone ? 0.4f : 0.2f);
        Handles.DrawLine(
            new Vector3(detailZoneStart, rect.yMin, 0),
            new Vector3(detailZoneStart, rect.yMax, 0)
        );
        Handles.DrawLine(
            new Vector3(detailZoneEnd, rect.yMin, 0),
            new Vector3(detailZoneEnd, rect.yMax, 0)
        );
    }

    private void DrawTimeAxis(Rect rect, float totalTime)
    {
        float interval = 0.5f;
        int steps = Mathf.CeilToInt(totalTime / interval);

        for (int i = 0; i <= steps; i++)
        {
            float time = i * interval;
            if (time > totalTime) break;

            float ratio = time / totalTime;
            float xPos = rect.xMin + (rect.width * ratio);

            Handles.color = new Color(1, 1, 1, 0.2f);
            Handles.DrawLine(
                new Vector3(xPos, rect.yMin),
                new Vector3(xPos, rect.yMax)
            );

            GUI.color = Color.yellow;
            GUI.Label(
                new Rect(xPos, rect.yMin, 50, 20),
                time.ToString("0.0")
            );
        }
        GUI.color = Color.white;
    }

    private void DrawBpmTimelineGrid(Rect rect, float totalTime)
    {
        if (!chartDataAsset || !uiState.showGrid) return;
        var cd = chartDataAsset.chartData;

        Color measureColor = new Color(1f, 1f, 1f, 0.8f);
        Color beatColor = new Color(1f, 1f, 1f, 0.4f);
        Color resolutionColor = new Color(1f, 1f, 1f, 0.1f);

        foreach (var bc in cd.bpmData.bpmChanges.OrderBy(x => x.position.ToTotalTicks()))
        {
            float startSec = BeatTimeConverter.ConvertBeatToSeconds(bc.position, cd.bpmData.bpmChanges);
            float secPerBeat = 60f / bc.bpm;
        
            // 마디선
            DrawGridLines(rect, totalTime, startSec, secPerBeat * bc.beatsPerMeasure, 1, measureColor);
        
            // 비트선
            DrawGridLines(rect, totalTime, startSec, secPerBeat * bc.beatsPerMeasure, bc.beatsPerMeasure, beatColor);
        
            // Resolution에 따른 세부 선
            int divPerBeat = GetResolutionDivisions(uiState.resolution) / bc.beatsPerMeasure;
            if (divPerBeat > 0)
            {
                DrawGridLines(rect, totalTime, startSec, secPerBeat, divPerBeat, resolutionColor);
            }
        }
    }

    private int GetResolutionDivisions(NoteResolution resolution)
    {
        switch (resolution)
        {
            case NoteResolution.Quarter: return 4;  // 마디당 4개
            case NoteResolution.Eighth: return 8;   // 마디당 8개
            case NoteResolution.Sixteenth: return 16; // 마디당 16개
            case NoteResolution.ThirtySecond: return 32; // 마디당 32개
            default: return 4;
        }
    }

    private void DrawGridLines(Rect rect, float totalTime, float startSec, float measureDuration, int divisionsPerMeasure, Color color)
    {
        float interval = measureDuration / divisionsPerMeasure;
    
        Handles.color = color;
        float curSec = startSec;
        while (curSec <= totalTime)
        {
            float ratio = curSec / totalTime;
            float xPos = rect.xMin + (rect.width * ratio);
            Handles.DrawLine(new Vector3(xPos, rect.yMin), new Vector3(xPos, rect.yMax));
            curSec += interval;
        }
    }
    
    private void DrawVertLine(Rect rect, float totalTime, float tSec, Color col, float thick)
    {
        float ratio = tSec / totalTime;
        ratio = Mathf.Clamp01(ratio);
        float xPos = rect.xMin + rect.width * ratio;
        Handles.color = col;
        Handles.DrawAAPolyLine(thick, new Vector3(xPos, rect.yMin), new Vector3(xPos, rect.yMax));
    }

    private void DrawPlaybackCursor(Rect rect, float totalTime)
    {
        if (!Application.isPlaying || !RuntimeAudioController.instance) return;

        float pos = RuntimeAudioController.instance.GetTime();
        float len = RuntimeAudioController.instance.GetLength();

        if (isPreviewMode)
        {
            pos = previewStartTime + ((pos - previewStartTime) * previewPlaybackSpeed);
        }

        if (pos > len) pos = len;
        float ratio = (len > 0) ? pos / len : 0f;
        float xPos = rect.xMin + rect.width * ratio;

        Color cursorColor = RuntimeAudioController.instance.IsPlaying() ?
            (isPreviewMode ? Color.yellow : Color.green) :
            new Color(1f, 0.5f, 0f);

        Handles.color = cursorColor;
        Handles.DrawAAPolyLine(2f, new Vector3(xPos, rect.yMin), new Vector3(xPos, rect.yMax));
    }

    private void HandleDetailZoneDrag(Rect rect, float normalizedX, Event e)
    {
        float zoneWidth = 0.2f;
        float zoneStart = detailZoneCenter - (zoneWidth * 0.5f);
        float zoneEnd = detailZoneCenter + (zoneWidth * 0.5f);

        switch (e.type)
        {
            case EventType.MouseDown:
                if (e.button == 0 && e.alt)
                {
                    if (normalizedX >= zoneStart && normalizedX <= zoneEnd)
                    {
                        isDraggingDetailZone = true;
                        dragDetailZoneOffset = normalizedX - detailZoneCenter;
                        e.Use();
                    }
                }
                break;

            case EventType.MouseDrag:
                if (isDraggingDetailZone && e.button == 0)
                {
                    detailZoneCenter = Mathf.Clamp01(normalizedX - dragDetailZoneOffset);
                    e.Use();
                    GUI.changed = true;
                }
                break;

            case EventType.MouseUp:
                if (isDraggingDetailZone && e.button == 0)
                {
                    isDraggingDetailZone = false;
                    e.Use();
                    GUI.changed = true;
                }
                break;
        }
    }
}
