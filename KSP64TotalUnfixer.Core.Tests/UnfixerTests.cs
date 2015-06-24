﻿using System;
using System.IO;
using System.Linq;
using System.Threading;
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
        private void SetupFoldersHard()
        {
            Directory.Delete(String.Concat(KspTestPath, "\\GameData"), recursive: true);
            Utilities.DirectoryCopy(String.Concat(KspTestPath, "\\GameData_BK_HARD"), String.Concat(KspTestPath, "\\GameData"), true);
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