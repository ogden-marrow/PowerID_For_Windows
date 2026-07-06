using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace PowerID.Views.Components;

/// <summary>Icon, title, and description block used on the Info page, mirrors InfoSection.swift.</summary>
public sealed partial class InfoSection : UserControl
{
    public static readonly DependencyProperty IconProperty =
        DependencyProperty.Register(nameof(Icon), typeof(string), typeof(InfoSection), new PropertyMetadata(string.Empty, OnPropertyChanged));

    public static readonly DependencyProperty TitleProperty =
        DependencyProperty.Register(nameof(Title), typeof(string), typeof(InfoSection), new PropertyMetadata(string.Empty, OnPropertyChanged));

    public static readonly DependencyProperty DescriptionProperty =
        DependencyProperty.Register(nameof(Description), typeof(string), typeof(InfoSection), new PropertyMetadata(string.Empty, OnPropertyChanged));

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

    public string Description
    {
        get => (string)GetValue(DescriptionProperty);
        set => SetValue(DescriptionProperty, value);
    }

    public InfoSection()
    {
        InitializeComponent();
        Refresh();
    }

    private static void OnPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        (d as InfoSection)?.Refresh();
    }

    private void Refresh()
    {
        IconText.Text = Icon;
        TitleText.Text = Title;
        DescriptionText.Text = Description;
    }
}
