using System.Collections.Generic;
using System.Diagnostics;
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

        public static string Instructions
        {
            get { return "Please select the GAMEDATA folder that you want to enable for x64 (WARNING: take a backup first)"; }
        }


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
            // Create list of dlls to unfix

            var paths = new List<string>();
           
            DispatcherHelper.CheckBeginInvokeOnUI(() =>
            {
                NumberOfDlls = paths.Count;
                FilesProcessed = 0;
            });

            var startinfo = new ProcessStartInfo("x64-unfixer.exe")
            {
                WindowStyle = ProcessWindowStyle.Hidden,
                CreateNoWindow = false
            };

            foreach (var path in paths)
            {
                startinfo.Arguments = "\"" + path + "\"";
                var unfixerProcess = Process.Start(startinfo);
                if (unfixerProcess != null) unfixerProcess.WaitForExit();

                DispatcherHelper.CheckBeginInvokeOnUI(() =>
                {
                    FilesProcessed++;
                });

            }

        }

       

        private void UnfixerRunAsyncTask()
        {
           Task.Run(() => RunUnfixer()).ContinueWith(tsk =>
           {
               MessageBox.Show("Process completed: enjoy you KSP x64!", "Process completed");
           });
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