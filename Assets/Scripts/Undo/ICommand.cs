namespace Undo
{
   public interface ICommand
   {
      string GetCommandName();
      void Undo();
   }
}