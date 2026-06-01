using UnityEngine;

namespace Game.Presentation.Services
{
    public sealed class UnityAudioService : MonoBehaviour, IAudioService
    {
        public void PlayOneShot(AudioEventId id, AudioParams p = default)
        {
            // Placeholder: log to console in prototype
            Debug.Log($"[Audio] PlayOneShot: {id.Value}");
        }

        public void PlayMusic(AudioEventId id)
        {
            Debug.Log($"[Audio] PlayMusic: {id.Value}");
        }

        public void StopMusic(AudioEventId id)
        {
            Debug.Log($"[Audio] StopMusic: {id.Value}");
        }
    }
}
