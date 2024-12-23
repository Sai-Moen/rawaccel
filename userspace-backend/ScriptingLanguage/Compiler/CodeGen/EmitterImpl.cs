using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using userspace_backend.ScriptingLanguage.Compiler.Parser;
using userspace_backend.ScriptingLanguage.Compiler.Tokenizer;
using userspace_backend.ScriptingLanguage.Script;

namespace userspace_backend.ScriptingLanguage.Compiler.CodeGen;

/// <summary>
/// Emits AST(s) into programs, which the interpreter can execute.
/// </summary>
[SuppressMessage("Style", "IDE0290:Use primary constructor", Justification = "Makes it unreadable")]
public class EmitterImpl : IEmitter
{
    private List<byte> byteCode = [];
    private Dictionary<Number, DataAddress> numberMap = [];

    private readonly IDictionary<string, MemoryAddress> assignmentAddresses;
    private readonly IDictionary<string, MemoryAddress> functionAddresses;

    private readonly Dictionary<string, StackAddress> tempFunctionArgs = [];

    public EmitterImpl(CompilerContext ctx, IDictionary<string, MemoryAddress> assignmentAddrs, IDictionary<string, MemoryAddress> functionAddrs)
    {
        context = ctx;
        assignmentAddresses = assignmentAddrs;
        functionAddresses = functionAddrs;
    }

    private readonly CompilerContext context;
    internal CompilerContext Context { get => context; }

    #region Methods

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
        int arity = 0;
        foreach (Token arg in args)
        {
            bool success = tempFunctionArgs.TryAdd(context.GetSymbol(arg), arity++);
            Debug.Assert(success, "Parser didn't check for duplicate function local names?");
        }
        Program program = Emit(code);
        program.Arity = arity;
        tempFunctionArgs.Clear();
        return program;
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
                    bool isCompound = op.Type == TokenType.Compound;
                    InstructionType modify = isCompound ? EmitCompoundAssignment(op) : default;

