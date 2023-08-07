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
        private TMP_InputField imageWidthInput;
        private TMP_InputField imageHeightInput;

        private ToolData toolData;

        public void ResetprojectExistsText()
        {
            projectExistsText.SetActive(false);
        }

        public void OnCreate()
        {
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

            toolData = new ToolData
            {
                lastUpdated = System.DateTime.Now.ToBinary(),
                projectName = projectName.text,
                imageWidth = imageWidth,
                imageHeight = imageHeight,
            };

            DataPersistenceManager.instance.ChangeSelectedProfileId(projectName.text);
            DataPersistenceManager.instance.toolData = toolData;
            DataPersistenceManager.instance.SaveTool();
            SceneManager.LoadSceneAsync("DrawScene");
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
