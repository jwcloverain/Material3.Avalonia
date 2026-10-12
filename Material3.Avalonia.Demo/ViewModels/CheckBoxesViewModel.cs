using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;

namespace Material3.Avalonia.Demo.ViewModels;

/// <summary>Demonstrates independent options and an aggregated parent checkbox.</summary>
public sealed class CheckBoxesViewModel : INotifyPropertyChanged
{
    private bool _email = true;
    private bool _push;
    private bool _sms;
    private int _activations;

    /// <summary>Creates the checkbox commands.</summary>
    public CheckBoxesViewModel()
    {
        SelectAllCommand = new ActionCommand(() =>
        {
            var next = AllSelected != true;
            Email = Push = Sms = next;
        });
        CountActivationCommand = new ActionCommand(() =>
        {
            _activations++;
            Changed(nameof(ActivationText));
        });
    }

    /// <summary>Gets or sets email notifications.</summary>
    public bool Email
    {
        get => _email;
        set => SetOption(ref _email, value);
    }

    /// <summary>Gets or sets push notifications.</summary>
    public bool Push
    {
        get => _push;
        set => SetOption(ref _push, value);
    }

    /// <summary>Gets or sets SMS notifications.</summary>
    public bool Sms
    {
        get => _sms;
        set => SetOption(ref _sms, value);
    }

    /// <summary>Gets the aggregate selection without changing the native toggle cycle.</summary>
    public bool? AllSelected => Email && Push && Sms ? true : !Email && !Push && !Sms ? false : null;

    /// <summary>Selects all options unless all are already selected.</summary>
    public ICommand SelectAllCommand { get; }

    /// <summary>Records an activation independently of the checked binding.</summary>
    public ICommand CountActivationCommand { get; }

    /// <summary>Gets the activation count.</summary>
    public string ActivationText => $"Command activations: {_activations}";

    /// <inheritdoc />
    public event PropertyChangedEventHandler? PropertyChanged;

    private void SetOption(ref bool field, bool value, [CallerMemberName] string? name = null)
    {
        if (field == value) return;
        field = value;
        Changed(name);
        Changed(nameof(AllSelected));
    }

    private void Changed(string? name) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));

    private sealed class ActionCommand(Action action) : ICommand
    {
        public bool CanExecute(object? parameter) => true;
        public void Execute(object? parameter) => action();

        public event EventHandler? CanExecuteChanged
        {
            add { }
            remove { }
        }
    }
}