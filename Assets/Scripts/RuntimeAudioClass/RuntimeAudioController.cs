using UnityEngine;

namespace MyRhythmEditor
{
    /// <summary>
    /// PlayMode에서 AudioSource를 통해
    /// 안정적으로 재생/일시정지/정지/스크럽.
    /// 씬에 GameObject + AudioSource + RuntimeAudioController 붙여 사용.
    /// EditorWindow가 instance로 접근하여 오디오 제어.
    /// </summary>
    public class RuntimeAudioController : MonoBehaviour
    {
        public static RuntimeAudioController instance;

        public AudioSource audioSrc;
        private bool isPaused = false;

// RuntimeAudioController에 추가
        public bool IsPaused()
        {
            return audioSrc != null && !audioSrc.isPlaying && audioSrc.time > 0;
        }
        
        void Awake()
        {
            if(!instance) instance = this;
            else if(instance != this) Destroy(gameObject);
        }

        public void SetClip(AudioClip clip)
        {
            if(audioSrc) audioSrc.clip = clip;
        }

        public void Play(bool loop)
        {
            if(!audioSrc) return;
            audioSrc.loop = loop;
            if(isPaused)
            {
                audioSrc.UnPause();
                isPaused = false;
            }
            else
            {
                audioSrc.time = 0f;
                audioSrc.Play();
            }
        }

        public void Pause()
        {
            if(!audioSrc || !audioSrc.isPlaying) return;
            audioSrc.Pause();
            isPaused = true;
        }

        public void Stop()
        {
            if(!audioSrc) return;
            audioSrc.Stop();
            audioSrc.time = 0f;
            isPaused = false;
        }

        public void SetTime(float t)
        {
            if(!audioSrc || !audioSrc.clip) return;
            float len = audioSrc.clip.length;
            t = Mathf.Clamp(t, 0f, len);
            audioSrc.time = t;
            if(!audioSrc.isPlaying && !isPaused)
            {
                audioSrc.Play();
            }
        }

        public void SetPitch(float pitch)
        {
            if (audioSrc != null)
            {
                audioSrc.pitch = pitch;
            }
        }
        
        public float GetTime()
        {
            if(!audioSrc || !audioSrc.clip) return 0f;
            return audioSrc.time;
        }

        public float GetLength()
        {
            if(!audioSrc || !audioSrc.clip) return 0f;
            return audioSrc.clip.length;
        }

        public bool IsPlaying()
        {
            return audioSrc && audioSrc.isPlaying && !isPaused;
        }
    }
}
