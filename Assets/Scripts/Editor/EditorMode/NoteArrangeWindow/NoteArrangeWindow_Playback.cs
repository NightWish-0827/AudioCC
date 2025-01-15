using UnityEngine;
using UnityEditor;
using MyRhythmEditor;

public partial class NoteArrangeWindow
{
    // 재생 관련 필드
    private bool isPlaying = false;
    private float playbackTime = 0f;
    private double lastPlaybackTime;
    private AudioClip currentMusicClip; // 추가
    
    private void InitializePlayback()
    {
        // RuntimeAudioController가 없다면 경고 표시
        if (RuntimeAudioController.instance == null)
        {
            Debug.LogWarning("RuntimeAudioController가 씬에 없습니다. 오디오 재생이 불가능합니다.");
        }
    }

    private void UpdatePlayback()
    {
        if (!isPlaying || chartDataAsset == null) return;

        // RuntimeAudioController에서 현재 재생 시간 가져오기
        playbackTime = RuntimeAudioController.instance.GetTime();
        
        // 뷰포트 자동 스크롤
        viewportStartTime = playbackTime - (viewportDuration * 0.25f);
        Repaint();
        
        // 재생이 끝났는지 확인
        if (!RuntimeAudioController.instance.IsPlaying())
        {
            StopPlayback();
        }
        
        // 재생 위치 표시
        DrawPlaybackLine();
    }

    private void DrawPlaybackLine()
    {
        if (!isPlaying) return;
        
        Rect viewportRect = GUILayoutUtility.GetLastRect();
        float yPos = GetYPositionForTime(playbackTime, viewportRect);
        
        Handles.color = Color.red;
        Handles.DrawLine(
            new Vector3(viewportRect.x, yPos),
            new Vector3(viewportRect.xMax, yPos)
        );
    }

    private void StartPlayback()
    {
        if (chartDataAsset == null || currentMusicClip == null) return;
        
        var audioController = RuntimeAudioController.instance;
        if (audioController == null) return;

        isPlaying = true;
        lastPlaybackTime = EditorApplication.timeSinceStartup;
        
        // RuntimeAudioController로 오디오 재생
        audioController.SetClip(currentMusicClip);
        audioController.SetTime(playbackTime);
        audioController.Play(false); // loop = false
        
        EditorApplication.update += UpdatePlayback;
    }

    private void StopPlayback()
    {
        isPlaying = false;
        RuntimeAudioController.instance?.Stop();
        EditorApplication.update -= UpdatePlayback;
    }

    private void SeekPlayback(float time)
    {
        playbackTime = Mathf.Max(0, time);
        if (isPlaying && RuntimeAudioController.instance != null)
        {
            RuntimeAudioController.instance.SetTime(playbackTime);
        }
    }

    // ChartEditorWindow와 동기화할 때 AudioClip도 가져오기
   
}