using UnityEngine;

namespace Game.Presentation.Services
{
    public sealed class SimpleVfxService : MonoBehaviour, IVfxService
    {
        public void Play(VfxEventId id, VfxContext ctx)
        {
            Debug.Log($"[Vfx] Play: {id.Value} at {ctx.Position}");
        }
    }
}
