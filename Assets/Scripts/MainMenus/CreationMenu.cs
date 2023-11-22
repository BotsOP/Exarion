using System;
using System.Collections;
using System.IO;
using System.Text;
using Crosstales.FB;
using DataPersistence;
using DataPersistence.Data;
using Dummiesman;
using TMPro;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.SceneManagement;
using UnityEngine.Serialization;

namespace MainMenus
{
    public class CreationMenu : MonoBehaviour
    {
        [Header("Menu")]
        [SerializeField] private TMP_Text creationErrorMsg;
        [SerializeField] private TMP_Text meshNameText;

        [Header("Input")]
        [SerializeField] private TMP_InputField projectName;
        [SerializeField] private TMP_InputField imageWidthInput;
        [SerializeField] private TMP_InputField imageHeightInput;
        
        private string[] extensions = { "obj" };

        private Mesh mesh;
        private bool project3D;

        private ToolData toolData;
        private ToolMetaData metaData;

        public void ResetErrorMsg()
        {
            creationErrorMsg.gameObject.SetActive(false);
        }

        public void SetError(string _errorMsg)
        {
            creationErrorMsg.text = _errorMsg;
            creationErrorMsg.gameObject.SetActive(true);
        }

        public void OnCreate()
        {
            projectName.text = projectName.text == "" ? "My First Project" : projectName.text;
            bool isProjectNameTaken = DataPersistenceManager.instance.IsProfileIDTaken(projectName.text);

            if (isProjectNameTaken)
            {
                SetError("Project name already exist");
                return;
            }

            int imageWidth = 2048;
            int imageHeight = 2048;

            bool success = int.TryParse(imageWidthInput.text, out int num);
            num = Mathf.Clamp(num, 1, 16384);
            if (success) { imageWidth = num; }
            success = int.TryParse(imageHeightInput.text, out num);
            num = Mathf.Clamp(num, 1, 16384);
            if (success) { imageHeight = num; }

            metaData = new ToolMetaData(System.DateTime.Now.ToBinary(), projectName.text);

            if (project3D)
            {
                if (mesh == null)
                {
                    SetError("Add a mesh");
                    return;
                }
                toolData = new ToolData3D
                {
                    imageWidth = imageWidth,
                    imageHeight = imageHeight,
                };
                ToolData3D temp = (ToolData3D)toolData;
                temp.SaveMesh(mesh);
                metaData.projectType = ProjectType.PROJECT3D;
            }
            else
            {
                toolData = new ToolData2D
                {
                    imageWidth = imageWidth,
                    imageHeight = imageHeight,
                };
                metaData.projectType = ProjectType.PROJECT2D;
            }
            
            DataPersistenceManager.instance.InitializeNewTool(projectName.text, metaData, toolData);
            DataPersistenceManager.instance.SaveTool();
            SceneManager.LoadSceneAsync(project3D ? "3DDrawScene" : "2DDrawScene");
        }

        public void ImportMesh()
        {
            String path = FileBrowser.Instance.OpenSingleFile("Open file", "", "", extensions);
            StartCoroutine(OutputRoutineOpen(path));
            meshNameText.text = path;
            
        }
        
        private IEnumerator OutputRoutineOpen(string url)
        {
            UnityWebRequest www = UnityWebRequest.Get(url);
            yield return www.SendWebRequest();
            if (www.result != UnityWebRequest.Result.Success)
            {
                Debug.Log("WWW ERROR: " + www.error);
            }
            else
            {
                //Load OBJ Model
                MemoryStream textStream = new MemoryStream(Encoding.UTF8.GetBytes(www.downloadHandler.text));

                GameObject modelHolder = new OBJLoader().Load(textStream);
                modelHolder.transform.position = new Vector3(100, 0, 0);
                GameObject meshOb = modelHolder.transform.GetChild(0).gameObject;
                mesh = meshOb.GetComponent<MeshFilter>().sharedMesh;
            }
        }

        public void SetProjectType(bool _project3D)
        {
            project3D = _project3D;
        }

        public void ActiveGameObject(GameObject _gameObject)
        {
            _gameObject.SetActive(true);
        }
        public void DeActiveGameObject(GameObject _gameObject)
        {
            _gameObject.SetActive(false);
        }

        public void ActivateMenu()
        {
            gameObject.SetActive(true);
        }

        public void DeactivateMenu()
        {
            gameObject.SetActive(false);
        }
    }
}
