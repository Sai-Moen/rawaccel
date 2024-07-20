﻿using scripting.Lexing;
using scripting.Script;
using scripting.Parsing;
using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace scripting.Generating;

/// <summary>
/// Emits AST(s) into programs, which the interpreter can execute.
/// </summary>
public class Emitter(IMemoryMap addresses) : IEmitter
{
    #region Fields

    private List<byte> byteCode = [];
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
        byteCode = new(estimatedAmount);
        AddInstruction(InstructionType.Start);

        numberMap = [];

        callback();

        AddInstruction(InstructionType.End);
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
                        OnSpecialAssignment(isInline, InstructionType.LoadIn, modify, InstructionType.StoreIn);
                    }
                    else if (identifier.Type == TokenType.Output)
                    {
                        OnSpecialAssignment(isInline, InstructionType.LoadOut, modify, InstructionType.StoreOut);
                    }
                    else
                    {
                        MemoryAddress address = addresses[identifier.Symbol];
                        if (isInline)
                        {
                            AddInstruction(InstructionType.Load, (byte[])address);
                            AddInstruction(InstructionType.Swap);
                            AddInstruction(modify);
                        }
                        AddInstruction(InstructionType.Store, (byte[])address);
                    }
                }
                break;
            case ASTTag.Return:
                AddInstruction(InstructionType.Return);
                break;
            case ASTTag.If:
                {
                    ASTIf ast = union.astIf;

                    EmitExpression(ast.Condition);

                    CodeAddress ifJumpTargetIndex = AddDefaultJump(InstructionType.Jz);
                    EmitBlock(ast.If);
                    CodeAddress ifJumpTarget;
                    if (ast.Else is null)
                    {
                        ifJumpTarget = byteCode.Count - 1;
                    }
                    else
                    {
                        CodeAddress elseJumpTargetIndex = AddDefaultJump(InstructionType.Jmp);
                        ifJumpTarget = byteCode.Count - 1;
                        EmitBlock(ast.Else);
                        CodeAddress elseJumpTarget = byteCode.Count - 1;
                        SetAddress(elseJumpTargetIndex, (byte[])elseJumpTarget);
                    }
                    SetAddress(ifJumpTargetIndex, (byte[])ifJumpTarget);
                }
                break;
            case ASTTag.While:
                {
                    ASTWhile ast = union.astWhile;

                    CodeAddress loopJumpTarget = byteCode.Count - 1;
                    EmitExpression(ast.Condition);
                    
                    CodeAddress whileJumpTargetIndex = AddDefaultJump(InstructionType.Jz);
                    EmitBlock(ast.While);

                    AddInstruction(InstructionType.Jmp, (byte[])loopJumpTarget);
                    CodeAddress whileJumpTarget = byteCode.Count - 1;
                    SetAddress(whileJumpTargetIndex, (byte[])whileJumpTarget);
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
                AddInstruction(InstructionType.LoadNumber, (byte[])dAddress);
                break;
            case TokenType.Parameter:
            case TokenType.Variable:
                MemoryAddress mAddress = addresses[token.Symbol];
                AddInstruction(InstructionType.Load, (byte[])mAddress);
                break;
            case TokenType.Input:
                AddInstruction(InstructionType.LoadIn);
                break;
            case TokenType.Output:
                AddInstruction(InstructionType.LoadOut);
                break;
            case TokenType.Constant:
                AddInstruction(OnConstant(token));
                break;
            case TokenType.Arithmetic:
                InstructionType arithmetic = OnArithmetic(token);

                // attempt to convert [...LoadE, Pow...] to [...Exp...]
                if (arithmetic == InstructionType.Pow && byteCode.Count > 0)
                {
                    InstructionType prev = (InstructionType)byteCode[^1];
                    if (prev == InstructionType.LoadE)
                    {
                        byteCode[^1] = (byte)InstructionType.Exp;
                        break;
                    }
                }

                AddInstruction(arithmetic);
                break;
            case TokenType.Comparison:
                AddInstruction(OnComparison(token));
                break;
            case TokenType.Function:
                AddInstruction(OnFunction(token));
                break;
            default:
                throw EmitError("Cannot emit token!", token);
        }
    }

    #endregion

    #region ByteCode Helpers

    private void AddInstruction(InstructionType type)
    {
        Debug.Assert(type.AddressLength() == 0);

        byteCode.Add((byte)type);
    }

    private void AddInstruction(InstructionType type, byte[] address)
    {
        Debug.Assert(type.AddressLength() == address.Length);

        byteCode.Add((byte)type);
        byteCode.AddRange(address);
    }

    private void SetAddress(CodeAddress start, byte[] address)
    {
        int offset = start.Address;
        for (int i = 0; i < address.Length; i++)
        {
            byteCode[offset + i] = address[i];
        }
    }

    private CodeAddress AddDefaultJump(InstructionType jump)
    {
        Debug.Assert(jump.IsBranch());

        byte[] address = (byte[])default(CodeAddress);
        AddInstruction(jump, address);
        return byteCode.Count - address.Length; // index of jump target address
    }

    #endregion

    #region Emit Helpers

    private void OnSpecialAssignment(bool isInline, InstructionType load, InstructionType modify, InstructionType store)
    {
        if (isInline)
        {
            AddInstruction(load);
            AddInstruction(InstructionType.Swap);
            AddInstruction(modify);
        }
        AddInstruction(store);
    }

    private static InstructionType OnAssignment(Token token) => token.Symbol switch
    {
        Tokens.ASSIGN => InstructionType.Store,
        Tokens.IADD => InstructionType.Add,
        Tokens.ISUB => InstructionType.Sub,
        Tokens.IMUL => InstructionType.Mul,
        Tokens.IDIV => InstructionType.Div,
        Tokens.IMOD => InstructionType.Mod,
        Tokens.IPOW => InstructionType.Pow,

        _ => throw EmitError("Cannot emit assignment!", token),
    };

    private static InstructionType OnConstant(Token token) => token.Symbol switch
    {
        Tokens.ZERO => InstructionType.LoadZero,
        Tokens.CONST_E => InstructionType.LoadE,
        Tokens.CONST_PI => InstructionType.LoadPi,
        Tokens.CONST_TAU => InstructionType.LoadTau,
        Tokens.CONST_CAPACITY => InstructionType.LoadCapacity,

        _ => throw EmitError("Cannot emit constant!", token),
    };

    private static InstructionType OnArithmetic(Token token) => token.Symbol switch
    {
        Tokens.ADD => InstructionType.Add,
        Tokens.SUB => InstructionType.Sub,
        Tokens.MUL => InstructionType.Mul,
        Tokens.DIV => InstructionType.Div,
        Tokens.MOD => InstructionType.Mod,
        Tokens.POW => InstructionType.Pow,

        _ => throw EmitError("Cannot emit arithmetic!", token)
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

        _ => throw EmitError("Cannot emit comparison!", token),
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

        _ => throw EmitError("Cannot emit function!", token),
    };

    #endregion

    private static EmitException EmitError(string error, Token token)
    {
        return new EmitException(error, token.Line);
    }
}
