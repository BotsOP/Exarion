using UnityEngine;

namespace Crosstales.FB.Demo.Util
{
   /// <summary>Controls the custom wrapper in demo builds.</summary>
   [HelpURL("https://www.crosstales.com/media/data/assets/FileBrowser/api/class_crosstales_1_1_f_b_1_1_demo_1_1_util_1_1_custom_wrapper_controller.html")]
   public class CustomWrapperController : MonoBehaviour
   {
      #region Variables

      public Crosstales.FB.Wrapper.BaseCustomFileBrowser Wrapper;

      private bool isCustom;
      private Crosstales.FB.Wrapper.BaseCustomFileBrowser previousWrapper;

      #endregion


      #region MonoBehaviour methods

      private void Start()
      {
         isCustom = FileBrowser.Instance.CustomMode;
         previousWrapper = FileBrowser.Instance.CustomWrapper;

         FileBrowser.Instance.CustomWrapper = Wrapper;
         FileBrowser.Instance.CustomMode = true;
      }

      private void OnDestroy()
      {
         if (FileBrowser.Instance != null)
         {
            FileBrowser.Instance.CustomMode = isCustom;
            FileBrowser.Instance.CustomWrapper = previousWrapper;
         }
      }

      #endregion
   }
}
// © 2020-2023 crosstales LLC (https://www.crosstales.com)