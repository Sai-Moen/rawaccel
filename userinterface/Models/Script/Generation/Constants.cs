using System.Diagnostics;

namespace userinterface.Models.Script.Generation
{
    public static class Constants
    {
        public const int CAPACITY = MemoryAddress.MaxValue + 1;
        public const int MAX_PARAMETERS = 8;
        public const int MAX_VARIABLES = CAPACITY - MAX_PARAMETERS;

        static Constants()
        {
            Debug.Assert(CAPACITY == MAX_PARAMETERS + MAX_VARIABLES);
        }
    }
}
