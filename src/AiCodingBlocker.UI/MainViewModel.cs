using System.ComponentModel;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text.Json;
using AiCodingBlocker.Core;
using AiCodingBlocker.Core.State;

namespace AiCodingBlocker.UI;

/// <summary>
/// What the window shows. IsOn starts from state.json and afterwards changes only from the service's reply.
/// </summary>
public sealed class MainViewModel : INotifyPropertyChanged
{
    private readonly Action<string> _showError;
    private bool _isOn;
    private bool _isBusy;

    public MainViewModel(StateStore state, Action<string> showError)
    {
        _showError = showError;
        _isOn = state.Load();
        ToggleCommand = new RelayCommand(ToggleAsync, () => !IsBusy);

        if (_isOn)
            _ = RefreshAsync();
    }

    /// <summary>
    /// On startup while ON: re-send ON so the service picks up config.json edits.
    /// Quiet unless the service reports errors; nothing was clicked, so no "not running" popup.
    /// </summary>
    private async Task RefreshAsync()
    {
        try
        {
            var reply = await PipeClient.SendAsync(Command.On);
            IsOn = reply.IsOn;
            if (reply.Errors.Count > 0)
                _showError(string.Join(Environment.NewLine, reply.Errors));
        }
        catch (Exception ex) when (ex is TimeoutException or IOException or JsonException or UnauthorizedAccessException)
        {
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public RelayCommand ToggleCommand { get; }

    public string StatusText => IsOn ? "Focus Mode is ON" : "Focus Mode is OFF";

    public bool IsOn
    {
        get => _isOn;
        private set
        {
            if (Set(ref _isOn, value))
                OnPropertyChanged(nameof(StatusText));
        }
    }

    public bool IsBusy
    {
        get => _isBusy;
        private set
        {
            if (Set(ref _isBusy, value))
                ToggleCommand.RaiseCanExecuteChanged();
        }
    }

    private async Task ToggleAsync()
    {
        IsBusy = true;
        try
        {
            var reply = await PipeClient.SendAsync(IsOn ? Command.Off : Command.On);
            IsOn = reply.IsOn;
            if (reply.Errors.Count > 0)
                _showError(string.Join(Environment.NewLine, reply.Errors));
        }
        catch (Exception ex) when (ex is TimeoutException or IOException or JsonException or UnauthorizedAccessException)
        {
            // Toggle stays as it was: we don't know anything new about the real state.
            _showError("Service not running." + Environment.NewLine + Environment.NewLine + ex.Message);
        }
        finally
        {
            IsBusy = false;
        }
    }

    private bool Set<T>(ref T field, T value, [CallerMemberName] string? name = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value))
            return false;
        field = value;
        OnPropertyChanged(name);
        return true;
    }

    private void OnPropertyChanged(string? name) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}
