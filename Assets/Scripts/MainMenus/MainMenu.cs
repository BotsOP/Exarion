using System.Collections;
using System.Collections.Generic;
using DataPersistence;
using MainMenus;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class MainMenu : Menu
{
    [Header("Menu Navigation")]
    [SerializeField] private SaveSlotsMenu saveSlotsMenu;

    [Header("Menu Buttons")]
    [SerializeField] private Button newToolButton;
    [SerializeField] private Button continueToolButton;
    [SerializeField] private Button loadToolButton;

    public int sceneIndex;

    private void Start() 
    {
        DisableButtonsDependingOnData();
    }

    private void DisableButtonsDependingOnData() 
    {
        if (!DataPersistenceManager.instance.HasGameData()) 
        {
            continueToolButton.interactable = false;
            loadToolButton.interactable = false;
        }
    }

    public void OnNewGameClicked() 
    {
        saveSlotsMenu.ActivateMenu(false);
        this.DeactivateMenu();
    }

    public void OnLoadGameClicked() 
    {
        saveSlotsMenu.ActivateMenu(true);
        this.DeactivateMenu();
    }

    public void OnContinueGameClicked() 
    {
        DisableMenuButtons();
        // save the game anytime before loading a new scene
        DataPersistenceManager.instance.SaveTool();
        // load the next scene - which will in turn load the game because of 
        // OnSceneLoaded() in the DataPersistenceManager
        SceneManager.LoadSceneAsync(sceneIndex);
    }

    private void DisableMenuButtons() 
    {
        loadToolButton.interactable = false;
        continueToolButton.interactable = false;
    }

    public void ActivateMenu() 
    {
        this.gameObject.SetActive(true);
        DisableButtonsDependingOnData();
    }

    public void DeactivateMenu() 
    {
        this.gameObject.SetActive(false);
    }
}
