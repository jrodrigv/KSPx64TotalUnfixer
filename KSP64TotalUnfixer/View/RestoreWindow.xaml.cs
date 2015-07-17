using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using GalaSoft.MvvmLight.Messaging;
using KSPx64TotalUnfixer.UI.ViewModel;

namespace KSPx64TotalUnfixer.UI.View
{
    /// <summary>
    /// Interaction logic for RestoreWindow.xaml
    /// </summary>
    public partial class RestoreWindow : Window
    {
        public RestoreWindow()
        {
            InitializeComponent();
            Closing += (s, e) => ViewModelLocator.Cleanup();
            Messenger.Default.Register<List<string>>(this, "Restore", x => ((RestoreViewModel)DataContext).LoadRestoreList(x));



            Messenger.Default.Register<NotificationMessage>(this, (message) =>
            {

                switch (message.Notification)
                {
                    case "CloseRestoreWindow":

                        this.Close();

                        break;
                }
            });
        }
    }
}
