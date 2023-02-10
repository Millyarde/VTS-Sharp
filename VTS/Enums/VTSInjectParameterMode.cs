using System;
using System.ComponentModel;

namespace VTS
{
    [Serializable]
    internal enum VTSInjectParameterMode
    {
        [Description("set")]
        SET = 0,
        [Description("add")]
        ADD = 1
    }
}
