using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Windows.UI;

namespace PowerID.Views.Components;

/// <summary>Reusable card showing an icon, title, and value - mirrors StatusCard.swift.</summary>
public sealed partial class StatusCard : UserControl
{
    public static readonly DependencyProperty IconProperty =
        DependencyProperty.Register(nameof(Icon), typeof(string), typeof(StatusCard), new PropertyMetadata(string.Empty, OnPropertyChanged));

    public static readonly DependencyProperty TitleProperty =
        DependencyProperty.Register(nameof(Title), typeof(string), typeof(StatusCard), new PropertyMetadata(string.Empty, OnPropertyChanged));

    public static readonly DependencyProperty ValueProperty =
        DependencyProperty.Register(nameof(Value), typeof(string), typeof(StatusCard), new PropertyMetadata(string.Empty, OnPropertyChanged));

    public static readonly DependencyProperty AccentColorProperty =
        DependencyProperty.Register(nameof(AccentColor), typeof(Color), typeof(StatusCard), new PropertyMetadata(Colors.DodgerBlue, OnPropertyChanged));

    public string Icon
    {
        get => (string)GetValue(IconProperty);
        set => SetValue(IconProperty, value);
    }

    public string Title
    {
        get => (string)GetValue(TitleProperty);
        set => SetValue(TitleProperty, value);
    }

    public string Value
    {
        get => (string)GetValue(ValueProperty);
        set => SetValue(ValueProperty, value);
    }

    public Color AccentColor
    {
        get => (Color)GetValue(AccentColorProperty);
        set => SetValue(AccentColorProperty, value);
    }

    public StatusCard()
    {
        InitializeComponent();
        Refresh();
    }

    private static void OnPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        (d as StatusCard)?.Refresh();
    }

    private void Refresh()
    {
        IconGlyph.Text = Icon;
        TitleText.Text = Title;
        ValueText.Text = Value;
        IconGlyph.Foreground = new SolidColorBrush(AccentColor);
        IconBackground.Background = new SolidColorBrush(Color.FromArgb(38, AccentColor.R, AccentColor.G, AccentColor.B));
    }
}
