using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace PowerID.Views.Components;

/// <summary>Icon + title row used above a grouped card, mirrors DetailSection.swift's header.</summary>
public sealed partial class SectionHeader : UserControl
{
    public static readonly DependencyProperty IconProperty =
        DependencyProperty.Register(nameof(Icon), typeof(string), typeof(SectionHeader), new PropertyMetadata(string.Empty, OnPropertyChanged));

    public static readonly DependencyProperty TitleProperty =
        DependencyProperty.Register(nameof(Title), typeof(string), typeof(SectionHeader), new PropertyMetadata(string.Empty, OnPropertyChanged));

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

    public SectionHeader()
    {
        InitializeComponent();
        Refresh();
    }

    private static void OnPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        (d as SectionHeader)?.Refresh();
    }

    private void Refresh()
    {
        IconText.Text = Icon;
        TitleText.Text = Title;
    }
}
