namespace scripting_tests.FormulaTests;

[TestClass]
public class PowerTest
{
    public const string POWER = @"
Gain output cap Power mode as a RawAccelScript.

[
    Scale := 1 > 0;
    Cap := 0 >= 0;
    Exponent := 0.05 > 0;
    Offset := 0 >= 0;
]

    offset_floor := pow(Offset / (Exponent + 1), 1 / Exponent) / Scale;
    constant := ;
    "; // TODO: implement variable evaluation order resolution
}
