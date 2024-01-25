using System;
using Antlr4.Runtime;

namespace AntlrCSharp
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("RSL BiS Translator, (C) M. Śmiałek, K. Rybiński");
            try
            {
                string input = "Use case Find client Main scenario\n{role ? cashier; client type}\n" +
                               "00: Cashier <select> find client\n" +
                               "01: System <read> default client search\n" +
                               "02: System <show> client search form" +
                               "03: Cashier <enter> client search" +
                               "04: Cashier <select> search" +
                               "05: System <close> client search form" +
                               "06: System <check> client search" +
                               "[client search ? valid]" +
                               "07: System <read> client" +
                               "08: System <show> client window" +
                               "09: System <execute> finite element method algoritm" +
                               "10: Cashier <select> close" +
                               "11: System <close> client window" +
                               "-> end ! OK, client type, client name ! Jan";

                AntlrInputStream inputStream = new AntlrInputStream(input);
                RslBisLexer rslBisLexer = new RslBisLexer(inputStream);
                CommonTokenStream commonTokenStream = new CommonTokenStream(rslBisLexer);
                RslBisParser rslBisParser = new RslBisParser(commonTokenStream);
                RslBisParser.StartContext startContext = rslBisParser.start();
                RslBisGenerator visitor = new RslBisGenerator(){Verbose = true};
                visitor.Visit(startContext);
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error: " + ex);
            }
        }
    }
}
