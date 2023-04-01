﻿using System.IO;
using UnityEngine;

namespace UI
{
    public class ExportPNG
    {
        public void SaveImageToFile(RenderTexture _rt, string _path)
        {
            // Allocate
            var sRgbRenderTex = RenderTexture.GetTemporary(_rt.width, _rt.height, 0, RenderTextureFormat.ARGB32,
                RenderTextureReadWrite.sRGB);
            var tex = new Texture2D(_rt.width, _rt.height, TextureFormat.ARGB32, mipChain: false, linear: false);
            
            // Linear to Gamma Conversion
            Graphics.Blit(_rt, sRgbRenderTex);
        
            // Copy memory from RenderTexture
            var tmp = RenderTexture.active;
            RenderTexture.active = sRgbRenderTex;
            tex.ReadPixels(new Rect(0, 0, _rt.width, _rt.height), 0, 0);
            tex.Apply();
            RenderTexture.active = tmp;
            
            byte[] bytes = tex.EncodeToPNG();
            string filePath = Application.dataPath + "/Results";
            if (!Directory.Exists(filePath)) Directory.CreateDirectory(filePath);
            filePath = filePath + "/Results" + ".png";
            File.WriteAllBytes(filePath, bytes);
            // Open File in saved location
            filePath = filePath.Replace(@"/", @"\");
            System.Diagnostics.Process.Start("explorer.exe", "/select," + filePath);
        }
    }
}