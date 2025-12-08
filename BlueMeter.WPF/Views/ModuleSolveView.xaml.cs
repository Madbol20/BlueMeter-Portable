using System.Windows;
using System.Windows.Input;
using BlueMeter.WPF.ViewModels;

namespace BlueMeter.WPF.Views;

/// <summary>
/// ModuleSolveView.xaml interaction logic
/// </summary>
public partial class ModuleSolveView : Window
{
    public ModuleSolveView(ModuleSolveViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
    }

    private void Footer_ConfirmClick(object sender, RoutedEventArgs e)
    {
        Close();
    }

    private void Footer_CancelClick(object sender, RoutedEventArgs e)
    {
        Close();
    }

    private void Window_PreviewKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Escape)
        {
            e.Handled = true;
            Close();
        }
    }

    protected override void OnClosed(System.EventArgs e)
    {
        // Dispose ViewModel when window closes
        if (DataContext is ModuleSolveViewModel vm)
        {
            vm.Dispose();
        }
        base.OnClosed(e);
    }
}