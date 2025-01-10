using UnityEngine;
using UnityEditor;
using System.Linq;

public partial class ChartEditorWindow
{
    // ─────────────────────────────────────
    // Note(노트) 그리기 + 입력 처리
    // ─────────────────────────────────────
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
            float timeRatio = n.timeSec / totalTime;
            float xPos = rect.xMin + (rect.width * timeRatio);

            float laneY = rect.yMin + n.lane * (noteH + 4);

            if (n.isLong && n.length > 0f)
            {
                float endTimeRatio = (n.timeSec + n.length) / totalTime;
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
                        var newNote = new MyRhythmEditor.NoteData
                        {
                            timeSec = timePosition,
                            lane = 0,
                            noteType = "Normal",
                            isLong = e.shift,
                            length = 0f
                        };
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
                else if (e.button == 1)
                {
                    // 우클릭: 노트 제거
                    var note = FindNoteAtTime(timePosition, diff);
                    if (note != null)
                    {
                        Undo.RecordObject(chartDataAsset, "RemoveNote");
                        diff.notes.Remove(note);
                        EditorUtility.SetDirty(chartDataAsset);
                        e.Use();
                    }
                }
                break;

            case EventType.MouseDrag:
                if (isDraggingNote && draggingNote != null && e.button == 0)
                {
                    Undo.RecordObject(chartDataAsset, "Move Note");

                    if (isDraggingLongTail)
                    {
                        // Shift+드래그: 롱노트 길이 조절
                        float newLength = timePosition - dragOffsetTime;
                        if (newLength > 0f)
                        {
                            draggingNote.isLong = true;
                            draggingNote.length = newLength;
                        }
                    }
                    else if (e.alt)
                    {
                        // Alt+드래그: 노트 시간 이동
                        draggingNote.timeSec = timePosition;
                    }

                    EditorUtility.SetDirty(chartDataAsset);
                    e.Use();
                    GUI.changed = true;
                }
                break;

            case EventType.MouseUp:
                if (isDraggingNote && draggingNote != null)
                {
                    Undo.RecordObject(chartDataAsset, "Finish Move Note");

                    if (!isDraggingLongTail && e.alt)
                    {
                        draggingNote.timeSec = timePosition;
                    }

                    EditorUtility.SetDirty(chartDataAsset);
                    isDraggingNote = false;
                    isDraggingLongTail = false;
                    draggingNote = null;
                    dragOffsetTime = 0f;
                    e.Use();
                    GUI.changed = true;
                }
                break;
        }
    }

    private MyRhythmEditor.NoteData FindNoteAtTime(float time, MyRhythmEditor.DifficultyChart diff)
    {
        float tolerance = 0.1f;
        return diff.notes.FirstOrDefault(n =>
            !n.isLong ?
            Mathf.Abs(n.timeSec - time) < tolerance :
            time >= n.timeSec - tolerance &&
            time <= n.timeSec + n.length + tolerance
        );
    }
}
