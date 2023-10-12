using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace MainMenus
{
    public class SaveSlotSlim : MonoBehaviour
    {
        [Header("Content")]
        [SerializeField] private TextMeshProUGUI projectNameText;
        
        [NonSerialized] public string projectName;
        [NonSerialized] public MainMenu mainMenu;

        private Button saveSlotButton;

        public void OnClicked()
        {
            mainMenu.OnSaveSlotClicked(this);
        }

        public void SetData(string _projectName)
        {
            projectName = _projectName;
            projectNameText.text = _projectName;
        }

        public void SetInteractable(bool interactable)
        {
            saveSlotButton.interactable = interactable;
        }
    }
}