                    Token identifier = ast.Identifier;
                    TokenType type = identifier.Type;
                    switch (type)
                    {
                        case TokenType.Input:
                            EmitRegisterAssignment(isCompound, InstructionType.LoadIn, modify, InstructionType.StoreIn);
                            break;
                        case TokenType.Output:
                            EmitRegisterAssignment(isCompound, InstructionType.LoadOut, modify, InstructionType.StoreOut);
                            break;
                        case TokenType.Parameter:
                        case TokenType.Immutable:
                        case TokenType.Persistent:
                            EmitMemoryAssignment(
                                (byte[])assignmentAddresses[context.GetSymbol(identifier)],
                                isCompound, InstructionType.LoadPersistent, modify, InstructionType.StorePersistent);
                            break;
                        case TokenType.Impersistent:
                            EmitMemoryAssignment(
                                (byte[])assignmentAddresses[context.GetSymbol(identifier)],
                                isCompound, InstructionType.LoadImpersistent, modify, InstructionType.StoreImpersistent);
                            break;
                        case TokenType.FunctionLocal:
                            EmitMemoryAssignment(
                                (byte[])tempFunctionArgs[context.GetSymbol(identifier)],
                                isCompound, InstructionType.LoadStack, modify, InstructionType.StoreStack);
                            break;
                        default:
                            Debug.Fail("Unreachable: parser shouldn't allow this TokenType?");
                            break;
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
                    if (ast.Else.Length == 0)
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
                {
                    ASTReturn ast = union.astReturn;

                    Token[] expression = ast.Expression;
                    if (expression.Length > 0)
                    {
                        EmitExpression(expression);
                        AddInstruction(InstructionType.StoreOut);
                    }

                    AddInstruction(InstructionType.Return);
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
        TokenType type = token.Type;
        switch (type)
        {
            case TokenType.Number:
                Number number = Number.Parse(context.GetSymbol(token), token);
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
                MemoryAddress mAddress = assignmentAddresses[context.GetSymbol(token)];
                AddInstruction(type.MapToLoad(), (byte[])mAddress);
                break;
            case TokenType.Input:
                AddInstruction(InstructionType.LoadIn);
                break;
            case TokenType.Output:
                AddInstruction(InstructionType.LoadOut);
                break;
            case TokenType.Constant:
                AddInstruction(EmitConstant(token));
                break;
            case TokenType.Arithmetic:
                InstructionType arithmetic = EmitArithmetic(token);

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
                AddInstruction(EmitComparison(token));
                break;
            case TokenType.Function:
                MemoryAddress functionAddress = functionAddresses[context.GetSymbol(token)];
                AddInstruction(InstructionType.Call, (byte[])functionAddress);
                break;
            case TokenType.FunctionLocal:
                StackAddress stackAddress = tempFunctionArgs[context.GetSymbol(token)];
                AddInstruction(InstructionType.LoadStack, (byte[])stackAddress);
                break;
            case TokenType.MathFunction:
                AddInstruction(EmitMathFunction(token));
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
            byteCode[offset + i] = address[i];
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

    private void EmitRegisterAssignment(bool isCompound, InstructionType load, InstructionType modify, InstructionType store)
    {
        if (isCompound)
        {
            AddInstruction(load);
            AddInstruction(InstructionType.Swap);
            AddInstruction(modify);
        }
        AddInstruction(store);
    }

    private void EmitMemoryAssignment(byte[] address, bool isCompound, InstructionType load, InstructionType modify, InstructionType store)
    {
        if (isCompound)
        {
            AddInstruction(load, address);
            AddInstruction(InstructionType.Swap);
            AddInstruction(modify);
        }
        AddInstruction(store, address);
    }

    private static InstructionType EmitConstant(Token token)
    {
        Debug.Assert(token.Type == TokenType.Constant);
        return (ExtraIndexConstant)token.ExtraIndex switch
        {
            ExtraIndexConstant.Zero => InstructionType.LoadZero,
            ExtraIndexConstant.E => InstructionType.LoadE,
            ExtraIndexConstant.Pi => InstructionType.LoadPi,
            ExtraIndexConstant.Tau => InstructionType.LoadTau,
            ExtraIndexConstant.Capacity => InstructionType.LoadCapacity,

            _ => throw EmitError($"Unknown ExtraIndexConstant value: {token.ExtraIndex}", token)
        };
    }

    private static InstructionType EmitCompoundAssignment(Token token)
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

    private static InstructionType EmitArithmetic(Token token)
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

    private static InstructionType EmitComparison(Token token)
    {
        Debug.Assert(token.Type == TokenType.Comparison);
        return (ExtraIndexComparison)token.ExtraIndex switch
        {
            ExtraIndexComparison.Or => InstructionType.Or,
            ExtraIndexComparison.And => InstructionType.And,
            ExtraIndexComparison.LessThan => InstructionType.Lt,
            ExtraIndexComparison.GreaterThan => InstructionType.Gt,
            ExtraIndexComparison.LessThanOrEqual => InstructionType.Le,
            ExtraIndexComparison.GreaterThanOrEqual => InstructionType.Ge,
            ExtraIndexComparison.Equal => InstructionType.Eq,
            ExtraIndexComparison.NotEqual => InstructionType.Ne,
            ExtraIndexComparison.Not => InstructionType.Not,

            _ => throw EmitError($"Unknown ExtraIndexComparison value: {token.ExtraIndex}", token)
        };
    }

    private static InstructionType EmitMathFunction(Token token)
    {
        Debug.Assert(token.Type == TokenType.MathFunction);
        return (ExtraIndexMathFunction)token.ExtraIndex switch
        {
            ExtraIndexMathFunction.Abs => InstructionType.Abs,
            ExtraIndexMathFunction.Sign => InstructionType.Sign,
            ExtraIndexMathFunction.CopySign => InstructionType.CopySign,

            ExtraIndexMathFunction.Round => InstructionType.Round,
            ExtraIndexMathFunction.Trunc => InstructionType.Trunc,
            ExtraIndexMathFunction.Floor => InstructionType.Floor,
            ExtraIndexMathFunction.Ceil => InstructionType.Ceil,
            ExtraIndexMathFunction.Clamp => InstructionType.Clamp,

            ExtraIndexMathFunction.Min => InstructionType.Min,
            ExtraIndexMathFunction.Max => InstructionType.Max,
            ExtraIndexMathFunction.MinMagnitude => InstructionType.MinM,
            ExtraIndexMathFunction.MaxMagnitude => InstructionType.MaxM,

            ExtraIndexMathFunction.Sqrt => InstructionType.Sqrt,
            ExtraIndexMathFunction.Cbrt => InstructionType.Cbrt,

            ExtraIndexMathFunction.Log => InstructionType.Log,
            ExtraIndexMathFunction.Log2 => InstructionType.Log2,
            ExtraIndexMathFunction.Log10 => InstructionType.Log10,
            ExtraIndexMathFunction.LogB => InstructionType.LogB,
            ExtraIndexMathFunction.ILogB => InstructionType.ILogB,

            ExtraIndexMathFunction.Sin => InstructionType.Sin,
            ExtraIndexMathFunction.Sinh => InstructionType.Sinh,
            ExtraIndexMathFunction.Asin => InstructionType.Asin,
            ExtraIndexMathFunction.Asinh => InstructionType.Asinh,

            ExtraIndexMathFunction.Cos => InstructionType.Cos,
            ExtraIndexMathFunction.Cosh => InstructionType.Cosh,
            ExtraIndexMathFunction.Acos => InstructionType.Acos,
            ExtraIndexMathFunction.Acosh => InstructionType.Acosh,

            ExtraIndexMathFunction.Tan => InstructionType.Tan,
            ExtraIndexMathFunction.Tanh => InstructionType.Tanh,
            ExtraIndexMathFunction.Atan => InstructionType.Atan,
            ExtraIndexMathFunction.Atanh => InstructionType.Atanh,
            ExtraIndexMathFunction.Atan2 => InstructionType.Atan2,

            ExtraIndexMathFunction.FusedMultiplyAdd => InstructionType.FusedMultiplyAdd,
            ExtraIndexMathFunction.ScaleB => InstructionType.ScaleB,

            _ => throw EmitError($"Unknown ExtraIndexMathFunction value: {token.ExtraIndex}", token)
        };
    }

    #endregion

    private static EmitException EmitError(string error, Token token)
    {
        return new EmitException(error, token);
    }
}
