using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace userspace_backend.Model.EditableSettings
{
    public static class Parsers
    {
        public static StringParser StringParser = new StringParser();
        public static IntParser IntParser = new IntParser();
        public static DoubleParser DoubleParser = new DoubleParser();
    }

    public interface IParser<T>
    {
        bool TryParse(string input, out T parsedValue);
    }

    public class StringParser : IParser<string>
    {
        public bool TryParse(string input, out string parsedValue)
        {
            parsedValue = input;
            return true;
        }
    }

    public class IntParser : IParser<int>
    {
        public bool TryParse(string input, out int parsedValue)
        {
            if (int.TryParse(input, out int innerParsedValue))
            {
                parsedValue = innerParsedValue;
                return true;
            }

            parsedValue = default;
            return false;
        }
    }

    public class DoubleParser : IParser<double>
    {
        public bool TryParse(string input, out double parsedValue)
        {
            if (double.TryParse(input, out parsedValue))
            {
                return true;
            }

            parsedValue = default;
            return false;
        }
    }
}
