using DataPersistence;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace MainMenus
{
    public class CreationMenu : MonoBehaviour
    {
        [Header("Menu")]
        [SerializeField] 
        private GameObject projectExistsText;

        [Header("Input")]
        [SerializeField]
        private TMP_InputField projectName;
        [SerializeField]
        private TMP_InputField imageWidthInput;
        [SerializeField]
        private TMP_InputField imageHeightInput;

        private bool project3D;

        private ToolData toolData;

        public void ResetprojectExistsText()
        {
            projectExistsText.SetActive(false);
        }

        public void OnCreate()
        {
            projectName.text = projectName.text == "" ? "My First Project" : projectName.text;
            bool isProjectNameTaken = DataPersistenceManager.instance.IsProfileIDTaken(projectName.text);

            if (isProjectNameTaken)
            {
                projectExistsText.SetActive(true);
                return;
            }

            int imageWidth = 2048;
            int imageHeight = 2048;

            bool success = int.TryParse(imageWidthInput.text, out int num);
            if (success) { imageWidth = num; }
            success = int.TryParse(imageHeightInput.text, out num);
            if (success) { imageHeight = num; }

            if (project3D)
            {
                toolData = new ToolData3D
                {
                    lastUpdated = System.DateTime.Now.ToBinary(),
                    projectName = projectName.text,
                    imageWidth = imageWidth,
                    imageHeight = imageHeight,
                };
                toolData.projectType = ProjectType.PROJECT3D;
            }
            else
            {
                toolData = new ToolData2D
                {
                    lastUpdated = System.DateTime.Now.ToBinary(),
                    projectName = projectName.text,
                    imageWidth = imageWidth,
                    imageHeight = imageHeight,
                };
                toolData.projectType = ProjectType.PROJECT2D;
            }
            
            DataPersistenceManager.instance.ChangeSelectedProfileId(projectName.text);
            DataPersistenceManager.instance.toolData = toolData;
            DataPersistenceManager.instance.SaveTool();
            SceneManager.LoadSceneAsync(project3D ? "3DDrawScene" : "2DDrawScene");
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
