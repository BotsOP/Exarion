using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Timeline : MonoBehaviour
{
    [SerializeField] private GameObject newTimelineObject;
    [SerializeField] private GameObject newBrushClip;
    private Drawing.Drawing drawer;

    private void OnEnable()
    {
        
    }
    private void OnDisable()
    {
        
    }

    private void AddNewBrushClip()
    {
        //Instantiate new brushClip with brushstrokID linked
    }
}
