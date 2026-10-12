using Avalonia;
using Avalonia.Animation;
using Avalonia.Automation.Peers;
using Avalonia.Automation.Provider;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Data;
using Avalonia.Headless;
using Avalonia.Input;
using Avalonia.Input.Raw;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Styling;
using Avalonia.Threading;
using Avalonia.VisualTree;
using FluentAssertions;
using Material3.Avalonia.Attached;
using Material3.Avalonia.Attached.Controls;
using Material3.Avalonia.Controls.Primitives;
using Material3.Avalonia.Controls.Helpers;
using Material3.Avalonia.Density;
using Material3.Avalonia.Motion;
using Material3.Avalonia.Theme;
using Material3.Avalonia.Tokens;
using System.Windows.Input;

namespace Material3.Avalonia.Tests.Controls;

public sealed class CheckBoxTests : IDisposable
{
    private readonly MaterialTheme _theme;
    private readonly List<Window> _windows = [];
    private readonly bool _reduceMotion;

    public CheckBoxTests()
    {
        TestApp.EnsureStarted();
        _reduceMotion = MotionSettings.ReduceMotion;
        _theme = new MaterialTheme { MotionScheme = null };
        Application.Current!.Styles.Add(_theme);
        MotionSettings.ReduceMotion = true;
    }

    [Fact]
    public void LabelAndEmptyRowSpace_ToggleOnceAndPreserveBindingAndCommand()
    {
        var source = new CheckBox();
        var command = new CountingCommand();
        var check = new CheckBox
            { Content = "Label", HorizontalAlignment = HorizontalAlignment.Stretch, Command = command };
        using var binding = check.Bind(ToggleButton.IsCheckedProperty,
            new Binding(nameof(CheckBox.IsChecked)) { Source = source, Mode = BindingMode.TwoWay });
        var window = Show(check);
        var clicks = 0;
        check.Click += (_, _) => clicks++;
        var positions = new[] { new Point(2, 2), new Point(24, 24), new Point(65, 24), new Point(350, 2) };
        foreach (var point in positions)
        {
            var previous = check.IsChecked;
            Click(window, check, point);
            check.IsChecked.Should().Be(!previous);
            source.IsChecked.Should().Be(check.IsChecked);
        }

        clicks.Should().Be(positions.Length);
        command.Count.Should().Be(positions.Length);
        source.IsChecked = true;
        check.IsChecked.Should().BeTrue();
    }

    [Theory]
    [InlineData(MaterialDensity.Default, 48)]
    [InlineData(MaterialDensity.Dense1, 44)]
    public void TargetEdges_AreSymmetricAndClickable(MaterialDensity density, double height)
    {
        var check = new CheckBox { Content = "Label", Margin = new Thickness(13, 17, 11, 19) };
        DensityAssist.SetDensity(check, density);
        var window = Show(check);
        var circle = Part<Grid>(check, "PART_FocusTarget");
        var top = circle.TranslatePoint(default, check)!.Value.Y;
        top.Should().BeApproximately((height - 40) / 2, 0.01);
        check.Bounds.Height.Should().Be(height);
        foreach (var x in new[] { 0.5, 24, 47, 60 })
        foreach (var inset in new[] { 0.5, 1.5, 2.5, 3.5 })
        foreach (var y in new[] { inset, height - inset })
        {
            var previous = check.IsChecked;
            Click(window, check, new Point(x, y));
            check.IsChecked.Should().Be(!previous, $"click at {x}, {y} must reach the row");
        }
    }

