using System;
using System.Collections.Generic;
using System.Text;

namespace Payzen.Domain.Enums
{
    public enum AttendanceStatus
    {
        Present = 1,
        Absent = 2,
        Holiday = 3,
        Leave = 4
    }

    public enum AttendanceSource
    {
        System = 1,
        Manual = 2
    }
}
