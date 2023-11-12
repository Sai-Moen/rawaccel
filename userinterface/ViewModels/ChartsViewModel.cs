using Avalonia;
using Avalonia.Data;
using ReactiveUI;
using System;
using System.Collections.Generic;
using userinterface.Models.Charts;

namespace userinterface.ViewModels;

public sealed class ChartsViewModel : ViewModelBase
{
    public static double FromLeft => 0;
    public static double FromBottom => 0;

    private double lmmx = 0;
    public double LMMX
    {
        get => lmmx;
        set => this.RaiseAndSetIfChanged(ref lmmx, value);
    }

    private double lmmy = 0;
    public double LMMY
    {
        get => lmmy;
        set => this.RaiseAndSetIfChanged(ref lmmy, value);
    }

    public Chart[] Charts { get; } =
    {
        new("XSensitivity"),
        new("XGain"),
        new("XVelocity"),

        new("YSensitivity"),
        new("YGain"),
        new("YVelocity"),
    };

    public void SetLastMouseMove(float x, float y)
    {
        LMMX = x;
        LMMY = y;
    }
}
