using UnityEngine;

namespace Game.Presentation.Services
{
    public readonly struct VfxEventId
    {
        public string Value { get; }
        public VfxEventId(string value) { Value = value; }
    }

    public readonly struct VfxContext
    {
        public Vector3 Position { get; }
        public Transform Parent { get; }
        public VfxContext(Vector3 position, Transform parent = null)
        {
            Position = position;
            Parent = parent;
        }
    }

    public interface IVfxService
    {
        void Play(VfxEventId id, VfxContext ctx);
    }
}
