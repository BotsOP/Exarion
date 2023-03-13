using System;
using System.Collections;
using System.Collections.Generic;
using Drawing;
using UnityEngine;
using UnityEngine.UI;

public class UIManager : MonoBehaviour
{
    [SerializeField] private StrokeManager strokeManager;
    [SerializeField] private RawImage paintBoard;
    [SerializeField] private RawImage displayBoard;

    private void Start()
    {
        paintBoard.texture = strokeManager.drawer.rt;
        //displayBoard.texture = strokeManager.drawer.rt;
    }
}
