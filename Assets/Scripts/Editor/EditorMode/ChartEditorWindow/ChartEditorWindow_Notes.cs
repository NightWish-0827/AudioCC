using UnityEngine;
using UnityEditor;
using System.Linq;
using MyRhythmEditor;

public partial class ChartEditorWindow
{
    // BeatPosition의 상수를 참조하기 위한 필드 추가
    private const int TICKS_PER_BEAT = BeatPosition.TICKS_PER_BEAT;
    private const int BEATS_PER_MEASURE = BeatPosition.BEATS_PER_MEASURE;
    
    private void DrawNotes(Rect rect, float totalTime)
    {
        if (!chartDataAsset) return;
        var chart = chartDataAsset.chartData;
        if (chart.difficulties.Count == 0) return;
        if (selectedDiffIndex >= chart.difficulties.Count) return;
        var diff = chart.difficulties[selectedDiffIndex];

        float noteH = 8f;
        float noteW = 6f;
        
        foreach (var n in diff.notes)
        {
            float timeRatio = BeatTimeConverter.ConvertBeatToSeconds(n.position, 
                chartDataAsset.chartData.bpmData.bpmChanges) / totalTime;
            
            float xPos = rect.xMin + (rect.width * timeRatio);
            float laneY = rect.yMin + n.lane * (noteH + 4);

            if (n.isLong)
            {
                // 롱노트 길이를 BeatPosition 기반으로 계산
                BeatPosition endPos = n.position + n.length;
                float endTimeRatio = BeatTimeConverter.ConvertBeatToSeconds(endPos, 
                    chartDataAsset.chartData.bpmData.bpmChanges) / totalTime;
                float endXPos = rect.xMin + (rect.width * endTimeRatio);

                Rect longRect = new Rect(
                    xPos - (noteW * 0.5f),
                    laneY,
                    endXPos - xPos + noteW,
                    noteH
                );

                Color c = (n == draggingNote) ? Color.magenta : new Color(0, 1, 0.5f, 0.9f);
                Handles.DrawSolidRectangleWithOutline(longRect, c, Color.black);
            }
            else
            {
                Rect noteRect = new Rect(
                    xPos - (noteW * 0.5f),
                    laneY,
                    noteW,
                    noteH
                );

                Color c = (n == draggingNote) ? Color.magenta : new Color(0, 1, 1, 0.9f);
                Handles.DrawSolidRectangleWithOutline(noteRect, c, Color.black);
            }
        }
    }

