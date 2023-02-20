using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

public class Timeline : MonoBehaviour
{
    public VisualElement timelineClip;
    private List<VisualElement> clips;

    private void OnEnable()
    {
        VisualElement root = GetComponent<UIDocument>().rootVisualElement;
        

        List<VisualElement> edges = root.Query(name: "TimelineEdge").ToList();

        foreach (var edge in edges)
        {
            edge.AddManipulator(new TimelineClipResizer());
        }
        
        clips = root.Query(className: "red_box").ToList();

        foreach (var clip in clips)
        {
            clip.AddManipulator(new TimelineClipDragger());
        }
        
        Debug.Log($"{clips[0].layout.x} {clips[0].layout.width} | {clips[1].layout.x} {clips[1].layout.width}");
        
        ScrollView timeline = root.Q<ScrollView>("TimeLineScrollView");

        // VisualElement timelineClip1 = root.Q("TimelineClip1");
        // timelineClip1.AddManipulator(new TimelineClipDragger());
        //
        // VisualElement timelineClip2 = root.Q("TimelineClip2");
        // timelineClip2.AddManipulator(new TimelineClipDragger());
        //
        // VisualElement timelineClip3 = root.Q("TimelineClip3");
        // timelineClip3.AddManipulator(new TimelineClipDragger());
        //
        // VisualElement timelineClip4 = root.Q("TimelineClip4");
        // timelineClip4.AddManipulator(new TimelineClipDragger());
        //
        // VisualElement timelineClip5 = root.Q("TimelineClip5");
        // timelineClip5.AddManipulator(new TimelineClipDragger());
        //
        // VisualElement timelineClip6 = root.Q("TimelineClip6");
        // timelineClip6.AddManipulator(new TimelineClipDragger());
        
    }

    private void Update()
    {
        if (clips[0].layout.x + clips[0].layout.width > clips[1].layout.x && clips[1].layout.x + clips[1].layout.width > clips[0].layout.x)
        {
            Debug.Log($"true");
        }
        Debug.Log($"{clips[0].layout.x} {clips[0].layout.width} | {clips[1].layout.x} {clips[1].layout.width}");
    }
}
