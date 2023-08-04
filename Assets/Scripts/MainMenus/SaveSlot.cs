using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace MainMenus
{
    public class SaveSlot : MonoBehaviour
    {
        [NonSerialized] public string profileId = "";

        [Header("Content")]
        [SerializeField] private GameObject noDataContent;
        [SerializeField] private GameObject hasDataContent;
        [SerializeField] private TextMeshProUGUI projectName;
        
        public bool hasData { get; private set; } = false;

        public SaveSlotsMenu saveSlotsMenu;

        private Button saveSlotButton;

        private void Awake() 
        {
            saveSlotButton = this.GetComponent<Button>();
            saveSlotButton.onClick.AddListener(OnClicked);
        }

        public void OnClicked()
        {
            saveSlotsMenu.OnSaveSlotClicked(this);
        }

        public void SetData(ToolData data) 
        {
            if (data == null)  // there's no data for this profileId
            {
                hasData = false;
                noDataContent.SetActive(true);
                hasDataContent.SetActive(false);
            }
            else             // there is data for this profileId
            {
                hasData = true;
                noDataContent.SetActive(false);
                hasDataContent.SetActive(true);

                projectName.text = data.GetProjectName();
            }
        }

        public string GetProfileId() 
        {
            return this.profileId;
        }

        public void SetInteractable(bool interactable)
        {
            saveSlotButton.interactable = interactable;
        }
    }
}
