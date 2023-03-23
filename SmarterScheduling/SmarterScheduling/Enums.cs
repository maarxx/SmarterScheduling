using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SmarterScheduling
{
    public enum PawnState
    {
        SLEEP,
        JOY,
        WORK,
        MEDITATE,
        ANYTHING
    }

    public enum ImmuneSensitivity
    {
        SENSITIVE,
        BALANCED,
        BRUTAL
    }

    public enum ScheduleType
    {
        WORK,
        MAXMOOD
    }
}
