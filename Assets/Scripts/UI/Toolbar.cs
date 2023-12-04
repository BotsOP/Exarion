using System;
using System.Collections.Generic;
using Managers;
using TMPro;
using UnityEngine;
using EventType = Managers.EventType;

namespace UI
{
    public enum ToolType
    {
        brush,
        select,
        move,
        rotate,
        resize,
        Polygon,
        Circle,
        Square,
        Line,
    }
    public class Toolbar : MonoBehaviour
    {
        [SerializeField] private GameObject currentToolSettings;
        [SerializeField] private List<ToolButton> buttons;
        [SerializeField] private Color selectedColor;
        [SerializeField] private Color notSelectedColor;
        [SerializeField] private bool use3D;
        private ToolType lastTool = ToolType.brush;
        private ToolType currentTool = ToolType.brush;

        private void Update()
        {
            //move
            if (Input.GetKeyDown(KeyCode.W))
            {
                lastTool = currentTool;
                currentTool = ToolType.move;
                buttons[(int)currentTool].backgroundImage.color = selectedColor;
                buttons[(int)lastTool].backgroundImage.color = notSelectedColor;
                EventSystem<ToolType>.RaiseEvent(EventType.CHANGE_TOOLTYPE, ToolType.move);
            }
            else if (Input.GetKeyUp(KeyCode.W))
            {
                EventSystem<ToolType>.RaiseEvent(EventType.CHANGE_TOOLTYPE, lastTool);
                buttons[(int)currentTool].backgroundImage.color = notSelectedColor;
                buttons[(int)lastTool].backgroundImage.color = selectedColor;
                currentTool = lastTool;
            }
            
            //select
            if (Input.GetKeyDown(KeyCode.S))
            {
                lastTool = currentTool;
                currentTool = ToolType.select;
                buttons[(int)currentTool].backgroundImage.color = selectedColor;
                buttons[(int)lastTool].backgroundImage.color = notSelectedColor;
                EventSystem<ToolType>.RaiseEvent(EventType.CHANGE_TOOLTYPE, ToolType.select);
            }
            else if (Input.GetKeyUp(KeyCode.S))
            {
                EventSystem<ToolType>.RaiseEvent(EventType.CHANGE_TOOLTYPE, lastTool);
                buttons[(int)currentTool].backgroundImage.color = notSelectedColor;
                buttons[(int)lastTool].backgroundImage.color = selectedColor;
                currentTool = lastTool;
            }

            if (!use3D)
            {
                //rotate
                if (Input.GetKeyDown(KeyCode.E))
                {
                    lastTool = currentTool;
                    currentTool = ToolType.rotate;
                    buttons[(int)currentTool].backgroundImage.color = selectedColor;
                    buttons[(int)lastTool].backgroundImage.color = notSelectedColor;
                    EventSystem<ToolType>.RaiseEvent(EventType.CHANGE_TOOLTYPE, ToolType.rotate);
                }
                else if (Input.GetKeyUp(KeyCode.E))
                {
                    EventSystem<ToolType>.RaiseEvent(EventType.CHANGE_TOOLTYPE, lastTool);
                    buttons[(int)currentTool].backgroundImage.color = notSelectedColor;
                    buttons[(int)lastTool].backgroundImage.color = selectedColor;
                    currentTool = lastTool;
                }
                
                //resize
                if (Input.GetKeyDown(KeyCode.R))
                {
                    lastTool = currentTool;
                    currentTool = ToolType.resize;
                    buttons[(int)currentTool].backgroundImage.color = selectedColor;
                    buttons[(int)lastTool].backgroundImage.color = notSelectedColor;
                    EventSystem<ToolType>.RaiseEvent(EventType.CHANGE_TOOLTYPE, ToolType.resize);
                }
                else if (Input.GetKeyUp(KeyCode.R))
                {
                    EventSystem<ToolType>.RaiseEvent(EventType.CHANGE_TOOLTYPE, lastTool);
                    buttons[(int)currentTool].backgroundImage.color = notSelectedColor;
                    buttons[(int)lastTool].backgroundImage.color = selectedColor;
                    currentTool = lastTool;
                }
                
                //brush
                if (Input.GetKeyDown(KeyCode.B))
                {
                    lastTool = currentTool;
                    currentTool = ToolType.brush;
                    buttons[(int)currentTool].backgroundImage.color = selectedColor;
                    buttons[(int)lastTool].backgroundImage.color = notSelectedColor;
                    EventSystem<ToolType>.RaiseEvent(EventType.CHANGE_TOOLTYPE, ToolType.brush);
                }
                else if (Input.GetKeyUp(KeyCode.B))
                {
                    EventSystem<ToolType>.RaiseEvent(EventType.CHANGE_TOOLTYPE, lastTool);
                    buttons[(int)currentTool].backgroundImage.color = notSelectedColor;
                    buttons[(int)lastTool].backgroundImage.color = selectedColor;
                    currentTool = lastTool;
                }
                
                //polygon
                if (Input.GetKeyDown(KeyCode.F))
                {
                    lastTool = currentTool;
                    currentTool = ToolType.Polygon;
                    buttons[(int)currentTool].backgroundImage.color = selectedColor;
                    buttons[(int)lastTool].backgroundImage.color = notSelectedColor;
                    EventSystem<ToolType>.RaiseEvent(EventType.CHANGE_TOOLTYPE, currentTool);
                }
                else if (Input.GetKeyUp(KeyCode.F))
                {
                    EventSystem<ToolType>.RaiseEvent(EventType.CHANGE_TOOLTYPE, lastTool);
                    buttons[(int)currentTool].backgroundImage.color = notSelectedColor;
                    buttons[(int)lastTool].backgroundImage.color = selectedColor;
                    currentTool = lastTool;
                }
                
                //line
                if (Input.GetKeyDown(KeyCode.D))
                {
                    lastTool = currentTool;
                    currentTool = ToolType.Line;
                    buttons[(int)currentTool].backgroundImage.color = selectedColor;
                    buttons[(int)lastTool].backgroundImage.color = notSelectedColor;
                    EventSystem<ToolType>.RaiseEvent(EventType.CHANGE_TOOLTYPE, currentTool);
                }
                else if (Input.GetKeyUp(KeyCode.D))
                {
                    EventSystem<ToolType>.RaiseEvent(EventType.CHANGE_TOOLTYPE, lastTool);
                    buttons[(int)currentTool].backgroundImage.color = notSelectedColor;
                    buttons[(int)lastTool].backgroundImage.color = selectedColor;
                    currentTool = lastTool;
                }
                
                //Square
                if (Input.GetKeyDown(KeyCode.G))
                {
                    lastTool = currentTool;
                    currentTool = ToolType.Square;
                    buttons[(int)currentTool].backgroundImage.color = selectedColor;
                    buttons[(int)lastTool].backgroundImage.color = notSelectedColor;
                    EventSystem<ToolType>.RaiseEvent(EventType.CHANGE_TOOLTYPE, currentTool);
                }
                else if (Input.GetKeyUp(KeyCode.G))
                {
                    EventSystem<ToolType>.RaiseEvent(EventType.CHANGE_TOOLTYPE, lastTool);
                    buttons[(int)currentTool].backgroundImage.color = notSelectedColor;
                    buttons[(int)lastTool].backgroundImage.color = selectedColor;
                    currentTool = lastTool;
                }
                
                //circle
                if (Input.GetKeyDown(KeyCode.V))
                {
                    lastTool = currentTool;
                    currentTool = ToolType.Circle;
                    buttons[(int)currentTool].backgroundImage.color = selectedColor;
                    buttons[(int)lastTool].backgroundImage.color = notSelectedColor;
                    EventSystem<ToolType>.RaiseEvent(EventType.CHANGE_TOOLTYPE, currentTool);
                }
                else if (Input.GetKeyUp(KeyCode.V))
                {
                    EventSystem<ToolType>.RaiseEvent(EventType.CHANGE_TOOLTYPE, lastTool);
                    buttons[(int)currentTool].backgroundImage.color = notSelectedColor;
                    buttons[(int)lastTool].backgroundImage.color = selectedColor;
                    currentTool = lastTool;
                }
            }
        }
        public void OnClick(ToolButton _toolButton)
        {
            currentTool = _toolButton.toolType;
            buttons[(int)currentTool].backgroundImage.color = selectedColor;
            buttons[(int)lastTool].backgroundImage.color = notSelectedColor;
            lastTool = _toolButton.toolType;
            
            currentToolSettings.SetActive(false);
            currentToolSettings = _toolButton.settingsObject;
            currentToolSettings.SetActive(true);

            EventSystem<ToolType>.RaiseEvent(EventType.CHANGE_TOOLTYPE, _toolButton.toolType);
        }

        public void ChangedShapeSides(TMP_InputField _input)
        {
            EventSystem<int>.RaiseEvent(EventType.CHANGE_TOOLTYPE, int.Parse(_input.text));
        }
    }
}
