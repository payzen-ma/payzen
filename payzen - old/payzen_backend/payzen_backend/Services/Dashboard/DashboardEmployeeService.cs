using payzen_backend.DTOs.Dashboard;

namespace payzen_backend.Services.Dashboard
{
    public class DashboardEmployeeService : IDashboardEmployeeService
    {
        public async Task<EmployeeDashboardDataDto> GetEmployeeDashboardDataAsync(int employeeId)
        {
            // Simulate DB delay
            await Task.Delay(500);

            // Mock Data matching the frontend initial state
            return new EmployeeDashboardDataDto
            {
                EmployeeName = "Nadia Benchekroun",
                Initials = "NB",
                Role = "Ingénieure logiciel",
                Department = "Département IT",
                ContractType = "CDI",
                Matricule = "EMP-00089",
                Manager = "H. Alami",
                Seniority = "2 ans 1 mois",

                SalaryNet = 9840m,
                PaidDate = "28 mars",

                LeavesRemaining = 18,
                LeavesTotal = 26,

                PresenceDays = 17,
                PresenceTotal = 21,

                ExtraHours = 4.5m,

                LeavesDetails = new List<LeaveDetailDto>
                {
                    new LeaveDetailDto { Label = "Congé annuel", Remaining = 18, Total = 26, ColorClass = "bg-emerald-500" },
                    new LeaveDetailDto { Label = "Congé maladie", Remaining = 8, Total = 10, ColorClass = "bg-blue-500" },
                    new LeaveDetailDto { Label = "Récupération", Remaining = 1, Total = 2, ColorClass = "bg-indigo-500" },
                    new LeaveDetailDto { Label = "Report N-1", Text = "2 j (expire juin)", ColorClass = "bg-gray-500", IsText = true }
                },

                ContractInfo = new List<ContractInfoDto>
                {
                    new ContractInfoDto { Label = "Type de contrat", Value = "CDI" },
                    new ContractInfoDto { Label = "Poste", Value = "Ingénieure logiciel" },
                    new ContractInfoDto { Label = "Date d'embauche", Value = "3 févr. 2024" },
                    new ContractInfoDto { Label = "Mutuelle privée", Value = "Oui", IsTag = true, TagColor = "success" },
                    new ContractInfoDto { Label = "CIMR", Value = "6% salarié" },
                    new ContractInfoDto { Label = "N° CNSS", Value = "2345678" }
                },

                PayslipDetails = new List<PayslipDetailDto>
                {
                    new PayslipDetailDto { Label = "Salaire de base", Value = "11 500 MAD", Type = "normal" },
                    new PayslipDetailDto { Label = "Prime transport", Value = "+ 500 MAD", Type = "normal" },
                    new PayslipDetailDto { Label = "CNSS salarié", Value = "- 776 MAD", Type = "deduction" },
                    new PayslipDetailDto { Label = "AMO salarié", Value = "- 260 MAD", Type = "deduction" },
                    new PayslipDetailDto { Label = "Retenue IR", Value = "- 1124 MAD", Type = "deduction" },
                    new PayslipDetailDto { Label = "Net à payer", Value = "9 840 MAD", Type = "net" }
                },

                Documents = new List<EmployeeDocumentDto>
                {
                    new EmployeeDocumentDto { Title = "Bulletin mars 2026", Subtitle = "Disponible le 28 mars", Status = "À venir" },
                    new EmployeeDocumentDto { Title = "Bulletin fév. 2026", Subtitle = "28 févr. 2026", Status = "Télécharger" },
                    new EmployeeDocumentDto { Title = "Attestation de travail", Subtitle = "12 janv. 2026", Status = "Télécharger" },
                    new EmployeeDocumentDto { Title = "Contrat de travail", Subtitle = "3 févr. 2024", Status = "Télécharger" }
                }
            };
        }
    }
}
