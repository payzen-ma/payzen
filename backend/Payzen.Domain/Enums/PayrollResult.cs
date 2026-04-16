using System;
using System.Collections.Generic;
using System.Text;

namespace Payzen.Domain.Enums
{
    public enum PayrollResultStatus
    {
        Pending = 0,
        Processing = 1,
        OK = 2,
        Error = 3,
        ManualReviewRequired = 4,
    }
}
