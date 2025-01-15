using System.Collections;
using System.Collections.Generic;
using System.Linq;
using MyRhythmEditor;
using UnityEditor;
using UnityEngine;

public partial class NoteArrangeWindow
{
    private void HandleInput(Rect viewportRect)
    {
        Event e = Event.current;
        if (!viewportRect.Contains(e.mousePosition)) return;

        switch (e.type)
        {
            case EventType.MouseDown:
                HandleMouseDown(viewportRect, e);
                break;
            case EventType.MouseDrag:
                HandleMouseDrag(viewportRect, e);
                break;
            case EventType.MouseUp:
                HandleMouseUp(e);
                break;
            case EventType.KeyDown:
                HandleKeyDown(e, viewportRect);
                break;
            case EventType.ContextClick:
                HandleContextMenu(viewportRect, e);
                break;
        }
    }

    private void HandleKeyDown(Event e, Rect viewportRect)
    {
        if (e.keyCode == KeyCode.Delete)
        {
            if (selectedNote != null)
            {
                DeleteSelectedNote();
                e.Use();
            }
        }
        
        if (e.control || e.command)
        {
            switch (e.keyCode)
            {
                case KeyCode.C:
                    CopySelectedNote();
                    e.Use();
                    break;
                case KeyCode.V:
                    PasteNotes(GetTimeFromYPosition(Event.current.mousePosition.y, viewportRect));
                    e.Use();
                    break;
                case KeyCode.D:
                    DuplicateSelectedNote();
                    e.Use();
                    break;
            }
        }
    }

    private void CopySelectedNote()
    {
        if (selectedNote == null) return;

        copiedNotes.Clear();
        copiedNotes.Add(new NoteData
        {
            position = selectedNote.position,
            lane = selectedNote.lane,
            isLong = selectedNote.isLong,
            length = selectedNote.length
        });
        
        copyStartPosition = selectedNote.position;
    }

    private void PasteNotes(float targetTime)
    {
        if (copiedNotes.Count == 0 || chartDataAsset == null) return;

        var currentDiff = chartDataAsset.chartData.difficulties[selectedDiffIndex];
        
        Undo.RecordObject(chartDataAsset, "Paste Notes");

        // 붙여넣기 위치 계산
        BeatPosition targetBeat = BeatTimeConverter.ConvertSecondsToBeat(
            SnapTimeToGrid(targetTime),
            chartDataAsset.chartData.bpmData.bpmChanges
        );

        // 복사한 노트와 붙여넣을 위치의 차이 계산
        BeatPosition offset = targetBeat - copyStartPosition;

        foreach (var copiedNote in copiedNotes)
        {
            var newNote = new NoteData
            {
                position = copiedNote.position + offset,
                lane = copiedNote.lane,
                isLong = copiedNote.isLong,
                length = copiedNote.length
            };

            currentDiff.notes.Add(newNote);
        }

        EditorUtility.SetDirty(chartDataAsset);
    }

    private void DuplicateSelectedNote()
    {
        if (selectedNote == null) return;

        CopySelectedNote();
        
        // 현재 선택된 노트의 다음 박자에 붙여넣기
        float currentTime = BeatTimeConverter.ConvertBeatToSeconds(
            selectedNote.position,
            chartDataAsset.chartData.bpmData.bpmChanges
        );
        
        float nextBeatTime = currentTime + GetTimePerBeat(currentTime, 
            chartDataAsset.chartData.bpmData.bpmChanges);
            
        PasteNotes(nextBeatTime);
    }

    private void DeleteSelectedNote()
    {
        if (!chartDataAsset || selectedNote == null) return;
        
        var currentDiff = chartDataAsset.chartData.difficulties[selectedDiffIndex];
        
        Undo.RecordObject(chartDataAsset, "Delete Note");
        currentDiff.notes.Remove(selectedNote);
        EditorUtility.SetDirty(chartDataAsset);
        
        selectedNote = null;
    }

    private void HandleContextMenu(Rect viewportRect, Event e)
    {
        float time = GetTimeFromYPosition(e.mousePosition.y, viewportRect);
        int lane = GetLaneFromXPosition(e.mousePosition.x, viewportRect);
        
        var note = FindNoteAtPosition(time, lane);
        
        GenericMenu menu = new GenericMenu();
        
        if (note != null)
        {
            menu.AddItem(new GUIContent("Copy"), false, () => 
            {
                selectedNote = note;
                CopySelectedNote();
            });
            
            menu.AddItem(new GUIContent("Duplicate"), false, () =>
            {
                selectedNote = note;
                DuplicateSelectedNote();
            });
            
            menu.AddSeparator("");
            
            menu.AddItem(new GUIContent("Delete Note"), false, () => 
            {
                selectedNote = note;
                DeleteSelectedNote();
            });
            
            menu.AddItem(new GUIContent("Toggle Long Note"), note.isLong, () => 
            {
                Undo.RecordObject(chartDataAsset, "Toggle Long Note");
                note.isLong = !note.isLong;
                if (!note.isLong) note.length = new BeatPosition(0, 0, 0);
                EditorUtility.SetDirty(chartDataAsset);
            });
        }
        else
        {
            menu.AddItem(new GUIContent("Paste"), copiedNotes.Count > 0, () =>
            {
                PasteNotes(time);
            });
            
            menu.AddDisabledItem(new GUIContent("Delete Note"));
            menu.AddDisabledItem(new GUIContent("Toggle Long Note"));
        }
        
        menu.ShowAsContext();
        e.Use();
    }

