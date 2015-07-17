
using System.Windows;
using GalaSoft.MvvmLight.Messaging;
using KSPx64TotalUnfixer.UI.ViewModel;

namespace KSPx64TotalUnfixer.UI.View
{
    /// <summary>
    /// Interaction logic for ResultsWindow.xaml
    /// </summary>
    public partial class ResultsWindow : Window
    {
        public ResultsWindow()
        {
            InitializeComponent();
            Closing += (s, e) => ViewModelLocator.Cleanup();
            Messenger.Default.Register<string>(this, "Results", x => ((ResultsViewModel) DataContext).ResultsOutput = x);

        }
    }
}
