using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace userspace_backend.ScriptingLanguage.Compiler;

/// <summary>
/// Exception for errors related to emitting bytecode into a program.
/// </summary>
public sealed class EmitException : CompilationException
{
    public EmitException(string message)
        : base(message)
    { }

    public EmitException(string message, Token suspect)
        : base(message, suspect)
    { }
}

/// <summary>
/// Emits AST(s) into programs, which the interpreter can execute.
/// </summary>
public class Emitter(Context context)
{
    private readonly Context context = context;
    private List<byte> byteCode = [];
    private Dictionary<Number, DataAddress> numberMap = [];

    private readonly Dictionary<string, MemoryAddress> persistentAddresses = [];
    private readonly Dictionary<string, MemoryAddress> impersistentAddresses = [];
    private readonly Dictionary<string, MemoryAddress> functionAddresses = [];

    private readonly Dictionary<string, StackAddress> tempFunctionArgs = [];

    public int PersistentCount => persistentAddresses.Count;
    public int ImpersistentCount => impersistentAddresses.Count;

    public void AddParameter(string symbol)
    {
        persistentAddresses.Add(symbol, (MemoryAddress)persistentAddresses.Count);
    }

    public void AddAssign(Token assign)
    {
        Dictionary<string, MemoryAddress> assignAddresses = assign.Kind switch
        {
            TokenKind.Immutable or
            TokenKind.Persistent => persistentAddresses,
            TokenKind.Impersistent => impersistentAddresses,

            _ => throw EmitError("Cannot determine assignment mapping!", assign),
        };
        assignAddresses.Add(context.GetSymbol(assign), (MemoryAddress)assignAddresses.Count);
    }

    public void AddFunction(Token function)
    {
        functionAddresses.Add(context.GetSymbol(function), (MemoryAddress)functionAddresses.Count);
    }

    public Program Emit(IList<Token> code)
    {
        return EmitWithCallback(() => EmitExpression(code), code.Count);
    }

    public Program Emit(IList<ASTNode> code)
    {
        return EmitWithCallback(() => EmitBlock(code), code.Count);
    }

    public Program EmitFunction(IList<Token> args, IList<ASTNode> code)
    {
        StackAddress arity = 0;
        foreach (Token arg in args)
        {
            bool success = tempFunctionArgs.TryAdd(context.GetSymbol(arg), arity++);
            Debug.Assert(success, "Parser didn't check for duplicate function local names?");
        }
        Program program = Emit(code);
        program.Arity = (int)arity;
        tempFunctionArgs.Clear();
        return program;
    }

    private Program EmitWithCallback(Action callback, int estimatedAmount)
    {
        byteCode = new(estimatedAmount);
        AddInstruction(InstructionKind.Start);

        numberMap = [];

        callback();

        AddInstruction(InstructionKind.End);
        byte[] code = [.. byteCode];
        byteCode.Clear();

        Number[] data = new Number[numberMap.Count];
        foreach ((Number number, DataAddress dataAddress) in numberMap)
            data[dataAddress.ToIndex()] = number;
        numberMap.Clear();

        return new Program(code, data);
    }

