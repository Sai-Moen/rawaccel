using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using userinterface.ViewModels;

namespace userinterface.Views;

public partial class NamedEditableFieldView : UserControl
{
    public NamedEditableFieldView()
    {
        InitializeComponent();
    }

    public static readonly StyledProperty<string> FieldNameProperty =
        Avalonia.AvaloniaProperty.Register<NamedEditableFieldView, string>(nameof(FieldName));

    public string FieldName
    {
        get => GetValue(FieldNameProperty);
        set
        {
            SetValue(FieldNameProperty, value);
            textBlock.Text = value;
        }
    }
}