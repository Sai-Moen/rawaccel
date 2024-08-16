using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace scripting.Parsing;

/// <summary>
/// Saves a statement as an AST node (tagged union).
/// </summary>
/// <param name="Tag">Tag</param>
/// <param name="Union">Union</param>
public readonly record struct ASTNode(ASTTag Tag, ASTUnion Union) : IASTNode
{
    public ASTAssign? Assign     => Tag == ASTTag.Assign ? Union.astAssign : null;
    public ASTIf? If             => Tag == ASTTag.If ? Union.astIf : null;
    public ASTWhile? While       => Tag == ASTTag.While ? Union.astWhile : null;
    public ASTFunction? Function => Tag == ASTTag.Function ? Union.astFunction : null;
    public ASTReturn? Return     => Tag == ASTTag.Return ? Union.astReturn : null;

    public static ASTNode Unwrap(IASTNode ast)
    {
        ASTTag tag = ast.Tag;
        ASTUnion union = new();
        switch (tag)
        {
            case ASTTag.Assign:
                union.astAssign = ast.Assign!;
                break;
            case ASTTag.If:
                union.astIf = ast.If!;
                break;
            case ASTTag.While:
                union.astWhile = ast.While!;
                break;
            case ASTTag.Function:
                union.astFunction = ast.Function!;
                break;
            case ASTTag.Return:
                union.astReturn = ast.Return!;
                break;
            default:
                throw new ArgumentException("Unsupported tag value!", nameof(ast));
        }
        return new(tag, union);
    }
}

/// <summary>
/// Union of all possible statements.
/// </summary>
[StructLayout(LayoutKind.Explicit)]
public struct ASTUnion
{
    [FieldOffset(0)] public ASTAssign astAssign;
    [FieldOffset(0)] public ASTIf astIf;
    [FieldOffset(0)] public ASTWhile astWhile;
    [FieldOffset(0)] public ASTFunction astFunction;
    [FieldOffset(0)] public ASTReturn astReturn;
}

/// <summary>
/// Struct-of-Arrays IBlock implementation.
/// </summary>
public class Block : IBlock
{
    private readonly List<ASTTag> tags;
    private readonly List<ASTUnion> unions;

    public Block()
    {
        tags = [];
        unions = [];
    }

    public Block(int capacity)
    {
        tags = new(capacity);
        unions = new(capacity);
    }

    public Block(IEnumerable<IASTNode> items)
        : this()
    {
        foreach (IASTNode ast in items)
            Add(ast);
    }

    public Block(IEnumerable<ASTNode> items)
        : this()
    {
        foreach (IASTNode ast in items)
            Add(ast);
    }

    public IASTNode this[int index]
    {
        get => new ASTNode(tags[index], unions[index]);
        set
        {
            ASTNode item = ASTNode.Unwrap(value);
            tags[index] = item.Tag;
            unions[index] = item.Union;
        }
    }

    // by convention, use tags as the 'main' list
    public int Count => tags.Count;

    public bool IsReadOnly => false;

    public void Add(IASTNode item)
    {
        Add(ASTNode.Unwrap(item));
    }

    public void Add(ASTNode item)
    {
        tags.Add(item.Tag);
        unions.Add(item.Union);
    }

    public void Clear()
    {
        tags.Clear();
        unions.Clear();
    }

    public bool Contains(IASTNode item)
    {
        return Contains(ASTNode.Unwrap(item));
    }

    public bool Contains(ASTNode item)
    {
        return IndexOf(item) != -1;
    }

    public void CopyTo(IASTNode[] array, int arrayIndex)
    {
        ArgumentNullException.ThrowIfNull(array);
        ArgumentOutOfRangeException.ThrowIfLessThan(arrayIndex, 0);

        int len = array.Length;
        if (Count > len - arrayIndex)
        {
            // only blame array if the elements can't fit for any arrayIndex
            string paramName = Count > len ? nameof(array) : nameof(arrayIndex);
            throw new ArgumentException("Not enough space for all elements!", paramName);
        }

        int i = arrayIndex;
        foreach (IASTNode ast in this)
            array[i++] = ast;
    }

    public IEnumerator<IASTNode> GetEnumerator()
    {
        for (int i = 0; i < Count; i++)
        {
            yield return this[i];
        }
    }

    public int IndexOf(IASTNode item)
    {
        for (int i = 0; i < Count; i++)
        {
            if (item == this[i])
                return i;
        }
        return -1;
    }

    public int IndexOf(ASTNode item)
    {
        for (int i = 0; i < Count; i++)
        {
            if (item == new ASTNode(tags[i], unions[i]))
                return i;
        }
        return -1;
    }

    public void Insert(int index, IASTNode item)
    {
        Insert(index, ASTNode.Unwrap(item));
    }

    public void Insert(int index, ASTNode item)
    {
        tags.Insert(index, item.Tag);
        unions.Insert(index, item.Union);
    }

    public bool Remove(IASTNode item)
    {
        return Remove(ASTNode.Unwrap(item));
    }

    public bool Remove(ASTNode item)
    {
        int index = IndexOf(item);
        if (index == -1)
            return false;

        RemoveAt(index);
        return true;
    }

    public void RemoveAt(int index)
    {
        tags.RemoveAt(index);
        unions.RemoveAt(index);
    }

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}
