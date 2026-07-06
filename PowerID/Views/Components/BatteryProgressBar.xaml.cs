using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;

namespace PowerID.Views.Components;

/// <summary>Animated horizontal progress bar showing the battery level as a filled, rounded rectangle.</summary>
public sealed partial class BatteryProgressBar : UserControl
{
    public static readonly DependencyProperty LevelProperty =
        DependencyProperty.Register(nameof(Level), typeof(int), typeof(BatteryProgressBar), new PropertyMetadata(0, OnValueChanged));

    public static readonly DependencyProperty GradientBrushProperty =
        DependencyProperty.Register(nameof(GradientBrush), typeof(Brush), typeof(BatteryProgressBar), new PropertyMetadata(null, OnValueChanged));

    public int Level
    {
        get => (int)GetValue(LevelProperty);
        set => SetValue(LevelProperty, value);
    }

    public Brush? GradientBrush
    {
        get => (Brush?)GetValue(GradientBrushProperty);
        set => SetValue(GradientBrushProperty, value);
    }

    public BatteryProgressBar()
    {
        InitializeComponent();
        SizeChanged += (_, _) => UpdateFill();
    }

    private static void OnValueChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        (d as BatteryProgressBar)?.UpdateFill();
    }

    private void UpdateFill()
    {
        FillRect.Fill = GradientBrush;
        var clampedLevel = Math.Clamp(Level, 0, 100);
        FillRect.Width = Math.Max(0, ActualWidth) * clampedLevel / 100.0;
        FillRect.Height = ActualHeight;
    }
}
