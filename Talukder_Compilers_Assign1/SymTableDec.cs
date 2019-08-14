using Talukder_Compilers_Assign1;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Talukder_Compilers_Assign1
{

    public enum VarType {charType, intType, floatType};
    public enum EntryType {constEntry, varEntry, functionEntry, parameterEntry};

    public class symTableRecord
    {
        public string Lexeme;
        public int depth;
        public Lexer.Symbol Token;
        public symTableRecord() {}
        public symTableRecord(string lex, Lexer.Symbol token, int dpth)
        {
            Lexeme = lex;
            depth = dpth;
            Token = token;
        }
    }

    public class varType : symTableRecord
    {
        VarType TypeOfVariable;
        public int Offset;
        public int size;

        public varType(string lex, Lexer.Symbol token, int dpth) : base(lex, token, dpth) {}

        public varType(string lex, Lexer.Symbol token, int dpth, VarType varT, int off) : base(lex, token, dpth)
        {
            TypeOfVariable = varT;
            Offset = off + getSize(varT);
            size = getSize(varT);
        }

        public static int getSize(VarType vt)
        {
            int os = 0;
            if (vt == VarType.charType)
                os = 1;
            else if (vt == VarType.intType)
                os = 2;
            else if (vt == VarType.floatType)
                os = 4;
            return os;
        }
    }
    public class functionType : symTableRecord
    {
        public int SizeOfLocal = 0;
        int? NumberOfParameters;
        VarType ReturnType;
        public LinkedList<parameterType> ParameterList = new LinkedList<parameterType>();

        public functionType(string lex, Lexer.Symbol token, int dpth, VarType returnType, LinkedList<parameterType> parameters) : base(lex, token, dpth)
        {
            ReturnType = returnType;
            NumberOfParameters = parameters?.Count;
            SizeOfLocal = SizeOfParameters(parameters);
        }

        public int SizeOfParameters(LinkedList<parameterType> parameters)
        {
            int size = 0;
            if (parameters != null)
            {
                foreach (var item in parameters)
                {
                    if (item.TypeOfParemeter == VarType.charType)
                        size += 1;
                    else if (item.TypeOfParemeter == VarType.intType)
                        size = size + 2;
                    else if (item.TypeOfParemeter == VarType.floatType)
                        size = size + 4;
                    ParameterList.AddLast(new parameterType(item.Lexeme, item.depth, item.TypeOfParemeter, item.Offset));
                }
                return size;
            }
            else
                return size;
        }
    }

    public class parameterType : symTableRecord
    {
        public VarType TypeOfParemeter;
        public int Offset;
        public int Size;

        public parameterType(string lex, int dpth, VarType vt, int offset) : base(lex, Lexer.Symbol.identifier, dpth)
        {
            depth = dpth;
            Lexeme = lex;
            Offset = offset + 2 + varType.getSize(vt);
            Size = varType.getSize(vt);
            if (vt == VarType.charType)
                TypeOfParemeter = VarType.charType;
            else if(vt == VarType.intType)
                TypeOfParemeter = VarType.intType;
            else if (vt == VarType.floatType)
                TypeOfParemeter = VarType.floatType;
        }
    }
    public class constantType : symTableRecord
    {
        VarType TypeOfConstant;
        int Offset;
        public int? Value = null;
        public double? ValueR = null;

        public constantType(string lex, Lexer.Symbol token, int dpth, VarType vt, int? val = null, double? valR = null) : base(lex, token, dpth)
        {
            TypeOfConstant = vt;
            Offset = findOffset();
            if (val != null)
                Value = val;
            else if (valR != null)
                ValueR = valR;
            else
                Console.WriteLine("Error! Values are null.");
        }

        public constantType(string lex, Lexer.Symbol token, int dpth, constantType cd) : base(lex, token, dpth)
        {
            Value = cd.Value;
            ValueR = cd.ValueR;
            Offset = cd.Offset;
            TypeOfConstant = cd.TypeOfConstant;
        }
        public int findOffset()
        {
            int offSet = 0;
            if (Value != null) { Offset = 2;}
            else {Offset = 4;}
            return offSet;
        }
    }

    public class SymbolTable
    {
        public LinkedList<symTableRecord>[] symTable;
        const int TableSize = 211;
        public SymbolTable()
        {
            symTable = new LinkedList<symTableRecord>[TableSize];
        }
        public int hash(string lexeme)
        {
            int key = lexeme.GetHashCode() % TableSize;         //gets hashcode within the size of the table
            return Math.Abs(key);
        }

        //inserts into symbol table after going through checks
        public void insert(string lex, Lexer.Symbol token, int dpth, EntryType et, VarType vt, LinkedList<parameterType> p = null, constantType c = null)
        {
            int key = hash(lex);
            bool found = false;
            foreach (var i in symTable)
            {
                if (i != null)
                    foreach (var j in i)
                    {
                        if (j.Lexeme == lex && j.depth == dpth)
                        {
                            found =  true;
                        }
                    }
            }

            if (found)
            {
                Console.WriteLine($"Error Found: {lex} already exits at depth: {dpth}");
                Console.ReadLine();
                Environment.Exit(0);                
            }
            else
            {
                if (symTable[key] == null)
                {
                    symTable[key] = new LinkedList<symTableRecord>();
                }
                switch (et)
                {
                    case EntryType.varEntry:
                        symTable[key].AddFirst(new varType(lex, token, dpth, vt, calcVarOffset(dpth)));
                        break;
                    case EntryType.constEntry:
                        symTable[key].AddFirst(new constantType(lex, token, dpth, c));
                        break;
                    case EntryType.functionEntry:
                        symTable[key].AddFirst(new functionType(lex, token, dpth, vt, p));
                        break;
                    case EntryType.parameterEntry:
                        int totalOffset = 0;
                        for (int i = 0; i < TableSize; i++)
                        {
                            if (symTable[i] != null)
                            {
                                var TsymTable = new LinkedList<symTableRecord>(symTable[i]);
                                foreach (parameterType item in TsymTable.OfType<parameterType>())
                                {
                                    if (item.depth == dpth)
                                    {
                                        totalOffset = item.Size + totalOffset;
                                    }
                                }
                            }
                        }
                        symTable[key].AddFirst(new parameterType(lex, dpth, vt, totalOffset));
                        break;
                }
            }
        }

        // this searches symbol table for a specified lexeme
        public symTableRecord lookup(string lexeme)
        {
            int key = hash(lexeme);
            return symTable[key]?.Where(x => x.Lexeme == lexeme).FirstOrDefault();
        }

        //this deletes all the table entries at the specified depth
        public void deleteDepth(int dpth)
        {
            for (int i = 0; i < TableSize; i++)
            {
                if (symTable[i] != null)
                {
                    var TsymTable = new LinkedList<symTableRecord>(symTable[i]);
                    foreach (var it in TsymTable)
                    {
                        if (it.depth == dpth)
                        {
                            symTable[i].Remove(it);
                        }                        
                        if (symTable[i].Count == 0)
                        {
                            symTable[i] = null;
                        }
                        
                    }
                }
            }
        }
        //calculates variable offsets
        public int calcVarOffset(int dpth)
        {
            int totalOffset = 0;
            for (int i = 0; i < TableSize; i++)
            {
                if (symTable[i] != null)
                {
                    var TsymTable = new LinkedList<symTableRecord>(symTable[i]);
                    foreach (var item in TsymTable.OfType<varType>())
                    {
                        if (item.depth == dpth)
                        {
                            totalOffset = item.size + totalOffset;
                        }
                    }
                }
            }
            return totalOffset;
        }

    }

    public class convertToAsm
    {
        public static List<string> newTacDat = new List<string>();
        Regex BasePointer = new Regex(@"^_BP[-|+]\d+$");
        List<string> tac = null;

        public convertToAsm(params string[] param)
        {
            tac = param.ToList();

            if (tac == null)
            {
                Console.WriteLine($"TAC conversion Failed!");
                Console.ReadLine();
                Environment.Exit(0);
            }
            foreach (var item in tac)
            {
                Trace.Write(item + " ");
            }
            Trace.WriteLine("");

            if (tac.Count == 0)
            {
                return;
            }
            else if (tac.Count == 1)
                One(tac[0]);
            else if (tac.Count == 2)
                Two(tac[0], tac[1]);
            else if (tac.Count == 3)
                Three(tac[0], tac[1], tac[2]);
            else if (tac.Count == 4)
                Four(tac[0], tac[1], tac[2], tac[3]);
            else if (tac.Count == 5)
                Five(tac[0], tac[1], tac[2], tac[3], tac[4]);
        }        

        public static string formatBP(String str)
        {
            str = str.Replace("_", "");
            str = str.Insert(0, "[");
            str = str.Insert(str.Length, "]");
            return str;
        }

        public void One(string first)
        {
            newTacDat.Add(first);
        }
        public void Two(string first, string second)
        {
            if (BasePointer.IsMatch(first))
                first = formatBP(first);
            if (BasePointer.IsMatch(second))
                second = formatBP(second);
            newTacDat.Add(first + " " + second);
        }
        public void Three(string first, string second, string third)
        {
            string watcher = first + " " + second + " " + third;
            if (BasePointer.IsMatch(first)) first = formatBP(first);
            if (BasePointer.IsMatch(second)) second = formatBP(second);
            if (BasePointer.IsMatch(third)) third = formatBP(third);
            if (third.StartsWith("-"))
            {
                third = formatBP(third.Substring(1));
                newTacDat.Add($"MOV AX,{third}");
                newTacDat.Add($"NEG AX");
                newTacDat.Add($"MOV {third},AX");
            }
            if (second == "=")
            {
                if (third == "AX")
                {
                    newTacDat.Add($"MOV {first},AX");
                }
                else
                {
                    newTacDat.Add($"MOV AX,{third}");
                    newTacDat.Add($"MOV {first},AX");
                }
            }
            else
            {
                newTacDat.Add(watcher);
            }
        }
        public void Four(string first, string second, string third, string fourth)
        {
            if (BasePointer.IsMatch(first))
                first = formatBP(first);
            if (BasePointer.IsMatch(second))
                second = formatBP(second);
            if (BasePointer.IsMatch(third))
                third = formatBP(third);
            if (BasePointer.IsMatch(fourth))
                fourth = formatBP(fourth);
        }

        public void Five(string first, string second, string third, string fourth, string fifth)
        {
            if (BasePointer.IsMatch(first))
                first = formatBP(first);
            if (BasePointer.IsMatch(second))
                second = formatBP(second);
            if (BasePointer.IsMatch(third))
                third = formatBP(third);
            if (BasePointer.IsMatch(fourth))
                fourth = formatBP(fourth);
            if (BasePointer.IsMatch(fifth))
                fifth = formatBP(fifth);

            Trace.WriteLine($"{first}  {second} {third} {fourth} {fifth}");
            if (fourth == "+")
            {
                newTacDat.Add($"MOV AX,{third}");
                newTacDat.Add($"ADD AX,{fifth}");
                newTacDat.Add($"MOV {first},AX");
            }
            else if (fourth == "-")
            {
                newTacDat.Add($"MOV AX,{third}");
                newTacDat.Add($"SUB AX,{fifth}");
                newTacDat.Add($"MOV {first},AX");

            }
            else if (fourth == "*")
            {
                newTacDat.Add($"MOV AX,{third}");
                newTacDat.Add($"MOV BX, {fifth}");
                newTacDat.Add($"IMUL BX");
                newTacDat.Add($"MOV {first},AX");
            }            
        }
    }
}
