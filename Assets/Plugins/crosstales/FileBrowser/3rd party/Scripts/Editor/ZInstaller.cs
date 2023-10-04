#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;

namespace Crosstales.FB.Addons
{
   /// <summary>Installs the Demos-package.</summary>
   [InitializeOnLoad]
   public abstract class ZInstaller : Crosstales.Common.EditorTask.BaseInstaller
   {
      #region Constructor

      static ZInstaller()
      {
#if !CT_FB_DEMO && !CT_DEVELOP
         string path = $"{Application.dataPath}{Crosstales.FB.EditorUtil.EditorConfig.ASSET_PATH}";

         installPackage(path, "Demos.unitypackage", "CT_FB_DEMO");
#endif
      }

      #endregion
   }
}
#endif
// © 2022-2023 crosstales LLC (https://www.crosstales.com)