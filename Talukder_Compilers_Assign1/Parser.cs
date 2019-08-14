using Talukder_Compilers_Assign1;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Security.Permissions;
using System.Text;
using System.Threading.Tasks;
using System.Text.RegularExpressions;

/* 
 * Assignment 8 generates assembly language.
 */
namespace Talukder_Compilers_Assign1
{
    public class Parser : Lexer
    {
        public LinkedList<parameterType> parameters = null;
        public functionType newFuncType = null;
        public Stack<object> stackA = new Stack<object>();
        public List<string> tacOutput = new List<string>();
        public SymbolTable table;
        public int parser_depth;
        VarType variableEntry;
        public string tempLexeme;
        public string currentFunction = "";
        public string _tempVar = "";
        public int _t = 0;
        bool signCond = false;
        bool sOpCond = true;

        public Parser(string inputFile) : base(inputFile)
        {
            parser_depth = 1;
            file += ".asm";
            table = new SymbolTable();
        }

        public void Run()
        {
            GetNextToken();
            Prog();
            
            if (Token != Symbol.eoft)
            {
                Console.WriteLine($"Error - Unused Symbols starting at Line {lineNo}");
                Console.ReadLine();
                Environment.Exit(0);
            }
            
            var main = table.lookup("main");
            if (main is functionType) { }
            else
            {                
                Console.WriteLine($"Error! main could not be found in program.");
                Console.ReadLine();
                Environment.Exit(0);        
            }
            var directory = Path.Combine(Directory.GetCurrentDirectory() + "\\" + file);
            List<string> tacList = new List<string>();
            List<object> ret = new List<object>();
            int j = 0;
            tacList.Add(".MODEL SMALL");
            tacList.Add(".586");
            tacList.Add(".STACK 100H");
            tacList.Add(".DATA");
            for (int i = 0; i < 211; i++)
            {
                if (table.symTable[i] != null)
                {
                    var TTable = new LinkedList<symTableRecord>(table.symTable[i]);
                    foreach (var item in TTable)
                    {
                        if (item.depth == 1)
                        {
                            ret.Add(item);
                        }
                    }
                }
            }
            var x = ret;
            foreach (var dat in x)
            {                
                if (dat is constantType)
                {
                    var ptr = dat as constantType;
                    int? val_int = ptr.Value;
                    double? valr_float = ptr.ValueR;
                    if (val_int != null)
                        tacList.Add($"{ptr.Lexeme} DW {ptr.Value}");
                    else
                        tacList.Add($"{ptr.Lexeme} DW {ptr.ValueR}");
                }
                if (dat is varType)
                {
                    var ptr = dat as varType;
                    if (ptr.size == 1)
                    {
                        tacList.Add($"{ptr.Lexeme} DB ?");
                    }
                    else
                        tacList.Add($"{ptr.Lexeme} DW ?");
                }
                
            }
            foreach (var dat in Literals)
            {
                tacList.Add($"_S{j++} DB {dat},\"$\"");
            }
            tacList.Add(".CODE");
            tacList.Add("INCLUDE IO.ASM");
            tacList.Add("");
            foreach (var item in tacOutput)
            {
                var processed = new convertToAsm(item.Split(' '));
            }
            foreach (var dat in convertToAsm.newTacDat)
            {
                tacList.Add(dat);
            }
            tacList.Add("_startproc PROC");
            tacList.Add("MOV AX, @DATA");
            tacList.Add($"MOV DS,AX");
            tacList.Add($"CALL main");
            tacList.Add($"MOV AX, 4C00H");
            tacList.Add($"INT 21H");
            tacList.Add("_startproc ENDP");
            tacList.Add("END _startproc");
            tacOutput = tacList;
            System.IO.File.WriteAllLines(directory, tacOutput);
        }
        public void Match(Symbol desiredSymbol)
        {
            if (Token == desiredSymbol)
            {
                if (desiredSymbol == Symbol.identifier)
                { tempLexeme = Lexeme;}
                GetNextToken();
            }
            else
            {
                Console.WriteLine("ERROR: Expected {0} but got {1} on line {2}", desiredSymbol, Token, lineNo);
                Console.WriteLine("Press any key to exit...");
                Console.Read();  
            }
        }

