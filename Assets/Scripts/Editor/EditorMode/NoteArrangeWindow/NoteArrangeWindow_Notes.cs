using System.Collections;
using System.Collections.Generic;
using MyRhythmEditor;
using UnityEditor;
using UnityEngine;

public partial class NoteArrangeWindow
{
    private void DrawNotes(Rect viewportRect)
    {
        if (!chartDataAsset || chartDataAsset.chartData.difficulties.Count == 0) return;

        var currentDiff = chartDataAsset.chartData.difficulties[selectedDiffIndex];
        int keyCount = (int)chartDataAsset.chartData.keyType;
        float laneWidth = viewportRect.width / keyCount;

        foreach (var note in currentDiff.notes)
        {
            DrawNote(viewportRect, note, laneWidth);
        }
    }

    private void DrawNote(Rect viewportRect, NoteData note, float laneWidth)
    {
        float noteTime = BeatTimeConverter.ConvertBeatToSeconds(
            note.position,
            chartDataAsset.chartData.bpmData.bpmChanges
        );

        float yPos = GetYPositionForTime(noteTime, viewportRect);

        // 뷰포트 범위 체크 (클릭 판정 범위 고려)
        if (yPos < viewportRect.y - NOTE_HEIGHT - NOTE_CLICK_TOLERANCE ||
            yPos > viewportRect.yMax + NOTE_HEIGHT + NOTE_CLICK_TOLERANCE)
            return;

        Rect noteRect = new Rect(
            viewportRect.x + (note.lane * laneWidth),
            yPos - (NOTE_HEIGHT * 0.5f),
            laneWidth,
            NOTE_HEIGHT
        );

        DrawNoteGraphic(noteRect, note);

        if (note.isLong)
        {
            DrawLongNoteBody(noteRect, note, viewportRect);
        }
    }

    private bool IsNoteInViewport(float noteTime)
    {
        return noteTime >= viewportStartTime &&
               noteTime <= viewportStartTime + viewportDuration;
    }

    private Rect CalculateNoteRect(Rect viewportRect, NoteData note, float noteTime, float laneWidth)
    {
        float yPos = GetYPositionForTime(noteTime, viewportRect);

        return new Rect(
            viewportRect.x + (note.lane * laneWidth),
            yPos - (noteHeight * 0.5f),
            laneWidth,
            noteHeight
        );
    }

    private void DrawNoteGraphic(Rect noteRect, NoteData note)
    {
        Color noteColor = note == selectedNote ?
            Color.yellow :
            (note.isLong ? new Color(0.3f, 1f, 0.3f, 0.9f) : new Color(1f, 1f, 1f, 0.9f));

        // 노트 테두리
        EditorGUI.DrawRect(noteRect, new Color(0, 0, 0, 0.7f));

        // 실제 노트 (약간 안쪽으로)
        Rect innerRect = new Rect(
            noteRect.x + 1,
            noteRect.y + 1,
            noteRect.width - 2,
            noteRect.height - 2
        );
        EditorGUI.DrawRect(innerRect, noteColor);
    }
    
    private void DrawLongNoteBody(Rect noteRect, NoteData note, Rect viewportRect)
    {
        float endTime = BeatTimeConverter.ConvertBeatToSeconds(
            note.position + note.length,
            chartDataAsset.chartData.bpmData.bpmChanges
        );

        float endY = GetYPositionForTime(endTime, viewportRect);

        // 롱노트 시작점과 끝점이 모두 뷰포트 밖에 있는 경우 체크
        if ((noteRect.y < viewportRect.y - NOTE_HEIGHT && endY < viewportRect.y - NOTE_HEIGHT) ||
            (noteRect.y > viewportRect.yMax + NOTE_HEIGHT && endY > viewportRect.yMax + NOTE_HEIGHT))
            return;

        // 롱노트 본체
        Rect bodyRect = new Rect(
            noteRect.x + 2, // 약간의 여백
            Mathf.Min(noteRect.y + NOTE_HEIGHT * 0.5f, endY),
            noteRect.width - 4, // 양쪽 여백
            Mathf.Abs(noteRect.y + NOTE_HEIGHT * 0.5f - endY)
        );

        EditorGUI.DrawRect(bodyRect, new Color(0.3f, 1f, 0.3f, 0.4f));
    }

    private bool IsNoteVisible(float noteTime, Rect viewportRect)
    {
        float yPos = GetYPositionForTime(noteTime, viewportRect);
        return yPos >= viewportRect.y - noteHeight && yPos <= viewportRect.yMax + noteHeight;
    }
}
