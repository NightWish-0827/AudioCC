using System.Collections.Generic;
using System.Linq;
using MyRhythmEditor;
using UnityEditor;
using UnityEngine;

public partial class NoteArrangeWindow : EditorWindow
{
    // 공통 사용 필드 
    private ChartDataAsset chartDataAsset;
    private int selectedDiffIndex = 0;

    // 뷰포트 관련 
    private Vector2 scrollPosition;
    private float zoomLevel = 1f;
    private float viewportStartTime = 0;
    private float viewportDuration = 10f;
    private float totalHeight;

    // 레인 관련 
    private float laneHeight = 60f;
    private float noteHeight = 20f;
    private Color[] laneColors;
    private const float NOTE_HEIGHT = 12f; // 노트 높이를 12픽셀로 조정
    private const float NOTE_CLICK_TOLERANCE = 8f; // 클릭 판정 범위

    // 에디터 상태
    private bool isDraggingNote = false;
    private NoteData selectedNote = null;
    private bool isEditingLongNote = false;

    // 스냅 관련 
    private bool snapEnabled = true;
    private int snapDivision = 8;

    private List<NoteData> copiedNotes = new List<NoteData>();
    private BeatPosition copyStartPosition;

    [MenuItem("H.Audio CTRL/Note Arranager")]
    public static void OpenWindow()
    {
        var window = GetWindow<NoteArrangeWindow>("Note Arranger");
        window.Show();
    }

    private void OnEnable()
    {
        InitializeLaneColors();
        InitializePlayback();
    }

    private void OnDisable()
    {
        StopPlayback();
    }

    private void OnGUI()
    {
        DrawToolbar();

        if (!chartDataAsset)
        {
            EditorGUILayout.HelpBox("차트 데이터를 선택해주세요.", MessageType.Info);
            return;
        }

        EditorGUILayout.BeginVertical();
        DrawMainContent();
        EditorGUILayout.EndVertical();
    }

    private void DrawMainContent()
    {
        DrawDifficultySelector();
        DrawViewportControls();

        // 스크롤 뷰 시작
        scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);

        // 전체 높이 계산 (곡 길이 기반)
        if (currentMusicClip != null)
        {
            totalHeight = currentMusicClip.length * zoomLevel * 100;
        }

        // 고정된 높이의 컨텐츠 영역
        Rect viewportRect = EditorGUILayout.GetControlRect(
            GUILayout.Height(totalHeight)
        );

        // 뷰포트 시작/끝 시간 계산
        float scrollRatio = scrollPosition.y / totalHeight;
        viewportStartTime = currentMusicClip ? currentMusicClip.length * scrollRatio : 0f;

        DrawGridAndMeasures(viewportRect);
        DrawLanes(viewportRect);
        DrawNotes(viewportRect);
        DrawPlaybackLine(viewportRect); // 여기에서 재생 커서 그리기
        HandleInput(viewportRect);

