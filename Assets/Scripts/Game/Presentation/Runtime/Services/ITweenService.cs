using System.Threading.Tasks;
using UnityEngine;

namespace Game.Presentation.Services
{
    public enum EaseType
    {
        Linear,
        OutQuad,
        InOutQuad,
        InQuad,
        OutBack,
        InBack
    }

    public interface ITweenService
    {
        Task MoveTo(Transform target, Vector3 position, float duration, EaseType ease = EaseType.OutQuad);
        Task RotateTo(Transform target, Quaternion rotation, float duration, EaseType ease = EaseType.OutQuad);
        Task ScaleTo(Transform target, Vector3 scale, float duration, EaseType ease = EaseType.OutQuad);
        Task FadeCanvasGroup(CanvasGroup group, float alpha, float duration, EaseType ease = EaseType.Linear);
        Task PunchScale(Transform target, Vector3 amount, float duration);
        Task Delay(float duration);
    }
}