    [Fact]
    public void NativeThreeStateAndAutomation_RemainAvailable()
    {
        var check = new CheckBox { Content = "Cycle", IsThreeState = true };
        var window = Show(check);
        check.Focus(NavigationMethod.Tab);
        foreach (var expected in new bool?[] { true, null, false })
        {
            window.KeyPress(Key.Space, RawInputModifiers.None, PhysicalKey.Space, " ");
            window.KeyRelease(Key.Space, RawInputModifiers.None, PhysicalKey.Space, " ");
            check.IsChecked.Should().Be(expected);
        }

        var peer = ControlAutomationPeer.CreatePeerForElement(check)!;
        peer.GetAutomationControlType().Should().Be(AutomationControlType.CheckBox);
        ((IToggleProvider)peer).Toggle();
        check.IsChecked.Should().BeTrue();
        check.IsEnabled = false;
        Click(window, check, new Point(24, 24));
        check.IsChecked.Should().BeTrue();
    }

    [Fact]
    public void DensityAndWideContent_DoNotResizeFocusAdornerTarget()
    {
        var check = new CheckBox { Content = "A wide label", HorizontalAlignment = HorizontalAlignment.Stretch };
        var parent = new StackPanel { Children = { check } };
        DensityAssist.SetDensity(parent, MaterialDensity.Dense5);
        var window = Show(parent);
        check.Bounds.Height.Should().Be(48);
        var label = check.GetVisualDescendants().OfType<TextBlock>().Single(x => x.Text == "A wide label");
        var initialLabelX = label.TranslatePoint(default, window)!.Value.X;
        var initialIndicatorX = Part<Grid>(check, "PART_FocusTarget").TranslatePoint(default, window)!.Value.X;
        foreach (var (density, height) in new[]
                 {
                     (MaterialDensity.Dense1, 44d), (MaterialDensity.Dense2, 40d), (MaterialDensity.Dense3, 36d),
                     (MaterialDensity.Dense4, 32d), (MaterialDensity.Dense5, 28d)
                 })
        {
            DensityAssist.SetDensity(check, density);
            Layout(window);
            check.Bounds.Height.Should().Be(height);
            check.Focus(NavigationMethod.Tab);
            Layout(window);
            var adornerLayer = AdornerLayer.GetAdornerLayer(check)!;
            var adorner = adornerLayer.Children.Single(x => AdornerLayer.GetAdornedElement(x) is not null);
            var target = AdornerLayer.GetAdornedElement(adorner)!;
            target.Bounds.Size.Should().Be(new Size(Math.Min(40, height), Math.Min(40, height)));
            adorner.GetVisualAncestors().Should().Contain(adornerLayer);
            label.TranslatePoint(default, window)!.Value.X.Should().Be(initialLabelX);
            (target.TranslatePoint(default, window)!.Value.X + target.Bounds.Width / 2).Should()
                .Be(initialIndicatorX + 20);
            var ringBounds = new Rect(adorner.TranslatePoint(default, window)!.Value, adorner.Bounds.Size);
            var labelBounds = new Rect(label.TranslatePoint(default, window)!.Value, label.Bounds.Size);
            ringBounds.Intersects(labelBounds).Should().BeFalse();
            FocusRing.SetBrush(check, Brushes.Magenta);
            ((Border)adorner).BorderBrush.Should().BeSameAs(Brushes.Magenta);
            window.Focus();
        }

        check.Content = null;
        check.HorizontalAlignment = HorizontalAlignment.Left;
        Layout(window);
        check.Bounds.Size.Should().Be(new Size(28, 28));
    }

