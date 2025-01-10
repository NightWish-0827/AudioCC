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

    private void OnDisable()
    {
        // EditorAudioPlayer.StopAllClips();
    }

    private void OnGUI()
    {
        mainWindowScrollPos = EditorGUILayout.BeginScrollView(mainWindowScrollPos);

        if (!chartDataAsset)
        {
            EditorGUILayout.HelpBox("No ChartDataAsset found. Using temporary in-memory asset.", MessageType.Warning);
        }

        EditorGUILayout.Space();
        EditorGUILayout.BeginVertical("box");
        {
            // 난이도 섹션
            DrawKeyAndDifficultySection(); 
            // 오디오 섹션
            DrawAudioSection();
        }
        EditorGUILayout.EndVertical();

        EditorGUILayout.Space();
        EditorGUILayout.BeginVertical("box");
        {
            // BPM 섹션
            DrawBpmSection();
        }
        EditorGUILayout.EndVertical();

        EditorGUILayout.Space();
        // Export/Import 섹션
        DrawExportImport();

        EditorGUILayout.Space();
        // 파형 미리보기
        DrawWavePreview();

        EditorGUILayout.EndScrollView();

        // Repaint 로직
        if (Application.isPlaying)
        {
            if (RuntimeAudioController.instance && 
                (RuntimeAudioController.instance.IsPlaying() || RuntimeAudioController.instance.IsPaused()))
            {
                Repaint();
            }
        }
    }
}
