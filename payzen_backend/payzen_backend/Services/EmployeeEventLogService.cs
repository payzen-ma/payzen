using payzen_backend.Data;
using payzen_backend.Models.Event;

namespace payzen_backend.Services
{
    public class EmployeeEventLogService
    {
        private readonly AppDbContext _db;

        public EmployeeEventLogService(AppDbContext db)
        {
            _db = db;
        }

        /// <summary>
        /// Enregistre un �v�nement de modification pour un employ�
        /// </summary>
        public async Task LogEventAsync(
            int employeeId,
            string eventName,
            string? oldValue,
            int? oldValueId,
            string? newValue,
            int? newValueId,
            int createdBy)
        {
            var eventLog = new EmployeeEventLog
            {
                employeeId = employeeId,
                eventName = eventName,
                oldValue = oldValue,
                oldValueId = oldValueId,
                newValue = newValue,
                newValueId = newValueId,
                createdAt = DateTimeOffset.UtcNow,
                createdBy = createdBy
            };

            _db.EmployeeEventLogs.Add(eventLog);
            await _db.SaveChangesAsync();
        }
        /// <summary>
        /// Enregistre un �v�nement simple (sans ID)
        /// </summary>
        public async Task LogSimpleEventAsync(
            int employeeId,
            string eventName,
            string? oldValue,
            string? newValue,
            int createdBy)
        {
            await LogEventAsync(employeeId, eventName, oldValue, null, newValue, null, createdBy);
        }

        /// <summary>
        /// Enregistre un �v�nement avec ID uniquement (pour les relations)
        /// </summary>
        public async Task LogRelationEventAsync(
            int employeeId,
            string eventName,
            int? oldValueId,
            string? oldValueName,
            int? newValueId,
            string? newValueName,
            int createdBy)
        {
            await LogEventAsync(employeeId, eventName, oldValueName, oldValueId, newValueName, newValueId, createdBy);
        }

        /// <summary>
        /// Constantes pour les noms d'�v�nements
        /// </summary>
        public static class EventNames
        {
            // Informations personnelles
            public const string FirstNameChanged = "FirstName_Changed";
            public const string LastNameChanged = "LastName_Changed";
            public const string CinNumberChanged = "CIN_Changed";
            public const string DateOfBirthChanged = "DateOfBirth_Changed";
            public const string PhoneChanged = "Phone_Changed";
            public const string EmailChanged = "Email_Changed";

            // Statuts et informations
            public const string StatusChanged = "Status_Changed";
            public const string GenderChanged = "Gender_Changed";
            public const string NationalityChanged = "Nationality_Changed";
            public const string EducationLevelChanged = "EducationLevel_Changed";
            public const string MaritalStatusChanged = "MaritalStatus_Changed";

            // Epouses et enfants
            public const string SpouseAdded = "Spouse_Added";
            public const string SpouseUpdated = "Spouse_Updated";
            public const string SpouseDeleted = "Spouse_Deleted";
            public const string ChildAdded = "Child_Added";
            public const string ChildUpdated = "Child_Updated";
            public const string ChildDeleted = "Child_Deleted";

            // Organisation
            public const string DepartmentChanged = "Department_Changed";
            public const string ManagerChanged = "Manager_Changed";
            public const string CompanyChanged = "Company_Changed";

            // Contrat et emploi
            public const string ContractCreated = "Contract_Created";
            public const string ContractUpdated = "Contract_Updated";
            public const string ContractTerminated = "Contract_Terminated";
            public const string JobPositionChanged = "JobPosition_Changed";
            public const string ContractTypeChanged = "ContractType_Changed";
            public const string ContractStartDateChanged = "CONTRACT_START_DATE_CHANGED";
            public const string CategoryChanged = "Category_Changed";

            // Salaire
            public const string SalaryCreated = "Salary_Created";
            public const string SalaryUpdated = "Salary_Updated";
            public const string SalaryComponentAdded = "SalaryComponent_Added";
            public const string SalaryComponentUpdated = "SalaryComponent_Updated";
            public const string SalaryComponentDeleted = "SalaryComponent_Deleted";

            // Adresse
            public const string AddressCreated = "Address_Created";
            public const string AddressUpdated = "Address_Updated";

            // Identifiants
            public const string CnssNumberChanged = "CNSS_Changed";
            public const string CimrNumberChanged = "CIMR_Changed";

            // Cycle de vie
            public const string EmployeeCreated = "Employee_Created";
            public const string EmployeeDeleted = "Employee_Deleted";
        }
    }
}