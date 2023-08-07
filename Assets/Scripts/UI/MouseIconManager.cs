using System;
using System.Collections;
using System.Collections.Generic;
using Managers;
using UnityEngine;
using EventType = Managers.EventType;

public enum MouseIcon
{
    arrow,
    point,
    grab,
    ew,
}
public class MouseIconManager : MonoBehaviour
{
    [SerializeField] private Texture2D arrow;
    [SerializeField] private Texture2D point;
    [SerializeField] private Texture2D grab;
    [SerializeField] private Texture2D ew;

    public Vector2 test;

    private void OnEnable()
    {
        EventSystem<MouseIcon>.Subscribe(EventType.CHANGE_MOUSE_ICON, ChangeMouseIcon);
    }

    private void OnDisable()
    {
        EventSystem<MouseIcon>.Unsubscribe(EventType.CHANGE_MOUSE_ICON, ChangeMouseIcon);
    }

    private void ChangeMouseIcon(MouseIcon _mouseIcon)
    {
        switch (_mouseIcon)
        {
            case MouseIcon.arrow:
                Cursor.SetCursor(arrow, test, CursorMode.Auto);
                break;
            case MouseIcon.point:
                Cursor.SetCursor(point, Vector2.zero, CursorMode.Auto);
                break;
            case MouseIcon.grab:
                Cursor.SetCursor(grab, Vector2.zero, CursorMode.Auto);
                break;
            case MouseIcon.ew:
                Cursor.SetCursor(ew, Vector2.zero, CursorMode.Auto);
                break;
        }
    }
}
