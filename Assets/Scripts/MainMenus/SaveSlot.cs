using System;
using DataPersistence.Data;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace MainMenus
{
    public class SaveSlot : MonoBehaviour
    {

        [Header("Content")]
        [SerializeField] private TextMeshProUGUI projectName;
        [SerializeField] private RawImage displayImg;
        
        [NonSerialized] public MainMenu mainMenu;

        public Texture2D displayTexture;
        public ToolMetaData projectData;
        private Button saveSlotButton;

        private void Awake() 
        {
            saveSlotButton = GetComponent<Button>();
            saveSlotButton.onClick.AddListener(OnClicked);
        }

        private void OnClicked()
        {
            //mainMenu.OnSaveSlotClicked(this);
        }
        
        public void SetData(ToolMetaData _data)
        {
            projectData = _data;
            
            projectName.text = projectData.projectName;
            
            //if (_data.displayImg is null) return;
            //if (_data.displayImg.Length <= 0) return;
            displayTexture = new Texture2D(1, 1, TextureFormat.ARGB32, false);
            //displayTexture.LoadImage(_data.displayImg);
            displayTexture.filterMode = FilterMode.Point;
            displayImg.texture = displayTexture;
        }

        public void SetInteractable(bool interactable)
        {
            saveSlotButton.interactable = interactable;
        }
    }
}
