using System.Windows;
using System.Windows.Controls;

namespace ApliqxPos.Controls;

public partial class NumericKeypad : UserControl
{
    public NumericKeypad()
    {
        InitializeComponent();
    }

    private void Btn_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button btn && btn.Content is string key)
        {
            // We can send this to the ViewModel via a command or messenger
            // For simplicity in this POS context, we'll try to get the ViewModel from DataContext
            if (DataContext is ViewModels.PosViewModel vm)
            {
                vm.KeypadInput(key);
            }
        }
    }

    private void Btn_Backspace(object sender, RoutedEventArgs e)
    {
        if (DataContext is ViewModels.PosViewModel vm)
        {
            vm.KeypadInput("Backspace");
        }
    }

    private void Btn_Clear(object sender, RoutedEventArgs e)
    {
        if (DataContext is ViewModels.PosViewModel vm)
        {
            vm.KeypadInput("Clear");
        }
    }

    private void Btn_Done(object sender, RoutedEventArgs e)
    {
        if (DataContext is ViewModels.PosViewModel vm)
        {
            vm.IsNumericKeypadOpen = false;
        }
    }
}
