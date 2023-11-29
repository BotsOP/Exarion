using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using DataPersistence;
using DataPersistence.Data;
using Drawing;
using Managers;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using EventType = Managers.EventType;
using FileBrowser = Crosstales.FB.FileBrowser;

namespace UI
{
    public class UIManager : MonoBehaviour, IDataPersistence
    {
        public static bool isInteracting;
        public static bool stopInteracting;
        public static bool isFullView = true;
        public static bool center = true;
        public static int imageWidth;
        public static int imageHeight;
        
        [Header("Viewport")]
        [SerializeField] private RawImage viewImageFull;
        [SerializeField] private RawImage viewImageFocus;
        [SerializeField] private RawImage displayImageFull;
        [SerializeField] private RawImage displayImageFocus;
        [SerializeField] private RawImage saveIcon;
        [SerializeField] private AnimationCurve saveIconCurve;
        [SerializeField] private Camera viewCam;
        [SerializeField] private Camera displayCam;
        [SerializeField] private GameObject fullView;
        [SerializeField] private GameObject focusView;
        [SerializeField] private GameObject fileMenu;
        [SerializeField] private Image fullViewButton;
        [SerializeField] private Image focusViewButton;
        [SerializeField] private RectTransform overlayControl;
        [SerializeField] private RectTransform overlayFullView;
        [SerializeField] private RectTransform overlayFocusView;
        [SerializeField] private RectTransform timelineControl;
        [SerializeField] private RectTransform timelineFullView;
        [SerializeField] private RectTransform timelineFocusView;
        [SerializeField] private RectTransform uvImageChannelContent;
        [SerializeField] private RawImage currentUVChannelImage;
        
        [Header("Select Deselect")]
        [SerializeField] private Color selectedColor;
        [SerializeField] private Color backgroundColor;
        
        [Header("Select Deselect 2")]
        [SerializeField] private Color selectedColor2;
        [SerializeField] private Color backgroundColor2;
        
        [Header("UI input")]
        [SerializeField] private TMP_InputField brushSizeInput;
        [SerializeField] private Slider brushSizeSlider;
        [SerializeField] private Button pngButton;
        [SerializeField] private Button exrButton;
        [SerializeField] private Image pivotButton;
        [SerializeField] private Image centerButton;
        [SerializeField] private GameObject uvImageChannelButton;

        private List<RawImage> uvChannelButtonBackgrounds;
        private CustomRenderTexture viewFullRT;
        private CustomRenderTexture viewFocusRT;
        private CustomRenderTexture displayFullRT;
        private CustomRenderTexture displayFocusRT;
        private static RectTransform rectTransformViewFull;
        private RectTransform rectTransformDisplayFull;
        private static RectTransform rectTransformViewFocus;
        private RectTransform rectTransformDisplayFocus;
        private bool pngToggle;
        private string projectName;
        private bool exportPNG;
        private bool toggleSave;
        private float startTime;
        private int subMeshCount;
        private int currentUVChannel;
        private string[] extensions = { "" };
        private byte[] imgData;
        private Texture2D importedTexture;
        private Texture2D[] channelTextures;
        
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
            EventSystem.Subscribe(EventType.SAVED, Saved);
        }

        private void OnDisable()
        {
            EventSystem<float>.Unsubscribe(EventType.SET_BRUSH_SIZE, SetBrushSize);
            EventSystem<bool>.Unsubscribe(EventType.IS_INTERACTING, IsInteracting);
            EventSystem.Unsubscribe(EventType.SAVED, Saved);
        }

