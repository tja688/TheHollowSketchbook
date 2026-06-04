namespace Game.Presentation.Services
{
    public readonly struct AudioEventId
    {
        public string Value { get; }
        public AudioEventId(string value) { Value = value; }
    }

    public readonly struct AudioParams
    {
        public float Volume { get; }
        public float Pitch { get; }
        public AudioParams(float volume = 1f, float pitch = 1f) { Volume = volume; Pitch = pitch; }
    }

    public interface IAudioService
    {
        void PlayOneShot(AudioEventId id, AudioParams p = default);
        void PlayMusic(AudioEventId id);
        void StopMusic(AudioEventId id);
    }
}
