using UnityEngine;
using UnityEngine.UI;

namespace WrongLine.UI
{
    /// <summary>
    /// Shows a slider's value by revealing a filled image (<see cref="Image.fillAmount"/>) instead of stretching one: the brush
    /// sprite keeps its shape. Follows the value every frame, so values set without a notification show too.
    /// </summary>
    [RequireComponent(typeof(Slider))]
    public sealed class SliderFill : MonoBehaviour
    {
        [SerializeField] private Image _fill;

        private Slider _slider;

        private void Awake() => _slider = GetComponent<Slider>();

        private void LateUpdate()
        {
            if (_fill != null)
            {
                _fill.fillAmount = _slider.normalizedValue;
            }
        }
    }
}