        EditorGUILayout.EndScrollView();
    }


    private void DrawDifficultySelector()
    {
        if (chartDataAsset.chartData.difficulties.Count == 0) return;

        string[] diffNames = chartDataAsset.chartData.difficulties
            .Select(x => x.difficultyName).ToArray();

        selectedDiffIndex = EditorGUILayout.Popup(
            "Difficulty",
            selectedDiffIndex,
            diffNames
        );
    }

    private void DrawViewportControls()
    {
        EditorGUILayout.BeginHorizontal();

        zoomLevel = EditorGUILayout.Slider("Zoom", zoomLevel, 0.1f, 5f);
        viewportStartTime = EditorGUILayout.FloatField("Start Time", viewportStartTime);
        viewportDuration = EditorGUILayout.FloatField("Duration", viewportDuration);

        EditorGUILayout.EndHorizontal();

        // 스냅 컨트롤 추가
        EditorGUILayout.BeginHorizontal();
        snapEnabled = EditorGUILayout.Toggle("Snap to Grid", snapEnabled);
        if (snapEnabled)
        {
            string[] snapOptions = { "1/4", "1/8", "1/16", "1/32" };
            int[] snapValues = { 4, 8, 16, 32 };
            int currentIndex = System.Array.IndexOf(snapValues, snapDivision);
            int newIndex = EditorGUILayout.Popup("Snap Division", currentIndex, snapOptions);
            snapDivision = snapValues[newIndex];
        }
        EditorGUILayout.EndHorizontal();
    }

    private float SnapTimeToGrid(float time)
    {
        if (!snapEnabled || chartDataAsset == null) return time;

        var bpmChanges = chartDataAsset.chartData.bpmData.bpmChanges;
        if (bpmChanges.Count == 0) return time;

        // 시간을 비트 위치로 변환
        BeatPosition beatPos = BeatTimeConverter.ConvertSecondsToBeat(time, bpmChanges);

        // 스냅 간격 계산 (틱 단위)
        int ticksPerSnap = BeatPosition.TICKS_PER_BEAT / snapDivision;

        // 가장 가까운 스냅 위치로 반올림
        int snappedTicks = Mathf.RoundToInt((float)beatPos.TotalTicks / ticksPerSnap) * ticksPerSnap;

        // 스냅된 틱을 BeatPosition으로 변환
        BeatPosition snappedPos = BeatPosition.FromTicks(snappedTicks);

        // 다시 시간으로 변환
        return BeatTimeConverter.ConvertBeatToSeconds(snappedPos, bpmChanges);
    }

    private void DrawGridAndMeasures(Rect viewportRect)
    {
        if (chartDataAsset == null || chartDataAsset.chartData.bpmData.bpmChanges.Count == 0) return;

        var bpmChanges = chartDataAsset.chartData.bpmData.bpmChanges;
        float songLength = currentMusicClip ? currentMusicClip.length : 60f;

        float firstBpm = bpmChanges[0].bpm;
        float secondsPerBeat = 60f / firstBpm;
        float secondsPerMeasure = secondsPerBeat * BeatPosition.BEATS_PER_MEASURE;

        // 전체 마디 수 계산
        int totalMeasures = Mathf.CeilToInt(songLength / secondsPerMeasure);

        for (int measure = 0; measure <= totalMeasures; measure++)
        {
            float measureStartTime = measure * secondsPerMeasure;
            float measureYPos = GetYPositionForTime(measureStartTime, viewportRect);

            // 마디선 그리기 (흰색)
            DrawMeasureLine(viewportRect, measureYPos, new Color(1, 1, 1, 0.8f));
            DrawMeasureNumber(viewportRect, measureYPos, measure);

            // 마디 내 박자선과 분할선 그리기
            int beatsPerMeasure = BeatPosition.BEATS_PER_MEASURE;
            int divisionsPerBeat = snapDivision / 4; // 4분음표 기준으로 분할

            for (int beat = 0; beat < beatsPerMeasure; beat++)
            {
                // 박자선 (4분음표) 그리기
                if (beat > 0) // 마디선과 겹치지 않게
                {
                    float beatTime = measureStartTime + (beat * secondsPerBeat);
                    float beatYPos = GetYPositionForTime(beatTime, viewportRect);
                    DrawMeasureLine(viewportRect, beatYPos, new Color(1, 1, 1, 0.5f));
                }

                // 분할선 그리기
                for (int div = 1; div < divisionsPerBeat; div++)
                {
                    float divTime = measureStartTime + (beat * secondsPerBeat) + (div * secondsPerBeat / divisionsPerBeat);
                    float divYPos = GetYPositionForTime(divTime, viewportRect);

                    // 8분음표는 좀 더 진하게, 16분음표는 연하게
                    float alpha = (div % 2 == 0) ? 0.3f : 0.15f;
                    DrawMeasureLine(viewportRect, divYPos, new Color(1, 1, 1, alpha));
                }
            }
        }
    }

    private void DrawBeatLine(Rect viewportRect, float yPos, Color color)
    {
        if (yPos < viewportRect.y || yPos > viewportRect.yMax) return;

        Handles.color = color;
        Handles.DrawLine(
            new Vector3(viewportRect.x, yPos),
            new Vector3(viewportRect.xMax, yPos)
        );
    }

    // Y 좌표 계산 함수 수정 (하->상 방향)
    private float GetYPositionForTime(float time, Rect viewportRect)
    {
        if (currentMusicClip == null) return viewportRect.y;

        float normalizedTime = time / currentMusicClip.length;
        return Mathf.Lerp(viewportRect.yMax, viewportRect.y, normalizedTime);
    }

    private float GetTimeFromYPosition(float yPos, Rect viewportRect)
    {
        if (currentMusicClip == null) return 0f;

        float normalizedPos = Mathf.InverseLerp(viewportRect.yMax, viewportRect.y, yPos);
        return currentMusicClip.length * normalizedPos;
    }

    private int GetLaneFromXPosition(float xPos, Rect viewportRect)
    {
        int keyCount = (int)chartDataAsset.chartData.keyType;
        float laneWidth = viewportRect.width / keyCount;
        int lane = Mathf.FloorToInt((xPos - viewportRect.x) / laneWidth);
        return Mathf.Clamp(lane, 0, keyCount - 1);
    }

    private void DrawMeasureLine(Rect viewportRect, float yPos, Color color)
    {
        if (yPos < viewportRect.y || yPos > viewportRect.yMax) return;

        Handles.color = color;
        Handles.DrawLine(
            new Vector3(viewportRect.x, yPos),
            new Vector3(viewportRect.xMax, yPos)
        );
    }

    private void DrawMeasureNumber(Rect viewportRect, float yPos, int measure)
    {
        GUIStyle style = new GUIStyle(EditorStyles.label)
        {
            alignment = TextAnchor.MiddleLeft,
            normal = { textColor = Color.white }
        };

        Rect labelRect = new Rect(
            viewportRect.x + 5,
            yPos - 10,
            50,
            20
        );

        GUI.Label(labelRect, measure.ToString(), style);
    }

    private float GetTimePerBeat(float time, List<BpmChange> bpmChanges)
    {
        float currentBpm = BeatTimeConverter.GetBpmAtTime(time, bpmChanges);
        return 60f / currentBpm; // 초 단위로 반환
    }
}
//