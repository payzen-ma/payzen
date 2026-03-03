namespace payzen_backend.Models.Permissions
{
    public class Roles
    {
        public int Id { get; set; }
        public required string Name { get; set; }
        public required string Description { get; set; }
        public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
        public DateTimeOffset? UpdatedAt { get; set; }
        public DateTimeOffset? DeletedAt { get; set; }
        public int CreatedBy { get; set; }
        public int? UpdatedBy { get; set; }
        public int? DeletedBy { get; set; }
    }
}
