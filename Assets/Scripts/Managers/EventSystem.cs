using System.Collections.Generic;

namespace Managers
{
    public enum EventType
    {
        DRAW,
        FINISHED_STROKE,
        TIME,
        ADD_TIME,
        RESET_TIME,
        SET_BRUSH_SIZE,
        STOPPED_SETTING_BRUSH_SIZE,
        REDRAW_STROKE,
        REDRAW_STROKES,
        REDRAW_ALL,
        REMOVE_STROKE,
        MOVE_STROKE,
        RESIZE_STROKE,
        ROTATE_STROKE,
        UPDATE_CLIP,
        VIEW_CHANGED,
        SPAWN_STAMP,
        SELECT_BRUSHSTROKE,
        SELECT_TIMELINECLIP,
        ADD_SELECT,
        REMOVE_SELECT,
        CLEAR_SELECT,
        ADD_STROKE,
        IS_INTERACTING,
        ADD_COMMAND,
        DUPLICATE_STROKE,
        GROUP_CLIPS,
        UNGROUP_CLIPS,
        CHANGE_MOUSE_ICON,
        CHANGE_TOOLTYPE,
        CHANGE_PAINTTYPE,
        RESIZE_TIMELINE,
        UPDATE_CLIP_INFO,
        CHANGE_DRAW_ORDER,
        CHANGE_BRUSH_SIZE,
        TIMELINE_PAUSE,
        SAVED,
        CHANGED_MODEL,
        IMPORT_MODEL_TEXTURE,
        UPDATE_SUBMESH_COUNT,
    }


    public static class EventSystem
    {
        private static Dictionary<EventType, System.Action> eventRegister = new();

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

        public static void RaiseEvent(EventType _evt, T _arg1, A _arg2)
        {
            eventRegister[_evt]?.Invoke(_arg1, _arg2);
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

        public static void RaiseEvent(EventType _evt, T _arg1, A _arg2, B _arg3)
        {
            eventRegister[_evt]?.Invoke(_arg1, _arg2, _arg3);
        }
    }

    public static class EventSystem<T, A, B, C>
    {
        private static Dictionary<EventType, System.Action<T, A, B, C>> eventRegister = new Dictionary<EventType, System.Action<T, A, B, C>>();

        public static void Subscribe(EventType _evt, System.Action<T, A, B, C> _func)
        {
            if (!eventRegister.ContainsKey(_evt))
            {
                eventRegister.Add(_evt, null);
            }

            eventRegister[_evt] += _func;
        }

        public static void Unsubscribe(EventType _evt, System.Action<T, A, B, C> _func)
        {
            if (eventRegister.ContainsKey(_evt))
            {
                eventRegister[_evt] -= _func;
            }
        }

        public static void RaiseEvent(EventType _evt, T _arg1, A _arg2, B _arg3, C _arg4)
        {
            eventRegister[_evt]?.Invoke(_arg1, _arg2, _arg3, _arg4);
        }
    }
}