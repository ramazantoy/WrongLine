using PrimeTween;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace WrongLine.UI
{
    /// <summary>Press feedback for a brush button: it dips under the finger and springs back. Unscaled time, so it works while paused.</summary>
    [RequireComponent(typeof(Button))]
    public sealed class BrushButton : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerExitHandler
    {
        [SerializeField, Range(0.8f, 1f)] private float _pressedScale = 0.94f;

        private Button _button;
        private Tween _tween;

        private void Awake() => _button = GetComponent<Button>();

        private void OnDisable()
        {
            _tween.Stop();
            transform.localScale = Vector3.one;
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            if (_button.IsInteractable())
            {
                Animate(_pressedScale, 0.06f, Ease.OutQuad);
            }
        }

        public void OnPointerUp(PointerEventData eventData) => Release();

        public void OnPointerExit(PointerEventData eventData) => Release();

        private void Release()
        {
            if (!Mathf.Approximately(transform.localScale.x, 1f))
            {
                Animate(1f, 0.2f, Ease.OutBack);
            }
        }

        private void Animate(float scale, float seconds, Ease ease)
        {
            _tween.Stop();
            _tween = Tween.Scale(transform, scale, seconds, ease, useUnscaledTime: true);
        }
    }
}
