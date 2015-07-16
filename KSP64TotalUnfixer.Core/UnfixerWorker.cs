using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using KSPx64TotalUnfixer.Core.Properties;
using Mono.Cecil;
using Mono.Cecil.Cil;

namespace KSPx64TotalUnfixer.Core
{
    public enum UnfixState
    {
        NotProcessed,
        WhiteListed,
        Unfixed,
        NotUnfixed,
        Error
    }
    public class UnfixerWorker
    {
        public static Queue<String> DllsToUnfixQueue = new Queue<string>(); 
        public static Dictionary<string, UnfixState> UnfixingResultsDictionary = new Dictionary<string, UnfixState>();
        public static string KspPath;
        public static List<string> WhiteList  = new List<string>();

        public static void Setup(string kspPath)
        {
            DllsToUnfixQueue.Clear();
            UnfixingResultsDictionary.Clear();
            WhiteList.Clear();

            WhiteList = Utilities.ReadListFromFile(Resources.WhiteList);

            KspPath = kspPath;

            var gameDataPath = Path.Combine(kspPath, Resources.GameData);
           
            foreach (var dir in Directory.GetFiles(gameDataPath, Resources.AllDlls, SearchOption.AllDirectories))
            {
                if (WhiteList.Contains(Path.GetFileName(dir)))
                {
                    UnfixingResultsDictionary.Add(dir, UnfixState.WhiteListed);
                }
                else
                {
                     UnfixingResultsDictionary.Add(dir,UnfixState.NotProcessed);
                }
               
            }
            foreach (var dir in UnfixingResultsDictionary.Where(x => x.Value != UnfixState.WhiteListed))
            {
                 DllsToUnfixQueue.Enqueue(dir.Key);
            }   
        }
        public Task StartUnfixing(Action incrementProgress)
        {
            return Task.Run(() =>
            {
                while (DllsToUnfixQueue.Count > 0)
                {

                    string dllToUnfix;
                    lock (DllsToUnfixQueue)
                    {
                       dllToUnfix = DllsToUnfixQueue.Dequeue();
                    }
                    try
                    {
                        

                        var resolver = new DefaultAssemblyResolver();
                        resolver.AddSearchDirectory(Path.Combine(KspPath, @"KSP_Data\Managed"));
                        var rParam = new ReaderParameters(ReadingMode.Immediate) { AssemblyResolver = resolver };

                        var assembly = AssemblyDefinition.ReadAssembly(dllToUnfix, rParam);

                        if (ApplyStandardUnfix(assembly))
                        {
                            assembly.Write(dllToUnfix);
                            UnfixingResultsDictionary[dllToUnfix] = UnfixState.Unfixed;
                        }
                        else if (ApplyLevel2Unfix(assembly))
                        {
                            assembly.Write(dllToUnfix);
                            UnfixingResultsDictionary[dllToUnfix] = UnfixState.Unfixed;
                        }
                        else
                        {
                            UnfixingResultsDictionary[dllToUnfix] = UnfixState.NotUnfixed;
                        }
                        incrementProgress?.Invoke();
                    }
                    catch (Exception)
                    {
                        UnfixingResultsDictionary[dllToUnfix] = UnfixState.Error;

                    }
                    
                }
            });
        }


        private bool ApplyStandardUnfix(AssemblyDefinition assembly)
        {
            var updatedType = false;
            // Iterate through every single type in the module.
            foreach (var td in assembly.MainModule.Types)
            {
                var updatedMethod = false;

                if (!td.IsClass)
                    continue;

                // Iterate through every single method in the type.
                // This includes constructors, property implementors, etc.
                foreach (var md in td.Methods)
                {
                    if (!md.HasBody)
                        continue;

                    var ilp = md.Body.GetILProcessor();
                    var toReplace = new List<Instruction>();

                    foreach (
                        var fe in
                            ilp.Body.Instructions.Where(
                                fe =>
                                    (fe.OpCode == OpCodes.Call) && (fe.Operand != null) &&
                                    (((MethodReference) fe.Operand).Name == "get_Size") &&
                                    (((MethodReference) fe.Operand).DeclaringType != null) &&
                                    (((MethodReference) fe.Operand).DeclaringType.FullName ==
                                     "System.IntPtr")))
                    {
                        toReplace.Add(fe);
                        assembly.MainModule.Import(md);
                        updatedMethod = true;
                    }

                    foreach (var fe in toReplace)
                        ilp.Replace(fe, Instruction.Create(OpCodes.Ldc_I4_4));
                }

                if (!updatedMethod) continue;
                assembly.MainModule.Import(td);
                updatedType = true;
            }
            return updatedType;
        }


        private bool ApplyLevel2Unfix(AssemblyDefinition assembly)
        {
            var updatedType = false;
            // Iterate through every single type in the module.
            foreach (var td in assembly.MainModule.Types)
            {
                var updatedMethod = false;

                if (!td.IsClass)
                    continue;
                
                // Iterate through every single method in the type.
                // This includes constructors, property implementors, etc.
                foreach (var md in td.Methods)
                {
                  

                    if (!md.HasBody)
                        continue;

                    var ilp = md.Body.GetILProcessor();
                    var toReplace = new List<Instruction>();

                    foreach (var fe in ilp.Body.Instructions.Where(
                        fe =>
                            (fe.OpCode == OpCodes.Ldc_I8) && 
                            (fe.Operand.Equals(9223372036854775807)))
                            .Where(fe => fe.Previous?.OpCode.Name == "call" 
                            && fe.Previous?.Operand is MethodReference 
                            && ((MethodReference) fe.Previous.Operand).FullName.Contains("System.IntPtr::ToInt64()")))
                    {
                        toReplace.Add(fe);
                        assembly.MainModule.Import(md);
                        updatedMethod = true;
                    }

                    foreach (var fe in toReplace)
                    {
                       
                        fe.Operand = 1223372036854775807;
                        ilp.Replace(fe, fe);
                    }
                }

                if (!updatedMethod) continue;
                assembly.MainModule.Import(td);
                updatedType = true;
            }
            return updatedType;
        }

    }
}
