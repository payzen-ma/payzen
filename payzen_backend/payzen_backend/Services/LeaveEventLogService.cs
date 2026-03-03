using payzen_backend.Data;
using payzen_backend.Models.Leave;

namespace payzen_backend.Services
{
    public class LeaveEventLogService
    {
        private readonly AppDbContext _db;

        public LeaveEventLogService(AppDbContext db)
        {
            _db = db;
        }

        /// <summary>
        /// Enregistre un événement lié aux congés
        /// </summary>
        public async Task LogEventAsync(
            int companyId,
            int? employeeId,
            int? leaveRequestId,
            string eventName,
            string? oldValue,
            string? newValue,
            int createdBy)
        {
            var auditLog = new LeaveAuditLog
            {
                CompanyId = companyId,
                EmployeeId = employeeId,
                LeaveRequestId = leaveRequestId,
                EventName = eventName,
                OldValue = oldValue,
                NewValue = newValue,
                CreatedAt = DateTimeOffset.UtcNow,
                CreatedBy = createdBy
            };

            _db.LeaveAuditLogs.Add(auditLog);
            await _db.SaveChangesAsync();
        }

        /// <summary>
        /// Enregistre un événement simple (sans employé ni demande)
        /// </summary>
        public async Task LogSimpleEventAsync(
            int companyId,
            string eventName,
            string? oldValue,
            string? newValue,
            int createdBy)
        {
            await LogEventAsync(companyId, null, null, eventName, oldValue, newValue, createdBy);
        }

        /// <summary>
        /// Enregistre un événement lié à un employé
        /// </summary>
        public async Task LogEmployeeEventAsync(
            int companyId,
            int employeeId,
            string eventName,
            string? oldValue,
            string? newValue,
            int createdBy)
        {
            await LogEventAsync(companyId, employeeId, null, eventName, oldValue, newValue, createdBy);
        }

        /// <summary>
        /// Enregistre un événement lié à une demande de congé
        /// </summary>
        public async Task LogLeaveRequestEventAsync(
            int companyId,
            int? employeeId,
            int leaveRequestId,
            string eventName,
            string? oldValue,
            string? newValue,
            int createdBy)
        {
            await LogEventAsync(companyId, employeeId, leaveRequestId, eventName, oldValue, newValue, createdBy);
        }

        /// <summary>
        /// Constantes pour les noms d'événements Leave
        /// </summary>
        public static class EventNames
        {
            // LeaveType events
            public const string LeaveTypeCreated = "LeaveType_Created";
            public const string LeaveTypeUpdated = "LeaveType_Updated";
            public const string LeaveTypeDeleted = "LeaveType_Deleted";
            public const string LeaveTypeActivated = "LeaveType_Activated";
            public const string LeaveTypeDeactivated = "LeaveType_Deactivated";

            // LeaveTypePolicy events
            public const string PolicyCreated = "LeaveTypePolicy_Created";
            public const string PolicyUpdated = "LeaveTypePolicy_Updated";
            public const string PolicyDeleted = "LeaveTypePolicy_Deleted";
            public const string PolicyEnabled = "LeaveTypePolicy_Enabled";
            public const string PolicyDisabled = "LeaveTypePolicy_Disabled";

            // LeaveTypeLegalRule events
            public const string LegalRuleCreated = "LegalRule_Created";
            public const string LegalRuleUpdated = "LegalRule_Updated";
            public const string LegalRuleDeleted = "LegalRule_Deleted";

            // LeaveRequest events
            public const string RequestCreated = "LeaveRequest_Created";
            public const string RequestUpdated = "LeaveRequest_Updated";
            public const string RequestDeleted = "LeaveRequest_Deleted";
            public const string RequestSubmitted = "LeaveRequest_Submitted";
            public const string RequestApproved = "LeaveRequest_Approved";
            public const string RequestRejected = "LeaveRequest_Rejected";
            public const string RequestCancelled = "LeaveRequest_Cancelled";
            public const string RequestRenounced = "LeaveRequest_Renounced";

            // LeaveBalance events
            public const string BalanceCreated = "LeaveBalance_Created";
            public const string BalanceUpdated = "LeaveBalance_Updated";
            public const string BalanceAdjusted = "LeaveBalance_Adjusted";
            public const string BalanceRecalculated = "LeaveBalance_Recalculated";

            // LeaveRequestExemption events
            public const string ExemptionAdded = "Exemption_Added";
            public const string ExemptionUpdated = "Exemption_Updated";
            public const string ExemptionRemoved = "Exemption_Removed";

            // LeaveRequestAttachment events
            public const string AttachmentUploaded = "Attachment_Uploaded";
            public const string AttachmentDeleted = "Attachment_Deleted";

            // LeaveCarryOverAgreement events
            public const string CarryOverCreated = "CarryOverAgreement_Created";
            public const string CarryOverUpdated = "CarryOverAgreement_Updated";
            public const string CarryOverDeleted = "CarryOverAgreement_Deleted";
        }
    }
}
