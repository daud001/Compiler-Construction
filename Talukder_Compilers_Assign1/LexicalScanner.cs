using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Text.RegularExpressions;

namespace Talukder_Compilers_Assign1
{ 
    public class Lexer
    {
        //reserved words and other tokens
        public enum Symbol
        {
            ift, elset, whilet, floatt, intt, chart, breakt, continuet, voidentifier, identifier, relopt, addopt, muloppt, assignopt,
            lparenthesest, rparenthesest, lcurlyt, rcurlyt, commat, semicolont, periodt, lbrackett, rbrackett, eoft, unknownt,
            numt, literalt, voidt, constt, nott, returnt, cint, coutt, inopt, outopt, endlt
        };
        //reserve words
        private static readonly List<string> ReservedWords = new List<string> { "if", "else", "while", "float", "int", "char", "break", "continue", "void", "const", "return", "cin", "cout", "endl"};
        public Symbol ? Token = null;   // token declaration
        public string Lexeme = null; 
        public char ch;  
        public int lineNo = 1;   //lineNo start
        public int Value = 0;  
        public float ValueR = 0;  
        public string Literal; 
        public int index = 0;          //used as index for character array
        public char[] array;        //char array 'line' string gets converted to
        private StreamReader sr;
        private bool endOfFileFound = false; //bool statement to alter loops and for validation

        public bool v;
        public bool vR;

        public string file = "";
        public StreamReader reader;

        public Lexer(string inputFile)  //constructor
        {
            if (inputFile.Contains('.'))
            {
                file = inputFile.Substring(0, inputFile.IndexOf('.'));
            }
            else
            {
                Console.WriteLine($"UKNNOWN FILE TYPE");
                Console.ReadLine();
                Environment.Exit(0);
            }
            try
            {
                var path = Path.Combine(Directory.GetCurrentDirectory() + "\\" + inputFile);
                reader = new StreamReader(path);
            }
            catch (Exception)
            {
                Console.WriteLine($"Something went wrong when attempting to open {inputFile}");
            }

            /*******************************************/


            sr = new StreamReader(inputFile);
            string line = sr.ReadToEnd();    //read to end of file
            line = line + "\n";             //adds end of file to the end
            array = line.ToCharArray();     //string to char conversion
            GetNextChar();          
        }
        public char LookAhead()
        {
            char c;
            int i = index;
            c = array[i + 1];
            return c;
        }

        public bool IsValue()
        {
            return v;
        }
        public bool IsValueR()
        {
            return vR;
        }

        //gets next character of the character array tiLinkList the end of file
        public void GetNextChar()
        {          
            if (index <= array.Length - 1)
            {
                ch = array[index];
                index++;
                if (ch == '\n') //if '\n' found then go to next line
                {
                    lineNo++;
                }
            }
            else
            {
                endOfFileFound = true;
            }
        }

        public void GetNextToken()
        {
            while (Char.IsWhiteSpace(ch))  //checks for aLinkList sorts of white space
            {
                GetNextChar();

                if (endOfFileFound)  //if found then eoft
                {
                    Token = Symbol.eoft;
                    break;
                }
            }

            if (Token != Symbol.eoft)
            {
                ProcessToken();     //process token
            }
            else
            {
                Token = Symbol.eoft;
                sr.Close(); //close file
            }
        }

        //gets the token and determines how to process it depending on the characters in it 
        public void ProcessToken()
        {
            Lexeme = null;
            Lexeme += ch;           //first character stored in Lexeme
            GetNextChar();          //second character stored in ch           

            if (Char.IsLetter(Lexeme[0]))
            {
                ProcessWordToken();
            }
            else if (Char.IsNumber(Lexeme[0]))
            {
                ProcessNumToken();
            }
            else if (Lexeme[0] == '\"')
            {
                ProcessLiteralToken();
            }
            else if (Lexeme[0] == '/' &&  ch == '*')
            {
                ProcessComment();              
            }

            else
            {
                if ((Lexeme[0] == '=' || Lexeme[0] == '!' || Lexeme[0] == '<' || Lexeme[0] == '>' || Lexeme[0] == '|' || Lexeme[0] == '&') &&
                    ( ch == '=' || ch == '|' || ch == '&' || ch == '<' || ch == '>'))  //checks the second character for faster processing
                {   
                    ProcessDoubleToken();
                }
                else
                {
                    ProcessSingleToken();
                }
            }
            if (Lexeme[0] == '\n')
            {
                Lexeme = "ignore";  // this wiLinkList be ignore in display
                lineNo++;
                //GetNextToken();
            }
        }


        //processes word token by identifying them and assigning them
        public void ProcessWordToken()
        {
            int i = 0;
            while ((Char.IsLetterOrDigit(ch) || ch == '_') && ch != ' ') //validates conditions and iterates
            {               
                Lexeme = Lexeme + ch;
                i++;
                GetNextChar();
            }

            if (i > 27)
            {
                Token = Symbol.unknownt;                    // assigned to unknown identifier
                return;
            }          
            else if (ReservedWords.Contains(Lexeme))         //checks if Lexeme is in the reserved word list
            {
                switch (Lexeme)
                {
                    case "if":
                        Token = Symbol.ift;
                        break;
                    case "else":
                        Token = Symbol.elset;
                        break;
                    case "while":
                        Token = Symbol.whilet;
                        break;
                    case "float":
                        Token = Symbol.floatt;
                        break;
                    case "int":
                        Token = Symbol.intt;
                        break;
                    case "char":
                        Token = Symbol.chart;
                        break;
                    case "break":
                        Token = Symbol.breakt;
                        break;
                    case "continue":
                        Token = Symbol.continuet;
                        break;
                    case "void":
                        Token = Symbol.voidt;
                        break;
                    case "const":
                        Token = Symbol.constt;
                        break;
                    case "return":
                        Token = Symbol.returnt;
                        break;
                    case "cin":
                        Token = Symbol.cint;
                        break;
                    case "cout":
                        Token = Symbol.coutt;
                        break;
                    case "endl":
                        Token = Symbol.endlt;                        
                        break;
                    default:
                        Token = Symbol.identifier;
                        break;
                }
           }
            else
            {
                Token = Symbol.identifier;
            }
        }

