using scripting.Interpretation;
using scripting.Lexical;
using scripting.Script;
using System;
using System.Collections.Generic;

namespace scripting.Semantical;

/// <summary>
/// Represents a program consisting of executable Instructions.
/// </summary>
public class Program
{
    #region Fields

    private readonly InstructionUnion[] instructions;
    private readonly StaticData data;

    #endregion

    #region Constructors

    /// <summary>
    /// Initializes the InstructionList so that the Interpreter can execute it in order.
    /// </summary>
    /// <param name="code">Contains a parsed TokenList that can be emitted to bytecode.</param>
    /// <param name="addresses">Maps identifiers to memory addresses.</param>
    /// <exception cref="EmitException"/>
    public Program(ITokenList code, IDictionary<string, MemoryAddress> addresses)
    {
        InstructionList instructionList = new(code.Count)
        {
            InstructionType.Start
        };

        Dictionary<Number, DataAddress> numberMap = [];

        CodeAddress lastExprStart = 0;
        int depth = 0;

        for (int i = 0; i < code.Count; i++)
        {
            Token token = code[i];
            switch (token.Type)
            {
                case TokenType.Number:
                    Number number = Number.Parse(token.Symbol, token.Line);
                    if (!numberMap.TryGetValue(number, out DataAddress dAddress))
                    {
                        dAddress = numberMap.Count;
                        numberMap.Add(number, dAddress);
                    }
                    instructionList.Add(InstructionType.LoadNumber, new(dAddress));
                    break;
                case TokenType.Parameter:
                case TokenType.Variable:
                    MemoryAddress mAddress = addresses[token.Symbol];
                    instructionList.Add(InstructionType.Load, new(mAddress));
                    break;
                case TokenType.Input:
                    instructionList.Add(InstructionType.LoadIn);
                    break;
                case TokenType.Output:
                    instructionList.Add(InstructionType.LoadOut);
                    break;
                case TokenType.Return:
                    instructionList.Add(InstructionType.Return);
                    break;
                case TokenType.Constant:
                    instructionList.Add(OnConstant(token));
                    break;
                case TokenType.Branch:
                    InstructionFlags flags = InstructionFlags.Continuation;
                    if (token.IsLoop()) flags |= InstructionFlags.IsLoop;

                    instructionList.Add(new Instruction(InstructionType.BranchMarker, flags), new(lastExprStart));
                    lastExprStart = instructionList.Count - 1;

                    depth++;
                    break;
                case TokenType.BranchEnd:
                    if (depth-- != 0)
                    {
                        Instruction marker = new(InstructionType.BranchMarker);
                        if (instructionList.TryFind(marker, depth, out CodeAddress index))
                        {
                            marker = instructionList[index].instruction;
                            if (marker.AllFlags(InstructionFlags.IsLoop))
                            {
                                InstructionOperand markerCondition = instructionList[index + 1].operand;
                                instructionList.Add(InstructionType.Jmp, markerCondition);
                            }

                            CodeAddress condition = instructionList.Count - 1;
                            instructionList.Replace(index, InstructionType.Jz, new(condition));
                            break;
                        }
                    }

                    throw ProgramError("Unexpected branch end!", token.Line);
                case TokenType.Assignment:
                    InstructionType modify = OnAssignment(token);
                    bool isInline = modify.IsInline();

                    // MUTATES i, because we don't want to add this token again on the next iteration
                    BaseToken target = code[++i].Base;
                    if (target.Type == TokenType.Input)
                    {
                        SpecialAssignment(
                            instructionList, isInline,
                            InstructionType.LoadIn, modify, InstructionType.StoreIn);
                    }
                    else if (target.Type == TokenType.Output)
                    {
                        SpecialAssignment(
                            instructionList, isInline,
                            InstructionType.LoadOut, modify, InstructionType.StoreOut);
                    }
                    else
                    {
                        MemoryAddress address = addresses[target.Symbol];
                        if (isInline)
                        {
                            instructionList.Add(InstructionType.Load, new(address));
                            instructionList.Add(InstructionType.Swap);
                            instructionList.Add(modify);
                        }
                        instructionList.Add(InstructionType.Store, new(address));
                    }

                    lastExprStart = instructionList.Count - 1;
                    break;
                case TokenType.Arithmetic:
                    InstructionType arithmetic = OnArithmetic(token);

                    // attempt to convert [...LoadE, Pow...] to [...Exp...]
                    if (arithmetic == InstructionType.Pow && instructionList.Count > 1)
                    {
                        Index lastIndex;
                        if (instructionList[lastIndex = ^1].instruction.Type == InstructionType.LoadE)
                        {
                            instructionList.RemoveAt(lastIndex.Value);
                            instructionList.Add(InstructionType.Exp);
                            break;
                        }
                    }

                    instructionList.Add(arithmetic);
                    break;
                case TokenType.Comparison:
                    instructionList.Add(OnComparison(token));
                    break;
                case TokenType.Function:
                    instructionList.Add(OnFunction(token));
                    break;
                default:
                    throw ProgramError("Cannot emit token!", token.Line);
            }
        }

        if (depth != 0)
        {
            throw ProgramError("Branch mismatch!");
        }

        data = new(numberMap.Count);
        foreach ((Number number, DataAddress dAddress) in numberMap)
        {
            data[dAddress] = number;
        }

        instructionList.Add(InstructionType.End);
        instructions = [.. instructionList];
    }

