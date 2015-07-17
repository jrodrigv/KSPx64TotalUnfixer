using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows;
using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.CommandWpf;
using GalaSoft.MvvmLight.Messaging;
using KSPx64TotalUnfixer.UI.Annotations;

namespace KSPx64TotalUnfixer.UI.ViewModel
{
    public class RestoreViewModel : ViewModelBase
    {
        public class RestoreItem : INotifyPropertyChanged
        {
            public bool IsChecked { get; set; }
            public string Dir { get; set; }
            public event PropertyChangedEventHandler PropertyChanged;

            [NotifyPropertyChangedInvocator]
            protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
            {
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
            }
        }

        private ObservableCollection<RestoreItem> _dllsToRestore = new ObservableCollection<RestoreItem>();

        public ObservableCollection<RestoreItem> DllsToRestore
        {
            get { return _dllsToRestore; }
            set { Set(() => DllsToRestore, ref _dllsToRestore, value); }
        }

        public RelayCommand RestoreProcessCommand { get; set; }
        public RestoreViewModel()
        {
            if (IsInDesignMode)
            {
                _dllsToRestore.Add(new RestoreItem() {IsChecked = false, Dir = "test1.dll"});
                _dllsToRestore.Add(new RestoreItem() { IsChecked = true, Dir = "test2.dll" });
                _dllsToRestore.Add(new RestoreItem() { IsChecked = false, Dir = "test3.dll" });
            }
            RestoreProcessCommand = new RelayCommand(StartRestoreProcess,() => DllsToRestore.Any( x => x.IsChecked));
        }

        private void StartRestoreProcess()
        {
            Task.Run(() =>
            {

                foreach (var dll in _dllsToRestore.Where(x => x.IsChecked))
                {
                    File.Copy(dll.Dir, Path.ChangeExtension(dll.Dir, "dll"), true);
                    File.Delete(dll.Dir);
                }
            }).Wait();
            if (MessageBox.Show("DLLs restored succesfully", "Restore complete")==MessageBoxResult.OK)
            {
                Messenger.Default.Send(new NotificationMessage(this, "CloseRestoreWindow"));
            }
        }

        public void LoadRestoreList(List<string> list)
        {
            _dllsToRestore.Clear();
            foreach (var dlldir in list)
            {
                _dllsToRestore.Add( new RestoreItem() { IsChecked = true, Dir = dlldir });
            }
        }
    }
}
