using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Simple example to demonstrate the basic usage of File Browser.
/// </summary>
public class SimpleFBExample : MonoBehaviour
{
   public string Extension = "txt";
   public Text Result;

   private void Start()
   {
      Result.text = "No file selected!";
   }

   public void OpenFile()
   {
      string file = Crosstales.FB.FileBrowser.Instance.OpenSingleFile(Extension);
      Result.text = string.IsNullOrEmpty(file) ? "<color=red>No file selected!</color>" : file;
   }
}
// © 2022-2023 crosstales LLC (https://www.crosstales.com)