    private void HandleWaveInput(Rect waveRect, float timePosition, float totalTime)
    {
        if (!chartDataAsset) return;
        var chart = chartDataAsset.chartData;
        if (chart.difficulties.Count == 0) return;
        if (selectedDiffIndex >= chart.difficulties.Count) return;
        var diff = chart.difficulties[selectedDiffIndex];

        Event e = Event.current;
        var bpmChanges = chartDataAsset.chartData.bpmData.bpmChanges;

        switch (e.type)
        {
            case EventType.MouseDown:
                if (e.button == 0 && enableNotePlacement)
                {
                    if (e.alt)
                    {
                        // Alt+클릭: 노트 드래그 시작
                        var existingNote = FindNoteAtTime(timePosition, diff);
                        if (existingNote != null)
                        {
                            isDraggingNote = true;
                            isDraggingLongTail = false;
                            draggingNote = existingNote;
                            dragOffsetTime = timePosition;
                            e.Use();
                            return;
                        }
                    }
                    else
                    {
                        // 일반 좌클릭 / Shift+클릭: 노트 생성
                        Undo.RecordObject(chartDataAsset, "AddNote");
                        
                        BeatPosition newPos = BeatTimeConverter.ConvertSecondsToBeat(timePosition, bpmChanges);
                        // 스냅 적용
                        newPos = SnapToBeat(newPos);

                        var newNote = e.shift ? 
                            NoteData.CreateLongNote(newPos, 0) : 
                            NoteData.CreateNormalNote(newPos, 0);

                        diff.notes.Add(newNote);

                        if (e.shift)
                        {
                            isDraggingNote = true;
                            isDraggingLongTail = true;
                            draggingNote = newNote;
                            dragOffsetTime = timePosition;
                        }

                        EditorUtility.SetDirty(chartDataAsset);
                        e.Use();
                    }
                }
                if (e.button == 1 && enableNotePlacement)  // 우클릭
                {
                    var noteToDelete = FindNoteAtTime(timePosition, diff);
                    if (noteToDelete != null)
                    {
                        Undo.RecordObject(chartDataAsset, "Delete Note");
                        diff.notes.Remove(noteToDelete);
                        EditorUtility.SetDirty(chartDataAsset);
                        e.Use();
                    }
                }
                else if (e.button == 0 && enableNotePlacement)
                {
                    // ... 기존 좌클릭 처리 코드 ...
                }
                break;

            case EventType.MouseDrag:
                if (isDraggingNote && draggingNote != null && e.button == 0)
                {
                    Undo.RecordObject(chartDataAsset, "Move Note");

                    if (isDraggingLongTail)
                    {
                        // Shift+드래그: 롱노트 길이 조절
                        BeatPosition currentPos = BeatTimeConverter.ConvertSecondsToBeat(timePosition, bpmChanges);
                        BeatPosition startPos = draggingNote.position;
                        BeatPosition lengthPos = currentPos - startPos;
                        
                        if (lengthPos.ToTotalTicks() > 0)
                        {
                            draggingNote.isLong = true;
                            draggingNote.length = lengthPos;
                        }
                    }
                    else if (e.alt)
                    {
                        // Alt+드래그: 노트 시간 이동
                        BeatPosition newPos = BeatTimeConverter.ConvertSecondsToBeat(timePosition, bpmChanges);
                        draggingNote.position = SnapToBeat(newPos);
                    }

                    EditorUtility.SetDirty(chartDataAsset);
                    e.Use();
                }
                break;

            case EventType.MouseUp:
                if (isDraggingNote && draggingNote != null)
                {
                    isDraggingNote = false;
                    isDraggingLongTail = false;
                    draggingNote = null;
                    dragOffsetTime = 0f;
                    e.Use();
                }
                break;
        }
    }

    // 스냅 기능 추가
    private BeatPosition SnapToBeat(BeatPosition pos)
    {
        int ticksPerDiv = TICKS_PER_BEAT / (int)uiState.resolution;
    
        // 전체 틱 수 계산
        int totalTicks = pos.ToTotalTicks();
    
        // 가장 가까운 division에 스냅
        int snappedTicks = Mathf.RoundToInt((float)totalTicks / ticksPerDiv) * ticksPerDiv;
    
        // 정규화된 BeatPosition 반환
        return BeatPosition.FromTotalTicks(snappedTicks);
    }

    private NoteData FindNoteAtTime(float time, DifficultyChart diff)
    {
        var bpmChanges = chartDataAsset.chartData.bpmData.bpmChanges;
        BeatPosition targetPos = BeatTimeConverter.ConvertSecondsToBeat(time, bpmChanges);
        
        float tolerance = BeatTimeConverter.ConvertBeatToSeconds(
            new BeatPosition(0, 0, 1), bpmChanges); // 1틱 만큼의 시간

        return diff.notes.FirstOrDefault(n =>
        {
            if (!n.isLong)
            {
                float noteTime = BeatTimeConverter.ConvertBeatToSeconds(n.position, bpmChanges);
                return Mathf.Abs(noteTime - time) < tolerance;
            }
            else
            {
                float startTime = BeatTimeConverter.ConvertBeatToSeconds(n.position, bpmChanges);
                float endTime = BeatTimeConverter.ConvertBeatToSeconds(n.position + n.length, bpmChanges);
                return time >= startTime - tolerance && time <= endTime + tolerance;
            }
        });
    }
}