        public void Prog()
        {
            if (Token == Symbol.chart || Token == Symbol.intt || Token == Symbol.floatt)
            {
                Type();
                Match(Symbol.identifier);
                Rest();
                Prog();
            }
            else if (Token == Symbol.constt)
            {
                Match(Symbol.constt);
                Match(Symbol.identifier);
                Match(Symbol.assignopt);
                bool aval = IsValue();
                Match(Symbol.numt);
                if (aval){
                    table.insert(tempLexeme, Symbol.identifier, parser_depth, EntryType.constEntry, variableEntry, null, new constantType(tempLexeme, Symbol.identifier, parser_depth, variableEntry, Value));
                }
                else {
                    table.insert(tempLexeme, Symbol.identifier, parser_depth, EntryType.constEntry, variableEntry, null, new constantType(tempLexeme, Symbol.identifier, parser_depth, variableEntry, null, ValueR));
                }
                Match(Symbol.semicolont);
                Prog();
            }
            else
            {
                //Do Nothing
            }
        }

        public void Type()
        {
            int varOff = 0;
            if (Token == Symbol.intt)
            {
                Match(Symbol.intt);
                variableEntry = VarType.intType;
                varOff = varOff + 2;
            }
            else if (Token == Symbol.floatt)
            {
                Match(Symbol.floatt);
                variableEntry = VarType.floatType;
                varOff = varOff + 4;
            }
            else if (Token == Symbol.chart)
            {
                Match(Symbol.chart);
                variableEntry = VarType.charType;
                varOff = varOff + 4;
            }
            else
            {
                Console.WriteLine("ERROR: Expected intt, float, or chart but got {0} on line {1}", Token, lineNo);
            }
            updateFuncSize(varOff);
        }
        public void updateFuncSize(int varOff)
        {
            if (newFuncType != null)
                newFuncType.SizeOfLocal += varOff;
        }

        public void Rest()
        {
            if (Token == Symbol.lparenthesest)
            {
                string funcName = tempLexeme;
                currentFunction = tempLexeme;
                int functionDepth = parser_depth;
                VarType functionType = variableEntry;
                table.insert(funcName, Symbol.identifier, functionDepth, EntryType.functionEntry, functionType, null);
                var fnode = table.lookup(funcName);
                Match(Symbol.lparenthesest);
                Paramlist();
                if (fnode is functionType)
                {
                    var newnode = fnode as functionType;
                    if (parameters is null)
                    { parameters = new LinkedList<parameterType>(); }
                    newnode.ParameterList = new LinkedList<parameterType>(parameters);
                    newFuncType = newnode;
                }
                parser_depth++;
                parameters = null;
                Match(Symbol.rparenthesest);
                Compound();
                tacOutput.Add($"ADD SP,{newFuncType?.SizeOfLocal}");
                tacOutput.Add("POP BP");
                tacOutput.Add($"RET {newFuncType.ParameterList.Count * 2}");
                newFuncType = null;
                tacOutput.Add($"{funcName} ENDP");
                tacOutput.Add("");
            }
            else
            {
                IdTail();
                Match(Symbol.semicolont);
                table.insert(tempLexeme, Symbol.identifier, parser_depth, EntryType.varEntry, variableEntry);
                Prog();
            }
        }

