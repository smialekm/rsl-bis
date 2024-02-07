using System;
using System.IO;
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
                string mainPath = "C:\\Users\\smial\\Desktop\\code\\";
                string input = File.ReadAllText(mainPath+"input.txt");

                AntlrInputStream inputStream = new AntlrInputStream(input);
                RslBisLexer rslBisLexer = new RslBisLexer(inputStream);
                CommonTokenStream commonTokenStream = new CommonTokenStream(rslBisLexer);
                RslBisParser rslBisParser = new RslBisParser(commonTokenStream);
                RslBisParser.StartContext startContext = rslBisParser.start();
                RslBisGenerator visitor = new RslBisGenerator(){Verbose = true};
                IntermediaryRepresentation result = visitor.Visit(startContext);
                
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
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error: " + ex);
            }
        }
    }
}
