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

        if (IsNoteInViewport(noteTime))
        {
            Rect noteRect = CalculateNoteRect(viewportRect, note, noteTime, laneWidth);
            DrawNoteGraphic(noteRect, note);
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
    
        if (note.isLong)
        {
            DrawLongNoteBody(noteRect, note);
        }
    }

    private void DrawLongNoteBody(Rect noteRect, NoteData note)
    {
        float endTime = BeatTimeConverter.ConvertBeatToSeconds(
            note.position + note.length, 
            chartDataAsset.chartData.bpmData.bpmChanges
        );
    
        if (IsNoteInViewport(endTime))
        {
            float endY = GetYPositionForTime(endTime, noteRect);
            Rect bodyRect = new Rect(
                noteRect.x,
                noteRect.y,
                noteRect.width,
                endY - noteRect.y
            );
        
            EditorGUI.DrawRect(bodyRect, new Color(0, 1, 0, 0.3f));
        }
    }
}
