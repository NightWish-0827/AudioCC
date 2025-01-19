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
            totalHeight = currentMusicClip.length * zoomLevel * 100; // 적절한 스케일 팩터
        }
        
        // 고정된 높이의 컨텐츠 영역
        Rect viewportRect = EditorGUILayout.GetControlRect(
            GUILayout.Height(totalHeight)
        );
        
        // 뷰포트 시작/끝 시간 계산 (스크롤 위치 기반)
        float scrollRatio = scrollPosition.y / totalHeight;
        viewportStartTime = currentMusicClip ? currentMusicClip.length * scrollRatio : 0f;
        
        // 그리드와 마디선은 아래에서 위로 그리도록 수정
        DrawGridAndMeasures(viewportRect);
        DrawLanes(viewportRect);
        DrawNotes(viewportRect);
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
    float songLength = currentMusicClip ? currentMusicClip.length : 60f; // 기본값 60초

    // 첫 번째 BPM 값 가져오기
    float firstBpm = bpmChanges[0].bpm;
    float secondsPerBeat = 60f / firstBpm;
    
    // 전체 마디 수 계산
    int totalMeasures = Mathf.CeilToInt(songLength / (secondsPerBeat * BeatPosition.BEATS_PER_MEASURE));

    for (int measure = 0; measure <= totalMeasures; measure++)
    {
        // 마디의 시작 시간 계산
        float measureTime = measure * secondsPerBeat * BeatPosition.BEATS_PER_MEASURE;
        float yPos = GetYPositionForTime(measureTime, viewportRect);

        // 마디선 그리기
        DrawMeasureLine(viewportRect, yPos, Color.white);
        DrawMeasureNumber(viewportRect, yPos, measure);

        // 비트선 그리기
        for (int beat = 1; beat < BeatPosition.BEATS_PER_MEASURE; beat++)
        {
            float beatTime = measureTime + (beat * secondsPerBeat);
            float beatYPos = GetYPositionForTime(beatTime, viewportRect);
            DrawBeatLine(viewportRect, beatYPos, new Color(1, 1, 1, 0.3f));
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
