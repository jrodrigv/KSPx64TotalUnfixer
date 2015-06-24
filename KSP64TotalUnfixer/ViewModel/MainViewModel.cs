using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.CommandWpf;
using GalaSoft.MvvmLight.Threading;
using KSPx64TotalUnfixer.Core;
using Microsoft.WindowsAPICodePack.Dialogs;

namespace KSPx64TotalUnfixer.UI.ViewModel
{
    /// <summary>
    /// This class contains properties that the main View can data bind to.
    /// <para>
    /// See http://www.galasoft.ch/mvvm
    /// </para>
    /// </summary>
    public class MainViewModel : ViewModelBase
    {
        private string _gameDataPath = string.Empty;
        private int _numberOfDlls;
        private int _filesProcessed;
    
        public string GameDataPath
        {
            get 
            {
                return _gameDataPath;
            }
            set
            {
                Set(() => GameDataPath, ref _gameDataPath, value);
            }
        }

        public RelayCommand DisplayFolderBrowserDialogCommand { get; set; }

        public RelayCommand UnfixCommand { get; set; }

        public int NumberOfDlls
        {
            get { return _numberOfDlls; }
            set { Set(() => NumberOfDlls, ref _numberOfDlls, value); }

        }

        public int FilesProcessed
        {
            get { return _filesProcessed; }
            set { Set(() => FilesProcessed, ref _filesProcessed, value); }
        }

        public static string Instructions => "Please select the GAMEDATA folder that you want to enable for x64 (WARNING: take a backup first)";


        /// <summary>
        /// Initializes a new instance of the MainViewModel class.
        /// </summary>
        public MainViewModel()
        {
            if (IsInDesignMode)
            {
                _gameDataPath = "C:\\Gamedata";
                _filesProcessed = 50;
                _numberOfDlls = 100;
            }
            _filesProcessed = 0;
            _numberOfDlls = 100;
            DisplayFolderBrowserDialogCommand = new RelayCommand(DisplayFolderBrowserDialog, () => true);
            UnfixCommand = new RelayCommand(UnfixerRunAsyncTask,
                () => (_gameDataPath != string.Empty));
        }

        private void RunUnfixer()
        {
      
            var logicalCores = Environment.ProcessorCount;
            UnfixerWorker.Setup(Directory.GetParent(GameDataPath).ToString());
            DispatcherHelper.CheckBeginInvokeOnUI(() =>
            {
                NumberOfDlls = UnfixerWorker.DllsToUnfixQueue.Count;
                FilesProcessed = 0;
            });

            var unfixerWorkers = new UnfixerWorker[logicalCores];
            var unfixerTasks = new Task[logicalCores];

          
            for (var i = 0; i < logicalCores; i++)
            {
                unfixerWorkers[i] = new UnfixerWorker();
                unfixerTasks[i] = unfixerWorkers[i].StartUnfixing(IncrementProgress);
            }
           
            Task.WaitAll(unfixerTasks);

        }

        public void IncrementProgress()
        {
            DispatcherHelper.CheckBeginInvokeOnUI(() =>
            {
                FilesProcessed++;
            });
        }

        private void UnfixerRunAsyncTask()
        {
            try
            {
                if (ValidatePaths())
                {
                    Task.Run(() => RunUnfixer()).ContinueWith(tsk =>
                    {
                        MessageBox.Show(GetResultsSummary(), "Process completed");
                    });
                }
                else
                {
                    MessageBox.Show("Error: KSP.exe not found");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        private string GetResultsSummary()
        {
            var s = new StringBuilder();
            s.AppendLine("Process completed: enjoy you KSP x64!");
            s.AppendLine("");
            s.AppendLine("Unfixed DLLs:");
            s.AppendLine("--------------------");
            foreach (var dll in UnfixerWorker.UnfixingResultsDictionary.Where(x => x.Value == UnfixState.Unfixed))
            {
                s.AppendLine(dll.Key);
            }
            s.AppendLine("--------------------");

            return s.ToString();
        }
        private bool ValidatePaths()
        {
            return Directory.GetFiles(Directory.GetParent(GameDataPath).ToString(), "KSP.exe").Count() == 1;
        }
  

        private void DisplayFolderBrowserDialog()
        {
            var dfb = new CommonOpenFileDialog
            {
                Title = "GameData folder",
                IsFolderPicker = true,
                AddToMostRecentlyUsedList = false,
                AllowNonFileSystemItems = false,
                EnsureFileExists = true,
                EnsurePathExists = true,
                EnsureReadOnly = false,
                EnsureValidNames = true,
                Multiselect = false,
                ShowPlacesList = true
            };
            if (dfb.ShowDialog() == CommonFileDialogResult.Ok)
            {
               GameDataPath = dfb.FileName;
               
            }
        }
    }
}