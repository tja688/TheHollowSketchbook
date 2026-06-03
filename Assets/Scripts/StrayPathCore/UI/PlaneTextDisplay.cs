using System.Collections;
using System.Globalization;
using TMPro;
using UnityEngine;

namespace StrayPathCore.UI
{
    public class PlaneTextDisplay : MonoBehaviour
    {
        [SerializeField] private TextMeshPro _text;
        [SerializeField] private string _initialText = "Type here";
        [SerializeField] private Color _textColor = Color.white;
        [SerializeField] private float _fontSize = 1f;
        [SerializeField] private TextAlignmentOptions _alignment = TextAlignmentOptions.Center;
        [SerializeField] private Vector2 _textAreaSize = new Vector2(8f, 4.5f);
        [SerializeField] private Vector3 _localOffset = new Vector3(0f, 0.02f, 0f);
        [SerializeField] private Vector3 _localEulerAngles = new Vector3(-90f, 0f, 0f);
        [SerializeField] private Vector3 _localScale = Vector3.one;
        [SerializeField] private TextOverflowModes _overflowMode = TextOverflowModes.Masking;
        [SerializeField] private float _fadeDuration = 0.25f;
        [SerializeField] private bool _captureKeyboardInput = true;

        private Coroutine _fadeRoutine;
        private float _alpha = 1f;

        public TextMeshPro Text => _text;
        public string TextValue => _text != null ? _text.text : string.Empty;
        public float Alpha => _alpha;

        private void Awake()
        {
            EnsureTextObject();
            if (string.IsNullOrEmpty(_text.text))
                _text.text = _initialText;
            ApplyVisualSettings();
        }

        private void OnValidate()
        {
            if (_text != null)
                ApplyVisualSettings();
        }

        private void Update()
        {
            if (!_captureKeyboardInput)
                return;

            string input = Input.inputString;
            if (string.IsNullOrEmpty(input))
                return;

            for (int i = 0; i < input.Length; i++)
            {
                char character = input[i];
                if (character == '\b')
                    DeleteLastCharacter();
                else if (character == '\n' || character == '\r')
                    AppendText("\n");
                else
                    AppendText(character.ToString());
            }
        }

        public void SetText(string value)
        {
            EnsureTextObject();
            _text.text = value;
        }

        public void AppendText(string value)
        {
            EnsureTextObject();
            _text.text += value;
        }

        public void DeleteLastCharacter()
        {
            EnsureTextObject();
            string value = _text.text;
            if (string.IsNullOrEmpty(value))
                return;

            int[] indexes = StringInfo.ParseCombiningCharacters(value);
            _text.text = value.Substring(0, indexes[indexes.Length - 1]);
        }

        public void ClearText()
        {
            SetText(string.Empty);
        }

        public void FadeIn()
        {
            FadeTo(1f, _fadeDuration);
        }

        public void FadeOut()
        {
            FadeTo(0f, _fadeDuration);
        }

        public void FadeTo(float targetAlpha, float duration)
        {
            EnsureTextObject();
            if (_fadeRoutine != null)
                StopCoroutine(_fadeRoutine);
            _fadeRoutine = StartCoroutine(FadeRoutine(Mathf.Clamp01(targetAlpha), Mathf.Max(0f, duration)));
        }

        public void SetAlpha(float alpha)
        {
            _alpha = Mathf.Clamp01(alpha);
            if (_text == null)
                return;

            Color color = _text.color;
            color.a = _alpha;
            _text.color = color;
        }

        private IEnumerator FadeRoutine(float targetAlpha, float duration)
        {
            float startAlpha = _alpha;
            if (duration <= 0f)
            {
                SetAlpha(targetAlpha);
                _fadeRoutine = null;
                yield break;
            }

            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                SetAlpha(Mathf.Lerp(startAlpha, targetAlpha, elapsed / duration));
                yield return null;
            }

            SetAlpha(targetAlpha);
            _fadeRoutine = null;
        }

        private void EnsureTextObject()
        {
            if (_text != null)
                return;

            var textTransform = transform.Find("PlaneText");
            GameObject textObject;
            if (textTransform == null)
            {
                textObject = new GameObject("PlaneText", typeof(RectTransform));
                textObject.transform.SetParent(transform, false);
            }
            else
            {
                textObject = textTransform.gameObject;
            }

            _text = textObject.GetComponent<TextMeshPro>();
            if (_text == null)
                _text = textObject.AddComponent<TextMeshPro>();
        }

        private void ApplyVisualSettings()
        {
            _text.color = _textColor;
            _text.fontSize = _fontSize;
            _text.alignment = _alignment;
            _text.enableWordWrapping = true;
            _text.overflowMode = _overflowMode;
            _text.rectTransform.sizeDelta = _textAreaSize;
            _text.rectTransform.localPosition = _localOffset;
            _text.rectTransform.localEulerAngles = _localEulerAngles;
            _text.rectTransform.localScale = _localScale;
            SetAlpha(_textColor.a);
        }
    }
}
