namespace Undo
{
   public interface ICommand
   {
      string GetCommandName();
      void Execute();

      void Undo();
   }
}