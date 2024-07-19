﻿using System;
using System.Collections;
using System.Collections.Generic;

namespace scripting.Parsing;

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

    public Block(IBlock block)
    {
        tags = new(block.Count);
        unions = new(block.Count);
        foreach (ASTNode ast in block)
        {
            Add(ast);
        }
    }

    public ASTNode this[int index]
    {
        get => new(tags[index], unions[index]);
        set
        {
            tags[index] = value.Tag;
            unions[index] = value.Union;
        }
    }

    // by convention, use tags as the 'main' list
    public int Count => tags.Count;

    public bool IsReadOnly => false;

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

    public bool Contains(ASTNode item)
    {
        return IndexOf(item) != -1;
    }

    public void CopyTo(ASTNode[] array, int arrayIndex)
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
        foreach (ASTNode ast in this)
        {
            array[i++] = ast;
        }
    }

    public IEnumerator<ASTNode> GetEnumerator()
    {
        for (int i = 0; i < Count; i++)
        {
            yield return this[i];
        }
    }

    public int IndexOf(ASTNode item)
    {
        for (int i = 0; i < Count; i++)
        {
            if (this[i] == item) return i;
        }
        return -1;
    }

    public void Insert(int index, ASTNode item)
    {
        tags.Insert(index, item.Tag);
        unions.Insert(index, item.Union);
    }

    public bool Remove(ASTNode item)
    {
        int index = IndexOf(item);
        if (index == -1) return false;

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
