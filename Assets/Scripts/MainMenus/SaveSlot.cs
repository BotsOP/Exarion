using System;
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
        
        [NonSerialized] public string profileId = "";
        [NonSerialized] public SaveSlotsMenu saveSlotsMenu;

        private ToolData toolData;
        private Button saveSlotButton;

        private void Awake() 
        {
            saveSlotButton = GetComponent<Button>();
            saveSlotButton.onClick.AddListener(OnClicked);
        }

        private void OnClicked()
        {
            saveSlotsMenu.OnSaveSlotClicked(this);
        }

        public void SetData(ToolData _data)
        {
            toolData = _data;
            
            projectName.text = toolData.GetProjectName();
            
            if (_data.displayImg is null) return;
            if (_data.displayImg.Length <= 0) return;
            Texture2D tex = new Texture2D(1, 1);
            tex.LoadImage(_data.displayImg);
            displayImg.texture = tex;
        }

        public string GetProfileId() 
        {
            return profileId;
        }

        public void SetInteractable(bool interactable)
        {
            saveSlotButton.interactable = interactable;
        }
    }
}
