using scripting;
using scripting.Interpreting;
using scripting.Script;
using System;
using System.Diagnostics;
using System.IO;

namespace scripting_tests;

/// <summary>
/// Command-Line Interface for the scripting class library.
/// </summary>
public static class CLI
{
    /// <summary>
    /// Entry point.
    /// </summary>
    /// <param name="args">Command-Line arguments.</param>
    public static void Main(string[] args)
    {
        if (args.Length == 0)
        {
            throw new ArgumentException("Path of the script is required!", nameof(args));
        }

        IInterpreter interpreter = Wrapper.LoadScriptFromFile(args[0]);
        Callbacks callbacks = interpreter.Callbacks;

        double[] xs = new double[Constants.LUT_POINTS_CAPACITY];
        if (args.Length >= 2)
        {
            int trim = 0;

            string inputs = File.ReadAllText(args[1]);
            foreach (string input in inputs.Split(';'))
            {
                if (xs.Length <= trim)
                {
                    throw new IndexOutOfRangeException("Too many points in the input file!");
                }

                xs[trim++] = double.Parse(input);
            }

            var temp = xs;
            xs = new double[trim];
            Array.Copy(temp, xs, trim);
        }
        else for (int i = 0; i < Constants.LUT_POINTS_CAPACITY; i++)
        {
            xs[i] = i + 1;
        }

        interpreter.Init();
        Trace.WriteLine(callbacks.Calculate(xs));
    }
}
