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
        
        // 뷰포트 범위 체크
        if (yPos < viewportRect.y - noteHeight || yPos > viewportRect.yMax + noteHeight) 
            return;

        Rect noteRect = new Rect(
            viewportRect.x + (note.lane * laneWidth),
            yPos - (noteHeight * 0.5f),
            laneWidth,
            noteHeight
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
            (note.isLong ? Color.green : Color.white);
    
        EditorGUI.DrawRect(noteRect, new Color(0, 0, 0, 0.5f)); // 그림자
        noteRect.y -= 1; // 약간 위로 올려서 그림자 효과
        EditorGUI.DrawRect(noteRect, noteColor);
    }

     private void DrawLongNoteBody(Rect noteRect, NoteData note, Rect viewportRect)
    {
        float endTime = BeatTimeConverter.ConvertBeatToSeconds(
            note.position + note.length, 
            chartDataAsset.chartData.bpmData.bpmChanges
        );

        float endY = GetYPositionForTime(endTime, viewportRect);

        // 롱노트 시작점과 끝점이 모두 뷰포트 밖에 있는 경우 체크
        if ((noteRect.y < viewportRect.y && endY < viewportRect.y) ||
            (noteRect.y > viewportRect.yMax && endY > viewportRect.yMax))
            return;

        Rect bodyRect = new Rect(
            noteRect.x,
            Mathf.Min(noteRect.y + noteHeight * 0.5f, endY),
            noteRect.width,
            Mathf.Abs(noteRect.y + noteHeight * 0.5f - endY)
        );

        EditorGUI.DrawRect(bodyRect, new Color(0, 1, 0, 0.3f));
    }

    private bool IsNoteVisible(float noteTime, Rect viewportRect)
    {
        float yPos = GetYPositionForTime(noteTime, viewportRect);
        return yPos >= viewportRect.y - noteHeight && yPos <= viewportRect.yMax + noteHeight;
    }
}
