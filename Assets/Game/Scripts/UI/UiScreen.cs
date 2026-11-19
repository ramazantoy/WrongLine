using PrimeTween;
using UnityEngine;

namespace WrongLine.UI
{
    /// <summary>
    /// One screen of the game's single UI canvas: a rect with a <see cref="CanvasGroup"/> that fades in and out and, if asked, grows
    /// from nothing and shrinks back. Screens are children of one canvas instead of a canvas each. A hidden screen is inactive, so it
    /// costs nothing and takes no input; <see cref="Hide"/> deactivates it when the animation ends. Unscaled time: it works while paused.
    /// </summary>
    [RequireComponent(typeof(CanvasGroup))]
    public sealed class UiScreen : MonoBehaviour
    {
        /// <summary>A scale of exactly zero breaks the canvas' matrices; the screen shrinks to this instead.</summary>
        private const float SmallestScale = 0.001f;

        [Tooltip("Whether the screen is open when the scene starts.")]
        [SerializeField] private bool _startOpen;

        [Tooltip("The scale it grows from and shrinks to. 1 only fades; 0 grows from nothing.")]
        [SerializeField, Range(0f, 1f)] private float _hiddenScale = 1f;

        [Tooltip("What grows and shrinks, if not the screen itself: a dialog's card, while the dimmed ground behind it only fades.")]
        [SerializeField] private Transform _scaleTarget;

        [SerializeField, Min(0.05f)] private float _seconds = 0.25f;

        private CanvasGroup _group;
        private Tween _fade;
        private Tween _zoom;

        // Unset until the screen is first shown or hidden: it then starts as _startOpen says. Not set in Awake, which Unity runs on
        // the first activation, that is, in the middle of the first Show, and would undo it.
        private bool? _state;

        private CanvasGroup Group => _group != null ? _group : (_group = GetComponent<CanvasGroup>());

        private Transform ScaleTarget => _scaleTarget != null ? _scaleTarget : transform;

        private float HiddenScale => Mathf.Max(_hiddenScale, SmallestScale);

        /// <summary>True from the moment <see cref="Show"/> is called until <see cref="Hide"/> is: the animation may still be running.</summary>
        public bool IsOpen => _state ?? _startOpen;

        private void Awake()
        {
            // A screen that is active at the start but not meant to be open starts out of sight.
            if (_state == null && !_startOpen)
            {
                SetHiddenLook();
            }
        }

        private void OnDisable() => StopTweens();

        public void Show()
        {
            if (IsOpen)
            {
                return;
            }

            _state = true;
            StopTweens();
            if (!gameObject.activeSelf)
            {
                gameObject.SetActive(true);
                SetHiddenLook();
            }

            Group.interactable = true;
            Group.blocksRaycasts = true;
            _fade = Tween.Alpha(Group, 1f, _seconds, Ease.OutQuad, useUnscaledTime: true);
            if (_hiddenScale < 1f)
            {
                _zoom = Tween.Scale(ScaleTarget, 1f, _seconds, Ease.OutBack, useUnscaledTime: true);
            }
        }

        public void Hide()
        {
            if (!IsOpen)
            {
                return;
            }

            _state = false;
            StopTweens();
            Group.interactable = false;
            Group.blocksRaycasts = false;
            // Stopped on purpose when the screen is shown again before it has faded out: its end is then not wanted.
            _fade = Tween.Alpha(Group, 0f, _seconds, Ease.InQuad, useUnscaledTime: true).OnComplete(this, static screen => screen.Finish(), warnIfTargetDestroyed: false);
            if (_hiddenScale < 1f)
            {
                _zoom = Tween.Scale(ScaleTarget, HiddenScale, _seconds, Ease.InBack, useUnscaledTime: true);
            }
        }

        // Only the hide animation ends here: showing the screen again stops it first, so this never runs for a screen that is open.
        private void Finish() => gameObject.SetActive(false);

        private void SetHiddenLook()
        {
            Group.alpha = 0f;
            Group.interactable = false;
            Group.blocksRaycasts = false;
            ScaleTarget.localScale = Vector3.one * HiddenScale;
        }

        private void StopTweens()
        {
            _fade.Stop();
            _zoom.Stop();
        }
    }
}
