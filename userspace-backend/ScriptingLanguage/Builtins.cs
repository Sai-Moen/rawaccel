namespace userspace_backend.ScriptingLanguage;

/// <summary>
/// Provides Built-in curves as const strings.
/// </summary>
public static class Builtins
{
    /// <summary>
    /// No Accel, but as a script.
    /// </summary>
    public const string NO_ACCEL = "[]{}";

    /// <summary>
    /// Arc mode by SaiMoen.
    /// </summary>
    public const string ARC =
        """
        Arc mode by SaiMoen.

        [

            Input_Offset := 0  [0};
            Limit        := 4  [0};
            Midpoint     := 16 (0};

        ]

            const pLimit := Limit - 1;

        {

            if (x <= Input_Offset) { ret; }

            x -= Input_Offset;
            y += (pLimit / x) * (x - Midpoint * atan(x / Midpoint));

        }
        """;

    /// <summary>
    /// Preservation of RawAccel v1.6.1 Motivity.
    /// </summary>
    public const string MOTIVITY =
        """
        Preservation of RawAccel v1.6.1 Motivity.

        [

            Gain := false;

            Growth_Rate := 1   (0};
            Motivity    := 1.5 (1};
            Midpoint    := 5   (0};

        ]

            const accel := e ^ Growth_Rate;
            const motivity := 2 * log(Motivity);
            const midpoint := log(Midpoint);
            const constant := -motivity / 2;

        {
        }

        distribution
        {
        }
        """;
}
