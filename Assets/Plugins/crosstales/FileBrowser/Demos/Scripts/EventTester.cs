using UnityEngine;

namespace Crosstales.FB.Demo
{
   /// <summary>Simple test script for all UnityEvent-callbacks.</summary>
   [ExecuteInEditMode]
   [HelpURL("https://www.crosstales.com/media/data/assets/FileBrowser/api/class_crosstales_1_1_f_b_1_1_demo_1_1_event_tester.html")]
   public class EventTester : MonoBehaviour
   {
      public void OnOpenFilesCompleted(bool selected, string singleFile, string listOfFiles)
      {
         Debug.Log($"OnOpenFilesCompleted: {selected} - {singleFile} - {listOfFiles}");

         if (selected)
         {
            string[] files = listOfFiles.Split(';');

            Debug.Log($"All files: {files.CTDump()}");
         }
         else
         {
            Debug.LogWarning("Nothing selected!");
         }
      }

      public void OnOpenFoldersCompleted(bool selected, string singleFolder, string listOfFolders)
      {
         Debug.Log($"OnOpenFoldersCompleted: {selected} - {singleFolder} - {listOfFolders}");

         if (selected)
         {
            string[] folders = listOfFolders.Split(';');

            Debug.Log($"All folders: {folders.CTDump()}");
         }
         else
         {
            Debug.LogWarning("Nothing selected!");
         }
      }

      public void OnSaveFileCompleted(bool selected, string saveFile)
      {
         Debug.Log($"OnSaveFileCompleted: {selected} - {saveFile}");

         if (selected)
         {
            Debug.Log($"Selected save file: {saveFile}");
         }
         else
         {
            Debug.LogWarning("Nothing selected!");
         }
      }
   }
}
// © 2020-2023 crosstales LLC (https://www.crosstales.com)