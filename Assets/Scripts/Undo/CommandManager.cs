using System.Collections.Generic;
using UnityEngine;

namespace Undo
{
    public class CommandManager : MonoBehaviour
    {
        private Stack<ICommand> historyStack     = new Stack<ICommand>();
        private Stack<ICommand> redoHistoryStack = new Stack<ICommand>();

        public void AddCommand(ICommand command)
        {
            historyStack.Push(command);
        }

        public void Undo()
        {
            if(historyStack.Count > 0)
            {
                redoHistoryStack.Push(historyStack.Peek());
                historyStack.Pop().Undo();
            }
        }

        public void Redo()
        {
            if(redoHistoryStack.Count > 0)
            {
                historyStack.Push(redoHistoryStack.Peek());
                redoHistoryStack.Pop().Execute();
            }
        }

        private void Update()
        {
            if (Input.GetKey(KeyCode.LeftControl) && Input.GetKeyDown(KeyCode.C))
            {
                Undo();
                return;
            }
            if (Input.GetKey(KeyCode.LeftShift) && Input.GetKeyDown(KeyCode.C))
            {
                Redo();
                return;
            }
        }
    }
}
