using UnityEngine;

public enum AnchorPresets
{
    TopLeft,
    TopCenter,
    TopRight,

    MiddleLeft,
    MiddleCenter,
    MiddleRight,

    BottomLeft,
    BottonCenter,
    BottomRight,
    BottomStretch,

    VertStretchLeft,
    VertStretchRight,
    VertStretchCenter,

    HorStretchTop,
    HorStretchMiddle,
    HorStretchBottom,

    StretchAll
}

public enum PivotPresets
{
    TopLeft,
    TopCenter,
    TopRight,

    MiddleLeft,
    MiddleCenter,
    MiddleRight,

    BottomLeft,
    BottomCenter,
    BottomRight,
}

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
            var tempRT = RenderTexture.GetTemporary(_width, _height, 0, RenderTextureFormat.ARGB32);
            Graphics.Blit(_rt, tempRT);
            var tex = new Texture2D(_width, _height);
            tex.filterMode = FilterMode.Point;
            var tmp = RenderTexture.active;
            
            RenderTexture.active = tempRT;
            tex.ReadPixels(new Rect(0, 0, _width, _height), 0, 0);
            tex.Apply();
            RenderTexture.active = tmp;
            byte[] bytes = tex.EncodeToPNG();
            return bytes;
        }
        
        public static void SetAnchor(this RectTransform source, AnchorPresets allign, int offsetX=0, int offsetY=0)
        {
            source.anchoredPosition = new Vector3(offsetX, offsetY, 0);

            switch (allign)
            {
                case(AnchorPresets.TopLeft):
                {
                    source.anchorMin = new Vector2(0, 1);
                    source.anchorMax = new Vector2(0, 1);
                    break;
                }
                case (AnchorPresets.TopCenter):
                {
                    source.anchorMin = new Vector2(0.5f, 1);
                    source.anchorMax = new Vector2(0.5f, 1);
                    break;
                }
                case (AnchorPresets.TopRight):
                {
                    source.anchorMin = new Vector2(1, 1);
                    source.anchorMax = new Vector2(1, 1);
                    break;
                }

                case (AnchorPresets.MiddleLeft):
                {
                    source.anchorMin = new Vector2(0, 0.5f);
                    source.anchorMax = new Vector2(0, 0.5f);
                    break;
                }
                case (AnchorPresets.MiddleCenter):
                {
                    source.anchorMin = new Vector2(0.5f, 0.5f);
                    source.anchorMax = new Vector2(0.5f, 0.5f);
                    break;
                }
                case (AnchorPresets.MiddleRight):
                {
                    source.anchorMin = new Vector2(1, 0.5f);
                    source.anchorMax = new Vector2(1, 0.5f);
                    break;
                }

                case (AnchorPresets.BottomLeft):
                {
                    source.anchorMin = new Vector2(0, 0);
                    source.anchorMax = new Vector2(0, 0);
                    break;
                }
                case (AnchorPresets.BottonCenter):
                {
                    source.anchorMin = new Vector2(0.5f, 0);
                    source.anchorMax = new Vector2(0.5f,0);
                    break;
                }
                case (AnchorPresets.BottomRight):
                {
                    source.anchorMin = new Vector2(1, 0);
                    source.anchorMax = new Vector2(1, 0);
                    break;
                }

                case (AnchorPresets.HorStretchTop):
                {
                    source.anchorMin = new Vector2(0, 1);
                    source.anchorMax = new Vector2(1, 1);
                    break;
                }
                case (AnchorPresets.HorStretchMiddle):
                {
                    source.anchorMin = new Vector2(0, 0.5f);
                    source.anchorMax = new Vector2(1, 0.5f);
                    break;
                }
                case (AnchorPresets.HorStretchBottom):
                {
                    source.anchorMin = new Vector2(0, 0);
                    source.anchorMax = new Vector2(1, 0);
                    break;
                }

                case (AnchorPresets.VertStretchLeft):
                {
                    source.anchorMin = new Vector2(0, 0);
                    source.anchorMax = new Vector2(0, 1);
                    break;
                }
                case (AnchorPresets.VertStretchCenter):
                {
                    source.anchorMin = new Vector2(0.5f, 0);
                    source.anchorMax = new Vector2(0.5f, 1);
                    break;
                }
                case (AnchorPresets.VertStretchRight):
                {
                    source.anchorMin = new Vector2(1, 0);
                    source.anchorMax = new Vector2(1, 1);
                    break;
                }

                case (AnchorPresets.StretchAll):
                {
                    source.anchorMin = new Vector2(0, 0);
                    source.anchorMax = new Vector2(1, 1);
                    break;
                }
            }
        }

        public static void SetPivot(this RectTransform source, PivotPresets preset)
        {

            switch (preset)
            {
                case (PivotPresets.TopLeft):
                {
                    source.pivot = new Vector2(0, 1);
                    break;
                }
                case (PivotPresets.TopCenter):
                {
                    source.pivot = new Vector2(0.5f, 1);
                    break;
                }
                case (PivotPresets.TopRight):
                {
                    source.pivot = new Vector2(1, 1);
                    break;
                }

                case (PivotPresets.MiddleLeft):
                {
                    source.pivot = new Vector2(0, 0.5f);
                    break;
                }
                case (PivotPresets.MiddleCenter):
                {
                    source.pivot = new Vector2(0.5f, 0.5f);
                    break;
                }
                case (PivotPresets.MiddleRight):
                {
                    source.pivot = new Vector2(1, 0.5f);
                    break;
                }

                case (PivotPresets.BottomLeft):
                {
                    source.pivot = new Vector2(0, 0);
                    break;
                }
                case (PivotPresets.BottomCenter):
                {
                    source.pivot = new Vector2(0.5f, 0);
                    break;
                }
                case (PivotPresets.BottomRight):
                {
                    source.pivot = new Vector2(1, 0);
                    break;
                }
            }
        }
    }
}

