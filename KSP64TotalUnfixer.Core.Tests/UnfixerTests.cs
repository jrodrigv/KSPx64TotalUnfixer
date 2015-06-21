using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using KSPx64TotalUnfixer.Core;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace KSP64TotalUnfixer.Core.Tests
{
    [TestClass]
    public class UnfixerTests
    {
        public const string KspTestPath = "E:\\Steam\\SteamApps\\common\\Kerbal Space Program - test";

        private void SetupFolders()
        {
             Directory.Delete(String.Concat(KspTestPath,"\\GameData"), recursive:true);
             Utilities.DirectoryCopy(String.Concat(KspTestPath, "\\GameData_BK"), String.Concat(KspTestPath, "\\GameData"),true);
        }

        [TestMethod]
        public void UnfixSuccesfull()
        {
            //Setup
            SetupFolders();
            Utilities.SetupDllsFromPath(Path.Combine(KspTestPath,@"GameData"));
            
            //Arrange
            var unfixerWorker = new UnfixerWorker()
            {   QueueToProcess = Utilities.DllsToUnfixQueue,
                Results = Utilities.UnfixingResultsDictionary,
                KspPath = KspTestPath};

        
            //Act 1
            var taskFirtPass = unfixerWorker.StartUnfixing();
            taskFirtPass.Wait();
            var firstPassCount = Utilities.UnfixingResultsDictionary.Values.Count(x => x == "Unfixed");

          

            //Arrange 2
            Utilities.SetupDllsFromPath(Path.Combine(KspTestPath, @"GameData"));
            unfixerWorker = new UnfixerWorker()
            {
                QueueToProcess = Utilities.DllsToUnfixQueue,
                Results = Utilities.UnfixingResultsDictionary,
                KspPath = KspTestPath
            };
            //Act2
            var taskSecondPass   = unfixerWorker.StartUnfixing();
            taskSecondPass.Wait();
            var secondPassCount = Utilities.UnfixingResultsDictionary.Values.Count(x => x == "Unfixed");

            //Assert
            Assert.IsTrue(firstPassCount >= 1 && secondPassCount == 0);
        }
    }
}
