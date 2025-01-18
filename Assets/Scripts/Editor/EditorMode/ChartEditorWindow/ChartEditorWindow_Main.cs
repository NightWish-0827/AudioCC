using UnityEngine;
using UnityEditor;
using MyRhythmEditor;
using System.Linq;
using System.IO;
using System.Collections.Generic;

public partial class ChartEditorWindow : EditorWindow
{
    // ─────────────────────────────────────
    // Fields (기존 동일)
    // ─────────────────────────────────────
    private Vector2 mainWindowScrollPos;
    private ChartDataAsset chartDataAsset;
    private AudioClip waveClip;
    private bool loopPlayback = false;
    private float waveAmplitudeFactor = 1f;
    private float waveZoomX = 10f;
    private float waveZoomY = 1.5f;
    private bool useLineWave = true;
    private bool useBpmTimeline = true;
    private Vector2 waveScrollPos;

    // 노트
    private bool enableNotePlacement = false;
    private bool isDraggingNote = false;
    private bool isDraggingLongTail = false;
    private NoteData draggingNote = null;
    private float dragOffsetTime;

    // 선택된 난이도
    private int selectedDiffIndex = 0;
    private string newDiffName = "Easy";

    // Export/Import
    private string lastExportPath = "";
    private string lastImportPath = "";

    private bool isPreviewMode = false;
    private float previewPlaybackSpeed = 1f;
    private float previewStartTime = 0f;

    // 상세 보기 구역 관련 필드
    private float detailZoneCenter = 0.5f;  
    private bool isDraggingDetailZone = false;
    private float dragDetailZoneOffset = 0f;
    
//    private bool showGrid = true;
  //  private bool showBeatNumbers = true;
    private NoteResolution currentResolution = NoteResolution.Sixteenth;
    private Color gridColorMeasure = new Color(1, 1, 1, 1f);
    private Color gridColorBeat = new Color(1, 1, 1, 0.5f);
    private Color gridColorTick = new Color(1, 1, 1, 0.2f);

    private struct EditorUIState
    {
        public bool showGrid;
        public bool showBeatNumbers;
        public NoteResolution resolution;
        public float waveformZoomX;
        public float waveformZoomY;
        public bool useBpmTimeline;
        public bool enableNotePlacement;
    }
    
    private EditorUIState uiState = new EditorUIState
    {
        showGrid = true,
        showBeatNumbers = true,
        resolution = NoteResolution.Sixteenth,
        waveformZoomX = 10f,
        waveformZoomY = 1.5f,
        useBpmTimeline = true,
        enableNotePlacement = false
    };
    
    public enum NoteResolution
    {
        Quarter = 4,
        Eighth = 8,
        Sixteenth = 16,
        ThirtySecond = 32
    }

    private void DrawBeatGrid(Rect rect, float totalTime)
    {
        if (!chartDataAsset) return;
        var bpmData = chartDataAsset.chartData.bpmData;
        
        // 마디선 그리기
        for (int measure = 0; measure <= totalTime; measure++)
        {
            var pos = new BeatPosition(measure, 0, 0);
            float timeRatio = BeatTimeConverter.ConvertBeatToSeconds(pos, bpmData.bpmChanges) / totalTime;
            float xPos = rect.xMin + (rect.width * timeRatio);
            
            Handles.color = Color.white;
            Handles.DrawLine(new Vector3(xPos, rect.yMin, 0), new Vector3(xPos, rect.yMax, 0));
        }
        
        // 비트선 그리기
        Handles.color = new Color(1, 1, 1, 0.5f);
        for (int measure = 0; measure <= totalTime; measure++)
        {
            for (int beat = 1; beat < BEATS_PER_MEASURE; beat++)
            {
                var pos = new BeatPosition(measure, beat, 0);
                float timeRatio = BeatTimeConverter.ConvertBeatToSeconds(pos, bpmData.bpmChanges) / totalTime;
                float xPos = rect.xMin + (rect.width * timeRatio);
                
                Handles.DrawLine(new Vector3(xPos, rect.yMin, 0), new Vector3(xPos, rect.yMax, 0));
            }
        }
        
        // 틱선 그리기 (현재 해상도에 따라)
        if (currentResolution >= NoteResolution.Sixteenth)
        {
            Handles.color = new Color(1, 1, 1, 0.2f);
            for (int measure = 0; measure <= totalTime; measure++)
            {
                for (int beat = 0; beat < BEATS_PER_MEASURE; beat++)
                {
                    for (int tick = 1; tick < TICKS_PER_BEAT; tick++)
                    {
                        var pos = new BeatPosition(measure, beat, tick);
                        float timeRatio = BeatTimeConverter.ConvertBeatToSeconds(pos, bpmData.bpmChanges) / totalTime;
                        float xPos = rect.xMin + (rect.width * timeRatio);
                        
                        Handles.DrawLine(new Vector3(xPos, rect.yMin, 0), new Vector3(xPos, rect.yMax, 0));
                    }
                }
            }
        }
    }
    
    
    // ─────────────────────────────────────
    // Menu (기존 동일)
    // ─────────────────────────────────────
    [MenuItem("H.Audio CTRL/Audio.CC")]
    public static void OpenWindow()
    {
        var win = GetWindow<ChartEditorWindow>("Audio.CC");
        win.Show();
    }
       
