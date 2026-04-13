using System;
using System.Collections.Generic;
using System.Text;

namespace Payzen.Domain.Enums.Dashboard
{
    public enum DashboardHrDeclarationType
    {
        CNSS = 1,
        AMO = 2,
        IR = 3,
        OTHER = 4
    }

    public enum DashboardHrDeclarationStatus
    {
        PENDING = 1,
        SUBMITTED = 2,
        REJECTED = 3,
        OVERDUE = 4
    }

    public enum DashboardHrMovementType
    {
        ENTRY = 1,
        EXIT = 2
    }
}
