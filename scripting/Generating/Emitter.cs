using scripting.Lexing;
using scripting.Script;
using scripting.Parsing;
using System;
using System.Collections.Generic;

namespace scripting.Generating;

/// <summary>
/// Emits AST(s) into programs, which the interpreter can execute.
/// </summary>
public class Emitter(IMemoryMap addresses) : IEmitter
{
    #region Fields

    private ByteCode byteCode = [];
    private Dictionary<Number, DataAddress> numberMap = [];

    #endregion

    #region Methods

    public Program Emit(ITokenList code)
    {
        return EmitWithCallback(() => EmitExpression(code), code.Count);
    }

    public Program Emit(IBlock code)
    {
        return EmitWithCallback(() => EmitBlock(code), code.Count);
    }

    private Program EmitWithCallback(Action callback, int estimatedAmount)
    {
        byteCode = new(estimatedAmount)
        {
            InstructionType.Start
        };
        numberMap = [];

        callback();

        byteCode.Add(InstructionType.End);
        byte[] code = [.. byteCode];
        byteCode.Clear();

        StaticData data = new(numberMap.Count);
        foreach ((Number number, DataAddress dAddress) in numberMap)
        {
            data[dAddress] = number;
        }
        numberMap.Clear();

        return new Program(code, data);
    }

    private void EmitBlock(IBlock code)
    {
        foreach (ASTNode node in code)
        {
            EmitStatement(node);
        }
    }

    private void EmitStatement(ASTNode stmnt)
    {
        ASTUnion union = stmnt.Union;
        switch (stmnt.Tag)
        {
            case ASTTag.Assign:
                {
                    ASTAssign ast = union.astAssign;

                    EmitExpression(ast.Initializer);

                    InstructionType modify = OnAssignment(ast.Operator);
                    bool isInline = modify.IsInline();

                    Token identifier = ast.Identifier;
                    if (identifier.Type == TokenType.Input)
                    {
                        OnSpecialAssignment(
                            byteCode, isInline,
                            InstructionType.LoadIn, modify, InstructionType.StoreIn);
                    }
                    else if (identifier.Type == TokenType.Output)
                    {
                        OnSpecialAssignment(
                            byteCode, isInline,
                            InstructionType.LoadOut, modify, InstructionType.StoreOut);
                    }
                    else
                    {
                        MemoryAddress address = addresses[identifier.Symbol];
                        if (isInline)
                        {
                            byteCode.Add(InstructionType.Load, (byte[])address);
                            byteCode.Add(InstructionType.Swap);
                            byteCode.Add(modify);
                        }
                        byteCode.Add(InstructionType.Store, (byte[])address);
                    }
                }
                break;
            case ASTTag.Return:
                byteCode.Add(InstructionType.Return);
                break;
            case ASTTag.If:
                {
                    ASTIf ast = union.astIf;

                    EmitExpression(ast.Condition);

                    CodeAddress ifJumpTargetIndex = byteCode.AddDefaultJump(InstructionType.Jz);
                    EmitBlock(ast.If);
                    CodeAddress ifJumpTarget;
                    if (ast.Else is null)
                    {
                        ifJumpTarget = byteCode.Last;
                    }
                    else
                    {
                        CodeAddress elseJumpTargetIndex = byteCode.AddDefaultJump(InstructionType.Jmp);
                        ifJumpTarget = byteCode.Last;
                        EmitBlock(ast.Else);
                        CodeAddress elseJumpTarget = byteCode.Last;
                        byteCode.SetAddress(elseJumpTargetIndex, (byte[])elseJumpTarget);
                    }
                    byteCode.SetAddress(ifJumpTargetIndex, (byte[])ifJumpTarget);
                }
                break;
            case ASTTag.While:
                {
                    ASTWhile ast = union.astWhile;

                    CodeAddress loopJumpTarget = byteCode.Last;
                    EmitExpression(ast.Condition);
                    
                    CodeAddress whileJumpTargetIndex = byteCode.AddDefaultJump(InstructionType.Jz);
                    EmitBlock(ast.While);

                    byteCode.Add(InstructionType.Jmp, (byte[])loopJumpTarget);
                    CodeAddress whileJumpTarget = byteCode.Last;
                    byteCode.SetAddress(whileJumpTargetIndex, (byte[])whileJumpTarget);
                }
                break;
        }
    }

    private void EmitExpression(ITokenList expr)
    {
        foreach (Token token in expr)
        {
            EmitToken(token);
        }
    }

    private void EmitToken(Token token)
    {
        switch (token.Type)
        {
            case TokenType.Number:
                Number number = Number.Parse(token.Symbol, token.Line);
                if (!numberMap.TryGetValue(number, out DataAddress dAddress))
                {
                    dAddress = (DataAddress)numberMap.Count;
                    numberMap.Add(number, dAddress);
                }
                byteCode.Add(InstructionType.LoadNumber, (byte[])dAddress);
                break;
            case TokenType.Parameter:
            case TokenType.Variable:
                MemoryAddress mAddress = addresses[token.Symbol];
                byteCode.Add(InstructionType.Load, (byte[])mAddress);
                break;
            case TokenType.Input:
                byteCode.Add(InstructionType.LoadIn);
                break;
            case TokenType.Output:
                byteCode.Add(InstructionType.LoadOut);
                break;
            case TokenType.Constant:
                byteCode.Add(OnConstant(token));
                break;
            case TokenType.Arithmetic:
                InstructionType arithmetic = OnArithmetic(token);

                // attempt to convert [...LoadE, Pow...] to [...Exp...]
                if (arithmetic == InstructionType.Pow && byteCode.Count > 0)
                {
                    if ((InstructionType)byteCode[^1] == InstructionType.LoadE)
                    {
                        byteCode[^1] = (byte)InstructionType.Exp;
                        break;
                    }
                }

                byteCode.Add(arithmetic);
                break;
            case TokenType.Comparison:
                byteCode.Add(OnComparison(token));
                break;
            case TokenType.Function:
                byteCode.Add(OnFunction(token));
                break;
            default:
                throw EmitError("Cannot emit token!", token.Line);
        }
    }

    #endregion

    #region Helpers

    private static void OnSpecialAssignment(
        ByteCode instructionList, bool isInline,
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

    private static InstructionType OnConstant(Token token) => token.Symbol switch
    {
        Tokens.ZERO => InstructionType.LoadZero,
        Tokens.CONST_E => InstructionType.LoadE,
        Tokens.CONST_PI => InstructionType.LoadPi,
        Tokens.CONST_TAU => InstructionType.LoadTau,
        Tokens.CONST_CAPACITY => InstructionType.LoadCapacity,

        _ => throw EmitError("Cannot emit constant!", token.Line),
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

        _ => throw EmitError("Cannot emit assignment!", token.Line),
    };

    private static InstructionType OnArithmetic(Token token) => token.Symbol switch
    {
        Tokens.ADD => InstructionType.Add,
        Tokens.SUB => InstructionType.Sub,
        Tokens.MUL => InstructionType.Mul,
        Tokens.DIV => InstructionType.Div,
        Tokens.MOD => InstructionType.Mod,
        Tokens.POW => InstructionType.Pow,

        _ => throw EmitError("Cannot emit arithmetic!", token.Line)
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

        _ => throw EmitError("Cannot emit comparison!", token.Line),
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

        _ => throw EmitError("Cannot emit function!", token.Line),
    };

    #endregion

    private static EmitException EmitError(string error, uint line)
    {
        return new EmitException(error, line);
    }
}