    // ─────────────────────────────────────
    // Lifecycle
    // ─────────────────────────────────────
    private void OnEnable()
    {
        if (!chartDataAsset)
        {
            string defaultPath = "Assets/Data/NewChartData.asset";
            chartDataAsset = AssetDatabase.LoadAssetAtPath<ChartDataAsset>(defaultPath);

            if (!chartDataAsset)
            {
                chartDataAsset = CreateInstance<ChartDataAsset>();
                Debug.LogWarning("DefaultChartDataAsset not found. Using in-memory asset.");
            }
        }
    }

    private void DrawEditorToolbar()
    {
        EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
    
        // 왼쪽 그룹: 보기 옵션
        {
            uiState.showGrid = GUILayout.Toggle(uiState.showGrid, "Grid", EditorStyles.toolbarButton);
            uiState.showBeatNumbers = GUILayout.Toggle(uiState.showBeatNumbers, "Beat Numbers", EditorStyles.toolbarButton);
            GUILayout.Space(10);
        }

        // 중앙 그룹: 노트 편집 옵션
        {
            // toolbarLabel 대신 miniLabel 사용
            GUILayout.Label("Resolution:", EditorStyles.miniLabel, GUILayout.ExpandWidth(false));
            string[] resOptions = { "1/4", "1/8", "1/16", "1/32" };
            int resIndex = ((int)uiState.resolution / 4) - 1;
            int newResIndex = EditorGUILayout.Popup(resIndex, resOptions, EditorStyles.toolbarPopup, GUILayout.Width(50));
            uiState.resolution = (NoteResolution)((newResIndex + 1) * 4);

            GUILayout.Space(10);
            /*
            uiState.enableNotePlacement = GUILayout.Toggle(uiState.enableNotePlacement, 
                "Place Notes", EditorStyles.toolbarButton);
            */
            
            enableNotePlacement = GUILayout.Toggle(enableNotePlacement, 
                "Place Notes", EditorStyles.toolbarButton);
        }

        // 오른쪽 그룹: 줌 컨트롤
        {
            GUILayout.FlexibleSpace();
            // toolbarLabel 대신 miniLabel 사용
            GUILayout.Label("Zoom:", EditorStyles.miniLabel, GUILayout.ExpandWidth(false));
        
            float newZoomX = EditorGUILayout.Slider(uiState.waveformZoomX, 1f, 100f, 
                GUILayout.Width(100));
            if (newZoomX != uiState.waveformZoomX)
            {
                uiState.waveformZoomX = newZoomX;
                Repaint();
            }
        }

        EditorGUILayout.EndHorizontal();
    }

    private void DrawMainContent()
    {
        // 메인 컨텐츠 영역
        EditorGUILayout.BeginVertical();
        {

            
            // 차트 데이터 에셋 필드
            EditorGUI.BeginChangeCheck();
            chartDataAsset = (ChartDataAsset)EditorGUILayout.ObjectField(
                "Chart Data", chartDataAsset, typeof(ChartDataAsset), false);
            if (EditorGUI.EndChangeCheck() && chartDataAsset != null)
            {
                OnChartDataAssetChanged();
            }
            

            // 섹션들을 박스로 구분
            EditorGUILayout.BeginVertical("box");
            {
                DrawKeyAndDifficultySection();
                DrawAudioSection();
                DrawBpmSection();
            }
            EditorGUILayout.EndVertical();
     
            EditorGUILayout.Space();
            if (waveClip != null)
            {
                DrawEditorToolbar();
                DrawWavePreview();      
            }
            
            EditorGUILayout.Space();
            DrawExportImport();
        }
        EditorGUILayout.EndVertical();
    }
    
    

    private void OnGUI()
    {
        // 스크롤 뷰 시작
        mainWindowScrollPos = EditorGUILayout.BeginScrollView(mainWindowScrollPos);
        {
            DrawMainContent();
        }
        EditorGUILayout.EndScrollView();

        // 재생 중일 때 리페인트
        if (Application.isPlaying && RuntimeAudioController.instance && 
            (RuntimeAudioController.instance.IsPlaying() || RuntimeAudioController.instance.IsPaused()))
        {
            Repaint();
        }
    }
    
    private void OnChartDataAssetChanged()
    {
        // 차트 데이터 에셋이 변경되었을 때의 처리
        if (chartDataAsset != null)
        {
            selectedDiffIndex = 0;
            EditorUtility.SetDirty(chartDataAsset);
        }
    }
    
    // ChartDataAsset 접근자 추가
    public ChartDataAsset GetChartDataAsset()
    {
        return chartDataAsset;
    }

    // 선택된 난이도 인덱스 접근자 추가
    public int GetSelectedDifficultyIndex()
    {
        return selectedDiffIndex;
    }

    // 선택된 난이도 설정자 추가 (필요할 경우)
    public void SetSelectedDifficultyIndex(int index)
    {
        if (chartDataAsset != null && 
            chartDataAsset.chartData.difficulties.Count > index && 
            index >= 0)
        {
            selectedDiffIndex = index;
            Repaint();
        }
    }
    
    public AudioClip GetCurrentAudioClip()
    {
        return waveClip; // ChartEditorWindow에서 사용 중인 AudioClip 반환
    }
}
