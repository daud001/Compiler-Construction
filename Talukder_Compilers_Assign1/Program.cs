//Name: Abu Daud Talukder
//ID: 7301692
//Assignment: 08 (Assembly Code)
//Class: Compiler Construction Spring 2018
//Professor: Dr. Hamer

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

namespace Talukder_Compilers_Assign1
{
    class Program  
    {
        static void Main(string[] args)
        {            
            var inputFile = args[0];
            Lexer MyLex = new Lexer(inputFile);
            Parser MyParse = new Parser(inputFile);          //define parser and pass in lexer

            if(inputFile == null)
            {
                Console.Write("Error reading file.");            //validation
            }
            //  Console.WriteLine("Token              Lexeme                    Attributes");
            //  Console.WriteLine("---------------------------------------------------------");

            MyParse.Run();  //run the Prog in parser file   


            foreach (var output in MyParse.tacOutput)
            {
                Console.WriteLine(output);
            }
            Console.ReadKey();
        }
    }
}
