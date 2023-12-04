using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SetScreenSize : MonoBehaviour
{
    private const float FLOAT_PRECISION = 0.00001f;
    private float aspectRatio;
    private bool beingResized;
    private Vector2 cachedScreenRes;
    private void OnEnable()
    {
        aspectRatio = 16f / 9f;
        Screen.SetResolution(Screen.width, (int)(Screen.width * aspectRatio), FullScreenMode.Windowed);
        DontDestroyOnLoad(this);
    }

    void Update()
    {
        if (Math.Abs(Screen.width - cachedScreenRes.x) > FLOAT_PRECISION || Math.Abs(Screen.height - cachedScreenRes.y) > FLOAT_PRECISION)
        {
            beingResized = true;
        }
        
        if (Math.Abs((float)Screen.width / Screen.height - aspectRatio) > 0.2f && !beingResized)
        {
            Screen.SetResolution(Screen.width, (int)(Screen.width / aspectRatio), FullScreenMode.Windowed);
        }

        beingResized = false;
        cachedScreenRes = new Vector2(Screen.width, Screen.height);
    }
}
