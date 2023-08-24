using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Managers;
using UnityEngine;
using UnityEngine.SceneManagement;
using EventType = Managers.EventType;

namespace DataPersistence
{
    public class DataPersistenceManager : MonoBehaviour
    {
        [Header("Debugging")]
        [SerializeField] private bool   disableDataPersistence    = false;
        [SerializeField] private bool   initializeDataIfNull      = false;
        [SerializeField] private bool   overrideSelectedProfileID = false;
        [SerializeField] private string testSelectedProfileID     = "test";

        [Header("File Storage Config")]
        [SerializeField] private string fileName;
        [SerializeField] private bool   useEncryption;

        [Header("Auto Saving Configuration")]
        [SerializeField] private float autoSaveTimeSeconds = 60f;

        public ToolData toolData;
        private List<IDataPersistence> dataPersistenceObjects = new List<IDataPersistence>();
        private FileDataHandler dataHandler;

        private string selectedProfileID = "";

        private Coroutine autoSaveCoroutine;

        public static DataPersistenceManager instance { get; private set; }

        private void Awake() 
        {
            #region Singleton
            if (instance != null) 
            {
                Debug.Log("Found more than one Data Persistence Manager in the scene. Destroying the newest one.");
                Destroy(this.gameObject);
                return;
            }
            instance = this;
            DontDestroyOnLoad(this.gameObject);
            #endregion Singleton

            if (disableDataPersistence) 
            {
                Debug.LogWarning("Data Persistence is currently disabled!");
            }

            this.dataHandler = new FileDataHandler(Application.persistentDataPath, fileName, useEncryption);

            InitializeSelectedProfileId();
        }

        #region Sub/Unsub
        //Subscribe 
        private void OnEnable() 
        {
            SceneManager.sceneLoaded += OnSceneLoaded;
        }

        //Unsubscribe
        private void OnDisable() 
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
        }
        #endregion Sub/Unsub

        public void OnSceneLoaded(Scene _scene, LoadSceneMode _mode) 
        {
            this.dataPersistenceObjects = FindAllDataPersistenceObjects();
            LoadProject();

            // start up the auto saving coroutine
            if (autoSaveCoroutine != null) 
            {
                StopCoroutine(autoSaveCoroutine);
            }
            autoSaveCoroutine = StartCoroutine(AutoSave());
        }

        #region Profile Change/Delete/Initialize
        public void ChangeSelectedProfileId(string _newProfileID) 
        {
            // update the profile to use for saving and loading
            this.selectedProfileID = _newProfileID;
            // load the Tool, which will use that profile, updating our Tool data accordingly
            LoadProject();
        }
        public void DeleteProfileData(string _profileID) 
        {
            // delete the data for this profile id
            dataHandler.Delete(_profileID);
            // initialize the selected profile id
            InitializeSelectedProfileId();
            // reload the Tool so that our data matches the newly selected profile id
            LoadProject();
        }

        private void InitializeSelectedProfileId() 
        {
            this.selectedProfileID = dataHandler.GetMostRecentlyUpdatedProfileID();
            if (overrideSelectedProfileID) 
            {
                this.selectedProfileID = testSelectedProfileID;
                Debug.LogWarning("Overrode selected profile id with test id: " + testSelectedProfileID);
            }
            LoadProject();
        }
        #endregion Profile Change/Delete/Initialize

        public void NewTool() 
        {
            this.toolData = new ToolData();
        }

        public void LoadProject()
        {
            // return right away if data persistence is disabled
            if (disableDataPersistence) 
            {
                return;
            }

            // load any saved data from a file using the data handler
            this.toolData = dataHandler.Load(selectedProfileID);

            // start a new tool if the data is null and we're configured to initialize data for debugging purposes
            if (this.toolData == null && initializeDataIfNull) 
            {
                NewTool();
            }

            // if no data can be loaded, don't continue
            if (this.toolData == null) 
            {
                Debug.Log("No data was found. A New Tool needs to be started before data can be loaded.");
                return;
            }

            // push the loaded data to all other scripts that need it
            foreach (IDataPersistence dataPersistenceObj in dataPersistenceObjects) 
            {
                dataPersistenceObj.LoadData(toolData);
            }
        }

        public void SaveTool()
        {
            // return right away if data persistence is disabled
            if (disableDataPersistence) 
            {
                return;
            }

            // if we don't have any data to save, log a warning here
            if (this.toolData == null) 
            {
                Debug.LogWarning("No data was found. A New tool needs to be started before data can be saved.");
                return;
            }

            // pass the data to other scripts so they can update it
            foreach (IDataPersistence dataPersistenceObj in dataPersistenceObjects) 
            {
                dataPersistenceObj.SaveData(toolData);
            }

            // timestamp the data so we know when it was last saved
            toolData.lastUpdated = System.DateTime.Now.ToBinary();

            // save that data to a file using the data handler
            dataHandler.Save(toolData, selectedProfileID);
        }


        private List<IDataPersistence> FindAllDataPersistenceObjects() 
        {
            // FindObjectsofType takes in an optional boolean to include inactive gameobjects
            IEnumerable<IDataPersistence> dataPersistenceObjects = FindObjectsOfType<MonoBehaviour>(true)
                .OfType<IDataPersistence>();

            return new List<IDataPersistence>(dataPersistenceObjects);
        }

        public bool HasGameData() 
        {
            return toolData != null;
        }

        public bool IsProfileIDTaken(string _newProfileID)
        {
            Dictionary<string, ToolData> saveslots = dataHandler.LoadAllProfiles();
            bool isProfileIDTaken = saveslots.ContainsKey(_newProfileID);
            return isProfileIDTaken;
        }

        public Dictionary<string, ToolData> GetAllProfilesToolData() 
        {
            return dataHandler.LoadAllProfiles();
        }

        private IEnumerator AutoSave() 
        {
            while (true) 
            {
                yield return new WaitForSeconds(autoSaveTimeSeconds);
                SaveTool();
                EventSystem.RaiseEvent(EventType.SAVED);
                Debug.Log("Auto Saved Tool");
            }
        }
        private void OnApplicationQuit() 
        {
            SaveTool();
        }
    }
}
