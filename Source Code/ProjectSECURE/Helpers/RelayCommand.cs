using System;
using System.Windows.Input;

namespace ProjectSECURE.Helpers
{
    // Implementation of ICommand for MVVM command binding
    public class RelayCommand : ICommand
    {
        // Action to execute when the command is invoked
        private readonly Action<object?> execute;
        // Predicate to determine if the command can execute
        private readonly Predicate<object?>? canExecute;

        // Constructor sets up the execute action and optional canExecute predicate
        public RelayCommand(Action<object?> execute, Predicate<object?>? canExecute = null)
        {
            this.execute = execute ?? throw new ArgumentNullException(nameof(execute));
            this.canExecute = canExecute;
        }

        // Returns true if the command can execute, otherwise false
        public bool CanExecute(object? parameter) => canExecute?.Invoke(parameter) ?? true;

        // Executes the command action
        public void Execute(object? parameter) => execute(parameter);

        // Event to signal when CanExecute changes (used by WPF)
        public event EventHandler? CanExecuteChanged
        {
            add => CommandManager.RequerySuggested += value!;
            remove => CommandManager.RequerySuggested -= value!;
        }
    }
}