    private void HandleMouseDown(Rect viewportRect, Event e)
    {
        if (e.button == 0) // 좌클릭
        {
            float time = GetTimeFromYPosition(e.mousePosition.y, viewportRect);
            int lane = GetLaneFromXPosition(e.mousePosition.x, viewportRect);
        
            // 기존 노트 선택 확인
            selectedNote = FindNoteAtPosition(time, lane);
        
            if (selectedNote == null && !e.shift) // 새 노트 생성
            {
                CreateNewNote(time, lane);
            }
        
            isDraggingNote = selectedNote != null;
            isEditingLongNote = e.shift && selectedNote != null;
        
            e.Use();
        }
    }

    private void HandleMouseDrag(Rect viewportRect, Event e)
    {
        if (selectedNote != null)
        {
            float time = GetTimeFromYPosition(e.mousePosition.y, viewportRect);
            int lane = GetLaneFromXPosition(e.mousePosition.x, viewportRect);
        
            if (isEditingLongNote)
            {
                UpdateLongNoteLength(time);
            }
            else
            {
                UpdateNotePosition(time, lane);
            }
        
            e.Use();
        }
    }

    private void HandleMouseUp(Event e)
    {
        if (e.button == 0)
        {
            isDraggingNote = false;
            isEditingLongNote = false;
            e.Use();
        }
    }

    private NoteData FindNoteAtPosition(float time, int lane)
    {
        if (!chartDataAsset || chartDataAsset.chartData.difficulties.Count == 0) 
            return null;

        var currentDiff = chartDataAsset.chartData.difficulties[selectedDiffIndex];
        float tolerance = 0.1f; // 선택 허용 범위 (초)

        return currentDiff.notes.FirstOrDefault(note =>
        {
            float noteTime = BeatTimeConverter.ConvertBeatToSeconds(
                note.position,
                chartDataAsset.chartData.bpmData.bpmChanges
            );
            
            return note.lane == lane && 
                   Mathf.Abs(noteTime - time) < tolerance;
        });
    }

    private void CreateNewNote(float time, int lane)
    {
        if (!chartDataAsset || chartDataAsset.chartData.difficulties.Count == 0) 
            return;

        var currentDiff = chartDataAsset.chartData.difficulties[selectedDiffIndex];
        var bpmChanges = chartDataAsset.chartData.bpmData.bpmChanges;
            
        // 스냅 적용
        float snappedTime = SnapTimeToGrid(time);
            
        Undo.RecordObject(chartDataAsset, "Add Note");
            
        var newNote = new NoteData
        {
            position = BeatTimeConverter.ConvertSecondsToBeat(snappedTime, bpmChanges),
            lane = lane,
            isLong = false,
            length = new BeatPosition(0, 0, 0)
        };
            
        currentDiff.notes.Add(newNote);
        EditorUtility.SetDirty(chartDataAsset);
    }

    private void UpdateNotePosition(float time, int lane)
    {
        if (selectedNote == null) return;
            
        // 스냅 적용
        float snappedTime = SnapTimeToGrid(time);
            
        Undo.RecordObject(chartDataAsset, "Move Note");
            
        selectedNote.position = BeatTimeConverter.ConvertSecondsToBeat(
            snappedTime,
            chartDataAsset.chartData.bpmData.bpmChanges
        );
        selectedNote.lane = lane;
            
        EditorUtility.SetDirty(chartDataAsset);
    }

    private void UpdateLongNoteLength(float endTime)
    {
        if (selectedNote == null) return;
            
        // 스냅 적용
        float snappedEndTime = SnapTimeToGrid(endTime);
            
        Undo.RecordObject(chartDataAsset, "Edit Long Note");
            
        float startTime = BeatTimeConverter.ConvertBeatToSeconds(
            selectedNote.position,
            chartDataAsset.chartData.bpmData.bpmChanges
        );
            
        BeatPosition endBeat = BeatTimeConverter.ConvertSecondsToBeat(
            snappedEndTime,
            chartDataAsset.chartData.bpmData.bpmChanges
        );
            
        selectedNote.isLong = true;
        selectedNote.length = endBeat - selectedNote.position;
            
        EditorUtility.SetDirty(chartDataAsset);
    }
}