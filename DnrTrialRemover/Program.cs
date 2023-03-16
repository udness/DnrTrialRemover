using dnlib.DotNet;
using dnlib.DotNet.Emit;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DnrTrialRemover {
    internal class Program {
        static void Main(string[] args) {
            if(args.Length == 0) {
                Console.WriteLine("No arguments were given");
                Console.WriteLine(typeof(Program).Assembly.ManifestModule.Name + " <path/to/file>");
                Console.ReadKey();
                Environment.Exit(-1);
            }
            var module = ModuleDefMD.Load(args[0]);
            string output = args[0].Replace(Path.GetExtension(args[0]), "_removed" + Path.GetExtension(args[0]));
            RemoveTrial(module);
            module.Write(output, new dnlib.DotNet.Writer.ModuleWriterOptions(module) {
                Logger = DummyLogger.NoThrowInstance
            });
            Console.WriteLine("File saved in  : " + output);
            Console.WriteLine("Press any key to exit...");
            Console.ReadKey();
        }

        static void RemoveTrial(ModuleDefMD Module) {
            List<MethodDef> trialMethods = new List<MethodDef>();
            int countTrialCall = 0;
            foreach(var type in Module.Types) {
                foreach(var method in type.Methods.Where(x => x.HasBody)) {
                    var firstStr = method.Body.Instructions.FirstOrDefault(x => x.OpCode == OpCodes.Ldstr);
                    if(firstStr != null) {
                        if(firstStr.Operand.ToString() == "This assembly is protected by an unregistered version of Eziriz's \".NET Reactor\"! This assembly won't further work.")
                            trialMethods.Add(method);
                    }
                }
                    
            }

            Console.WriteLine($"Found {trialMethods.Count} trial declarations");

            foreach(var type in Module.Types) {
                foreach(var method in type.Methods.Where(x => x.HasBody)) {
                    var instrs = method.Body.Instructions;
                    for(int i = 0; i < instrs.Count; i++) {
                        var instr = instrs[i];
                        if(instr.OpCode != OpCodes.Call)
                            continue;
                        if(instr.Operand is MethodDef callMethod) {
                            if(trialMethods.Contains(callMethod)) {
                                instr.OpCode = OpCodes.Nop;
                                countTrialCall++;
                            }
                        }
                        
                    }
                }
            }

            foreach(var trial in trialMethods) {
                if(trial.DeclaringType != Module.GlobalType) {
                    
                    Module.Types.Remove(trial.DeclaringType);
                    continue;
                }
                Module.GlobalType.Methods.Remove(trial);
                
            }
            Console.WriteLine($"{countTrialCall} calls to trial check removed");

        }
    }
}
