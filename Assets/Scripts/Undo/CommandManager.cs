using System;
using System.Collections.Generic;
using Managers;
using UnityEngine;
using EventType = Managers.EventType;

namespace Undo
{
    public class CommandManager : MonoBehaviour
    {
        private Stack<ICommand> historyStack     = new Stack<ICommand>();

        private void OnEnable()
        {
            EventSystem<ICommand>.Subscribe(EventType.ADD_COMMAND, AddCommand);
        }
        private void OnDisable()
        {
            EventSystem<ICommand>.Unsubscribe(EventType.ADD_COMMAND, AddCommand);
        }

        private void AddCommand(ICommand _command)
        {
            Debug.Log($"Added command {_command.GetCommandName()}");
            historyStack.Push(_command);
        }

        private void Undo()
        {
            if(historyStack.Count > 0)
            {
                historyStack.Pop().Undo();
            }
        }

        private void Update()
        {
            if (Input.GetKey(KeyCode.LeftControl) && Input.GetKeyDown(KeyCode.C))
            {
                Undo();
            }
        }
    }
}
