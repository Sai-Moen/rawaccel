#pragma once

#include "rawaccel-base.hpp"

namespace rawaccel {

    struct arc_base {
        double ioffset;
        double limit;
        double midpoint;

        arc_base(const accel_args& args) :
            ioffset(args.input_offset),
            limit(args.limit - 1),
            midpoint(args.midpoint)
        {}
    };

    template <bool Gain> struct arc;

    template <>
    struct arc<LEGACY> : arc_base {
        arc(const accel_args& args) :
            arc_base(args)
        {
            midpoint *= midpoint; // only need squared midpoint in legacy
        }

        double operator()(double x, const accel_args&) const
        {
            double y = 1;
            if (x > ioffset)
            {
                x -= ioffset;
                y += limit / (1 + midpoint / (x * x));
            }
            return y;
        }
    };

    template <>
    struct arc<GAIN> : arc_base {
        using arc_base::arc_base;

        double operator()(double x, const accel_args&) const
        {
            double y = 1;
            if (x > ioffset)
            {
                x -= ioffset;
                y += (limit / x) * (x - midpoint * atan(x / midpoint));
            }
            return y;
        }
    };
}