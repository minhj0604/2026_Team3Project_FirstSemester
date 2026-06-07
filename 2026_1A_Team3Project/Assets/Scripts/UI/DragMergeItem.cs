using Team3Project.GameSystems;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Team3Project.UI
{
    public class DragMergeItem : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
    {
        [SerializeField] private ResourceFamily family;
        [SerializeField] private int stage;

        private Canvas canvas;
        private RectTransform rectTransform;
        private CanvasGroup canvasGroup;
        private Transform startParent;
        private Vector2 startPosition;

        public MergeResource Resource => new(family, stage);

        public void Configure(ResourceFamily resourceFamily, int resourceStage)
        {
            family = resourceFamily;
            stage = resourceStage;
        }

        private void Awake()
        {
            canvas = GetComponentInParent<Canvas>();
            rectTransform = GetComponent<RectTransform>();
            canvasGroup = GetComponent<CanvasGroup>();
            if (canvasGroup == null)
            {
                canvasGroup = gameObject.AddComponent<CanvasGroup>();
            }
        }

        public void OnBeginDrag(PointerEventData eventData)
        {
            startParent = transform.parent;
            startPosition = rectTransform.anchoredPosition;
            canvasGroup.blocksRaycasts = false;
            transform.SetParent(canvas.transform, true);
        }

        public void OnDrag(PointerEventData eventData)
        {
            rectTransform.anchoredPosition += eventData.delta / Mathf.Max(1f, canvas.scaleFactor);
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            canvasGroup.blocksRaycasts = true;
            if (transform.parent == canvas.transform)
            {
                transform.SetParent(startParent, true);
                rectTransform.anchoredPosition = startPosition;
            }
        }
    }
}
