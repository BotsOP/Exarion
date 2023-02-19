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

        VisualElement timelineClip1 = root.Q("TimelineClip1");
        timelineClip1.AddManipulator(new TimelineClip());
        
        VisualElement timelineClip2 = root.Q("TimelineClip2");
        timelineClip2.AddManipulator(new TimelineClip());
        
        VisualElement timelineClip3 = root.Q("TimelineClip3");
        timelineClip3.AddManipulator(new TimelineClip());
        
        VisualElement timelineClip4 = root.Q("TimelineClip4");
        timelineClip4.AddManipulator(new TimelineClip());
        
        VisualElement timelineClip5 = root.Q("TimelineClip5");
        timelineClip5.AddManipulator(new TimelineClip());
        
        VisualElement timelineClip6 = root.Q("TimelineClip6");
        timelineClip6.AddManipulator(new TimelineClip());
        
        ScrollView timeline = root.Q<ScrollView>("TimeLineScrollView");
    }
}
