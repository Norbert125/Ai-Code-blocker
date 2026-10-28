using System.Windows;
using AiCodingBlocker.Core;
using AiCodingBlocker.Core.State;

namespace AiCodingBlocker.UI;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        DataContext = new MainViewModel(
            new StateStore(Paths.StatePath),
            message => MessageBox.Show(this, message, "AI Code Blocker", MessageBoxButton.OK, MessageBoxImage.Warning));
    }
}
