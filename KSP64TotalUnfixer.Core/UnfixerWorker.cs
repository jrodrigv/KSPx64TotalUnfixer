using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Mono.Cecil;
using Mono.Cecil.Cil;

namespace KSPx64TotalUnfixer.Core
{
    public enum UnfixState
    {
        NotProcessed,
        Unfixed,
        Unnecessary,
        Error
    }
    public class UnfixerWorker
    {
        public static Queue<String> DllsToUnfixQueue = new Queue<string>(); 
        public static Dictionary<string, UnfixState> UnfixingResultsDictionary = new Dictionary<string, UnfixState>();
        public static string KspPath;


        public UnfixerWorker()
        {
            
        }
        public static void Setup(string kspPath)
        {
            UnfixerWorker.DllsToUnfixQueue.Clear();
            UnfixerWorker.UnfixingResultsDictionary.Clear();

            UnfixerWorker.KspPath = kspPath;

            var gameDataPath = Path.Combine(kspPath, @"GameData");
           
            foreach (var dir in Directory.GetFiles(gameDataPath, "*.dll", SearchOption.AllDirectories))
            {
                UnfixerWorker.DllsToUnfixQueue.Enqueue(dir);
                UnfixerWorker.UnfixingResultsDictionary.Add(dir,UnfixState.NotProcessed);
            }
        }
        public Task StartUnfixing()
        {
            return Task.Run(() =>
            {
                while (DllsToUnfixQueue.Count > 0)
                { 
                    var dllToUnfix = "";
                    lock (DllsToUnfixQueue)
                    {
                        dllToUnfix = DllsToUnfixQueue.Dequeue();
                    }

                    var resolver = new DefaultAssemblyResolver();
                    resolver.AddSearchDirectory(Path.Combine(KspPath, @"KSP_Data\Managed"));

                    try
                    {
                        var rParam = new ReaderParameters(ReadingMode.Immediate) {AssemblyResolver = resolver};

                        var assembly = AssemblyDefinition.ReadAssembly(dllToUnfix, rParam);

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

                        if (updatedType)
                        {
                            assembly.Write(dllToUnfix);
                            UnfixingResultsDictionary[dllToUnfix] = UnfixState.Unfixed;
                        }
                        else
                        {
                            UnfixingResultsDictionary[dllToUnfix] = UnfixState.Unnecessary;
                        }
                    }
                    catch (Exception)
                    {
                        UnfixingResultsDictionary[dllToUnfix] = UnfixState.Error;
                    }
                }
            });
        }

       
    }
}
