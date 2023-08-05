using UnityEngine;

namespace Managers
{
    public static class ExtensionMethods 
    {
        public static float Remap (this float value, float from1, float to1, float from2, float to2) 
        {
            return (value - from1) / (to1 - from1) * (to2 - from2) + from2;
        }
        public static float Remap (this int value, float from1, float to1, float from2, float to2) 
        {
            return (value - from1) / (to1 - from1) * (to2 - from2) + from2;
        }

        public static void Clear(this CustomRenderTexture _rt, bool _clearDepth, bool _clearColor, Color _color)
        {
            Graphics.SetRenderTarget(_rt);
            GL.Clear(_clearDepth, _clearColor, _color);
            Graphics.SetRenderTarget(null);
        }

        public static Texture2D ToTexture2D(this CustomRenderTexture _rt)
        {
            //var tempRT = RenderTexture.GetTemporary(_rt.width, _rt.height, 0, _rt.format);
            var tex = new Texture2D(_rt.width, _rt.height);
            var tmp = RenderTexture.active;
            RenderTexture.active = _rt;
            tex.ReadPixels(new Rect(0, 0, _rt.width, _rt.height), 0, 0);
            tex.Apply();
            RenderTexture.active = tmp;
            return tex;
        }
        
        public static byte[] ToBytesEXR(this RenderTexture _rt)
        {
            var tex = new Texture2D(_rt.width, _rt.height);
            var tmp = RenderTexture.active;
            RenderTexture.active = _rt;
            tex.ReadPixels(new Rect(0, 0, _rt.width, _rt.height), 0, 0);
            tex.Apply();
            RenderTexture.active = tmp;
            byte[] bytes = tex.EncodeToEXR(Texture2D.EXRFlags.OutputAsFloat);
            return bytes;
        }
        
        public static byte[] ToBytesEXR(this CustomRenderTexture _rt, int _width, int _height)
        {
            var tempRT = RenderTexture.GetTemporary(_width, _height, 0, _rt.format);
            var tex = new Texture2D(_width, _height);
            var tmp = RenderTexture.active;
            
            RenderTexture.active = tempRT;
            tex.ReadPixels(new Rect(0, 0, _width, _height), 0, 0);
            tex.Apply();
            RenderTexture.active = tmp;
            byte[] bytes = tex.EncodeToEXR(Texture2D.EXRFlags.OutputAsFloat);
            return bytes;
        }
        
        public static byte[] ToBytesPNG(this RenderTexture _rt)
        {
            var tex = new Texture2D(_rt.width, _rt.height);
            var tmp = RenderTexture.active;
            RenderTexture.active = _rt;
            tex.ReadPixels(new Rect(0, 0, _rt.width, _rt.height), 0, 0);
            tex.Apply();
            RenderTexture.active = tmp;
            byte[] bytes = tex.EncodeToPNG();
            return bytes;
        }
        
        public static byte[] ToBytesPNG(this CustomRenderTexture _rt, int _width, int _height)
        {
            var tempRT = RenderTexture.GetTemporary(_width, _height, 0, _rt.format);
            var tex = new Texture2D(_width, _height);
            var tmp = RenderTexture.active;
            
            RenderTexture.active = tempRT;
            tex.ReadPixels(new Rect(0, 0, _width, _height), 0, 0);
            tex.Apply();
            RenderTexture.active = tmp;
            byte[] bytes = tex.EncodeToPNG();
            return bytes;
        }
    }
}

