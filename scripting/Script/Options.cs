using scripting.Common;
using scripting.Script.OptionImpl;
using scripting.Semantical;
using scripting.Syntactical;
using System.Collections.Generic;

namespace scripting.Script;

internal interface IOption
{
    Program Program { get; }
}

public partial class Options
{
    private readonly Dictionary<string, IOption> options = [];

    internal void Add(ParsedOption parsed, IDictionary<string, MemoryAddress> addresses)
    {
        IOption option = parsed.Name switch
        {
            Distribution.NAME => new Distribution(parsed, addresses),

            _ => throw new GenerationException("Unknown Option!")
        };
        options.Add(parsed.Name, option);
    }
}
