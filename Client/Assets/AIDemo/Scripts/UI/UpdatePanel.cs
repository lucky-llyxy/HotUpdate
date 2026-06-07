using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace HotUpdateDemo.UI
{
    public sealed class UpdatePanel : MonoBehaviour
    {
        [SerializeField] private Text statusText;
        [SerializeField] private Text percentText;
        [SerializeField] private Slider progressSlider;
        [SerializeField] private Button enterButton;

        public void BindEnterButton(UnityAction action)
        {
            if (enterButton == null)
            {
                return;
            }

            enterButton.onClick.RemoveAllListeners();
            enterButton.onClick.AddListener(action);
        }

        public void SetProgress(float progress, string status)
        {
            progress = Mathf.Clamp01(progress);

            if (progressSlider != null)
            {
                progressSlider.value = progress;
            }

            if (percentText != null)
            {
                percentText.text = $"{Mathf.RoundToInt(progress * 100f)}%";
            }

            if (statusText != null)
            {
                statusText.text = status;
            }
        }

        public void SetButtonVisible(bool visible)
        {
            if (enterButton != null)
            {
                enterButton.gameObject.SetActive(visible);
            }
        }
    }
}
