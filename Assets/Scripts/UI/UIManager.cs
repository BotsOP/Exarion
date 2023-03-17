using System;
using System.Globalization;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace UI
{
    public class UIManager : MonoBehaviour
    {
        public bool isFullView;
        public RawImage viewImageFull;
        public RawImage viewImageFocus;
        public RawImage displayImageFull;
        public RawImage displayImageFocus;
        [SerializeField] private Camera viewCam;
        [SerializeField] private Camera displayCam;
        [SerializeField] private Color selectedColor;
        [SerializeField] private Color backgroundColor;
        [SerializeField] private GameObject fullView;
        [SerializeField] private GameObject focusView;
        [SerializeField] private Image cachedButton;
        [SerializeField] private TMP_InputField brushSizeInput;
        [SerializeField] private Slider brushSizeSlider;
        [SerializeField] private Slider speedSliderTimeline;
        [SerializeField] private Slider speedSliderShowcase;
        private CustomRenderTexture viewFullRT;
        private CustomRenderTexture viewFocusRT;
        private CustomRenderTexture displayFullRT;
        private CustomRenderTexture displayFocusRT;
        private RectTransform rectTransformViewFull;
        private RectTransform rectTransformViewFocus;
        private DrawingInput drawingInput;

        private void Awake()
        {
            drawingInput = new DrawingInput();
            
            rectTransformViewFull = viewImageFull.rectTransform;
            rectTransformViewFocus = viewImageFocus.rectTransform;
            
            Vector3[] viewCorners = new Vector3[4];
            rectTransformViewFull.GetWorldCorners(viewCorners);
            int imageWidth = (int)(viewCorners[2].x - viewCorners[0].x);
            int imageHeight = (int)(viewCorners[2].y - viewCorners[0].y);
            viewFullRT = new CustomRenderTexture(imageWidth, imageHeight);
            viewFocusRT = new CustomRenderTexture(imageWidth, imageHeight);
            
            viewCorners = new Vector3[4];
            rectTransformViewFocus.GetWorldCorners(viewCorners);
            imageWidth = (int)(viewCorners[2].x - viewCorners[0].x);
            imageHeight = (int)(viewCorners[2].y - viewCorners[0].y);
            displayFullRT = new CustomRenderTexture(imageWidth, imageHeight);
            displayFocusRT = new CustomRenderTexture(imageWidth, imageHeight);
            
            viewCam.targetTexture = viewFullRT;
            displayCam.targetTexture = displayFocusRT;
            
            viewImageFull.texture = viewFullRT;
            viewImageFocus.texture = viewFocusRT;
            displayImageFull.texture = displayFullRT;
            displayImageFocus.texture = displayFocusRT;
            
            EventSystem<float>.RaiseEvent(EventType.CHANGE_BRUSH_SIZE, brushSizeSlider.value);
            EventSystem<float>.RaiseEvent(EventType.TIME_SHOWCASE, speedSliderShowcase.value);
        }

        private void Update()
        {
            Debug.Log(Input.mousePosition);
            
            Vector3[] drawAreaCorners = new Vector3[4];
            rectTransformViewFull.GetWorldCorners(drawAreaCorners);
            drawingInput.UpdateDrawingInput(drawAreaCorners, viewCam.transform.position, viewCam.orthographicSize);
            
            EventSystem<float>.RaiseEvent(EventType.TIME, Time.time / speedSliderTimeline.value % 1);
        }

        public void SpeedSliderShowcaseChanged()
        {
            EventSystem<float>.RaiseEvent(EventType.TIME_SHOWCASE, speedSliderShowcase.value);
        }

        public void SwitchToFullView(Image buttonImage)
        {
            isFullView = true;
            buttonImage.GetComponent<Image>().color = selectedColor;
            if(cachedButton) { cachedButton.GetComponent<Image>().color = backgroundColor; }
            cachedButton = buttonImage;
        
            fullView.SetActive(true);
            focusView.SetActive(false);
        }
    
        public void SwitchToFocusView(Image buttonImage)
        {
            isFullView = false;
            buttonImage.GetComponent<Image>().color = selectedColor;
            if(cachedButton) { cachedButton.GetComponent<Image>().color = backgroundColor; }
            cachedButton = buttonImage;
        
            fullView.SetActive(false);
            focusView.SetActive(true);
        }

        public void OnBrushSizeChanged(bool sliderChanged)
        {
            Debug.Log($"value changed");
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
