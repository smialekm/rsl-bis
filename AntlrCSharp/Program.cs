﻿using System;
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
                string input = "Use case Find client Main scenario\n" +
                               "{role ? cashier; client type}\n" +
                               "00: Cashier <select> find client\n" +
                               "01: System <read> default client search\n" +
                               "02: System <show> client search form\n" +
                               "03: Cashier <enter> client search\n" +
                               "04: Cashier <select> search\n" +
                               "05: System <close> client search form\n" +
                               "06: System <check> client search\n" +
                               "[client search ? valid]\n" +
                               "07: System <read> client\n" +
                               "08: System <show> client window\n" +
                               "09: Cashier <select> close\n" +
                               "10: System <execute> finite element method algoritm\n" +
                               "11: System <close> client window\n" +
                               "-> end ! OK\n" +
                               "Scenario\n" +
                               "06: -\"-\n" +
                               "[client search ? invalid]\n" +
                               "A1: System <show> error message\n" +
                               "A2: Cachier <select> close\n" +
                               "-> end ! notOK\n" +
                               "Scenario\n" +
                               "A1: -\"-\n" +
                               "B1: Cashier <select> repeat\n" +
                               "-> rejoin 07\n" +
                               "Use case Show client list Main scenario\n" +
                               "{role ? cashier}\n" +
                               "00: Cashier <select> show client list\n" +
                               "01: System <read> client list\n" +
                               "02: System <show> client list form\n" +
                               "03: Cashier <select> close\n" +
                               "-> end ! OK\n" +
                               "Scenario\n" +
                               "02: -\"-" +
                               "A1: Cachier <invoke> Find client\n" +
                               "-> rejoin 01";

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
