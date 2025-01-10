using System;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace MyRhythmEditor
{
    /// <summary>
    /// 에디터 모드에서 오디오 재생(Reflection).
    /// Unity 2021.3.33f1에서 AudioUtil이 제거됐다면 isAvailable=false.
    /// </summary>
    [InitializeOnLoad]
    public static class EditorAudioPlayer
    {
        private static MethodInfo playMethod;
        private static MethodInfo stopMethod;
        private static MethodInfo stopAllMethod;

        private static AudioClip currentClip;
        private static bool isPlaying;
        private static double startTime;
        private static float clipLength;
        private static bool loopPlayback;

        public static bool isAvailable { get; private set;}= false;

        private static AudioSource editorAudioSource;
        
        
        // EditorAudioPlayer에 추가
        public static bool IsPaused { get; private set; }

        
        public static AudioSource GetAudioSource()
        {
            return editorAudioSource;
        }
        
        public static void SetTime(float time)
        {
            if (editorAudioSource != null && editorAudioSource.clip != null)
            {
                editorAudioSource.time = Mathf.Clamp(time, 0f, editorAudioSource.clip.length);
            }
        }
        
        static EditorAudioPlayer()
        {
            try
            {
                var audioUtilType= typeof(AudioImporter).Assembly.GetType("UnityEditor.AudioUtil");
                if(audioUtilType!=null)
                {
                    playMethod= audioUtilType.GetMethod(
                        "PlayClip",
                        BindingFlags.Static| BindingFlags.Public,
                        null,
                        new Type[]{ typeof(AudioClip), typeof(int), typeof(bool)},
                        null
                    );
                    stopMethod= audioUtilType.GetMethod(
                        "StopClip",
                        BindingFlags.Static| BindingFlags.Public
                    );
                    stopAllMethod= audioUtilType.GetMethod(
                        "StopAllClips",
                        BindingFlags.Static| BindingFlags.Public
                    );
                    if(playMethod!=null && stopMethod!=null && stopAllMethod!=null)
                    {
                        isAvailable= true;
                    }
                }
                EditorApplication.update+= UpdatePlayer;
            }
            catch
            {
                isAvailable= false;
            }
        }

        private static void UpdatePlayer()
        {
            if(!isPlaying|| currentClip==null) return;
            double elapsed= EditorApplication.timeSinceStartup- startTime;
            if(elapsed>= clipLength)
            {
                if(loopPlayback)
                {
                    startTime= EditorApplication.timeSinceStartup;
                }
                else
                {
                    StopClip();
                }
            }
        }

        public static void PlayClip(AudioClip clip, bool loop=false)
        {
            if(!isAvailable)
            {
                Debug.LogWarning("EditorAudioPlayer not available. Use PlayMode instead.");
                return;
            }
            if(clip==null|| playMethod==null) return;
            StopClip();

            currentClip= clip;
            clipLength= clip.length;
            loopPlayback= loop;
            isPlaying= true;
            startTime= EditorApplication.timeSinceStartup;

            playMethod.Invoke(null, new object[]{ clip, 0, loop});
        }

        public static void StopClip()
        {
            if(!isAvailable) return;
            if(!isPlaying|| currentClip==null|| stopMethod==null) return;

            stopMethod.Invoke(null, new object[]{ currentClip});
            currentClip= null;
            isPlaying= false;
        }

        public static void StopAllClips()
        {
            if(!isAvailable) return;
            if(stopAllMethod!=null) stopAllMethod.Invoke(null, null);
            currentClip= null;
            isPlaying= false;
        }

        public static bool IsPlaying => (isAvailable && isPlaying);

        public static float GetPlaybackPosition()
        {
            if(!isAvailable|| !isPlaying|| currentClip==null) return 0f;
            double elapsed= EditorApplication.timeSinceStartup- startTime;
            float pos= (float)elapsed;
            if(pos> clipLength && !loopPlayback) pos= clipLength;
            return pos;
        }
    }
}
