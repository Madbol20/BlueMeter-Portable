using System.Windows;
using System.Windows.Input;
using BlueMeter.WPF.ViewModels;

namespace BlueMeter.WPF.Views;

public partial class EncounterHistoryView : Window
{
    public EncounterHistoryView()
    {
        InitializeComponent();

        if (DataContext is EncounterHistoryViewModel viewModel)
        {
            viewModel.RequestClose += () => Close();
        }
    }

    private void DataGrid_MouseDoubleClick(object sender, MouseButtonEventArgs e)
    {
        if (DataContext is EncounterHistoryViewModel viewModel)
        {
            viewModel.LoadSelectedEncounterCommand.Execute(null);
        }
    }
}
