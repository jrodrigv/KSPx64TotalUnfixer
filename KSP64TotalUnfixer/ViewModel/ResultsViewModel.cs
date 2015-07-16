using GalaSoft.MvvmLight;

namespace KSPx64TotalUnfixer.UI.ViewModel
{
    public class ResultsViewModel : ViewModelBase
    {
        private string _resultsOutput = string.Empty;


        public string ResultsOutput
        {
            get { return _resultsOutput; }
            set { Set(() => ResultsOutput, ref _resultsOutput, value); }
        }

        public ResultsViewModel()
        {
            if (IsInDesignMode)
            {
                ResultsOutput = "tttttttttttttttttttttttttttttttttttttttttttttttttttttttttttttttttttttttttttttttt";
            }
        }
    }
}
