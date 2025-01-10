namespace MyRhythmEditor
{
    using FMODUnity;
    using FMOD.Studio;
    using UnityEngine;

    public class FmodAudioPlayer
    {
        private EventInstance eventInstance;
        private bool isPlaying;

        public void PlayEvent(string eventPath)
        {
            StopEvent();
            eventInstance = RuntimeManager.CreateInstance(eventPath);
            eventInstance.start();
            isPlaying = true;
        }

        public void StopEvent()
        {
            if (eventInstance.isValid())
            {
                eventInstance.stop(FMOD.Studio.STOP_MODE.IMMEDIATE);
                eventInstance.release();
            }
            isPlaying = false;
        }

        public bool IsPlaying() => isPlaying;

        public float GetCurrentTimeSeconds()
        {
            if (!eventInstance.isValid() || !isPlaying) return 0f;
            eventInstance.getTimelinePosition(out int ms);
            return ms / 1000f;
        }

        public void Dispose()
        {
            StopEvent();
        }
    }
}