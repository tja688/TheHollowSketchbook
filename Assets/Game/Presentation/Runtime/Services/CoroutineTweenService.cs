using System.Collections;
using System.Threading.Tasks;
using UnityEngine;

namespace Game.Presentation.Services
{
    public sealed class CoroutineTweenService : MonoBehaviour, ITweenService
    {
        public Task MoveTo(Transform target, Vector3 position, float duration, EaseType ease = EaseType.OutQuad)
        {
            if (duration <= 0f || target == null) return Task.CompletedTask;
            var tcs = new TaskCompletionSource<object>();
            StartCoroutine(MoveToCoroutine(target, position, duration, ease, tcs));
            return tcs.Task;
        }

        public Task RotateTo(Transform target, Quaternion rotation, float duration, EaseType ease = EaseType.OutQuad)
        {
            if (duration <= 0f || target == null) return Task.CompletedTask;
            var tcs = new TaskCompletionSource<object>();
            StartCoroutine(RotateToCoroutine(target, rotation, duration, ease, tcs));
            return tcs.Task;
        }

        public Task ScaleTo(Transform target, Vector3 scale, float duration, EaseType ease = EaseType.OutQuad)
        {
            if (duration <= 0f || target == null) return Task.CompletedTask;
            var tcs = new TaskCompletionSource<object>();
            StartCoroutine(ScaleToCoroutine(target, scale, duration, ease, tcs));
            return tcs.Task;
        }

        public Task FadeCanvasGroup(CanvasGroup group, float alpha, float duration, EaseType ease = EaseType.Linear)
        {
            if (duration <= 0f || group == null) return Task.CompletedTask;
            var tcs = new TaskCompletionSource<object>();
            StartCoroutine(FadeCoroutine(group, alpha, duration, ease, tcs));
            return tcs.Task;
        }

        public Task PunchScale(Transform target, Vector3 amount, float duration)
        {
            if (duration <= 0f || target == null) return Task.CompletedTask;
            var tcs = new TaskCompletionSource<object>();
            StartCoroutine(PunchScaleCoroutine(target, amount, duration, tcs));
            return tcs.Task;
        }

        public Task Delay(float duration)
        {
            if (duration <= 0f) return Task.CompletedTask;
            var tcs = new TaskCompletionSource<object>();
            StartCoroutine(DelayCoroutine(duration, tcs));
            return tcs.Task;
        }

        private static IEnumerator MoveToCoroutine(Transform target, Vector3 end, float duration, EaseType ease, TaskCompletionSource<object> tcs)
        {
            Vector3 start = target.position;
            float elapsed = 0f;
            while (elapsed < duration)
            {
                if (target == null) { tcs.TrySetResult(null); yield break; }
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                target.position = Vector3.Lerp(start, end, Ease(t, ease));
                yield return null;
            }
            if (target != null) target.position = end;
            tcs.TrySetResult(null);
        }

        private static IEnumerator RotateToCoroutine(Transform target, Quaternion end, float duration, EaseType ease, TaskCompletionSource<object> tcs)
        {
            Quaternion start = target.rotation;
            float elapsed = 0f;
            while (elapsed < duration)
            {
                if (target == null) { tcs.TrySetResult(null); yield break; }
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                target.rotation = Quaternion.Slerp(start, end, Ease(t, ease));
                yield return null;
            }
            if (target != null) target.rotation = end;
            tcs.TrySetResult(null);
        }

        private static IEnumerator ScaleToCoroutine(Transform target, Vector3 end, float duration, EaseType ease, TaskCompletionSource<object> tcs)
        {
            Vector3 start = target.localScale;
            float elapsed = 0f;
            while (elapsed < duration)
            {
                if (target == null) { tcs.TrySetResult(null); yield break; }
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                target.localScale = Vector3.Lerp(start, end, Ease(t, ease));
                yield return null;
            }
            if (target != null) target.localScale = end;
            tcs.TrySetResult(null);
        }

        private static IEnumerator FadeCoroutine(CanvasGroup group, float end, float duration, EaseType ease, TaskCompletionSource<object> tcs)
        {
            float start = group.alpha;
            float elapsed = 0f;
            while (elapsed < duration)
            {
                if (group == null) { tcs.TrySetResult(null); yield break; }
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                group.alpha = Mathf.Lerp(start, end, Ease(t, ease));
                yield return null;
            }
            if (group != null) group.alpha = end;
            tcs.TrySetResult(null);
        }

        private static IEnumerator PunchScaleCoroutine(Transform target, Vector3 amount, float duration, TaskCompletionSource<object> tcs)
        {
            Vector3 baseScale = target.localScale;
            Vector3 peak = baseScale + amount;
            float half = duration * 0.5f;
            float elapsed = 0f;
            while (elapsed < half)
            {
                if (target == null) { tcs.TrySetResult(null); yield break; }
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / half);
                target.localScale = Vector3.Lerp(baseScale, peak, Ease(t, EaseType.OutQuad));
                yield return null;
            }
            elapsed = 0f;
            while (elapsed < half)
            {
                if (target == null) { tcs.TrySetResult(null); yield break; }
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / half);
                target.localScale = Vector3.Lerp(peak, baseScale, Ease(t, EaseType.InQuad));
                yield return null;
            }
            if (target != null) target.localScale = baseScale;
            tcs.TrySetResult(null);
        }

        private static IEnumerator DelayCoroutine(float duration, TaskCompletionSource<object> tcs)
        {
            yield return new WaitForSeconds(duration);
            tcs.TrySetResult(null);
        }

        private static float Ease(float t, EaseType ease)
        {
            return ease switch
            {
                EaseType.Linear => t,
                EaseType.OutQuad => 1f - (1f - t) * (1f - t),
                EaseType.InOutQuad => t < 0.5f ? 2f * t * t : 1f - Mathf.Pow(-2f * t + 2f, 2f) / 2f,
                EaseType.OutBack => 1f + 2.70158f * Mathf.Pow(t - 1f, 3f) + 1.70158f * Mathf.Pow(t - 1f, 2f),
                EaseType.InBack => 2.70158f * t * t * t - 1.70158f * t * t,
                _ => t
            };
        }
    }
}
