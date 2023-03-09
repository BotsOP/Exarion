using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace MainMenus
{
    public class SaveSlot : MonoBehaviour
    {
        [Header("Profile")]
        [SerializeField] private string profileId = "";

        [Header("Content")]
        [SerializeField] private GameObject noDataContent;
        [SerializeField] private GameObject hasDataContent;
        [SerializeField] private TextMeshProUGUI projectName;

        [Header("Clear Data Button")]
        [SerializeField] private Button clearButton;

        public bool hasData { get; private set; } = false;

        private Button saveSlotButton;

        private void Awake() 
        {
            saveSlotButton = this.GetComponent<Button>();
        }

        public void SetData(ToolData data) 
        {
       
            if (data == null)  // there's no data for this profileId
            {
                hasData = false;
                noDataContent.SetActive(true);
                hasDataContent.SetActive(false);
                clearButton.gameObject.SetActive(false);
            }
            else             // there is data for this profileId
            {
                hasData = true;
                noDataContent.SetActive(false);
                hasDataContent.SetActive(true);
                clearButton.gameObject.SetActive(true);

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
            clearButton.interactable = interactable;
        }
    }
}