    [Fact]
    public void ErrorAndDisabledStates_ResolveAliasesAndDisplayNativeMessages()
    {
        var check = new CheckBox { Content = "Option", IsChecked = true };
        check.Resources["Selected"] = Brushes.Blue;
        check.Resources["MdCompCheckboxSelectedContainerBrush"] = new TokenAlias("Selected");
        check.Resources["MdCompCheckboxSelectedErrorContainerBrush"] = Brushes.Red;
        check.Resources["MdCompCheckboxSelectedDisabledContainerBrush"] = Brushes.Gray;
        var window = Show(check);
        var indicator = Part<Border>(check, "PART_Indicator");
        indicator.Background.Should().BeSameAs(Brushes.Blue);
        check.Resources["Selected"] = Brushes.Green;
        indicator.Background.Should().BeSameAs(Brushes.Green);
        CheckBoxAssist.SetIsError(check, true);
        indicator.Background.Should().BeSameAs(Brushes.Red);
        CheckBoxAssist.SetIsError(check, false);
        DataValidationErrors.SetErrors(check, new[] { "Selection is required" });
        Layout(window);
        indicator.Background.Should().BeSameAs(Brushes.Red);
        check.GetVisualDescendants().OfType<TextBlock>().Should().Contain(x => x.Text == "Selection is required");
        check.IsEnabled = false;
        indicator.Background.Should().BeSameAs(Brushes.Gray);
        indicator.Opacity.Should().Be(.38);
        DataValidationErrors.ClearErrors(check);
        check.IsEnabled = true;
        Layout(window);
        indicator.Background.Should().BeSameAs(Brushes.Green);
        check.GetVisualDescendants().OfType<TextBlock>().Should()
            .NotContain(x => x.IsEffectivelyVisible && x.Text == "Selection is required");
        check.Resources.ThemeDictionaries[ThemeVariant.Light] =
            new ResourceDictionary { ["VariantBrush"] = Brushes.Orange };
        check.Resources.ThemeDictionaries[ThemeVariant.Dark] =
            new ResourceDictionary { ["VariantBrush"] = Brushes.Purple };
        check.Resources["MdCompCheckboxSelectedContainerBrush"] = new TokenAlias("VariantBrush");
        window.RequestedThemeVariant = ThemeVariant.Light;
        indicator.Background.Should().BeSameAs(Brushes.Orange);
        window.RequestedThemeVariant = ThemeVariant.Dark;
        Layout(window);
        indicator.Background.Should().BeSameAs(Brushes.Purple);
    }

    [Fact]
    public void HoverFocusPressAndError_UseDistinctStateTokens()
    {
        var check = new CheckBox { IsChecked = true };
        check.Resources["MdCompCheckboxSelectedHoverContainerBrush"] = Brushes.Blue;
        check.Resources["MdCompCheckboxSelectedFocusContainerBrush"] = Brushes.Green;
        check.Resources["MdCompCheckboxSelectedPressedContainerBrush"] = Brushes.Yellow;
        check.Resources["MdCompCheckboxSelectedErrorPressedContainerBrush"] = Brushes.Red;
        check.Resources["MdCompCheckboxErrorFocusStateLayerBrush"] = Brushes.Magenta;
        Show(check);
        var indicator = Part<Border>(check, "PART_Indicator");
        var layer = Part<StateLayer>(check, "PART_StateLayer");
        var states = (IPseudoClasses)check.Classes;
        check.Classes.Add("m3-hovered");
        indicator.Background.Should().BeSameAs(Brushes.Blue);
        states.Set(":focus-visible", true);
        indicator.Background.Should().BeSameAs(Brushes.Green);
        states.Set(":pressed", true);
        indicator.Background.Should().BeSameAs(Brushes.Yellow);
        layer.Opacity.Should().BeGreaterThan(0);
        CheckBoxAssist.SetIsError(check, true);
        indicator.Background.Should().BeSameAs(Brushes.Red);
        layer.Brush.Should().BeSameAs(Brushes.Magenta);
        layer.Opacity.Should().BeGreaterThan(0);
        check.IsEnabled = false;
        layer.Opacity.Should().Be(0);
    }