        public void Paramlist()
        {
            if (Token == Symbol.intt || Token == Symbol.floatt || Token == Symbol.chart)
            {
                Type();
                Match(Symbol.identifier);
                parameters = new LinkedList<parameterType>();
                table.insert(tempLexeme, Symbol.identifier, parser_depth + 1, EntryType.parameterEntry, variableEntry);
                int offs = CalcFuncOffset(parameters);
                parameters.AddLast(new parameterType(tempLexeme, parser_depth, variableEntry, offs));
                Paramtail();
                parameters.ToList();
            }
            else
            {
                // Do Nothing
            }

        }
        public int CalcFuncOffset(LinkedList<parameterType> item)
        {
            int offset = 0;
            var part = item.LastOrDefault();
            if (part != null)
            {
                offset = part.Offset;
                if (part.TypeOfParemeter == VarType.charType)
                    offset += 1;
                else if (part.TypeOfParemeter == VarType.intType)
                    offset += 2;
                else if (part.TypeOfParemeter == VarType.floatType)
                    offset += 4;
            }
            return offset;
        }
        public void Paramtail()
        {
            if (Token == Symbol.commat)
            {
                Match(Symbol.commat);
                Type();
                Match(Symbol.identifier);
                table.insert(tempLexeme, Symbol.identifier, parser_depth + 1, EntryType.parameterEntry, variableEntry);
                int offs = CalcFuncOffset(parameters);
                parameters.AddLast(new parameterType(tempLexeme, parser_depth, variableEntry, offs));
                Paramtail();
            }
            else
            {
                // Do Nothing
            }
        }

        public void Compound()
        {
            Match(Symbol.lcurlyt);
            tacOutput.Add($"{currentFunction} PROC");
            tacOutput.Add("PUSH BP");
            tacOutput.Add("MOV BP,SP");
            Decl();
            tacOutput.Add($"SUB SP,{newFuncType?.SizeOfLocal}");
            Stat_List();
            RET_STAT();
            Match(Symbol.rcurlyt);
            table.deleteDepth(parser_depth);
            parser_depth--;
        }

        public void Decl()
        {
            if (Token == Symbol.chart || Token == Symbol.intt || Token == Symbol.floatt)
            {
                Type();
                IdList();
            }
            else if (Token == Symbol.constt)
            {
                Match(Symbol.constt);
                Match(Symbol.identifier);
                Match(Symbol.assignopt);
                bool aval = IsValue();
                Match(Symbol.numt);
                Match(Symbol.semicolont);
                if (aval)
                {
                    table.insert(tempLexeme, Symbol.identifier, parser_depth, EntryType.constEntry, variableEntry, null, new constantType(tempLexeme, Symbol.identifier, parser_depth, variableEntry, Value));
                }
                else
                {
                    table.insert(tempLexeme, Symbol.identifier, parser_depth, EntryType.constEntry, variableEntry, null, new constantType(tempLexeme, Symbol.identifier, parser_depth, variableEntry, null, ValueR));
                }
                Decl();
            }
            else
            {
                // Do Nothing
            }

        }
        public void IdList()
        {
            Match(Symbol.identifier);
            IdTail();
            Match(Symbol.semicolont);
            table.insert(tempLexeme, Symbol.identifier, parser_depth, EntryType.varEntry, variableEntry);
            Decl();
        }

        public void IdTail()
        {
            if (Token == Symbol.commat)
            {
                table.insert(tempLexeme, Symbol.identifier, parser_depth, EntryType.varEntry, variableEntry);
                updateFuncSize(2);
                Match(Symbol.commat);
                Match(Symbol.identifier);
                IdTail();
            }
        }

        public void Stat_List()
        {
            if (Token == Symbol.identifier || Token == Symbol.coutt || Token == Symbol.cint)
            {
                Statement();
                Match(Symbol.semicolont);
                Stat_List();
            }
            else
            {
            }
        }

