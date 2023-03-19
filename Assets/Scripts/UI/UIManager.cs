using System;
using System.Globalization;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace UI
{
    public class UIManager : MonoBehaviour
    {
        public static bool IsInteracting;
        [SerializeField] private RawImage viewImageFull;
        [SerializeField] private RawImage viewImageFocus;
        [SerializeField] private RawImage displayImageFull;
        [SerializeField] private RawImage displayImageFocus;
        [SerializeField] private Camera viewCam;
        [SerializeField] private Camera displayCam;
        [SerializeField] private Color selectedColor;
        [SerializeField] private Color backgroundColor;
        [SerializeField] private GameObject fullView;
        [SerializeField] private GameObject focusView;
        [SerializeField] private Image cachedButton;
        [SerializeField] private TMP_InputField brushSizeInput;
        [SerializeField] private Slider brushSizeSlider;
        private CustomRenderTexture viewFullRT;
        private CustomRenderTexture viewFocusRT;
        private CustomRenderTexture displayFullRT;
        private CustomRenderTexture displayFocusRT;
        private RectTransform rectTransformViewFull;
        private RectTransform rectTransformDisplayFull;
        private RectTransform rectTransformViewFocus;
        private RectTransform rectTransformDisplayFocus;

        private void Awake()
        {
            rectTransformViewFull = viewImageFull.rectTransform;
            rectTransformViewFocus = viewImageFocus.rectTransform;
            rectTransformDisplayFull = displayImageFull.rectTransform;
            rectTransformDisplayFocus = displayImageFocus.rectTransform;
            
            Vector3[] viewCorners = new Vector3[4];
            rectTransformViewFull.GetWorldCorners(viewCorners);
            int imageWidth = (int)(viewCorners[2].x - viewCorners[0].x);
            int imageHeight = (int)(viewCorners[2].y - viewCorners[0].y);
            viewFullRT = new CustomRenderTexture(imageWidth, imageHeight)
            {
                name = "viewFull",
            };
            rectTransformViewFocus.GetWorldCorners(viewCorners);
            imageWidth = (int)(viewCorners[2].x - viewCorners[0].x);
            imageHeight = (int)(viewCorners[2].y - viewCorners[0].y);
            viewFocusRT = new CustomRenderTexture(imageWidth, imageHeight)
            {
                name = "viewFocus",
            };
            
            rectTransformDisplayFull.GetWorldCorners(viewCorners);
            imageWidth = (int)(viewCorners[2].x - viewCorners[0].x);
            imageHeight = (int)(viewCorners[2].y - viewCorners[0].y);
            displayFullRT = new CustomRenderTexture(imageWidth, imageHeight)
            {
                name = "displayFull",
            };
            rectTransformDisplayFocus.GetWorldCorners(viewCorners);
            imageWidth = (int)(viewCorners[2].x - viewCorners[0].x);
            imageHeight = (int)(viewCorners[2].y - viewCorners[0].y);
            displayFocusRT = new CustomRenderTexture(imageWidth, imageHeight)
            {
                name = "displayFocusRT",
            };
            
            viewCam.targetTexture = viewFullRT;
            displayCam.targetTexture = displayFullRT;
            
            viewImageFull.texture = viewFullRT;
            viewImageFocus.texture = viewFocusRT;
            displayImageFull.texture = displayFullRT;
            displayImageFocus.texture = displayFocusRT;
            
            EventSystem<float>.RaiseEvent(EventType.CHANGE_BRUSH_SIZE, brushSizeSlider.value);
            EventSystem<RectTransform, RectTransform>.RaiseEvent(EventType.VIEW_CHANGED, rectTransformViewFull, rectTransformDisplayFull);
        }

        public void StartInteracting()
        {
            IsInteracting = true;
        }
        public void StopInteracting()
        {
            IsInteracting = false;
        }

        public void SwitchToFullView(Image buttonImage)
        {
            buttonImage.GetComponent<Image>().color = selectedColor;
            if(cachedButton) { cachedButton.GetComponent<Image>().color = backgroundColor; }
            cachedButton = buttonImage;
            
            EventSystem<RectTransform, RectTransform>.RaiseEvent(EventType.VIEW_CHANGED, rectTransformViewFull, rectTransformDisplayFull);
            viewCam.targetTexture = viewFullRT;
            displayCam.targetTexture = displayFullRT;
        
            fullView.SetActive(true);
            focusView.SetActive(false);
        }
    
        public void SwitchToFocusView(Image buttonImage)
        {
            buttonImage.GetComponent<Image>().color = selectedColor;
            if(cachedButton) { cachedButton.GetComponent<Image>().color = backgroundColor; }
            cachedButton = buttonImage;
            
            EventSystem<RectTransform, RectTransform>.RaiseEvent(EventType.VIEW_CHANGED, rectTransformViewFocus, rectTransformDisplayFocus);
            viewCam.targetTexture = viewFocusRT;
            displayCam.targetTexture = displayFocusRT;
        
            fullView.SetActive(false);
            focusView.SetActive(true);
        }

        public void OnBrushSizeChanged(bool sliderChanged)
        {
            if (sliderChanged)
            {
                brushSizeInput.text = brushSizeSlider.value.ToString(CultureInfo.CurrentCulture);
            }
            else
            {
                brushSizeSlider.value = int.Parse(brushSizeInput.text);
            }
            EventSystem<float>.RaiseEvent(EventType.CHANGE_BRUSH_SIZE, brushSizeSlider.value);
        }
    }
}
