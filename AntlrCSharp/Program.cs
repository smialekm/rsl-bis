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
                Console.WriteLine("Input from file: " + inputPath);
                string mainPath = args[1];
                Console.WriteLine("Output to folder: " + mainPath);
                
                string input = File.ReadAllText(inputPath);

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
    }
}
