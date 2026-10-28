using System.Windows.Input;

namespace AiCodingBlocker.UI;

/// <summary>Minimal async ICommand so the button can call into the ViewModel.</summary>
public sealed class RelayCommand(Func<Task> execute, Func<bool> canExecute) : ICommand
{
    public event EventHandler? CanExecuteChanged;

    public bool CanExecute(object? parameter) => canExecute();

    // async void is the ICommand contract; the ViewModel catches its own errors.
    public async void Execute(object? parameter) => await execute();

    public void RaiseCanExecuteChanged() => CanExecuteChanged?.Invoke(this, EventArgs.Empty);
}
