using Avalonia.Controls;
using Avalonia.Controls.Templates;
using Avalonia.Media;
using Material3.Avalonia.Attached;
using Material3.Avalonia.Density;
using Material3.Avalonia.Controls.Helpers;
using Avalonia.Interactivity;
using Material3.Avalonia.Demo.ViewModels;
using Material3.Avalonia.Tokens;

namespace Material3.Avalonia.Demo.Views;

/// <summary>The checkbox gallery shared by desktop and single-view hosts.</summary>
public partial class CheckBoxes : UserControl
{
    private bool _alternateColor;

    /// <summary>Creates the checkbox gallery.</summary>
    public CheckBoxes()
    {
        InitializeComponent();
        DataContext = new CheckBoxesViewModel();
    }

    private void OnTextClick(object? sender, RoutedEventArgs e)
    {
        var text = Equals(MotionOption.Content, "Short label")
            ? "A longer checkbox label that wraps naturally when the available width changes while the indicator and focus ring stay centered."
            : "Short label";
        MotionOption.Content = text;
        RtlMotionOption.Content = text;
    }

    private void OnTemplateClick(object? sender, RoutedEventArgs e)
    {
        IDataTemplate? template = MotionOption.ContentTemplate is null
            ? new FuncDataTemplate<string>((text, _) =>
            {
                var detail = new TextBlock { Text = "Custom supporting content" };
                Typography.SetTextStyle(detail, MdTextStyle.BodySmall);
                return new StackPanel
                    { Children = { new TextBlock { Text = text, TextWrapping = TextWrapping.Wrap }, detail } };
            })
            : null;
        MotionOption.ContentTemplate = template;
        RtlMotionOption.ContentTemplate = template;
    }

    private void OnWidthClick(object? sender, RoutedEventArgs e)
    {
        MotionOption.MaxWidth = MotionOption.MaxWidth == 480 ? 240 : 480;
        RtlMotionOption.MaxWidth = MotionOption.MaxWidth;
    }

    private void OnDensityClick(object? sender, RoutedEventArgs e)
    {
        var density = DensityAssist.GetDensity(MotionOption);
        var next = density == MaterialDensity.Dense5 ? MaterialDensity.Default : density - 1;
        DensityAssist.SetDensity(MotionOption, next);
        DensityAssist.SetDensity(RtlMotionOption, next);
    }

    private void OnValidationClick(object? sender, RoutedEventArgs e)
    {
        if (DataValidationErrors.GetHasErrors(ValidatedOption))
            DataValidationErrors.ClearErrors(ValidatedOption);
        else
            DataValidationErrors.SetErrors(ValidatedOption, new[] { "Choose at least one delivery method." });
    }

    private void OnRemapClick(object? sender, RoutedEventArgs e)
    {
        _alternateColor = !_alternateColor;
        RemappedOption.Resources["MdCompCheckboxSelectedContainerBrush"] =
            new TokenAlias(_alternateColor ? "MdSysTertiaryBrush" : "MdSysPrimaryBrush");
        RemappedOption.Resources["MdCompCheckboxSelectedIconBrush"] =
            new TokenAlias(_alternateColor ? "MdSysOnTertiaryBrush" : "MdSysOnPrimaryBrush");
    }
}