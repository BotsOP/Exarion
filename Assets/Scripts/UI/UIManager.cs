using System;
using System.Globalization;
using System.IO;
using AnotherFileBrowser.Windows;
using DataPersistence;
using Drawing;
using Managers;
using SFB;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using EventType = Managers.EventType;

namespace UI
{
    public class UIManager : MonoBehaviour, IDataPersistence
    {
        public static bool isInteracting;
        public static bool stopInteracting;
        public static bool isFullView = true;
        public static int imageWidth;
        public static int imageHeight;
        
        [Header("Viewport")]
        [SerializeField] private RawImage viewImageFull;
        [SerializeField] private RawImage viewImageFocus;
        [SerializeField] private RawImage displayImageFull;
        [SerializeField] private RawImage displayImageFocus;
        [SerializeField] private Camera viewCam;
        [SerializeField] private Camera displayCam;
        [SerializeField] private GameObject fullView;
        [SerializeField] private GameObject focusView;
        [SerializeField] private Image cachedButton;
        
        [Header("Select Deselect")]
        [SerializeField] private Color selectedColor;
        [SerializeField] private Color backgroundColor;
        
        [Header("UI input")]
        [SerializeField] private TMP_InputField brushSizeInput;
        [SerializeField] private Slider brushSizeSlider;
        [SerializeField] private Button pngButton;
        [SerializeField] private Button exrButton;
        
        private CustomRenderTexture viewFullRT;
        private CustomRenderTexture viewFocusRT;
        private CustomRenderTexture displayFullRT;
        private CustomRenderTexture displayFocusRT;
        private RectTransform rectTransformViewFull;
        private RectTransform rectTransformDisplayFull;
        private RectTransform rectTransformViewFocus;
        private RectTransform rectTransformDisplayFocus;
        private bool pngToggle;
        private string projectName;
        private bool exportPNG;

        private void OnEnable()
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
            
            EventSystem<float>.RaiseEvent(EventType.SET_BRUSH_SIZE, brushSizeSlider.value);
            EventSystem<float>.Subscribe(EventType.SET_BRUSH_SIZE, SetBrushSize);
            EventSystem<bool>.Subscribe(EventType.IS_INTERACTING, IsInteracting);
        }

        private void OnDisable()
        {
            EventSystem<float>.Unsubscribe(EventType.SET_BRUSH_SIZE, SetBrushSize);
        }

        private void Start()
        {
            EventSystem<RectTransform, RectTransform>.RaiseEvent(EventType.VIEW_CHANGED, rectTransformViewFull, rectTransformDisplayFull);
        }

        public void ExportResult()
        {
            string path = StandaloneFileBrowser.SaveFilePanel("Save File", "", projectName, pngToggle ? "png" : "exr");
            DrawingManager drawingManager = FindObjectOfType<DrawingManager>();
            byte[] bytes = pngToggle ? drawingManager.drawer.rt.ToBytesPNG() : drawingManager.drawer.rt.ToBytesEXR();
            File.WriteAllBytes(path, bytes);
        }

        public void BackToMainMenu()
        {
            DataPersistenceManager.instance.SaveTool();
            SceneManager.LoadScene("MainMenu");
        }
        public void PNGButton()
        {
            pngToggle = true;
            pngButton.transform.GetChild(2).gameObject.SetActive(true);
            exrButton.transform.GetChild(2).gameObject.SetActive(false);
        }
        public void EXRButton()
        {
            pngToggle = false;
            pngButton.transform.GetChild(2).gameObject.SetActive(false);
            exrButton.transform.GetChild(2).gameObject.SetActive(true);
        }
        public void ActivateGameObject(GameObject _gameObject)
        {
            _gameObject.SetActive(true);
        }
        public void DeactivateGameObject(GameObject _gameObject)
        {
            _gameObject.SetActive(false);
        }

        public void SaveProject()
        {
            DataPersistenceManager.instance.SaveTool();
        }
        public void SaveExit()
        {
            DataPersistenceManager.instance.SaveTool();
            Application.Quit();
        }

        public void IsInteracting(bool _isInteracting)
        {
            isInteracting = _isInteracting;
        }
        public void StopInteracting(bool _isInteracting)
        {
            stopInteracting = _isInteracting;
        }

        public void UpdateBrushType(TMP_Dropdown _index)
        {
            EventSystem<int>.RaiseEvent(EventType.CHANGE_PAINTTYPE, _index.value);
        }

        public void SwitchToFullView(Image _buttonImage)
        {
            isFullView = true;
            _buttonImage.GetComponent<Image>().color = selectedColor;
            if(cachedButton) { cachedButton.GetComponent<Image>().color = backgroundColor; }
            cachedButton = _buttonImage;
            
            EventSystem<RectTransform, RectTransform>.RaiseEvent(EventType.VIEW_CHANGED, rectTransformViewFull, rectTransformDisplayFull);
            viewCam.targetTexture = viewFullRT;
            displayCam.targetTexture = displayFullRT;
        
            fullView.SetActive(true);
            focusView.SetActive(false);
        }
    
        public void SwitchToFocusView(Image _buttonImage)
        {
            isFullView = false;
            _buttonImage.GetComponent<Image>().color = selectedColor;
            if(cachedButton) { cachedButton.GetComponent<Image>().color = backgroundColor; }
            cachedButton = _buttonImage;
            
            EventSystem<RectTransform, RectTransform>.RaiseEvent(EventType.VIEW_CHANGED, rectTransformViewFocus, rectTransformDisplayFocus);
            viewCam.targetTexture = viewFocusRT;
            displayCam.targetTexture = displayFocusRT;
        
            fullView.SetActive(false);
            focusView.SetActive(true);
        }

        public void OnBrushSizeChanged(bool _sliderChanged)
        {
            if (_sliderChanged)
            {
                brushSizeInput.text = brushSizeSlider.value.ToString(CultureInfo.CurrentCulture);
            }
            else
            {
                brushSizeSlider.value = int.Parse(brushSizeInput.text);
            }
            EventSystem<float>.RaiseEvent(EventType.SET_BRUSH_SIZE, brushSizeSlider.value);
        }

        private void SetBrushSize(float _brushSize)
        {
            int brushSize = (int)_brushSize;
            brushSizeInput.text = brushSize.ToString(CultureInfo.CurrentCulture);
            brushSizeSlider.value = brushSize;
        }
        public void LoadData(ToolData _data)
        {
            imageWidth = _data.imageWidth;
            imageHeight = _data.imageHeight;
            projectName = _data.projectName;
        }
        public void SaveData(ToolData _data)
        {
        }
    }
}
