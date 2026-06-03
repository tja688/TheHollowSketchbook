using System;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace StrayPathCore.Utils
{
    /// <summary>
    /// 场景切换管理器 —— 处理跨场景过渡动画与状态传递。
    /// 所有场景切换均应通过此管理器，避免直接调用 SceneManager.LoadScene。
    /// </summary>
    public class SceneTransitionManager : MonoBehaviour
    {
        public static SceneTransitionManager Instance { get; private set; }

        [Header("Transition Settings")]
        [SerializeField] private float fadeOutDuration = 0.3f;
        [SerializeField] private float fadeInDuration = 0.3f;
        [SerializeField] private Color fadeColor = Color.black;

        private Texture2D _fadeTexture;
        private bool _isFading;
        private float _fadeAlpha;
        private Action _onFadeComplete;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);

            _fadeTexture = new Texture2D(1, 1);
            _fadeTexture.SetPixel(0, 0, fadeColor);
            _fadeTexture.Apply();
        }

        // ==================== 公共接口 ====================

        public void TransitionTo(string sceneName, Action onComplete = null)
        {
            if (_isFading) return;
            StartCoroutine(TransitionCoroutine(sceneName, onComplete));
        }

        public void FadeOut(Action onComplete = null)
        {
            if (_isFading) return;
            StartCoroutine(FadeCoroutine(0f, 1f, fadeOutDuration, onComplete));
        }

        public void FadeIn(Action onComplete = null)
        {
            if (_isFading) return;
            StartCoroutine(FadeCoroutine(1f, 0f, fadeInDuration, onComplete));
        }

        // ==================== Coroutine ====================

        private IEnumerator TransitionCoroutine(string sceneName, Action onComplete)
        {
            _isFading = true;

            // Fade Out
            yield return FadeCoroutine(0f, 1f, fadeOutDuration, null);

            // Load Scene
            AsyncOperation asyncLoad = SceneManager.LoadSceneAsync(sceneName);
            asyncLoad.allowSceneActivation = false;
            while (asyncLoad.progress < 0.9f)
                yield return null;
            asyncLoad.allowSceneActivation = true;
            yield return new WaitUntil(() => asyncLoad.isDone);

            // Fade In
            yield return FadeCoroutine(1f, 0f, fadeInDuration, null);

            _isFading = false;
            onComplete?.Invoke();
        }

        private IEnumerator FadeCoroutine(float from, float to, float duration, Action onComplete)
        {
            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                _fadeAlpha = Mathf.Lerp(from, to, elapsed / duration);
                yield return null;
            }
            _fadeAlpha = to;
            onComplete?.Invoke();
        }

        // ==================== OnGUI Fade Overlay ====================

        private void OnGUI()
        {
            if (_fadeAlpha <= 0f) return;
            GUI.color = new Color(1f, 1f, 1f, _fadeAlpha);
            GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), _fadeTexture);
            GUI.color = Color.white;
        }
    }
}
