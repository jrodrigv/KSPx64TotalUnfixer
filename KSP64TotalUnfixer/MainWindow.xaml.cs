using System.Windows;
using GalaSoft.MvvmLight.Messaging;
using KSPx64TotalUnfixer.UI.View;
using KSPx64TotalUnfixer.UI.ViewModel;

namespace KSPx64TotalUnfixer.UI
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        /// <summary>
        /// Initializes a new instance of the MainWindow class.
        /// </summary>
        public MainWindow()
        {
            InitializeComponent();
            Closing += (s, e) => ViewModelLocator.Cleanup();

            Messenger.Default.Register<NotificationMessage>(this, (message) =>
            {
                var mainViewModel = (ViewModel.MainViewModel) message.Sender;
                switch (message.Notification)
                {
                    case "OpenResultsWindow":

                        
                        var resultsWindow = new ResultsWindow();
                        resultsWindow.Show();
                        Messenger.Default.Send<string>(mainViewModel.GetResultsSummary(), "Results");
                        break;
                }
            });
        }
    }
}