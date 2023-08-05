using System;
using System.Collections.Generic;
using System.Linq;
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

        [Header("Save slot settings")]
        [SerializeField] private SaveSlotPopUp saveSlotPopUp;
        [SerializeField] private RectTransform saveSlotContent;
        [SerializeField] private GameObject saveSlotButton;

        private List<SaveSlot> saveSlots;

        private void Start()
        {
            ActivateMenu();
        }

        public void OnSaveSlotClicked(SaveSlot _saveSlot) 
        {
            saveSlotPopUp.UpdateSaveSlot(_saveSlot.toolData, _saveSlot.displayTexture);
            // disable all buttons
            //DisableMenuButtons();
            //DataPersistenceManager.instance.ChangeSelectedProfileId(_saveSlot.GetProfileId());
            //SaveToolAndLoadScene();
            
            // confirmationPopupMenu.ActivateMenu("Starting a new Tool with this slot will override the currently saved data. Are you sure?",
            //    // 'yes'
            //    () => {
            //        DataPersistenceManager.instance.ChangeSelectedProfileId(_saveSlot.GetProfileId());
            //        DataPersistenceManager.instance.NewTool();
            //        SaveToolAndLoadScene();
            //    },
            //    // 'cancel'
            //    () => {
            //        this.ActivateMenu();
            //    }
            // );
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
            gameObject.SetActive(true);

            // load all of the profiles that exist
            Dictionary<string, ToolData> profilesGameData = DataPersistenceManager.instance.GetAllProfilesToolData();

            List<ToolData> saveSlotData = profilesGameData.Values.ToList();
            int low = 0;
            int high = saveSlotData.Count - 1;
            QuickSort(saveSlotData, low, high);

            saveSlotData.Reverse();

            foreach (var saveSlotInfo in saveSlotData)
            {
                GameObject saveSlotObject = Instantiate(saveSlotButton, saveSlotContent);
                SaveSlot saveSlot = saveSlotObject.GetComponent<SaveSlot>();
                saveSlot.SetData(saveSlotInfo);
                saveSlot.saveSlotsMenu = this;

                saveSlots.Add(saveSlot);
            }
        }
        
        private void QuickSort(List<ToolData> _dateList, int _low, int _high)
        {
            if (_low < _high)
            {
                int pi = Partition(_dateList, _low, _high);

                QuickSort(_dateList, _low, pi - 1);
                QuickSort(_dateList, pi + 1, _high);
            }
        }

        private int Partition(List<ToolData> _dateList, int low, int high)
        {
            long pivot = _dateList[high].lastUpdated;
            int i = (low - 1);

            for (int j = low; j <= high - 1; j++)
            {
                if (_dateList[j].lastUpdated < pivot)
                {
                    i++;
                    Swap(_dateList, i, j);
                }
            }
            Swap(_dateList, i + 1, high);
            return (i + 1);
        }

        private void Swap(List<ToolData> _dateList, int i, int j)
        {
            (_dateList[i], _dateList[j]) = (_dateList[j], _dateList[i]);
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
