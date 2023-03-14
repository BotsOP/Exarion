using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum EventType
{
    DRAW,
    STOPPED_DRAWING,
    REDRAW_STROKE,
    CHANGE_BRUSH_SIZE
}


public static class EventSystem
{
    private static Dictionary<EventType, System.Action> eventRegister = new Dictionary<EventType, System.Action>();

    public static void Subscribe(EventType _evt, System.Action _func)
    {
        if (!eventRegister.ContainsKey(_evt))
        {
            eventRegister.Add(_evt, null);
        }

        eventRegister[_evt] += _func;
    }

    public static void Unsubscribe(EventType _evt, System.Action _func)
    {
        if (eventRegister.ContainsKey(_evt))
        {
            eventRegister[_evt] -= _func;
        }
    }

    public static void RaiseEvent(EventType _evt)
    {
        eventRegister[_evt]?.Invoke();
    }
}

public static class EventSystem<T>
{
    private static Dictionary<EventType, System.Action<T>> eventRegister = new Dictionary<EventType, System.Action<T>>();

    public static void Subscribe(EventType _evt, System.Action<T> _func)
    {
        if (!eventRegister.ContainsKey(_evt))
        {
            eventRegister.Add(_evt, null);
        }

        eventRegister[_evt] += _func;
    }

    public static void Unsubscribe(EventType _evt, System.Action<T> _func)
    {
        if (eventRegister.ContainsKey(_evt))
        {
            eventRegister[_evt] -= _func;
        }
    }

    public static void RaiseEvent(EventType _evt, T _arg)
    {
        eventRegister[_evt]?.Invoke(_arg);
    }
}

public static class EventSystem<T, A>
{
    private static Dictionary<EventType, System.Action<T, A>> eventRegister = new Dictionary<EventType, System.Action<T, A>>();

    public static void Subscribe(EventType _evt, System.Action<T, A> _func)
    {
        if (!eventRegister.ContainsKey(_evt))
        {
            eventRegister.Add(_evt, null);
        }

        eventRegister[_evt] += _func;
    }

    public static void Unsubscribe(EventType _evt, System.Action<T, A> _func)
    {
        if (eventRegister.ContainsKey(_evt))
        {
            eventRegister[_evt] -= _func;
        }
    }

    public static void RaiseEvent(EventType _evt, T _arg, A _arg1)
    {
        eventRegister[_evt]?.Invoke(_arg, _arg1);
    }
}

public static class EventSystem<T, A, B>
{
    private static Dictionary<EventType, System.Action<T, A, B>> eventRegister = new Dictionary<EventType, System.Action<T, A, B>>();

    public static void Subscribe(EventType _evt, System.Action<T, A, B> _func)
    {
        if (!eventRegister.ContainsKey(_evt))
        {
            eventRegister.Add(_evt, null);
        }

        eventRegister[_evt] += _func;
    }

    public static void Unsubscribe(EventType _evt, System.Action<T, A, B> _func)
    {
        if (eventRegister.ContainsKey(_evt))
        {
            eventRegister[_evt] -= _func;
        }
    }

    public static void RaiseEvent(EventType _evt, T _arg, A _arg1, B _arg2)
    {
        eventRegister[_evt]?.Invoke(_arg, _arg1, _arg2);
    }
}