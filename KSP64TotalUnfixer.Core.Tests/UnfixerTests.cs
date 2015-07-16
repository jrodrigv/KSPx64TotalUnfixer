using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using KSPx64TotalUnfixer.Core;
using KSPx64TotalUnfixer.Core.Properties;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace KSP64TotalUnfixer.Core.Tests
{
    [TestClass]
    public class UnfixerTests
    {
        public static readonly string KspTestPath = @Resources.KSPDirectory;

        private void SetupFolders()
        {
            if (Directory.Exists(Path.Combine(KspTestPath, Resources.GameData)))
            {
                Directory.Delete(Path.Combine(KspTestPath, Resources.GameData), recursive:true);
            }
             Utilities.DirectoryCopy(Path.Combine(KspTestPath, Resources.GameData_BK), Path.Combine(KspTestPath, Resources.GameData), true);
        }

        [TestMethod]
        public void UpdateWhiteList()
        {
            //Arrange
            SetupFolders();
            UnfixerWorker.Setup(KspTestPath);
            var unfixerWorker = new UnfixerWorker();

            //Act 
            var taskFirtPass = unfixerWorker.StartUnfixing(null);
            taskFirtPass.Wait();
            var errorsDlls = UnfixerWorker.UnfixingResultsDictionary.Where(x => x.Value == UnfixState.Error);

            using (var file =
            new System.IO.StreamWriter("whitelist.txt", true))
            {
                foreach (var failedDll in errorsDlls)
                {
                    file.WriteLine(Path.GetFileName(failedDll.Key));
                }
            }

            SetupFolders();
            UnfixerWorker.Setup(KspTestPath);
            var unfixerWorker2 = new UnfixerWorker();
            var taskSecondPass = unfixerWorker2.StartUnfixing(null);
            taskSecondPass.Wait();

            var countErrorsDlls = UnfixerWorker.UnfixingResultsDictionary.Count(x => x.Value == UnfixState.Error);
            
            
            //Assert
            Assert.AreEqual(0,countErrorsDlls);
            File.Copy("whitelist.txt", @"..\..\..\KSP64TotalUnfixer.Core\whitelist.txt",true);
        }

        [TestMethod]
        public void ModsOnTheWhitelistAreNotUnfixed()
        {
            //Arrange
            SetupFolders();
            UnfixerWorker.Setup(KspTestPath);
            var unfixerWorker = new UnfixerWorker();
            
            //Act 
            var taskFirtPass = unfixerWorker.StartUnfixing(null);
            taskFirtPass.Wait();
            var notProcessedDlls = UnfixerWorker.UnfixingResultsDictionary.Values.Count(x => x == UnfixState.NotProcessed);

            //Assert
            Assert.AreEqual(UnfixerWorker.WhiteList.Count,notProcessedDlls);
        }

        [TestMethod]
        public void UnfixRoSuccesfully()
        {
            //Arrange
            SetupFolders();
            UnfixerWorker.Setup(KspTestPath);
            var unfixerWorker = new UnfixerWorker();
        
            //Act 
            var taskFirtPass = unfixerWorker.StartUnfixing(null);
            taskFirtPass.Wait();
            var firstPassCount = UnfixerWorker.UnfixingResultsDictionary.Values.Count(x => x == UnfixState.Unfixed);
            UnfixerWorker.Setup(KspTestPath);
            var taskSecondPass   = unfixerWorker.StartUnfixing(null);
            taskSecondPass.Wait();
            var secondPassCount = UnfixerWorker.UnfixingResultsDictionary.Values.Count(x => x == UnfixState.Unfixed);

            //Assert
            Assert.IsTrue(firstPassCount >= 1 && secondPassCount == 0);
        }
        
        [TestMethod]
        public void MultiWorkersUnfixSuccesfully()
        {
            //Arrange
            var logicalCores = Environment.ProcessorCount;
            SetupFolders();
            UnfixerWorker.Setup(KspTestPath);
            var monoUnfixerWorker = new UnfixerWorker();

            var timeBeforeExecuteMono = DateTime.Now;
            var taskFirtPass = monoUnfixerWorker.StartUnfixing(null);
            taskFirtPass.Wait();
            var monoTime = DateTime.Now.Subtract(timeBeforeExecuteMono).TotalMilliseconds;

            var monoPassCount = UnfixerWorker.UnfixingResultsDictionary.Values.Count(x => x == UnfixState.Unfixed);


            //Act
            SetupFolders();
            UnfixerWorker.Setup(KspTestPath);
            var unfixerWorkers = new UnfixerWorker [logicalCores];
            var unfixerTasks = new Task[logicalCores];

            var timeBeforeExecuteMulti = DateTime.Now;
            for (var i = 0; i < logicalCores; i++)
            {
                unfixerWorkers[i] = new UnfixerWorker();
                unfixerTasks[i] = unfixerWorkers[i].StartUnfixing(null);
            }

            Task.WaitAll(unfixerTasks);
            var multiTime = DateTime.Now.Subtract(timeBeforeExecuteMulti).TotalMilliseconds;

            var multiPassCount = UnfixerWorker.UnfixingResultsDictionary.Values.Count(x => x == UnfixState.Unfixed);

            //Assert
            Assert.AreEqual(monoPassCount,multiPassCount,"Error: multi count = "+multiPassCount+" != mono count= "+monoPassCount);
            Assert.IsTrue(multiTime < monoTime ,"Error: Multi Time = "+multiTime+" > Mono time = "+monoTime);
        }
    }
}
