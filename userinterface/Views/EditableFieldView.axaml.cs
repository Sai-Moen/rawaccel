using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Interactivity;
using System;
using userinterface.ViewModels;

namespace userinterface.Views
{
    public partial class EditableFieldView : UserControl
    {
        public EditableFieldView()
        {
            InitializeComponent();
        }

        public void TextBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Return)
            {
                FocusManager.Instance.Focus(null);
            }
        }

        public void LostFocusHandler(object sender, RoutedEventArgs args)
        {
            var viewModel = (EditableFieldViewModel)this.DataContext;
            viewModel?.TakeValueTextAsNewValue();
        }

        private void Binding(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
        {
        }

        private void Binding_1(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
        {
        }
    }
}
