namespace payzen_backend.Models.Event
{
    public class CompanyEventLog
    {
        public int Id { get; set; }
        public int employeeId { get; set; }
        public string eventName {get; set; } = null!;
        public string? oldValue { get; set; }
        public int? oldValueId { get; set; }
        public string? newValue { get; set; }
        public int? newValueId { get; set; }
        public DateTimeOffset createdAt { get; set; } = DateTimeOffset.UtcNow;
        public int createdBy { get; set; }
        public int companyId { get; set; }
    }
}
