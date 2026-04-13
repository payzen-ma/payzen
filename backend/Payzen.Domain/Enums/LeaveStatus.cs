using System;
using System.Collections.Generic;
using System.Text;

namespace Payzen.Domain.Enums;

/// <summary>
/// États possibles d'une demande de congé.
/// Suit le workflow : Pending → Approved/Rejected → Cancelled
/// </summary>
public enum LeaveStatus
{
    Pending,

    Approved,

    Rejected,

    Cancelled,

    PendingCancellation
}
public enum LeaveScope
{
    Global = 0,
    Company = 1
}

public enum LeaveAccrualMethod
{
    None = 0,
    Monthly = 1,
    Yearly = 2,
    ServiceBased = 3
}
public enum LeaveRequestStatus
{
    Draft = 0,
    Submitted = 1,
    Approved = 2,
    Rejected = 3,
    Cancelled = 4,
    Renounced = 5
}

public enum LeaveApprovalAction
{
    Submitted = 1,
    Approved = 2,
    Rejected = 3,
    Returned = 4,
    Cancelled = 5
}

public enum LeaveExemptionReasonType
{
    Holiday = 1,
    EmployeeAbsence = 2,
    AdminOverride = 3
}
