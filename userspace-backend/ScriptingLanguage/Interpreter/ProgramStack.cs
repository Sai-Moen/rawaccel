using System;
using System.Collections.Generic;
using userspace_backend.ScriptingLanguage.Compiler.CodeGen;
using userspace_backend.ScriptingLanguage.Script;

namespace userspace_backend.ScriptingLanguage.Interpreter;

/// <summary>
/// Replacement for Stack that actually allows for indexing.
/// </summary>
public class ProgramStack : List<Number>
{
    public Number this[StackAddress index]
    {
        get => this[(Index)index];
        set => this[(Index)index] = value;
    }

    public void Push(Number number)
    {
        Add(number);
    }

    public Number Pop()
    {
        StackAddress last = Count - 1;
        Number result = this[last];
        RemoveAt(last.Address);
        return result;
    }
}
