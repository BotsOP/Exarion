using UnityEngine;
using UnityEngine.UI;
using Crosstales.FB;

/// <summary>
/// Simple example to demonstrate the basic usage of File Browser with async-calls.
/// </summary>
public class SimpleAsyncFBExample : MonoBehaviour
{
   public string Extension = "txt";
   public Text Result;

   private void Start()
   {
      Result.text = "No file selected!";
   }

   private void OnEnable()
   {
      FileBrowser.Instance.OnOpenFilesComplete += onOpenFilesComplete;
   }

   private void OnDisable()
   {
      if (FileBrowser.Instance != null)
         FileBrowser.Instance.OnOpenFilesComplete -= onOpenFilesComplete;
   }

   public void OpenFile()
   {
      FileBrowser.Instance.OpenSingleFileAsync(Extension);
   }

   private void onOpenFilesComplete(bool selected, string singlefile, string[] files)
   {
      Result.text = selected ? singlefile : "<color=red>No file selected!</color>";
   }
}
// © 2022-2023 crosstales LLC (https://www.crosstales.com)