        public void Statement()
        {
            if (Token == Symbol.identifier)
                 AssignStat(); 
            else
                IOStat(); 
        }
        public void AssignStat()
        {
            var temp = table.lookup(this.Lexeme);
            _tempVar = temp?.Lexeme;
            var Eplace = new symTableRecord();
            string code = "";
            if (temp is null || temp is functionType || temp is constantType)
            {
                Console.WriteLine("Error! {0} variable undeclared at line {1}", Lexeme, lineNo);
                Console.WriteLine("Press any key to exit...");
                Console.Read();                      //wait for input to exit
                Environment.Exit(0);                 //exit
            }
            else
            {
                Match(Symbol.identifier);
                Match(Symbol.assignopt);
                signCond = SignOp();
                sOpCond = true;
                var lookit = table.lookup(this.Lexeme);
                if (lookit is null || lookit is varType || lookit is parameterType || lookit is constantType)
                {
                    Expr(ref Eplace);
                    code += processBP(temp);
                    code += " = ";
                    code += processBP(Eplace);
                    tacOutput.Add(code);
                }
                else
                { FuncCall(); }
                _tempVar = "";
            }
        }

        public string processBP(symTableRecord place)
        {
            if (place is varType)
            {
                var x = place as varType;
                if (x.depth == 1) { return x.Lexeme; }
                return "_BP-" + x.Offset.ToString();
            }
            else if (place is parameterType)
            {
                var x = place as parameterType;
                if (x.depth == 1) { return x.Lexeme;}
                return "_BP+" + x.Offset.ToString();
            }
            else if (place is constantType)
            {
                var x = place as constantType;
                int? val_int = x.Value;
                double? valr_float = x.ValueR;
                if (val_int != null)              
                    return val_int.ToString();
                else
                     return valr_float.ToString();
            }
            return "";
        }

        public void FuncCall()
        {
            currentFunction = table.lookup(this.Lexeme).Lexeme;
            Match(Symbol.identifier);
            Match(Symbol.lparenthesest);
            Params();
            tacOutput.Add($"CALL {currentFunction}");
            if (_tempVar != "")
            { tacOutput.Add($"{Pout(_tempVar)} = AX"); }

            while (stackA.Count > 0) { stackA.Pop(); }
            Match(Symbol.rparenthesest);
        }

        public void pushFunc(List<object> funcPass)
        {
            int x = funcPass.Count;
            for (int i = 0; i < funcPass.Count; i++)
            {
                if (funcPass[i] is string)
                { tacOutput.Add($"PUSH {funcPass[i]}"); }
                else if (funcPass[i] is parameterType)
                {
                    var temp = funcPass[i] as parameterType;
                    tacOutput.Add($"PUSH {Pout(temp.Lexeme)}");
                }
                else if (funcPass[i] is varType)
                {
                    var temp = funcPass[i] as varType;
                    tacOutput.Add($"PUSH {Pout(temp.Lexeme)}");
                }
            }
        }
        public void Params()
        {
            if (Token == Symbol.identifier)
            {
                if (table.lookup(this.Lexeme) != null)
                {
                    stackA.Push(table.lookup(this.Lexeme));
                    Match(Symbol.identifier);
                    ParamsTail();
                }
            }
            else if (Token == Symbol.numt)
            {
                stackA.Push(this.Lexeme);
                Match(Symbol.numt);
                ParamsTail();
            }
            else { }
        }

