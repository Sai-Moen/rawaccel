#pragma once

#include "rawaccel-base.hpp"

namespace rawaccel {

    struct arc_base {
        double limit;
        double midpoint;
        double offset;

        arc_base(const accel_args& args) :
            limit(args.limit - 1),
            midpoint(args.midpoint),
            offset(args.input_offset)
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
            if (x <= offset) return 1;
            x -= offset;
            return 1 + limit / (1 + midpoint / (x * x));
        }
    };

    template <>
    struct arc<GAIN> : arc_base {
        using arc_base::arc_base;

        double operator()(double x, const accel_args&) const
        {
            if (x <= offset) return 1;
            x -= offset;
            return 1 + (limit / x) * (x - midpoint * atan(x / midpoint));
        }
    };
}