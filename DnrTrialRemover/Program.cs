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
            int count = 0;
            string output = args[0].Replace(Path.GetExtension(args[0]), "_removed" + Path.GetExtension(args[0]));
            foreach(var type in module.Types)
                count += RemoveTrial(type);
            Console.WriteLine("Cleaned {0} Trial Calls.", count);
            if(!module.IsILOnly) module.NativeWrite(output);
            else module.Write(output);
            Console.WriteLine("File saved in  : " + output);
            Console.WriteLine("Press any key to exit...");
            Console.ReadKey();
        }

        static int RemoveTrial(TypeDef type) {
            int count = 0;
            foreach(var constructor in type.FindConstructors())
                count += RemoveTrialCalls(constructor);
            if(type.HasNestedTypes) {
                foreach(var nestedType in type.NestedTypes) {
                    foreach(var cons in nestedType.FindConstructors()) count += RemoveTrialCalls(cons);
                }
            }
            return count;
        }

        static int RemoveTrialCalls(MethodDef cctor) {
            int count = 0;
            if(cctor.HasBody) {
                var firstCall = cctor.Body.Instructions.First(x => x.OpCode == OpCodes.Call);
                if(firstCall.Operand is MethodDef method) {
                    if(method.HasBody) {
                        var firstString = method.Body.Instructions.First(x => x.OpCode == OpCodes.Ldstr).Operand.ToString();
                        if(firstString == "This assembly is protected by an unregistered version of Eziriz's \".NET Reactor\"! This assembly won't further work.") {
                            count++;
                            firstCall.OpCode = OpCodes.Nop;
                        }
                    }
                }
            }
            return count;
        }
    }
}
