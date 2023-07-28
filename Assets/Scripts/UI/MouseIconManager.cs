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
                Cursor.SetCursor(arrow, Vector2.zero, CursorMode.Auto);
                break;
        }
    }
}
