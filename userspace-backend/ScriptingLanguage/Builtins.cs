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

            if (x <= Input_Offset) { return; }

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

            const accel    := e ^ Growth_Rate;
            const motivity := 2 * log(Motivity);
            const midpoint := log(Midpoint);
            const constant := -motivity / 2;

            var denom := 0;

            fn legacy(speed)
            {
                denom := e ^ (accel * (midpoint - log(speed))) + 1;
                y := e ^ (motivity / denom + constant);
            }

            # calculation stuff
            let sum := 0;
            let a := 0;
            const partitions := 2;
            var interval := 0;
            var partition := 1;
            fn sigmoidSum(b)
            {
                interval := (b - a) / partitions;
                while (partition <= partitions)
                {
                    sum += legacy(a + partition * interval) * interval;
                    partition += 1;
                }
                a := b;
                y := sum;
            }

            # distribution stuff
            const rangeStart := -3;
            const rangeStop := 9;
            const rangeNum := 8;
            const rangeSize := (rangeStop - rangeStart) * rangeNum + 1;

            let inner := 0;
            let ep := rangeStart;
            let expScale := scaleb(1, ep) / rangeNum;

        {

            if (!Gain) {
                return legacy(x);
            }

            # still need to scale x because of log-log, not sure how?
            y := sigmoidSum(x) / x;

        }

        distribution(rangeSize)
        {

            if (ep < rangeStop)
            {
                if (inner < rangeNum)
                {
                    x := (inner + rangeNum) * expScale;
                    inner += 1;
                    return;
                }

                inner := 0;

                ep += 1;
                expScale := scaleb(1, ep) / rangeNum;
            }

            x := scaleb(1, ep);

        }
        """;
}