        //processes literal tokens
        public void ProcessLiteralToken()
        {
            while (ch != '\n' && (ch != '\"'))
            {
                Lexeme += ch;
                GetNextChar();
            }

            if (ch == '\"')
            {
                Lexeme += ch;
                GetNextChar();
                Literal = Lexeme;
                Token = Symbol.literalt;
                literals.Add(Lexeme);
            }
            else
                Token = Symbol.unknownt;
        }

        //processes num tokens
        public void ProcessNumToken()
        {          
            String str;
            bool error = false;
            while ((Char.IsDigit(ch) || ch == '.')) //chech decimal with period
            {
                Lexeme += ch;
                if (ch == '.')
                {
                    GetNextChar();
                    if (ch == '.' || ch == '\n' || ch == '\r' || ch == '\t' || ch == ' ')
                    {
                        Token = Symbol.unknownt;
                        error = true;
                        break;
                    }
                }
                else if (!error)
                {
                    GetNextChar();
                }
            }
            if (!error)
            {
                str = string.Join("", Lexeme.ToArray());
                if (str.Contains('.'))
                {
                    try
                    {
                        vR = true;
                        v = false;
                        ValueR = float.Parse(str);
                        Token = Symbol.numt;
                    }
                    catch { }

                }
                else
                {
                    try
                    {
                        v = true;
                        vR = false;
                        Value = int.Parse(str);
                        Token = Symbol.numt;
                    }
                    catch
                    { //

                    }
                }
            }
            else
                Token = Symbol.unknownt;
        }


        //processes comments
        public void ProcessComment()
        {
            bool iteration;
            if (ch == '*') //If multi line
            {
                iteration = true;
                GetNextChar();
                while (iteration == true)
                {
                    GetNextChar();
                    if (ch == '*')
                    {
                        GetNextChar();
                        if (ch == '/')
                        {
                            iteration = false;
                            GetNextChar();

                        }
                    }
                }
            }
            Lexeme = "ignore";
            //ProcessToken();
            this.GetNextToken();
            
        }

        // processes relational operators, multiplicity operators, additional operators, parentheses, comma etc.
        public void ProcessSingleToken()
        {
            switch (Lexeme)
            {
                case "+":
                case "-":
                    Token = Symbol.addopt;
                    break;
                case "*":
                case "/":
                case "%":
                    Token = Symbol.muloppt;
                    break;
                case "=":
                    Token = Symbol.assignopt;
                    break;
                case ">":
                case "<":
                    Token = Symbol.relopt;
                    break;
                case "(":
                    Token = Symbol.lparenthesest;
                    break;
                case ")":
                    Token = Symbol.rparenthesest;
                    break;
                case "{":
                    Token = Symbol.lcurlyt;
                    break;
                case "}":
                    Token = Symbol.rcurlyt;
                    break;
                case "[":
                    Token = Symbol.lbrackett;
                    break;
                case "]":
                    Token = Symbol.rbrackett;
                    break;
                case ",":
                    Token = Symbol.commat;
                    break;
                case ".":
                    Token = Symbol.periodt;
                    break;
                case ";":
                    Token = Symbol.semicolont;
                    break;
                case "!":
                    Token = Symbol.nott;
                    break;/*
                case "\"":
                    Token = Symbol.literalt;
                    break;*/
                default:
                    Token = Symbol.unknownt;
                    break;
            }
        }

        //processes tokens that have two characters
        public void ProcessDoubleToken()
        {
            Lexeme += ch;
            if (Lexeme[0] == '|')
            {
                Token = Symbol.addopt;
            }
            else if (Lexeme[0] == '=')
            {
                Token = Symbol.relopt;
            }
            else if (Lexeme[0] == '&')
            {
                Token = Symbol.muloppt;
            }
            else if (Lexeme[0] == '>' && Lexeme[1] == '>')
            {
                Token = Symbol.inopt;
            }
            else if (Lexeme[0] == '<' && Lexeme[1] == '<')
            {
                Token = Symbol.outopt;
            }
            else if (Lexeme[0] == '!')
            {
                Token = Symbol.relopt;
            }
            else
                Token = Symbol.unknownt;
            GetNextChar();
            
        }
        public List<string> Literals
        {
            get
            {
                return literals;
            }
        }
        private List<string> literals = new List<string>();
        //displays the token, lexeme, and attributes
        public void Display()
        {
            if ((Lexeme != "ignore") && Token != Symbol.eoft)
            {
                Console.Write("{0, -20}", Token);
       
                int number;
                bool result = Int32.TryParse(Lexeme, out number); //convert string to int

                if (!result)
                {
                    Console.Write("{0, -30}", Lexeme);
                }
                else
                {
                    Console.Write("{0, -30}", number);
                }

                if (Token == Symbol.literalt)
                {
                    string clean = Regex.Replace(Lexeme, "[\"]", "");  //remove " from literal
                    Console.Write("{0, -30}", clean);
                }
                else if(Token == Symbol.numt)
                {
                    bool val = (Lexeme.Contains("."));  //checks if Lexeme has period to determine type of value
       
                    if(val)
                    {
                        Console.Write("{0, -30}", ValueR);
                    }
                    else
                        Console.Write("{0, -20}", Value);

                    Console.Write("\n");
                    }
                else
                    Console.Write("\n");
            }
        }
    } 
}
