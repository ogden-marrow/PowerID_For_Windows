using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace PowerID.Views.Components;

/// <summary>Label/value pair row, mirrors DetailRow.swift.</summary>
public sealed partial class DetailRow : UserControl
{
    public static readonly DependencyProperty LabelProperty =
        DependencyProperty.Register(nameof(Label), typeof(string), typeof(DetailRow), new PropertyMetadata(string.Empty, OnPropertyChanged));

    public static readonly DependencyProperty ValueProperty =
        DependencyProperty.Register(nameof(Value), typeof(string), typeof(DetailRow), new PropertyMetadata(string.Empty, OnPropertyChanged));

    public string Label
    {
        get => (string)GetValue(LabelProperty);
        set => SetValue(LabelProperty, value);
    }

    public string Value
    {
        get => (string)GetValue(ValueProperty);
        set => SetValue(ValueProperty, value);
    }

    public DetailRow()
    {
        InitializeComponent();
        Refresh();
    }

    private static void OnPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        (d as DetailRow)?.Refresh();
    }

    private void Refresh()
    {
        LabelText.Text = Label;
        ValueText.Text = Value;
    }
}
