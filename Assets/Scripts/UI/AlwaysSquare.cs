using UnityEngine;

namespace UI
{
    public class AlwaysSquare : MonoBehaviour
    {
        [SerializeField] private RectTransform rectTransformParent;
        private RectTransform rectTransform;
        void Start()
        {
            rectTransform = GetComponent<RectTransform>();
        }

        void Update()
        {
            float width = rectTransformParent.rect.width;
            float height = rectTransformParent.rect.height;
            if (width > height)
            {
                rectTransform.sizeDelta = new Vector2(height, height);
            }
            if (height > width)
            {
                rectTransform.sizeDelta = new Vector2(width, width);
            }
        }
    }
}
