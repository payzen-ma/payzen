using System;
using System.Collections.Generic;
using System.Text;

namespace Payzen.Domain.Enums;

public enum AbsenceDurationType
{
    FullDay = 1,
    HalfDay = 2,
    Hourly = 3,
}

public enum AbsenceStatus
{
    Draft = 0,
    Submitted = 1,
    Approved = 2,
    Rejected = 3,
    Cancelled = 4,
    Expired = 5,
}