    private void EmitBlock(IList<ASTNode> code)
    {
        foreach (ASTNode node in code)
            EmitStatement(node);
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
                    bool isCompound = op.Kind == TokenKind.Compound;
                    InstructionKind modify = isCompound ? EmitCompoundAssignment(op) : default;

                    Token identifier = ast.Identifier;
                    switch (identifier.Kind)
                    {
                        case TokenKind.Input:
                            EmitRegisterAssignment(isCompound, InstructionKind.LoadIn, modify, InstructionKind.StoreIn);
                            break;
                        case TokenKind.Output:
                            EmitRegisterAssignment(isCompound, InstructionKind.LoadOut, modify, InstructionKind.StoreOut);
                            break;
                        case TokenKind.Parameter:
                        case TokenKind.Immutable:
                        case TokenKind.Persistent:
                            EmitMemoryAssignment(
                                persistentAddresses[context.GetSymbol(identifier)].ToBytes(),
                                isCompound, InstructionKind.LoadPersistent, modify, InstructionKind.StorePersistent);
                            break;
                        case TokenKind.Impersistent:
                            EmitMemoryAssignment(
                                impersistentAddresses[context.GetSymbol(identifier)].ToBytes(),
                                isCompound, InstructionKind.LoadImpersistent, modify, InstructionKind.StoreImpersistent);
                            break;
                        case TokenKind.FunctionLocal:
                            EmitMemoryAssignment(
                                tempFunctionArgs[context.GetSymbol(identifier)].ToBytes(),
                                isCompound, InstructionKind.LoadStack, modify, InstructionKind.StoreStack);
                            break;
                        default:
                            Debug.Fail("Unreachable: parser shouldn't allow this TokenKind?");
                            break;
                    }
                }
                break;
            case ASTTag.If:
                {
                    ASTIf ast = union.astIf;

                    EmitExpression(ast.Condition);

                    CodeAddress ifJumpTargetIndex = AddDefaultJump(InstructionKind.Jz);
                    EmitBlock(ast.If);

                    CodeAddress ifJumpTarget;
                    if (ast.Else.Length == 0)
                    {
                        ifJumpTarget = (CodeAddress)(byteCode.Count - 1);
                    }
                    else
                    {
                        CodeAddress elseJumpTargetIndex = AddDefaultJump(InstructionKind.Jmp);
                        ifJumpTarget = (CodeAddress)(byteCode.Count - 1);
                        EmitBlock(ast.Else);
                        CodeAddress elseJumpTarget = (CodeAddress)(byteCode.Count - 1);
                        SetAddress(elseJumpTargetIndex, elseJumpTarget.ToBytes());
                    }
                    SetAddress(ifJumpTargetIndex, ifJumpTarget.ToBytes());
                }
                break;
            case ASTTag.While:
                {
                    ASTWhile ast = union.astWhile;

                    CodeAddress loopJumpTarget = (CodeAddress)(byteCode.Count - 1);
                    EmitExpression(ast.Condition);

                    CodeAddress whileJumpTargetIndex = AddDefaultJump(InstructionKind.Jz);
                    EmitBlock(ast.While);

                    AddInstruction(InstructionKind.Jmp, loopJumpTarget.ToBytes());
                    CodeAddress whileJumpTarget = (CodeAddress)(byteCode.Count - 1);
                    SetAddress(whileJumpTargetIndex, whileJumpTarget.ToBytes());
                }
                break;
            case ASTTag.Return:
                {
                    ASTReturn ast = union.astReturn;

                    Token[] expression = ast.Expression;
                    if (expression.Length > 0)
                    {
                        EmitExpression(expression);
                        AddInstruction(InstructionKind.StoreOut);
                    }

                    AddInstruction(InstructionKind.Return);
                }
                break;
            default:
                Debug.Fail("Unreachable: passed wacky AST tag into this function?");
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
        switch (token.Kind)
        {
            case TokenKind.Zero:
                AddInstruction(InstructionKind.LoadZero);
                break;
            case TokenKind.Number:
                Number number = Number.Parse(context.GetSymbol(token), token);
                if (!numberMap.TryGetValue(number, out DataAddress dataAddress))
                {
                    dataAddress = (DataAddress)numberMap.Count;
                    numberMap.Add(number, dataAddress);
                }
                AddInstruction(InstructionKind.LoadNumber, dataAddress.ToBytes());
                break;
            case TokenKind.Parameter:
            case TokenKind.Immutable:
            case TokenKind.Persistent:
                MemoryAddress persistentAddress = persistentAddresses[context.GetSymbol(token)];
                AddInstruction(InstructionKind.LoadPersistent, persistentAddress.ToBytes());
                break;
            case TokenKind.Impersistent:
                MemoryAddress impersistentAddress = impersistentAddresses[context.GetSymbol(token)];
                AddInstruction(InstructionKind.LoadImpersistent, impersistentAddress.ToBytes());
                break;
            case TokenKind.Input:
                AddInstruction(InstructionKind.LoadIn);
                break;
            case TokenKind.Output:
                AddInstruction(InstructionKind.LoadOut);
                break;
            case TokenKind.Constant:
                AddInstruction(EmitConstant(token));
                break;
            case TokenKind.Arithmetic:
                InstructionKind arithmetic = EmitArithmetic(token);

                // attempt to convert [...LoadE, Pow...] to [...Exp...]
                if (arithmetic == InstructionKind.Pow && byteCode.Count > 0)
                {
                    InstructionKind prev = (InstructionKind)byteCode[^1];
                    if (prev == InstructionKind.LoadE)
                    {
                        byteCode[^1] = (byte)InstructionKind.Exp;
                        break;
                    }
                }

                AddInstruction(arithmetic);
                break;
            case TokenKind.Comparison:
                AddInstruction(EmitComparison(token));
                break;
            case TokenKind.FunctionName:
                MemoryAddress functionAddress = functionAddresses[context.GetSymbol(token)];
                AddInstruction(InstructionKind.Call, functionAddress.ToBytes());
                break;
            case TokenKind.FunctionLocal:
                StackAddress stackAddress = tempFunctionArgs[context.GetSymbol(token)];
                AddInstruction(InstructionKind.LoadStack, stackAddress.ToBytes());
                break;
            case TokenKind.MathFunction:
                AddInstruction(EmitMathFunction(token));
                break;
            default:
                throw EmitError("Cannot emit token!", token);
        }
    }

    #region ByteCode Helpers

    private void AddInstruction(InstructionKind instructionKind)
    {
        Debug.Assert(instructionKind.AddressLength() == 0);

        byteCode.Add((byte)instructionKind);
    }

    private void AddInstruction(InstructionKind instructionKind, ReadOnlySpan<byte> address)
    {
        Debug.Assert(instructionKind.AddressLength() == address.Length);

        byteCode.Add((byte)instructionKind);
        byteCode.AddRange(address);
    }

    private void SetAddress(CodeAddress start, ReadOnlySpan<byte> address)
    {
        for (int i = 0; i < address.Length; i++)
            byteCode[(int)start + i] = address[i];
    }

    private CodeAddress AddDefaultJump(InstructionKind jump)
    {
        Debug.Assert(jump.IsBranch());

        ReadOnlySpan<byte> address = default(CodeAddress).ToBytes();
        AddInstruction(jump, address);
        return (CodeAddress)(byteCode.Count - address.Length); // index of jump target address
    }

    #endregion

    #region Emit Helpers

    private void EmitRegisterAssignment(
        bool isCompound,
        InstructionKind load, InstructionKind modify, InstructionKind store)
    {
        if (isCompound)
        {
            AddInstruction(load);
            AddInstruction(InstructionKind.Swap);
            AddInstruction(modify);
        }
        AddInstruction(store);
    }

    private void EmitMemoryAssignment(
        ReadOnlySpan<byte> address, bool isCompound,
        InstructionKind load, InstructionKind modify, InstructionKind store)
    {
        if (isCompound)
        {
            AddInstruction(load, address);
            AddInstruction(InstructionKind.Swap);
            AddInstruction(modify);
        }
        AddInstruction(store, address);
    }

    private static InstructionKind EmitConstant(Token token)
    {
        Debug.Assert(token.Kind == TokenKind.Constant);
        return (ExtraIndexConstant)token.ExtraIndex switch
        {
            ExtraIndexConstant.Capacity => InstructionKind.LoadCapacity,
            ExtraIndexConstant.E => InstructionKind.LoadE,
            ExtraIndexConstant.Pi => InstructionKind.LoadPi,
            ExtraIndexConstant.Tau => InstructionKind.LoadTau,

            _ => throw EmitError($"Unknown ExtraIndexConstant value: {token.ExtraIndex}", token)
        };
    }

    private static InstructionKind EmitCompoundAssignment(Token token)
    {
        Debug.Assert(token.Kind == TokenKind.Compound);
        return (ExtraIndexCompound)token.ExtraIndex switch
        {
            ExtraIndexCompound.Add => InstructionKind.Add,
            ExtraIndexCompound.Sub => InstructionKind.Sub,
            ExtraIndexCompound.Mul => InstructionKind.Mul,
            ExtraIndexCompound.Div => InstructionKind.Div,
            ExtraIndexCompound.Mod => InstructionKind.Mod,
            ExtraIndexCompound.Pow => InstructionKind.Pow,

            _ => throw EmitError($"Unknown ExtraIndexCompound value: {token.ExtraIndex}", token)
        };
    }

    private static InstructionKind EmitArithmetic(Token token)
    {
        Debug.Assert(token.Kind == TokenKind.Arithmetic);
        return (ExtraIndexArithmetic)token.ExtraIndex switch
        {
            ExtraIndexArithmetic.Add => InstructionKind.Add,
            ExtraIndexArithmetic.Sub => InstructionKind.Sub,
            ExtraIndexArithmetic.Mul => InstructionKind.Mul,
            ExtraIndexArithmetic.Div => InstructionKind.Div,
            ExtraIndexArithmetic.Mod => InstructionKind.Mod,
            ExtraIndexArithmetic.Pow => InstructionKind.Pow,

            _ => throw EmitError($"Unknown ExtraIndexArithmetic value: {token.ExtraIndex}", token)
        };
    }

    private static InstructionKind EmitComparison(Token token)
    {
        Debug.Assert(token.Kind == TokenKind.Comparison);
        return (ExtraIndexComparison)token.ExtraIndex switch
        {
            ExtraIndexComparison.Or => InstructionKind.Or,
            ExtraIndexComparison.And => InstructionKind.And,
            ExtraIndexComparison.LessThan => InstructionKind.Lt,
            ExtraIndexComparison.GreaterThan => InstructionKind.Gt,
            ExtraIndexComparison.LessThanOrEqual => InstructionKind.Le,
            ExtraIndexComparison.GreaterThanOrEqual => InstructionKind.Ge,
            ExtraIndexComparison.Equal => InstructionKind.Eq,
            ExtraIndexComparison.NotEqual => InstructionKind.Ne,
            ExtraIndexComparison.Not => InstructionKind.Not,

            _ => throw EmitError($"Unknown ExtraIndexComparison value: {token.ExtraIndex}", token)
        };
    }

    private static InstructionKind EmitMathFunction(Token token)
    {
        Debug.Assert(token.Kind == TokenKind.MathFunction);
        return (ExtraIndexMathFunction)token.ExtraIndex switch
        {
            ExtraIndexMathFunction.Abs => InstructionKind.Abs,
            ExtraIndexMathFunction.Sign => InstructionKind.Sign,
            ExtraIndexMathFunction.CopySign => InstructionKind.CopySign,

            ExtraIndexMathFunction.Round => InstructionKind.Round,
            ExtraIndexMathFunction.Trunc => InstructionKind.Trunc,
            ExtraIndexMathFunction.Floor => InstructionKind.Floor,
            ExtraIndexMathFunction.Ceil => InstructionKind.Ceil,
            ExtraIndexMathFunction.Clamp => InstructionKind.Clamp,

            ExtraIndexMathFunction.Min => InstructionKind.Min,
            ExtraIndexMathFunction.Max => InstructionKind.Max,
            ExtraIndexMathFunction.MinMagnitude => InstructionKind.MinM,
            ExtraIndexMathFunction.MaxMagnitude => InstructionKind.MaxM,

            ExtraIndexMathFunction.Sqrt => InstructionKind.Sqrt,
            ExtraIndexMathFunction.Cbrt => InstructionKind.Cbrt,

            ExtraIndexMathFunction.Log => InstructionKind.Log,
            ExtraIndexMathFunction.Log2 => InstructionKind.Log2,
            ExtraIndexMathFunction.Log10 => InstructionKind.Log10,
            ExtraIndexMathFunction.LogB => InstructionKind.LogB,
            ExtraIndexMathFunction.ILogB => InstructionKind.ILogB,

            ExtraIndexMathFunction.Sin => InstructionKind.Sin,
            ExtraIndexMathFunction.Sinh => InstructionKind.Sinh,
            ExtraIndexMathFunction.Asin => InstructionKind.Asin,
            ExtraIndexMathFunction.Asinh => InstructionKind.Asinh,

            ExtraIndexMathFunction.Cos => InstructionKind.Cos,
            ExtraIndexMathFunction.Cosh => InstructionKind.Cosh,
            ExtraIndexMathFunction.Acos => InstructionKind.Acos,
            ExtraIndexMathFunction.Acosh => InstructionKind.Acosh,

            ExtraIndexMathFunction.Tan => InstructionKind.Tan,
            ExtraIndexMathFunction.Tanh => InstructionKind.Tanh,
            ExtraIndexMathFunction.Atan => InstructionKind.Atan,
            ExtraIndexMathFunction.Atanh => InstructionKind.Atanh,
            ExtraIndexMathFunction.Atan2 => InstructionKind.Atan2,

            ExtraIndexMathFunction.FusedMultiplyAdd => InstructionKind.FusedMultiplyAdd,
            ExtraIndexMathFunction.ScaleB => InstructionKind.ScaleB,

            _ => throw EmitError($"Unknown ExtraIndexMathFunction value: {token.ExtraIndex}", token)
        };
    }

    #endregion

    private static EmitException EmitError(string error, Token token)
    {
        return new EmitException(error, token);
    }
}
