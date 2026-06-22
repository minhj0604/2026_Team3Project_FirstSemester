using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using Team3Project.Dialogue;

namespace Team3Project.UI
{
    [RequireComponent(typeof(Button))]
    public class DirectButtonClicker : MonoBehaviour
    {
        private Button button;
        private RectTransform rectTransform;

        private void Awake()
        {
            button = GetComponent<Button>();
            rectTransform = GetComponent<RectTransform>();
        }

        private void Update()
        {
            if (DialogueManager.HasActiveDialogue())
            {
                return;
            }

            if (Mouse.current == null || !Mouse.current.leftButton.wasPressedThisFrame)
            {
                return;
            }

            if (button == null || !button.interactable || rectTransform == null)
            {
                return;
            }

            var canvas = GetComponentInParent<Canvas>();
            var camera = canvas == null || canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera;
            if (RectTransformUtility.RectangleContainsScreenPoint(rectTransform, Mouse.current.position.ReadValue(), camera))
            {
                button.onClick.Invoke();
            }
        }
    }
}
