using payzen_backend.Data;
using payzen_backend.Models.Event;

namespace payzen_backend.Services
{
    public class CompanyEventLogService
    {
        private readonly AppDbContext _db;

        public CompanyEventLogService(AppDbContext db)
        {
            _db = db;
        }

        /// <summary>
        /// Enregistre un événement de modification pour une entreprise
        /// </summary>
        public async Task LogEventAsync(
            int companyId,
            string eventName,
            string? oldValue,
            int? oldValueId,
            string? newValue,
            int? newValueId,
            int createdBy)
        {
            var eventLog = new CompanyEventLog
            {
                companyId = companyId,
                eventName = eventName,
                oldValue = oldValue,
                oldValueId = oldValueId,
                newValue = newValue,
                newValueId = newValueId,
                createdAt = DateTimeOffset.UtcNow,
                createdBy = createdBy
            };

            _db.CompanyEventLogs.Add(eventLog);
            await _db.SaveChangesAsync();
        }

        /// <summary>
        /// Enregistre un événement simple (sans ID)
        /// </summary>
        public async Task LogSimpleEventAsync(
            int companyId,
            string eventName,
            string? oldValue,
            string? newValue,
            int createdBy)
        {
            await LogEventAsync(companyId, eventName, oldValue, null, newValue, null, createdBy);
        }

        /// <summary>
        /// Enregistre un événement de relation (avec ID + libellé)
        /// </summary>
        public async Task LogRelationEventAsync(
            int companyId,
            string eventName,
            int? oldValueId,
            string? oldValueName,
            int? newValueId,
            string? newValueName,
            int createdBy)
        {
            await LogEventAsync(
                companyId,
                eventName,
                oldValueName,
                oldValueId,
                newValueName,
                newValueId,
                createdBy);
        }

        /// <summary>
        /// Constantes pour les noms d'événements entreprise
        /// </summary>
        public static class EventNames
        {
            // Identité entreprise
            public const string CompanyCreated = "Company_Created";
            public const string CompanyUpdated = "Company_Updated";
            public const string CompanyDeleted = "Company_Deleted";

            public const string CompanyNameChanged = "CompanyName_Changed";
            public const string LegalFormChanged = "LegalForm_Changed";
            public const string FoundingDateChanged = "FoundingDate_Changed";

            // Coordonnées
            public const string EmailChanged = "Email_Changed";
            public const string PhoneChanged = "Phone_Changed";
            public const string AddressChanged = "Address_Changed";
            public const string CityChanged = "City_Changed";
            public const string CountryChanged = "Country_Changed";

            // Statut
            public const string StatusChanged = "Status_Changed";
            public const string LicenceChanged = "Licence_Changed";

            // Identifiants légaux & fiscaux
            public const string IceNumberChanged = "ICE_Changed";
            public const string IfNumberChanged = "IF_Changed";
            public const string RcNumberChanged = "RC_Changed";
            public const string CnssNumberChanged = "CNSS_Changed";

            // Ajouts pour correspondre aux event names utilisés dans CompanyController
            public const string IceChanged = "Ice_Changed";
            public const string IfChanged = "If_Changed";
            public const string RcChanged = "Rc_Changed";
            public const string CnssChanged = "Cnss_Changed";
            public const string IsCabinetExpertChanged = "IsCabinetExpert_Changed";

            // Relations
            public const string AccountantChanged = "Accountant_Changed";
            public const string CabinetLinked = "Cabinet_Linked";
            public const string CabinetUnlinked = "Cabinet_Unlinked";

            // JobPosition events (CRUD)
            public const string JobPositionCreated = "JobPosition_Created";
            public const string JobPositionUpdated = "JobPosition_Updated";
            public const string JobPositionDeleted = "JobPosition_Deleted";

            // Webi Site
            public const string WebsiteChanged = "Website_Changed";

            // Enable or disable company
            public const string CompanyEnabled = "Company_Enabled";
            public const string CompanyDisabled = "Company_Disabled";
        }
    }
}
