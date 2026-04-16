using System;
using System.Collections.Generic;
using System.Text;

namespace Payzen.Domain.Enums
{
    public enum PayrollResultStatus
    {
        Pending = 0,
        OK = 2,
        Error = 3,
        ManualReviewRequired = 4,
        Approved = 5,
    }
}
