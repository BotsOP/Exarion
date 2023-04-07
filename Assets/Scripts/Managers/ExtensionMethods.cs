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
    }
}

