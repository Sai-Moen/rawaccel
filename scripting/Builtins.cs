namespace scripting;

/// <summary>
/// Provides Built-in curves as const strings.
/// </summary>
public static class Builtins
{
	// for reference, could be used in a regression test idk
    public const string STATIC = "[]{}";

    // change to c#11+ multiline string literals if we update to .NET8
	/// <summary>
	/// Arc mode by SaiMoen.
	/// </summary>
    public const string ARC =
        @"Arc mode by SaiMoen

			[

				Input_Offset := 0 >= 0;
				Limit := 4;
				Midpoint := 16 > 0;

			]

				pLimit := Limit - 1;

			{

				if (x > Input_Offset):
					x -= Input_Offset;
					y += (pLimit / x) * (x - Midpoint * atan(x / Midpoint));
				:

			}";
}