        public void ParamsTail()
        {
            if (Token == Symbol.commat)
            {
                Match(Symbol.commat);
                if (Token == Symbol.identifier)
                {
                    stackA.Push(table.lookup(this.Lexeme));
                    Match(Symbol.identifier);
                    ParamsTail();
                }
                else if (Token == Symbol.numt)
                {
                    stackA.Push(this.Lexeme);
                    Match(Symbol.numt);
                    ParamsTail();
                }
            }
            else
            {
                var z = stackA.ToList();
                pushFunc(z);
            }
        }
        public void IOStat()
        {
            if (Token == Symbol.cint)
            { InStat(); }
            else
            { OutStat(); }
        }
        public void OutStat()
        {
            Match(Symbol.coutt);
            Match(Symbol.outopt);
            OutOptions();
            OutEnd();
        }
        public void OutOptions()
        {
            if (Token == Symbol.identifier)
            {
                var newTemp2 = table.lookup(this.Lexeme);
                if (newTemp2 is varType)
                {
                    var x = newTemp2 as varType;
                    if (x.depth == parser_depth)
                        tacOutput.Add($"MOV AX,[BP-{x.Offset}]");
                    else
                        tacOutput.Add($"MOV AX,{x.Lexeme}");
                }
                else
                {
                    if (newTemp2 is parameterType)
                    {
                        var x = newTemp2 as parameterType;
                        tacOutput.Add($"MOV AX,[BP+{x.Offset}]");
                    }
                    else
                    {
                        Console.WriteLine($"Error: invalid variable, {this.Lexeme} at line {this.lineNo}");
                    }
                }
                tacOutput.Add($"CALL writeint");
                Match(Symbol.identifier);
            }
            else if (Token == Symbol.literalt)
            {
                var litCount = Literals.IndexOf(Lexeme);
                tacOutput.Add($"MOV DX,OFFSET _S{litCount}");
                tacOutput.Add($"CALL writestr");
                Match(Symbol.literalt);
            }
            else
            {
                Match(Symbol.endlt);
                tacOutput.Add($"CALL writeln");
            }
        }
        public void OutEnd()
        {
            if (Token == Symbol.outopt)
            {
                Match(Symbol.outopt);
                OutOptions();
                OutEnd();
            }
            else { }
        }

        public void InStat()
        {
            Match(Symbol.cint);
            Match(Symbol.inopt);
            var newTemp2 = table.lookup(this.Lexeme);
            Match(Symbol.identifier);
            tacOutput.Add($"CALL readint");
            if (newTemp2 is varType)
            {
                var x = newTemp2 as varType;
                if (x.depth == parser_depth)
                    tacOutput.Add($"MOV [BP-{x.Offset}],BX");
                else
                    tacOutput.Add($"MOV {x.Lexeme},BX");
            }
            else
            {
                var x = newTemp2 as parameterType;
                tacOutput.Add($"MOV [BP+{x.Offset}],BX");
            }
            InEnd();
        }

        public void InEnd()
        {
            if (Token == Symbol.inopt)
            {
                Match(Symbol.inopt);
                var newTemp2 = table.lookup(this.Lexeme);
                Match(Symbol.identifier);
                tacOutput.Add($"CALL readint");
                if (newTemp2 is varType)
                {
                    var x = newTemp2 as varType;
                    if (x.depth == parser_depth)
                        tacOutput.Add($"MOV [BP-{x.Offset}],BX");
                    else
                        tacOutput.Add($"MOV {x.Lexeme},BX");
                }
                else
                {
                    var x = newTemp2 as parameterType;
                    tacOutput.Add($"MOV [BP+{x.Offset}],BX");
                }
                InEnd();
            }
        }
        public string Pout(string lex)
        {
            var x = table.lookup(lex);

            if (x != null)
            {
                if (x.depth == this.parser_depth)
                { return processBP(x); }
            }
            return lex;
        }
        public void MoreTerm(ref symTableRecord Tplace)
        {
            string tac_code = "";
            if (Token == Symbol.addopt)
            {
                var place = new varType("_t" + _t++, Symbol.numt, this.parser_depth, VarType.intType, table.calcVarOffset(this.parser_depth));
                table.insert("_t" + _t, Symbol.numt, this.parser_depth, EntryType.varEntry, VarType.intType);
                var searchptr = table.lookup($"_t{_t}");
                tac_code += "_BP-" + place.Offset.ToString();
                tac_code = tac_code + " = " + processBP(Tplace);
                tac_code += " " + this.Lexeme + " ";
                Tplace = place;
                Match(Symbol.addopt);
                Term(ref Tplace);
                tac_code += processBP(Tplace);
                Tplace = searchptr;
                tacOutput.Add(tac_code);
                MoreTerm(ref Tplace);
            }
            else { }
        }
        public void Term(ref symTableRecord Tplace)
        {
            symTableRecord fnode = new symTableRecord();
            Factor(ref fnode);
            MoreFactor(ref fnode);
            Tplace = fnode;
        }

