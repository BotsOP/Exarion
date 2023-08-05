using System;
using System.Collections.Generic;
using DataPersistence;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace MainMenus
{
    public class SaveSlotsMenu : Menu
    {
        [Header("new project")]
        [SerializeField] private CreationMenu creationMenu;
        
        [Header("Confirmation Popup")]
        [SerializeField] private ConfirmationPopupMenu confirmationPopupMenu;

        [Header("Save slot settings")]
        [SerializeField] private RectTransform saveSlotContent;
        [SerializeField] private GameObject saveSlotButton;

        private List<SaveSlot> saveSlots;

        //private SaveSlot[] saveSlots;

        private bool isLoadingTool = false;

        private void Start()
        {
            ActivateMenu();
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
            confirmationPopupMenu.ActivateMenu("Starting a new Tool with this slot will override the currently saved data. Are you sure?",
               // 'yes'
               () => {
                   DataPersistenceManager.instance.ChangeSelectedProfileId(_saveSlot.GetProfileId());
                   DataPersistenceManager.instance.NewTool();
                   SaveToolAndLoadScene();
               },
               // 'cancel'
               () => {
                   this.ActivateMenu();
               }
            );
        }

        private void SaveToolAndLoadScene() 
        {
            // save the game anytime before loading a new scene
            DataPersistenceManager.instance.SaveTool();
            // load the scene
            SceneManager.LoadSceneAsync("DrawScene");
        }

        // public void OnClearClicked(SaveSlot saveSlot) 
        // {
        //     DisableMenuButtons();
        //
        //     confirmationPopupMenu.ActivateMenu(
        //         "Are you sure you want to delete this saved data?",
        //         // function to execute if we select 'yes'
        //         () => {
        //             DataPersistenceManager.instance.DeleteProfileData(saveSlot.GetProfileId());
        //             ActivateMenu(isLoadingTool);
        //         },
        //         // function to execute if we select 'cancel'
        //         () => {
        //             ActivateMenu(isLoadingTool);
        //         }
        //     );
        // }

        public void ActivateMenu() 
        {
            // set this menu to be active
            this.gameObject.SetActive(true);

            // load all of the profiles that exist
            Dictionary<string, ToolData> profilesGameData = DataPersistenceManager.instance.GetAllProfilesToolData();

            // ensure the back button is enabled when we activate the menu

            foreach (var saveSlotInfo in profilesGameData)
            {
                GameObject saveSlotObject = Instantiate(saveSlotButton, saveSlotContent);
                SaveSlot saveSlot = saveSlotObject.GetComponent<SaveSlot>();
                saveSlot.profileId = saveSlotInfo.Key;
                saveSlot.SetData(saveSlotInfo.Value);
                saveSlot.saveSlotsMenu = this;

                saveSlots.Add(saveSlot);
            }
        }

        public void NewProjectMenu()
        {
            creationMenu.ActivateMenu();
        }

        private void DisableMenuButtons() 
        {
            foreach (SaveSlot saveSlot in saveSlots) 
            {
                saveSlot.SetInteractable(false);
            }
        }
    }
}
