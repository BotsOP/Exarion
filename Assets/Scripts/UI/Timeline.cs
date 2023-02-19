using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

public class Timeline : MonoBehaviour
{
    private void OnEnable()
    {
        VisualElement root = GetComponent<UIDocument>().rootVisualElement;

        VisualElement timelineClip = root.Q("TimelineClip1");
        timelineClip.AddManipulator(new TimelineClip());
    }
}