    [Fact]
    public void Ripple_UsesCircleForOriginAndRowForCancellation()
    {
        var check = new CheckBox { Content = "Label", HorizontalAlignment = HorizontalAlignment.Stretch };
        var window = Show(check);
        var ripple = Part<InkRipple>(check, "PART_Ripple");
        ripple.ResolveOrigin(new Point(10, 20)).Should().Be(new Point(10, 20));
        ripple.ResolveOrigin(new Point(1, 1)).Should().Be(new Point(20, 20));
        ripple.ResolveOrigin(new Point(200, 20)).Should().Be(new Point(20, 20));
        var ended = 0;
        ripple.PressEnded += () => ended++;
        var start = check.TranslatePoint(new Point(100, 24), window)!.Value;
        window.MouseDown(start, MouseButton.Left);
        window.MouseMove(check.TranslatePoint(new Point(250, 24), window)!.Value);
        ended.Should().Be(0);
        window.MouseUp(start, MouseButton.Left);
        ended.Should().BeGreaterThan(0);
        ended = 0;
        window.MouseDown(start, MouseButton.Left);
        window.MouseMove(new Point(390, 180));
        ended.Should().BeGreaterThan(0);
        window.MouseUp(new Point(390, 180), MouseButton.Left);
        ripple.CenterOutsideShape = false;
        ripple.ResolveOrigin(new Point(200, 20)).Should().Be(new Point(200, 20));
        ripple.CenterOutsideShape = true;
        ripple.Bounded = false;
        ripple.ResolveOrigin(new Point(200, 20)).Should().Be(new Point(200, 20));
    }

    [Fact]
    public void LabelTouch_UsesOriginalPositionForSlopAndCleansUpOnDisableAndDetach()
    {
        var check = new CheckBox { Content = "Label", HorizontalAlignment = HorizontalAlignment.Stretch };
        var window = Show(check);
        var ripple = Part<InkRipple>(check, "PART_Ripple");
        var pointer = new Pointer(42, PointerType.Touch, true);
        var position = check.TranslatePoint(new Point(200, 24), window)!.Value;
        var properties =
            new PointerPointProperties(RawInputModifiers.LeftMouseButton, PointerUpdateKind.LeftButtonPressed);
        var ended = 0;
        ripple.PressEnded += () => ended++;

        void Press() => check.RaiseEvent(new PointerPressedEventArgs(check, pointer, window, position, 0,
            properties, KeyModifiers.None, 1));

        void Move(double dx) => check.RaiseEvent(new PointerEventArgs(InputElement.PointerMovedEvent, check,
            pointer, window, position + new Vector(dx, 0), 0, properties, KeyModifiers.None));

        Press();
        Move(2);
        ended.Should().Be(0);
        Move(10);
        ended.Should().Be(1);
        Press();
        check.IsEnabled = false;
        ended.Should().Be(2);
        check.IsEnabled = true;
        Press();
        window.Content = null;
        ended.Should().Be(3);
        pointer.Capture(null);
    }

    [Fact]
    public void RtlAndMultilineContent_KeepIndicatorAndAccessibleLabel()
    {
        var check = new CheckBox
        {
            Content = "A label that wraps across several lines in a narrow space", Width = 180,
            FlowDirection = FlowDirection.RightToLeft
        };
        var window = Show(check);
        var target = Part<Grid>(check, "PART_FocusTarget");
        var content = Part<global::Avalonia.Controls.Presenters.ContentPresenter>(check, "PART_ContentPresenter");
        target.TranslatePoint(default, window)!.Value.X.Should()
            .BeGreaterThan(content.TranslatePoint(default, window)!.Value.X);
        check.Bounds.Height.Should().BeGreaterThan(48);
        Part<IconPresenter>(check, "PART_Icon").FlowDirection.Should().Be(FlowDirection.LeftToRight);
        ControlAutomationPeer.CreatePeerForElement(check)!.GetName().Should().Be((string)check.Content);
    }

