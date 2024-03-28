using System;
using System.IO;
using System.Linq;
using Antlr4.Runtime;
using CodeModel;

namespace AntlrCSharp
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("RSL BiS Translator, (C) M. Śmiałek, K. Rybiński");
            try
            {
                if (2 != args.Count()) {
                    Console.WriteLine("Use with command line arguments: input-path output-path");
                    return;
                }
                string inputPath = args[0];
                Console.WriteLine("Input from folder: " + inputPath);
                string mainPath = args[1];
                Console.WriteLine("Output to folder: " + mainPath);
                
                string input = ReadDirectoryFiles(inputPath, false) + "\n";
                input += ReadDirectoryFiles(inputPath, true);

                AntlrInputStream inputStream = new AntlrInputStream(input);
                RslBisLexer rslBisLexer = new RslBisLexer(inputStream);
                CommonTokenStream commonTokenStream = new CommonTokenStream(rslBisLexer);
                RslBisParser rslBisParser = new RslBisParser(commonTokenStream);
                RslBisParser.StartContext startContext = rslBisParser.start();
                RslBisGenerator visitor = new RslBisGenerator(){Verbose = true};
                IntermediaryRepresentation result = visitor.Visit(startContext);

                foreach (ViewFunction func in result.ViewFunctions){
                    func.ToFile(mainPath + "view");
                }
                foreach (ControllerFunction func in result.ControllerFunctions){
                    func.ToFile(mainPath + "view\\controllers");
                }
                PresenterClass dispatcher = new PresenterClass(){name = "presentation dispatcher"};
                dispatcher.ToFile(mainPath + "view\\presenters");
                foreach (PresenterClass pc in result.PresenterClasses){
                    pc.ToFile(mainPath + "view\\presenters");
                }
                foreach (FileGenerator file in result.UseCaseClasses){
                    file.ToFile(mainPath + "usecases");
                }
                foreach (FileGenerator file in result.ServiceInterfaces){
                    file.ToFile(mainPath + "services");
                }
                result.ViewModel.ToFile(mainPath + "viewmodel");
                result.ToMainFile(mainPath);
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error: " + ex);
            }
        }

        static string ReadDirectoryFiles(string directory, bool notions){
            string input = "";
            string[] dirs = Directory.GetDirectories(directory);
            string[] files = Directory.GetFiles(directory);
            foreach (string file in files) 
                if ((notions && file.Contains('#')) || (!notions && !file.Contains('#'))) input += File.ReadAllText(file);
            foreach (string dir in dirs) input += ReadDirectoryFiles(dir, notions);
            return input;
        }
    }
}
