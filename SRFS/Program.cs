using Blocks.CommandLine;
using SRFS.Commands;
using System;
using System.Reflection;
using System.Linq;
using System.Collections.Generic;
using SRFS.Model;
using System.Security.Cryptography;
using System.IO;

namespace SRFS {

    public class Program : CommandLineProgram {

        public const string INVOCATION_NAME = "srfs";

        internal Program() : base(INVOCATION_NAME) {
            CommandTypes.Add(typeof(ListPartitionsCommand));
            CommandTypes.Add(typeof(MakeKeyCommand));
            CommandTypes.Add(typeof(FileMkfsCommand));
            CommandTypes.Add(typeof(MkfsCommand));
            CommandTypes.Add(typeof(FileRunCommand));
            CommandTypes.Add(typeof(RunCommand));
            CommandTypes.Add(typeof(CalcCommand));
        }

        static int Main(string[] args) {

            try {
                Program program = new Program();
                program.Version = new Version(1, 0, 0);
                return program.Execute(args);
            } catch (CommandLineArgumentException e) {
                Console.Error.WriteLine($"ERROR: {e.Message}");
                Console.Out.WriteLine($"Use \"{INVOCATION_NAME} -h\" for usage.");
                return 1;
            } catch (ArgumentException e) {
                Console.Error.WriteLine($"ERROR: {e.Message}");
                Console.Error.WriteLine(e.StackTrace);
                return 2;
            } catch (TargetInvocationException e) {
                Exception x = e.InnerException;
                while (x != null) {
                    Console.Error.WriteLine($"ERROR: {x.Message}");
                    Console.Error.WriteLine(x.StackTrace);
                    x = x.InnerException;
                }
                return 3;
            } catch (Exception e) {
                while (e != null) {
                    Console.Error.WriteLine($"ERROR: {e.Message}");
                    Console.Error.WriteLine(e.StackTrace);
                    e = e.InnerException;
                }
                return 4;
            }
        }
    }
}
