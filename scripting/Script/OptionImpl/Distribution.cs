using scripting.Script.OptionImpl;
using scripting.Semantical;
using scripting.Syntactical;
using System.Collections.Generic;

namespace scripting.Script.OptionImpl
{
    public class Distribution : IOption
    {
        public const string NAME = "distribution";

        internal Distribution(ParsedOption parsed, IDictionary<string, MemoryAddress> addresses)
        {
            // TODO
            // wire up all the things
            // validate args
            // create properties
        }

        public Program Program => throw new System.NotImplementedException();
    }
}

namespace scripting.Script
{
    public partial class Options
    {
        Distribution? Distribution => options.TryGetValue(Distribution.NAME, out IOption? value) ? (Distribution)value : null;
    }
}