    [Fact]
    public void Label_PreservesUserTypographyAndForeground()
    {
        var check = new CheckBox { Content = "Custom label", FontSize = 23, Foreground = Brushes.Magenta };
        Typography.SetTextStyle(check, MdTextStyle.BodySmall);
        Show(check);
        var text = check.GetVisualDescendants().OfType<TextBlock>().Single(x => x.Text == "Custom label");
        text.FontSize.Should().Be(12);
        text.LineHeight.Should().Be(16);
        Typography.SetTextStyle(check, null);
        text.FontSize.Should().Be(23);
        text.Foreground.Should().BeSameAs(Brushes.Magenta);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void ColorTransition_CanBeInterruptedWithoutChangingLayout(bool reduceMotion)
    {
        MotionSettings.ReduceMotion = reduceMotion;
        var check = new CheckBox { Content = "Stable layout", IsChecked = true };
        check.Resources["MdCompCheckboxSelectedContainerBrush"] = Brushes.Blue;
        check.Resources["MdCompCheckboxSelectedErrorContainerBrush"] = Brushes.Red;
        var window = Show(check, keepTransitions: true);
        var indicator = Part<Border>(check, "PART_Indicator");
        var bounds = check.Bounds;
        CheckBoxAssist.SetIsError(check, true);
        PumpUntil(() => indicator.Background is ISolidColorBrush { Color.R: > 0 and < 255 });
        CheckBoxAssist.SetIsError(check, false);
        PumpUntil(() => indicator.Background is ISolidColorBrush { Color: var color } && color == Colors.Blue);
        Layout(window);
        check.Bounds.Should().Be(bounds);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void ContentAndDensityMotion_InterruptsAndPreservesWrapping(bool reduceMotion)
    {
        MotionSettings.ReduceMotion = reduceMotion;
        var check = new CheckBox { Content = "Short", MaxWidth = 260 };
        var window = Show(check, keepTransitions: true);
        var original = check.Bounds.Size;
        const string longText = "A much longer label that wraps over multiple lines in the available width";
        var reference = new CheckBox { Content = longText, MaxWidth = 260 };
        var referenceWindow = Show(reference);
        var targetHeight = reference.Bounds.Height;
        targetHeight.Should().BeGreaterThan(48);
        check.Content = longText;
        Layout(window);
        if (!reduceMotion)
            PumpUntil(() =>
            {
                Layout(window);
                return check.Bounds.Height > 48 && check.Bounds.Height < targetHeight;
            });
        else
            check.Bounds.Height.Should().BeApproximately(targetHeight, 0.1);
        check.Content = "Short";
        DensityAssist.SetDensity(check, MaterialDensity.Dense5);
        Layout(window);
        if (!reduceMotion)
            PumpUntil(() =>
            {
                Layout(window);
                return check.Bounds.Height > 28 && check.Bounds.Height < targetHeight;
            });
        PumpUntil(() =>
        {
            Layout(window);
            return Math.Abs(check.Bounds.Height - 28) < 0.1;
        });
        check.Bounds.Width.Should().BeApproximately(original.Width, 1);
        Part<Grid>(check, "PART_FocusTarget").Bounds.Width.Should().BeApproximately(28, 1);
        reference.MaxWidth = 180;
        Layout(referenceWindow);
        check.MaxWidth = 180;
        check.Content = longText;
        PumpUntil(() =>
        {
            Layout(window);
            return Math.Abs(check.Bounds.Height - reference.Bounds.Height) < 0.1;
        });
        check.Bounds.Width.Should().BeLessThanOrEqualTo(180);
        check.MaxWidth = double.PositiveInfinity;
        check.Width = 220;
        check.Height = 70;
        check.Content = "Changed content";
        Layout(window);
        check.Bounds.Size.Should().Be(new Size(220, 70));
    }

    [Fact]
    public void ButtonFamilyAndCheckbox_CursorsAllowLocalOverrides()
    {
        foreach (var control in new Button[]
                     { new CheckBox(), new Button(), new ToggleButton(), new Material3.Avalonia.Controls.IconButton() })
        {
            Show(control);
            control.Cursor!.ToString().Should().Be("Hand");
            control.IsEnabled = false;
            control.Cursor!.ToString().Should().Be("Arrow");
            control.Cursor = new Cursor(StandardCursorType.Cross);
            control.IsEnabled = true;
            control.Cursor!.ToString().Should().Be("Cross");
        }
    }

    [Theory]
    [InlineData(FlowDirection.LeftToRight)]
    [InlineData(FlowDirection.RightToLeft)]
    public void DenseCircleMotion_KeepsIndicatorHorizontallyCentered(FlowDirection direction)
    {
        MotionSettings.ReduceMotion = false;
        var check = new CheckBox { Content = "Label", IsChecked = true, FlowDirection = direction };
        DensityAssist.SetDensity(check, MaterialDensity.Dense2);
        var window = Show(check, keepTransitions: true);
        var indicator = Part<Border>(check, "PART_Indicator");
        var circle = Part<Grid>(check, "PART_FocusTarget");
        var icon = Part<IconPresenter>(check, "PART_Icon");
        var center = indicator.TranslatePoint(new Point(9, 9), check)!.Value.X;
        DensityAssist.SetDensity(check, MaterialDensity.Dense5);
        PumpUntil(() =>
        {
            Layout(window);
            indicator.TranslatePoint(new Point(9, 9), check)!.Value.X.Should().BeApproximately(center, 0.01);
            icon.TranslatePoint(new Point(9, 9), check)!.Value.X.Should().BeApproximately(center, 0.01);
            indicator.TranslatePoint(new Point(9, 9), circle)!.Value.Y.Should()
                .BeApproximately(circle.Bounds.Height / 2, 0.01);
            return Math.Abs(circle.Bounds.Width - 28) < 0.01;
        });
    }

    [Fact]
    public void SelectionTransition_PreservesOutlineDuringFillAndCanReverse()
    {
        MotionSettings.ReduceMotion = false;
        var check = new CheckBox();
        var window = Show(check, keepTransitions: true);
        var indicator = Part<Border>(check, "PART_Indicator");
        check.IsChecked = true;
        indicator.BorderThickness.Left.Should().Be(2);
        PumpUntil(() => indicator.BorderThickness.Left is > 0 and < 2);
        check.IsChecked = false;
        PumpUntil(() => Math.Abs(indicator.BorderThickness.Left - 2) < 0.01);
        Layout(window);
        indicator.Bounds.Size.Should().Be(new Size(18, 18));
    }

    private static void PumpUntil(Func<bool> condition)
    {
        var timeout = System.Diagnostics.Stopwatch.StartNew();
        while (!condition() && timeout.Elapsed < TimeSpan.FromSeconds(2))
        {
            AvaloniaHeadlessPlatform.ForceRenderTimerTick();
            Dispatcher.UIThread.RunJobs();
            Thread.Sleep(5);
        }

        condition().Should().BeTrue();
    }

    private Window Show(Control content, bool keepTransitions = false)
    {
        var window = new Window { Width = 400, Height = 200, Content = content };
        _windows.Add(window);
        window.Show();
        Layout(window);
        if (!keepTransitions)
            foreach (var visual in content.GetVisualDescendants().OfType<Animatable>().Prepend(content))
                visual.Transitions = null;
        return window;
    }

    private static void Layout(Window window)
    {
        Dispatcher.UIThread.RunJobs();
        window.UpdateLayout();
    }

    private static T Part<T>(CheckBox check, string name) where T : Control =>
        check.GetVisualDescendants().OfType<T>().Single(x => x.Name == name && x.TemplatedParent == check);

    private static void Click(Window window, Control control, Point point)
    {
        var position = control.TranslatePoint(point, window)!.Value;
        window.MouseDown(position, MouseButton.Left);
        window.MouseUp(position, MouseButton.Left);
    }

    public void Dispose()
    {
        foreach (var window in _windows) window.Close();
        Application.Current!.Styles.Remove(_theme);
        MotionSettings.ReduceMotion = _reduceMotion;
    }

    private sealed class CountingCommand : ICommand
    {
        public int Count { get; private set; }
        public bool CanExecute(object? parameter) => true;
        public void Execute(object? parameter) => Count++;

        public event EventHandler? CanExecuteChanged
        {
            add { }
            remove { }
        }
    }
}