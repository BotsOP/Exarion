using System;
using System.Collections;
using System.Collections.Generic;
using Drawing;
using UnityEngine;
using UnityEngine.UI;

public class UIManager : MonoBehaviour
{
    public bool isFullView;
    public RawImage paintBoard;
    public RawImage paintBoard2;
    [SerializeField] private StrokeManager strokeManager;
    [SerializeField] private Color selectedColor;
    [SerializeField] private Color backgroundColor;
    [SerializeField] private GameObject fullView;
    [SerializeField] private GameObject focusView;
    [SerializeField] private Image cachedButton;

    private void Start()
    {
        paintBoard.texture = strokeManager.drawer.rt;
        paintBoard2.texture = strokeManager.drawer.rt;
    }

    public void SwitchToFullView(Image buttonImage)
    {
        isFullView = true;
        buttonImage.GetComponent<Image>().color = selectedColor;
        if(cachedButton) { cachedButton.GetComponent<Image>().color = backgroundColor; }
        cachedButton = buttonImage;
        
        fullView.SetActive(true);
        focusView.SetActive(false);
    }
    
    public void SwitchToFocusView(Image buttonImage)
    {
        isFullView = false;
        buttonImage.GetComponent<Image>().color = selectedColor;
        if(cachedButton) { cachedButton.GetComponent<Image>().color = backgroundColor; }
        cachedButton = buttonImage;
        
        fullView.SetActive(false);
        focusView.SetActive(true);
    }
}
