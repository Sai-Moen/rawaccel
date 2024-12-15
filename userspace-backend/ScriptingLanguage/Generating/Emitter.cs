using System;
using System.Collections.Generic;
using System.Diagnostics;
using userspace_backend.ScriptingLanguage.Lexing;
using userspace_backend.ScriptingLanguage.Parsing;
using userspace_backend.ScriptingLanguage.Script;

namespace userspace_backend.ScriptingLanguage.Generating;

/// <summary>
/// Emits AST(s) into programs, which the interpreter can execute.
/// </summary>
public class Emitter(
    IList<string> symbolSideTable,
    IDictionary<string, MemoryAddress> assignmentAddresses,
    IDictionary<string, MemoryAddress> functionAddresses)
    : IEmitter
{
    #region Fields

    private List<byte> byteCode = [];
    private Dictionary<Number, DataAddress> numberMap = [];

    #endregion

    #region Methods

    public Program Emit(IList<Token> code)
    {
        return EmitWithCallback(() => EmitExpression(code), code.Count);
    }

    public Program Emit(IList<IASTNode> code)
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
            data[dAddress] = number;
        numberMap.Clear();

        return new Program(code, data);
    }

    private void EmitBlock(IList<IASTNode> code)
    {
        foreach (IASTNode node in code)
            EmitStatement(ASTNode.Unwrap(node));
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

                    Token op = ast.Operator;
                    bool isCompound = op.Type == TokenType.Compound;
                    InstructionType modify = isCompound ? OnCompound(op) : default;

                    Token identifier = ast.Identifier;
                    TokenType type = identifier.Type;
                    if (type == TokenType.Input)
                    {
                        OnSpecialAssignment(isCompound, InstructionType.LoadIn, modify, InstructionType.StoreIn);
                    }
                    else if (type == TokenType.Output)
                    {
                        OnSpecialAssignment(isCompound, InstructionType.LoadOut, modify, InstructionType.StoreOut);
                    }
                    else
                    {
                        MemoryAddress address = assignmentAddresses[GetSymbol(identifier)];
                        if (isCompound)
                        {
                            InstructionType load = type.MapToLoad();
                            if (load == InstructionType.NoOp)
                            {
                                throw EmitError("Cannot map identifier's type to Load instruction!", identifier);
                            }

                            AddInstruction(load, (byte[])address);
                            AddInstruction(InstructionType.Swap);
                            AddInstruction(modify);
                        }
                        InstructionType store = type.MapToStore();
                        if (store == InstructionType.NoOp)
                            throw EmitError("Cannot map identifier's type to Store instruction!", identifier);

                        AddInstruction(store, (byte[])address);
                    }
                }
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
            case ASTTag.Return:
                AddInstruction(InstructionType.Return);
                break;
        }
    }

    private void EmitExpression(IList<Token> expr)
    {
        foreach (Token token in expr)
            EmitToken(token);
    }

    private void EmitToken(Token token)
    {
        TokenType type = token.Type;
        switch (type)
        {
            case TokenType.Number:
                Number number = Number.Parse(GetSymbol(token), token);
                if (!numberMap.TryGetValue(number, out DataAddress dAddress))
                {
                    dAddress = (DataAddress)numberMap.Count;
                    numberMap.Add(number, dAddress);
                }
                AddInstruction(InstructionType.LoadNumber, (byte[])dAddress);
                break;
            case TokenType.Parameter:
            case TokenType.Immutable:
            case TokenType.Persistent:
            case TokenType.Impersistent:
                MemoryAddress mAddress = assignmentAddresses[GetSymbol(token)];
                AddInstruction(type.MapToLoad(), (byte[])mAddress);
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
                MemoryAddress functionAddress = functionAddresses[GetSymbol(token)];
                AddInstruction(InstructionType.Call, (byte[])functionAddress);
                break;
            case TokenType.MathFunction:
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

    internal string GetSymbol(Token token)
    {
        uint index = (uint)token.SymbolIndex;
        if (index >= (uint)symbolSideTable.Count)
            throw EmitError($"SymbolIndex out of bounds: {index} >= {symbolSideTable.Count}", token);

        return symbolSideTable[(int)index];
    }

    private void OnSpecialAssignment(bool isCompound, InstructionType load, InstructionType modify, InstructionType store)
    {
        if (isCompound)
        {
            AddInstruction(load);
            AddInstruction(InstructionType.Swap);
            AddInstruction(modify);
        }
        AddInstruction(store);
    }

    private static InstructionType OnConstant(Token token)
    {
        Debug.Assert(token.Type == TokenType.Constant);
        return (ExtraIndexConstant)token.ExtraIndex switch
        {
            ExtraIndexConstant.Zero     => InstructionType.LoadZero,
            ExtraIndexConstant.E        => InstructionType.LoadE,
            ExtraIndexConstant.Pi       => InstructionType.LoadPi,
            ExtraIndexConstant.Tau      => InstructionType.LoadTau,
            ExtraIndexConstant.Capacity => InstructionType.LoadCapacity,

            _ => throw EmitError($"Unknown ExtraIndexConstant value: {token.ExtraIndex}", token)
        };
    }

    private static InstructionType OnCompound(Token token)
    {
        Debug.Assert(token.Type == TokenType.Compound);
        return (ExtraIndexCompound)token.ExtraIndex switch
        {
            ExtraIndexCompound.Add => InstructionType.Add,
            ExtraIndexCompound.Sub => InstructionType.Sub,
            ExtraIndexCompound.Mul => InstructionType.Mul,
            ExtraIndexCompound.Div => InstructionType.Div,
            ExtraIndexCompound.Mod => InstructionType.Mod,
            ExtraIndexCompound.Pow => InstructionType.Pow,

            _ => throw EmitError($"Unknown ExtraIndexCompound value: {token.ExtraIndex}", token)
        };
    }

    private static InstructionType OnArithmetic(Token token)
    {
        Debug.Assert(token.Type == TokenType.Arithmetic);
        return (ExtraIndexArithmetic)token.ExtraIndex switch
        {
            ExtraIndexArithmetic.Add => InstructionType.Add,
            ExtraIndexArithmetic.Sub => InstructionType.Sub,
            ExtraIndexArithmetic.Mul => InstructionType.Mul,
            ExtraIndexArithmetic.Div => InstructionType.Div,
            ExtraIndexArithmetic.Mod => InstructionType.Mod,
            ExtraIndexArithmetic.Pow => InstructionType.Pow,

            _ => throw EmitError($"Unknown ExtraIndexArithmetic value: {token.ExtraIndex}", token)
        };
    }

    private static InstructionType OnComparison(Token token)
    {
        Debug.Assert(token.Type == TokenType.Comparison);
        return (ExtraIndexComparison)token.ExtraIndex switch
        {
            ExtraIndexComparison.Or                 => InstructionType.Or,
            ExtraIndexComparison.And                => InstructionType.And,
            ExtraIndexComparison.LessThan           => InstructionType.Lt,
            ExtraIndexComparison.GreaterThan        => InstructionType.Gt,
            ExtraIndexComparison.LessThanOrEqual    => InstructionType.Le,
            ExtraIndexComparison.GreaterThanOrEqual => InstructionType.Ge,
            ExtraIndexComparison.Equal              => InstructionType.Eq,
            ExtraIndexComparison.NotEqual           => InstructionType.Ne,
            ExtraIndexComparison.Not                => InstructionType.Not,

            _ => throw EmitError($"Unknown ExtraIndexComparison value: {token.ExtraIndex}", token)
        };
    }

    private static InstructionType OnFunction(Token token)
    {
        Debug.Assert(token.Type == TokenType.MathFunction);
        return (ExtraIndexMathFunction)token.ExtraIndex switch
        {
            ExtraIndexMathFunction.Abs      => InstructionType.Abs,
            ExtraIndexMathFunction.Sign     => InstructionType.Sign,
            ExtraIndexMathFunction.CopySign => InstructionType.CopySign,

            ExtraIndexMathFunction.Round => InstructionType.Round,
            ExtraIndexMathFunction.Trunc => InstructionType.Trunc,
            ExtraIndexMathFunction.Floor => InstructionType.Floor,
            ExtraIndexMathFunction.Ceil  => InstructionType.Ceil,
            ExtraIndexMathFunction.Clamp => InstructionType.Clamp,

            ExtraIndexMathFunction.Min          => InstructionType.Min,
            ExtraIndexMathFunction.Max          => InstructionType.Max,
            ExtraIndexMathFunction.MinMagnitude => InstructionType.MinM,
            ExtraIndexMathFunction.MaxMagnitude => InstructionType.MaxM,

            ExtraIndexMathFunction.Sqrt => InstructionType.Sqrt,
            ExtraIndexMathFunction.Cbrt => InstructionType.Cbrt,

            ExtraIndexMathFunction.Log   => InstructionType.Log,
            ExtraIndexMathFunction.Log2  => InstructionType.Log2,
            ExtraIndexMathFunction.Log10 => InstructionType.Log10,
            ExtraIndexMathFunction.LogB  => InstructionType.LogB,
            ExtraIndexMathFunction.ILogB => InstructionType.ILogB,

            ExtraIndexMathFunction.Sin   => InstructionType.Sin,
            ExtraIndexMathFunction.Sinh  => InstructionType.Sinh,
            ExtraIndexMathFunction.Asin  => InstructionType.Asin,
            ExtraIndexMathFunction.Asinh => InstructionType.Asinh,

            ExtraIndexMathFunction.Cos   => InstructionType.Cos,
            ExtraIndexMathFunction.Cosh  => InstructionType.Cosh,
            ExtraIndexMathFunction.Acos  => InstructionType.Acos,
            ExtraIndexMathFunction.Acosh => InstructionType.Acosh,

            ExtraIndexMathFunction.Tan   => InstructionType.Tan,
            ExtraIndexMathFunction.Tanh  => InstructionType.Tanh,
            ExtraIndexMathFunction.Atan  => InstructionType.Atan,
            ExtraIndexMathFunction.Atanh => InstructionType.Atanh,
            ExtraIndexMathFunction.Atan2 => InstructionType.Atan2,

            ExtraIndexMathFunction.FusedMultiplyAdd => InstructionType.FusedMultiplyAdd,
            ExtraIndexMathFunction.ScaleB           => InstructionType.ScaleB,

            _ => throw EmitError($"Unknown ExtraIndexMathFunction value: {token.ExtraIndex}", token)
        };
    }

    #endregion

    private static EmitException EmitError(string error, Token token)
    {
        return new EmitException(error, token);
    }
}
