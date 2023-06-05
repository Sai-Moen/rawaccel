﻿using System;

namespace userinterface.Models.Script.Interaction
{
    public enum ScriptInterfaceType
    {
        Debug,
        Release,
    }

    public interface IScriptInterface
    {
        public void HandleException(TranspilerException exception);

        public void HandleMessage(string message);
    }

    public static class ScriptInterface
    {
        public static IScriptInterface Factory(ScriptInterfaceType ui) =>
            ui switch
            {
                ScriptInterfaceType.Debug   => new DebugInterface(),
                ScriptInterfaceType.Release => throw new NotImplementedException(),

                _ => throw new NotImplementedException(),
        };
    }
}