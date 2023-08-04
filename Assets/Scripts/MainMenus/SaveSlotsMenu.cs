using System.Collections.Generic;
using DataPersistence;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace MainMenus
{
    public class SaveSlotsMenu : Menu
    {
        [Header("Menu Navigation")]
        [SerializeField] private MainMenu mainMenu;

        [Header("Menu Buttons")]
        [SerializeField] private Button backButton;

        [Header("Confirmation Popup")]
        [SerializeField] private ConfirmationPopupMenu confirmationPopupMenu;

        [Header("Settings")]
        [SerializeField] private RectTransform saveSlotContent;
        [SerializeField] private GameObject saveSlotButton;

        private List<SaveSlot> saveSlots;

        //private SaveSlot[] saveSlots;

        private bool isLoadingTool = false;

        private void Awake() 
        {
            //saveSlots = this.GetComponentsInChildren<SaveSlot>();
        }

        public void OnSaveSlotClicked(SaveSlot _saveSlot) 
        {
            // disable all buttons
            DisableMenuButtons();

            if (isLoadingTool) // case - loading game
            {
                DataPersistenceManager.instance.ChangeSelectedProfileId(_saveSlot.GetProfileId());
                SaveToolAndLoadScene();
            }
            else if (_saveSlot.hasData) // case - new game, but the save slot has data
            {
                confirmationPopupMenu.ActivateMenu("Starting a new Tool with this slot will override the currently saved data. Are you sure?",
                    // 'yes'
                    () => {
                        DataPersistenceManager.instance.ChangeSelectedProfileId(_saveSlot.GetProfileId());
                        DataPersistenceManager.instance.NewTool();
                        SaveToolAndLoadScene();
                    },
                    // 'cancel'
                    () => {
                        this.ActivateMenu(isLoadingTool);
                    }
                );
            }
            else // case - new tool, and the save slot has no data
            {
                DataPersistenceManager.instance.ChangeSelectedProfileId(_saveSlot.GetProfileId());
                DataPersistenceManager.instance.NewTool();
                SaveToolAndLoadScene();
            }
        }

        private void SaveToolAndLoadScene() 
        {
            // save the game anytime before loading a new scene
            DataPersistenceManager.instance.SaveTool();
            // load the scene
            SceneManager.LoadSceneAsync("DrawScene");
        }

        public void OnClearClicked(SaveSlot saveSlot) 
        {
            DisableMenuButtons();

            confirmationPopupMenu.ActivateMenu(
                "Are you sure you want to delete this saved data?",
                // function to execute if we select 'yes'
                () => {
                    DataPersistenceManager.instance.DeleteProfileData(saveSlot.GetProfileId());
                    ActivateMenu(isLoadingTool);
                },
                // function to execute if we select 'cancel'
                () => {
                    ActivateMenu(isLoadingTool);
                }
            );
        }

        public void OnBackClicked() 
        {
            mainMenu.ActivateMenu();
            this.DeactivateMenu();
        }

        public void ActivateMenu(bool isLoadingGame) 
        {
            // set this menu to be active
            this.gameObject.SetActive(true);

            // set mode
            this.isLoadingTool = isLoadingGame;

            // load all of the profiles that exist
            Dictionary<string, ToolData> profilesGameData = DataPersistenceManager.instance.GetAllProfilesToolData();

            // ensure the back button is enabled when we activate the menu
            backButton.interactable = true;

            foreach (var saveSlotInfo in profilesGameData)
            {
                GameObject saveSlotObject = Instantiate(saveSlotButton, saveSlotContent);
                SaveSlot saveSlot = saveSlotObject.GetComponent<SaveSlot>();
                saveSlot.profileId = saveSlotInfo.Key;
                saveSlot.SetData(saveSlotInfo.Value);
                saveSlot.saveSlotsMenu = this;

                saveSlots.Add(saveSlot);
            }
            
            this.SetFirstSelected(backButton.gameObject.GetComponent<Button>());
        }

        public void DeactivateMenu() 
        {
            this.gameObject.SetActive(false);
        }

        private void DisableMenuButtons() 
        {
            foreach (SaveSlot saveSlot in saveSlots) 
            {
                saveSlot.SetInteractable(false);
            }
            backButton.interactable = false;
        }
    }
}
