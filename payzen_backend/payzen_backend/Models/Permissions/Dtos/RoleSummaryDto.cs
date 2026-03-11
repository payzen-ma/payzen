using System.Text.Json.Serialization;

namespace payzen_backend.Models.Permissions.Dtos
{
    public class RoleSummaryDto
    {
        [JsonPropertyName("id")]
        public int Id { get; set; }

        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;

        [JsonPropertyName("userCount")]
        public int UserCount { get; set; }

        // Conserver la casse demand�e par le front ("UsersLength")
        [JsonPropertyName("UsersLength")]
        public int UsersLength { get; set; }
    }
}