        public void Expr(ref symTableRecord e) { Relation(ref e); }

        public void Relation(ref symTableRecord e)
        {
            SimpleExpr(ref e);
        }

        public void SimpleExpr(ref symTableRecord e)
        {
            symTableRecord Tplace = new symTableRecord();
            if (!sOpCond)
            {
                sOpCond = true;
                signCond = SignOp();
            }
            Term(ref Tplace);
            MoreTerm(ref Tplace);
            e = Tplace;
        }
        public string insertNewTemp(ref symTableRecord Tplace)
        {
            string str = "";
            var place = new varType("_t" + _t++, Symbol.numt, this.parser_depth, VarType.intType, table.calcVarOffset(this.parser_depth));
            table.insert("_t" + _t, Symbol.numt, this.parser_depth, EntryType.varEntry, VarType.intType);
            str += "_BP-" + place.Offset.ToString();
            Tplace = place;
            return str;
        }

        public void Addop() { Match(Symbol.addopt); }

        public void Mulop()
        {
            Match(Symbol.muloppt);
        }

        public bool SignOp()
        {
            if (Token == Symbol.addopt && this.Lexeme == "-")
            {
                Match(Symbol.addopt);
                return true;
            }
            else if (Token == Symbol.nott)
            {
                Match(Symbol.nott);
                return true;
            }
            else { return false; }
        }

        public void RET_STAT()
        {
            string tac_code = "";
            Match(Symbol.returnt);
            var idptr = table.lookup(this.Lexeme);
            var Eplace = new symTableRecord();
            Expr(ref Eplace);
            tac_code += "AX = " + processBP(Eplace);
            tacOutput.Add(tac_code);
            Match(Symbol.semicolont);
        }
        public void MoreFactor(ref symTableRecord fnode)
        {
            symTableRecord Tplace = new symTableRecord();
            string tac_code = "";
            if (Token == Symbol.muloppt)
            {
                var place = new varType("_t" + _t++, Symbol.numt, this.parser_depth, VarType.intType, table.calcVarOffset(this.parser_depth));
                table.insert("_t" + _t, Symbol.numt, this.parser_depth, EntryType.varEntry, VarType.intType);
                var searchptr = table.lookup($"_t{_t}");
                tac_code = tac_code + "_BP-" + place.Offset.ToString();
                tac_code = tac_code + " = " + processBP(fnode);
                tac_code = tac_code + " " + this.Lexeme + " ";
                Match(Symbol.muloppt);
                Tplace = place;
                Factor(ref Tplace);
                tac_code += processBP(Tplace);
                fnode = searchptr;
                tacOutput.Add(tac_code);
                MoreFactor(ref Tplace);
            }
            else { }
        }
        public void Factor(ref symTableRecord Tplace)
        {
            switch (Token)
            {
                case Symbol.numt:
                    if (IsValue())
                    {
                        string tac_code = "";
                        tac_code = insertNewTemp(ref Tplace);
                        tac_code += " = " + Value.ToString();
                        tacOutput.Add(tac_code);
                    }
                    else if (IsValueR())
                    {
                        string tac_code = "";
                        tac_code = insertNewTemp(ref Tplace);
                        tac_code += " = " + ValueR.ToString();
                        tacOutput.Add(tac_code);
                    }
                    Match(Symbol.numt);
                    break;
                case Symbol.identifier:
                    Tplace = table.lookup(this.Lexeme);
                    if (signCond)
                    {
                        string tac_code = "";
                        tac_code = insertNewTemp(ref Tplace);
                        tac_code += " = " + "-" + processBP(Tplace);
                        tacOutput.Add(tac_code);
                        signCond = false;
                        sOpCond = false;
                    }
                    Match(Symbol.identifier);
                    break;
                case Symbol.lparenthesest:
                    Match(Symbol.lparenthesest);
                    sOpCond = false;
                    Expr(ref Tplace);
                    Match(Symbol.rparenthesest);
                    break;
                default:
                    break;
            }
        }
    }
}
