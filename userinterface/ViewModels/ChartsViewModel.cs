using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Shapes;
using Avalonia.Data;
using Avalonia.Media;
using ReactiveUI;
using System;
using System.Collections.Generic;

namespace userinterface.ViewModels;

public sealed class ChartsViewModel : ViewModelBase
{
    public const string SensitivityXName = "SensitivityX";
    public const string GainXName = "GainX";
    public const string VelocityXName = "VelocityX";

    public const string SensitivityYName = "SensitivityY";
    public const string GainYName = "GainY";
    public const string VelocityYName = "VelocityY";

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

    public List<Point> PointsXSens { get; set; }
    public List<Point> PointsXGain { get; set; }
    public List<Point> PointsXVel { get; set; }
    public List<Point> PointsYSens { get; set; }
    public List<Point> PointsYGain { get; set; }
    public List<Point> PointsYVel { get; set; }

    public ChartsViewModel()
    {
        const int cap = 400;

        double[] dummyX = new double[cap];
        double[] dummyY = new double[cap];
        for (int i = 0; i < cap; i++)
        {
            dummyX[i] = i;
            dummyY[i] = Math.Sqrt(i) * 8;
        }

        PointsXSens = new(Generate(dummyX, dummyY));
        PointsXGain = new(Generate(dummyX, dummyY));
        PointsXVel = new(Generate(dummyX, dummyY));
        PointsYSens = new(Generate(dummyX, dummyY));
        PointsYGain = new(Generate(dummyX, dummyY));
        PointsYVel = new(Generate(dummyX, dummyY));
    }

    public void SetLastMouseMove(float x, float y)
    {
        LMMX = x;
        LMMY = Math.Sqrt(y) * 8;
    }

    private static Point[] Generate(double[] xs, double[] ys)
    {
        if (ys.Length > xs.Length) throw new System.Exception("Cannot have more ys than xs!");

        Point[] points = new Point[ys.Length];
        for (int i = 0; i < ys.Length; i++)
        {
            points[i] = new(xs[i], -ys[i]);
        }
        return points;
    }
}
