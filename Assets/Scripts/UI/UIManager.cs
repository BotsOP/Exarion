using System;
using System.Globalization;
using Drawing;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace UI
{
    public class UIManager : MonoBehaviour
    {
        public bool isFullView;
        public RawImage paintBoard;
        public RawImage paintBoard2;
        [SerializeField] private DrawingManager drawingManager;
        [SerializeField] private Color selectedColor;
        [SerializeField] private Color backgroundColor;
        [SerializeField] private GameObject fullView;
        [SerializeField] private GameObject focusView;
        [SerializeField] private Image cachedButton;
        [SerializeField] private Slider brushSizeSlider;
        [SerializeField] private TMP_InputField brushSizeInput;
        [SerializeField] private Slider speedSliderTimeline;
        [SerializeField] private Slider speedSliderShowcase;
        
        private void Awake()
        {
            paintBoard.texture = drawingManager.drawer.rt;
            paintBoard2.texture = drawingManager.drawer.rt;
            EventSystem<float>.RaiseEvent(EventType.CHANGE_BRUSH_SIZE, brushSizeSlider.value);
            EventSystem<float>.RaiseEvent(EventType.TIME_SHOWCASE, speedSliderShowcase.value);
        }

        private void Update()
        {
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