    #endregion

    #region Static Methods

    private static void SpecialAssignment(
        InstructionList instructionList, bool isInline,
        InstructionType load, InstructionType modify, InstructionType store)
    {
        if (isInline)
        {
            instructionList.Add(load);
            instructionList.Add(InstructionType.Swap);
            instructionList.Add(modify);
        }
        instructionList.Add(store);
    }

    #endregion

    #region Properties

    public int Length => instructions.Length;

    public InstructionUnion this[CodeAddress index] => instructions[index];
    public Number this[DataAddress index] => data[index];

    #endregion

    #region Methods

    public InstructionOperand GetOperandFromNext(ref CodeAddress c) => this[++c].operand;

    #endregion

    #region Jump Tables

    private static InstructionType OnConstant(Token token) => token.Symbol switch
    {
        Tokens.ZERO => InstructionType.LoadZero,
        Tokens.CONST_E => InstructionType.LoadE,
        Tokens.CONST_PI => InstructionType.LoadPi,
        Tokens.CONST_TAU => InstructionType.LoadTau,
        Tokens.CONST_CAPACITY => InstructionType.LoadCapacity,

        _ => throw ProgramError("Cannot emit constant!", token.Line),
    };

    private static InstructionType OnAssignment(Token token) => token.Symbol switch
    {
        Tokens.ASSIGN => InstructionType.Store,
        Tokens.IADD => InstructionType.Add,
        Tokens.ISUB => InstructionType.Sub,
        Tokens.IMUL => InstructionType.Mul,
        Tokens.IDIV => InstructionType.Div,
        Tokens.IMOD => InstructionType.Mod,
        Tokens.IPOW => InstructionType.Pow,

        _ => throw ProgramError("Cannot emit assignment!", token.Line),
    };

    private static InstructionType OnArithmetic(Token token) => token.Symbol switch
    {
        Tokens.ADD => InstructionType.Add,
        Tokens.SUB => InstructionType.Sub,
        Tokens.MUL => InstructionType.Mul,
        Tokens.DIV => InstructionType.Div,
        Tokens.MOD => InstructionType.Mod,
        Tokens.POW => InstructionType.Pow,

        _ => throw ProgramError("Cannot emit arithmetic!", token.Line)
    };

    private static InstructionType OnComparison(Token token) => token.Symbol switch
    {
        Tokens.OR => InstructionType.Or,
        Tokens.AND => InstructionType.And,
        Tokens.LT => InstructionType.Lt,
        Tokens.GT => InstructionType.Gt,
        Tokens.LE => InstructionType.Le,
        Tokens.GE => InstructionType.Ge,
        Tokens.EQ => InstructionType.Eq,
        Tokens.NE => InstructionType.Ne,
        Tokens.NOT => InstructionType.Not,

        _ => throw ProgramError("Cannot emit comparison!", token.Line),
    };

    private static InstructionType OnFunction(Token token) => token.Symbol switch
    {
        Tokens.ABS => InstructionType.Abs,
        Tokens.SIGN => InstructionType.Sign,
        Tokens.COPY_SIGN => InstructionType.CopySign,

        Tokens.ROUND => InstructionType.Round,
        Tokens.TRUNC => InstructionType.Trunc,
        Tokens.FLOOR => InstructionType.Floor,
        Tokens.CEIL => InstructionType.Ceil,
        Tokens.CLAMP => InstructionType.Clamp,

        Tokens.MIN => InstructionType.Min,
        Tokens.MAX => InstructionType.Max,
        Tokens.MIN_MAGNITUDE => InstructionType.MinM,
        Tokens.MAX_MAGNITUDE => InstructionType.MaxM,

        Tokens.SQRT => InstructionType.Sqrt,
        Tokens.CBRT => InstructionType.Cbrt,

        Tokens.LOG => InstructionType.Log,
        Tokens.LOG_2 => InstructionType.Log2,
        Tokens.LOG_10 => InstructionType.Log10,
        Tokens.LOG_B => InstructionType.LogB,

        Tokens.SIN => InstructionType.Sin,
        Tokens.SINH => InstructionType.Sinh,
        Tokens.ASIN => InstructionType.Asin,
        Tokens.ASINH => InstructionType.Asinh,

        Tokens.COS => InstructionType.Cos,
        Tokens.COSH => InstructionType.Cosh,
        Tokens.ACOS => InstructionType.Acos,
        Tokens.ACOSH => InstructionType.Acosh,

        Tokens.TAN => InstructionType.Tan,
        Tokens.TANH => InstructionType.Tanh,
        Tokens.ATAN => InstructionType.Atan,
        Tokens.ATANH => InstructionType.Atanh,
        Tokens.ATAN2 => InstructionType.Atan2,

        Tokens.FUSED_MULTIPLY_ADD => InstructionType.FusedMultiplyAdd,
        Tokens.SCALE_B => InstructionType.ScaleB,

        _ => throw ProgramError("Cannot emit function!", token.Line),
    };

    #endregion

    private static EmitException ProgramError(string error)
    {
        return new EmitException(error);
    }

    private static EmitException ProgramError(string error, uint line)
    {
        return new EmitException(error, line);
    }
}