        private void Start()
        {
            EventSystem<RectTransform, RectTransform>.RaiseEvent(EventType.VIEW_CHANGED, rectTransformViewFull, rectTransformDisplayFull);
            uvChannelButtonBackgrounds = new List<RawImage>();
            
            for (int i = 0; i < subMeshCount; i++)
            {
                GameObject temp = Instantiate(uvImageChannelButton, uvImageChannelContent);
                UVImageChannelButton uvImageChannelButtonClass = temp.GetComponent<UVImageChannelButton>();
                uvImageChannelButtonClass.channel = i;
                
                temp.GetComponent<Button>().onClick.AddListener(delegate { SwitchedUVChannel(uvImageChannelButtonClass); });
                
                RawImage background = temp.GetComponent<RawImage>();
                background.color = i == 0 ? selectedColor : backgroundColor;
                uvChannelButtonBackgrounds.Add(background);
                
                temp.transform.GetChild(0).gameObject.GetComponent<TMP_Text>().text = "UV Channel " + i;
            }

            channelTextures = new Texture2D[subMeshCount];
            if (channelTextures.Length > 0)
            {
                if (channelTextures[0] is not null)
                {
                    currentUVChannelImage.texture = channelTextures[0];
                }
            }
        }

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                fileMenu.SetActive(!fileMenu.activeSelf);
            }

            if (toggleSave)
            {
                toggleSave = false;
                startTime = Time.time;
            }
            float newTime = Time.time - startTime;
            if (newTime < 1)
            {
                float alpha = saveIconCurve.Evaluate(newTime);
                Color color = new Color(0.8f, 0.8f, 0.8f, alpha);
                saveIcon.color = color;
            }

            if (Input.GetKeyDown(KeyCode.S) && Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.S) && Input.GetKeyDown(KeyCode.LeftShift))
            {
                SaveProject();
            }
        }

        private void SwitchedUVChannel(UVImageChannelButton _button)
        {
            currentUVChannel = _button.channel;
            foreach (var rawImage in uvChannelButtonBackgrounds)
            {
                rawImage.color = backgroundColor;
            }

            Texture2D tex = channelTextures[currentUVChannel];
            if (tex is not null)
            {
                currentUVChannelImage.texture = channelTextures[currentUVChannel];
            }
            else
            {
                currentUVChannelImage.texture = new Texture2D(1, 1);
            }
            _button.GetComponent<RawImage>().color = selectedColor;
        }
        
        public void LoadImage()
        {
            String path = FileBrowser.Instance.OpenSingleFile("Open file", "", "", extensions);
            StartCoroutine(LoadImage(path));
        }

        private IEnumerator LoadImage(string _path)
        {
            using (UnityWebRequest uwr = UnityWebRequestTexture.GetTexture(_path))
            {
                yield return uwr.SendWebRequest();

                if (uwr.isNetworkError || uwr.isHttpError)
                {
                    Debug.Log(uwr.error);
                }
                else
                {
                    var uwrTexture = DownloadHandlerTexture.GetContent(uwr);
                
                    imgData = uwrTexture.EncodeToPNG();
                    if (!LoadImage(out importedTexture))
                    {
                        Debug.LogError($"Image is null or holds no data");
                    }

                    channelTextures[currentUVChannel] = importedTexture;
                    currentUVChannelImage.texture = importedTexture;
                    EventSystem<int, Texture2D>.RaiseEvent(EventType.IMPORT_MODEL_TEXTURE, currentUVChannel, importedTexture);
                }
            }
        }
        
        private bool LoadImage(out Texture2D _texture)
        {
            _texture = null;
            if (imgData is null) return false;
            if (imgData.Length <= 0) return false;
        
            //Size doesnt matter ;)
            _texture = new Texture2D(1, 1);
            _texture.LoadImage(imgData);
            return true;
        }

        private void Saved()
        {
            toggleSave = true;
        }

        public static bool IsMouseInsideDrawArea()
        {
            Vector3[] viewCorners = new Vector3[4];
            if (isFullView)
            {
                rectTransformViewFull.GetWorldCorners(viewCorners);
            }
            else
            {
                rectTransformViewFocus.GetWorldCorners(viewCorners);
            }
            return Input.mousePosition.x > viewCorners[0].x && Input.mousePosition.x < viewCorners[2].x && 
                   Input.mousePosition.y > viewCorners[0].y && Input.mousePosition.y < viewCorners[2].y;
        }
        
        public void ExportResult()
        {
            string path = FileBrowser.Instance.SaveFile("Save texture", "", projectName, pngToggle ? "png" : "exr");
            DrawingManager2D drawingManager = FindObjectOfType<DrawingManager2D>();
            //byte[] bytes = pngToggle ? drawingManager.drawer.rt.ToBytesPNG() : drawingManager.drawer.rt.ToBytesEXR();
            byte[] bytes = pngToggle ? drawingManager.drawer.ReverseRtoB().ToBytesPNG() : drawingManager.drawer.ReverseRtoB().ToBytesEXR();
            File.WriteAllBytes(path, bytes);
        }

        public void ExportResult3d()
        {
            string path = FileBrowser.Instance.SaveFile("Save texture", "", projectName, pngToggle ? "png" : "exr");
            DrawingManager3D drawingManager = FindObjectOfType<DrawingManager3D>();
            List<CustomRenderTexture> rts = drawingManager.GetTextures();

            for (int i = 0; i < rts.Count; i++)
            {
                CustomRenderTexture result = rts[i];
                byte[] bytes = pngToggle ? result.ToBytesPNG() : result.ToBytesEXR();
                string tempPath = path.Insert(path.Length - 4, "_" + i);
                File.WriteAllBytes(tempPath, bytes);
            }
        }

        public void BackToMainMenu()
        {
            EventSystem.RaiseEvent(EventType.SAVED);
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

        public void PivotButton()
        {
            centerButton.color = backgroundColor2;
            pivotButton.color = selectedColor2;
            center = false;
        }
        public void CenterButton()
        {
            centerButton.color = selectedColor2;
            pivotButton.color = backgroundColor2;
            center = true;
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
            EventSystem.RaiseEvent(EventType.SAVED);
            DataPersistenceManager.instance.SaveTool();
        }
        public void SaveExit()
        {
            EventSystem.RaiseEvent(EventType.SAVED);
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
            fullViewButton.color = selectedColor;
            focusViewButton.color = backgroundColor;
            overlayControl.transform.SetParent(overlayFullView.transform);
            timelineControl.transform.SetParent(timelineFullView.transform);
            overlayControl.anchoredPosition = Vector3.zero;
            timelineControl.anchoredPosition = Vector3.zero;
            
            EventSystem<RectTransform, RectTransform>.RaiseEvent(EventType.VIEW_CHANGED, rectTransformViewFull, rectTransformDisplayFull);
            EventSystem<bool>.RaiseEvent(EventType.VIEW_CHANGED, true);
            viewCam.targetTexture = viewFullRT;
            displayCam.targetTexture = displayFullRT;
        
            fullView.SetActive(true);
            focusView.SetActive(false);
        }
    
        public void SwitchToFocusView(Image _buttonImage)
        {
            isFullView = false;
            fullViewButton.color = backgroundColor;
            focusViewButton.color = selectedColor;
            overlayControl.transform.SetParent(overlayFocusView.transform);
            timelineControl.transform.SetParent(timelineFocusView.transform);
            overlayControl.anchoredPosition = Vector3.zero;
            timelineControl.anchoredPosition = Vector3.zero;
            
            EventSystem<RectTransform, RectTransform>.RaiseEvent(EventType.VIEW_CHANGED, rectTransformViewFocus, rectTransformDisplayFocus);
            EventSystem<bool>.RaiseEvent(EventType.VIEW_CHANGED, false);
            viewCam.targetTexture = viewFocusRT;
            displayCam.targetTexture = displayFocusRT;
        
            fullView.SetActive(false);
            focusView.SetActive(true);
        }

        private bool changedBrushSize;
        public void OnBrushSizeChanged(bool _sliderChanged)
        {
            if(changedBrushSize) { return; }
            changedBrushSize = true;
            if (_sliderChanged)
            {
                brushSizeInput.text = brushSizeSlider.value.ToString(CultureInfo.CurrentCulture);
            }
            else
            {
                try
                {
                    brushSizeSlider.value = float.Parse(brushSizeInput.text);
                }
                catch (Exception e)
                {
                    //Debug.LogError(e);
                    throw;
                }
            }
            EventSystem<float>.RaiseEvent(EventType.SET_BRUSH_SIZE, brushSizeSlider.value);
        }

        public void LateUpdate()
        {
            changedBrushSize = false;
        }

        private void SetBrushSize(float _brushSize)
        {
            float brushSize = _brushSize;
            brushSizeInput.text = brushSize.ToString(CultureInfo.CurrentCulture);
            brushSizeSlider.value = brushSize;
        }
        
        public void LoadData(ToolData _data, ToolMetaData _metaData)
        {
            imageWidth = _data.imageWidth;
            imageHeight = _data.imageHeight;
            projectName = _metaData.projectName;
            if (_data is ToolData3D)
            {
                ToolData3D toolData3D = (ToolData3D)_data;
                subMeshCount = toolData3D.indices.Count;
                channelTextures = new Texture2D[subMeshCount];
                List<int> channels = new List<int>();
                List<Texture2D> texture2Ds = new List<Texture2D>();
                for (var i = 0; i < toolData3D.maskImg.Count; i++)
                {
                    var imgByte = toolData3D.maskImg[i];
                    if (imgByte == null || imgByte.Length == 0)
                    {
                        continue;
                    }
                    Texture2D tex = new Texture2D(1, 1);
                    tex.LoadImage(imgByte);
                    channels.Add(i);
                    texture2Ds.Add(tex);
                    channelTextures[i] = tex;
                }
                StartCoroutine(WaitAndPrint(channels, texture2Ds));
            }
        }
        
        private IEnumerator WaitAndPrint(List<int> _currentUVChannels, List<Texture2D> _importedTextures)
        {
            yield return new WaitForEndOfFrameUnit();
            yield return new WaitForEndOfFrameUnit();
            yield return new WaitForEndOfFrameUnit();
            for (int i = 0; i < _currentUVChannels.Count; i++)
            {
                EventSystem<int, Texture2D>.RaiseEvent(EventType.IMPORT_MODEL_TEXTURE, _currentUVChannels[i], _importedTextures[i]);
            }
        }
        public void SaveData(ToolData _data, ToolMetaData _metaData)
        {
            if (_data is ToolData3D)
            {
                ToolData3D toolData3D = (ToolData3D)_data;
                toolData3D.maskImg.Clear();
                for (var i = 0; i < subMeshCount; i++)
                {
                    if (channelTextures[i] == null)
                    {
                        toolData3D.maskImg.Add(new byte[0]);
                        continue;
                    }
                    toolData3D.maskImg.Add(channelTextures[i].EncodeToPNG());
                }
            }
        }
    }
}
