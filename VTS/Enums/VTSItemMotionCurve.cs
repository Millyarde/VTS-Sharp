using System;
using System.ComponentModel;

namespace VTS
{
    [Serializable]
    public enum VTSItemMotionCurve
    {
        [Description("linear")]
        LINEAR = 0,
        [Description("easeIn")]
        EASE_IN = 1,
        [Description("easeOut")]
        EASE_OUT = 2,
        [Description("easeBoth")]
        EASE_BOTH = 3,
        [Description("overshoot")]
        OVERSHOOT = 4,
        [Description("zip")]
        ZIP = 5
    }
}
