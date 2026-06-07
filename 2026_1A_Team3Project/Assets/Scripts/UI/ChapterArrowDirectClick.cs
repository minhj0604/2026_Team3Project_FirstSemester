using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

namespace Team3Project.UI
{
    public class ChapterArrowDirectClick : MonoBehaviour
    {
        private RectTransform previousArrow;
        private RectTransform nextArrow;

        private void Update()
        {
            if (SceneManager.GetActiveScene().name != "ChapterSelectScene")
            {
                return;
            }

            CacheArrows();
            if (Mouse.current == null || !Mouse.current.leftButton.wasPressedThisFrame)
            {
                return;
            }

            var mousePosition = Mouse.current.position.ReadValue();
            if (Contains(previousArrow, mousePosition))
            {
                SceneFlowBootstrap.ChangeChapter(-1);
            }
            else if (Contains(nextArrow, mousePosition))
            {
                SceneFlowBootstrap.ChangeChapter(1);
            }
        }

        private void CacheArrows()
        {
            if (previousArrow == null)
            {
                previousArrow = FindRect("Previous Chapter Arrow");
            }

            if (nextArrow == null)
            {
                nextArrow = FindRect("Next Chapter Arrow");
            }
        }

        private static RectTransform FindRect(string objectName)
        {
            var target = GameObject.Find(objectName);
            return target == null ? null : target.GetComponent<RectTransform>();
        }

        private static bool Contains(RectTransform rect, Vector2 screenPosition)
        {
            return rect != null && RectTransformUtility.RectangleContainsScreenPoint(rect, screenPosition);
        }
    }
}
