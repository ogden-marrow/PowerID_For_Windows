using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace PowerID.Views.Components;

/// <summary>Bulleted battery care tip, mirrors TipRow.swift.</summary>
public sealed partial class TipRow : UserControl
{
    public static readonly DependencyProperty TextProperty =
        DependencyProperty.Register(nameof(Text), typeof(string), typeof(TipRow), new PropertyMetadata(string.Empty, OnPropertyChanged));

    public string Text
    {
        get => (string)GetValue(TextProperty);
        set => SetValue(TextProperty, value);
    }

    public TipRow()
    {
        InitializeComponent();
        Refresh();
    }

    private static void OnPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        (d as TipRow)?.Refresh();
    }

    private void Refresh()
    {
        TipText.Text = Text;
    }
}
