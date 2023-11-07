using System;
using DataPersistence.Data;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace MainMenus
{
    public class SaveSlot : MonoBehaviour
    {

        [Header("Preset info")]
        [SerializeField] private TextMeshProUGUI projectNameGUI;
        [SerializeField] private RawImage displayImg;
        [SerializeField] private Shader displayShader;
        [SerializeField] private float timeIncrease;
        
        [Header("Runtime info")]
        public Texture2D displayTexture;
        public string projectName;
        
        [NonSerialized] public MainMenu mainMenu;
        
        public ToolMetaData metaData;
        private Material displayMat;
        private Button saveSlotButton;
        private float time;
        private static readonly int CustomTime = Shader.PropertyToID("_CustomTime");

        private void Awake() 
        {
            saveSlotButton = GetComponent<Button>();
            saveSlotButton.onClick.AddListener(OnClicked);
            displayMat = new Material(displayShader);
        }

        private void OnClicked()
        {
            mainMenu.OnSaveSlotClicked(this);
        }
        
        public void SetData(ToolMetaData _metaData)
        {
            metaData = _metaData;
            
            projectNameGUI.text = _metaData.projectName;
            projectName = _metaData.projectName;

            if (_metaData.results.Count == 0)
            {
                Debug.LogError($"No images saved");
                return;
            }

            if (_metaData.results[0] is null)
            {
                Debug.LogError($"Image is null");
                return;
            }

            if (_metaData.results[0].Length <= 0)
            {
                Debug.LogError($"No data stored in image");
                return;
            }
            displayTexture = new Texture2D(1, 1, TextureFormat.ARGB32, false);
            displayTexture.LoadImage(_metaData.results[0]);
            displayTexture.filterMode = FilterMode.Point;
            displayImg.texture = displayTexture;
            
            displayImg.material = displayMat;
        }

        private void FixedUpdate()
        {
            time = (time + timeIncrease) % 1;
            displayMat.SetFloat(CustomTime, time);
        }

        public void SetInteractable(bool interactable)
        {
            saveSlotButton.interactable = interactable;
        }
    }
}
