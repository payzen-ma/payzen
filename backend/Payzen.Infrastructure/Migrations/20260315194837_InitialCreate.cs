using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Payzen.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Authorities",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false).Annotation("SqlServer:Identity", "1, 1"),
                    Code = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    SortOrder = table.Column<int>(type: "int", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    CreatedBy = table.Column<int>(type: "int", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    UpdatedBy = table.Column<int>(type: "int", nullable: true),
                    DeletedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    DeletedBy = table.Column<int>(type: "int", nullable: true),
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Authorities", x => x.Id);
                }
            );

            migrationBuilder.CreateTable(
                name: "BusinessSectors",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false).Annotation("SqlServer:Identity", "1, 1"),
                    Code = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    IsStandard = table.Column<bool>(type: "bit", nullable: false),
                    SortOrder = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    CreatedBy = table.Column<int>(type: "int", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    UpdatedBy = table.Column<int>(type: "int", nullable: true),
                    DeletedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    DeletedBy = table.Column<int>(type: "int", nullable: true),
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BusinessSectors", x => x.Id);
                }
            );

            migrationBuilder.CreateTable(
                name: "CompanyEventLogs",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false).Annotation("SqlServer:Identity", "1, 1"),
                    employeeId = table.Column<int>(type: "int", nullable: false),
                    eventName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    oldValue = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    oldValueId = table.Column<int>(type: "int", nullable: true),
                    newValue = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    newValueId = table.Column<int>(type: "int", nullable: true),
                    companyId = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    CreatedBy = table.Column<int>(type: "int", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    UpdatedBy = table.Column<int>(type: "int", nullable: true),
                    DeletedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    DeletedBy = table.Column<int>(type: "int", nullable: true),
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CompanyEventLogs", x => x.Id);
                }
            );

            migrationBuilder.CreateTable(
                name: "Countries",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false).Annotation("SqlServer:Identity", "1, 1"),
                    CountryName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    CountryNameAr = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    CountryCode = table.Column<string>(type: "nvarchar(5)", maxLength: 5, nullable: false),
                    CountryPhoneCode = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    CreatedBy = table.Column<int>(type: "int", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    UpdatedBy = table.Column<int>(type: "int", nullable: true),
                    DeletedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    DeletedBy = table.Column<int>(type: "int", nullable: true),
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Countries", x => x.Id);
                }
            );

            migrationBuilder.CreateTable(
                name: "EducationLevels",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false).Annotation("SqlServer:Identity", "1, 1"),
                    Code = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    NameFr = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    NameAr = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    NameEn = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    LevelOrder = table.Column<int>(type: "int", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    CreatedBy = table.Column<int>(type: "int", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    UpdatedBy = table.Column<int>(type: "int", nullable: true),
                    DeletedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    DeletedBy = table.Column<int>(type: "int", nullable: true),
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EducationLevels", x => x.Id);
                }
            );

            migrationBuilder.CreateTable(
                name: "ElementCategories",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false).Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    SortOrder = table.Column<int>(type: "int", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    CreatedBy = table.Column<int>(type: "int", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    UpdatedBy = table.Column<int>(type: "int", nullable: true),
                    DeletedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    DeletedBy = table.Column<int>(type: "int", nullable: true),
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ElementCategories", x => x.Id);
                }
            );

            migrationBuilder.CreateTable(
                name: "EligibilityCriteria",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false).Annotation("SqlServer:Identity", "1, 1"),
                    Code = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    CreatedBy = table.Column<int>(type: "int", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    UpdatedBy = table.Column<int>(type: "int", nullable: true),
                    DeletedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    DeletedBy = table.Column<int>(type: "int", nullable: true),
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EligibilityCriteria", x => x.Id);
                }
            );

            migrationBuilder.CreateTable(
                name: "EmployeeEventLogs",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false).Annotation("SqlServer:Identity", "1, 1"),
                    employeeId = table.Column<int>(type: "int", nullable: false),
                    eventName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    oldValue = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    oldValueId = table.Column<int>(type: "int", nullable: true),
                    newValue = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    newValueId = table.Column<int>(type: "int", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    CreatedBy = table.Column<int>(type: "int", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    UpdatedBy = table.Column<int>(type: "int", nullable: true),
                    DeletedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    DeletedBy = table.Column<int>(type: "int", nullable: true),
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EmployeeEventLogs", x => x.Id);
                }
            );

            migrationBuilder.CreateTable(
                name: "Genders",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false).Annotation("SqlServer:Identity", "1, 1"),
                    Code = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    NameFr = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    NameAr = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    NameEn = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    CreatedBy = table.Column<int>(type: "int", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    UpdatedBy = table.Column<int>(type: "int", nullable: true),
                    DeletedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    DeletedBy = table.Column<int>(type: "int", nullable: true),
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Genders", x => x.Id);
                }
            );

            migrationBuilder.CreateTable(
                name: "LegalContractTypes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false).Annotation("SqlServer:Identity", "1, 1"),
                    Code = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    CreatedBy = table.Column<int>(type: "int", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    UpdatedBy = table.Column<int>(type: "int", nullable: true),
                    DeletedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    DeletedBy = table.Column<int>(type: "int", nullable: true),
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LegalContractTypes", x => x.Id);
                }
            );

            migrationBuilder.CreateTable(
                name: "LegalParameters",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false).Annotation("SqlServer:Identity", "1, 1"),
                    Code = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Label = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Value = table.Column<decimal>(type: "decimal(18,6)", nullable: false),
                    Unit = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Source = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    EffectiveFrom = table.Column<DateOnly>(type: "date", nullable: false),
                    EffectiveTo = table.Column<DateOnly>(type: "date", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    CreatedBy = table.Column<int>(type: "int", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    UpdatedBy = table.Column<int>(type: "int", nullable: true),
                    DeletedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    DeletedBy = table.Column<int>(type: "int", nullable: true),
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LegalParameters", x => x.Id);
                }
            );

            migrationBuilder.CreateTable(
                name: "MaritalStatuses",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false).Annotation("SqlServer:Identity", "1, 1"),
                    Code = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    NameFr = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    NameAr = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    NameEn = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    CreatedBy = table.Column<int>(type: "int", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    UpdatedBy = table.Column<int>(type: "int", nullable: true),
                    DeletedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    DeletedBy = table.Column<int>(type: "int", nullable: true),
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MaritalStatuses", x => x.Id);
                }
            );

            migrationBuilder.CreateTable(
                name: "Nationalities",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false).Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    CreatedBy = table.Column<int>(type: "int", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    UpdatedBy = table.Column<int>(type: "int", nullable: true),
                    DeletedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    DeletedBy = table.Column<int>(type: "int", nullable: true),
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Nationalities", x => x.Id);
                }
            );

            migrationBuilder.CreateTable(
                name: "OvertimeRateRules",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false).Annotation("SqlServer:Identity", "1, 1"),
                    Code = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    NameAr = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    NameFr = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    NameEn = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    AppliesTo = table.Column<int>(type: "int", nullable: false),
                    TimeRangeType = table.Column<int>(type: "int", nullable: false),
                    StartTime = table.Column<TimeOnly>(type: "time", nullable: true),
                    EndTime = table.Column<TimeOnly>(type: "time", nullable: true),
                    ApplicableDaysOfWeek = table.Column<int>(type: "int", nullable: true),
                    Multiplier = table.Column<decimal>(type: "decimal(5,2)", nullable: false),
                    CumulationStrategy = table.Column<int>(type: "int", nullable: false),
                    Priority = table.Column<int>(type: "int", nullable: false),
                    Category = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    EffectiveFrom = table.Column<DateOnly>(type: "date", nullable: true),
                    EffectiveTo = table.Column<DateOnly>(type: "date", nullable: true),
                    MinimumDurationHours = table.Column<decimal>(type: "decimal(5,2)", nullable: true),
                    MaximumDurationHours = table.Column<decimal>(type: "decimal(5,2)", nullable: true),
                    RequiresSuperiorApproval = table.Column<bool>(type: "bit", nullable: false),
                    LegalReference = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    DocumentationUrl = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    CreatedBy = table.Column<int>(type: "int", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    UpdatedBy = table.Column<int>(type: "int", nullable: true),
                    DeletedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    DeletedBy = table.Column<int>(type: "int", nullable: true),
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OvertimeRateRules", x => x.Id);
                }
            );

            migrationBuilder.CreateTable(
                name: "PayComponents",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false).Annotation("SqlServer:Identity", "1, 1"),
                    Code = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    NameFr = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    NameAr = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    NameEn = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    Type = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    IsTaxable = table.Column<bool>(type: "bit", nullable: false),
                    IsSocial = table.Column<bool>(type: "bit", nullable: false),
                    IsCIMR = table.Column<bool>(type: "bit", nullable: false),
                    ExemptionLimit = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    Version = table.Column<int>(type: "int", nullable: false),
                    ValidFrom = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ValidTo = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsRegulated = table.Column<bool>(type: "bit", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    SortOrder = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    CreatedBy = table.Column<int>(type: "int", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    UpdatedBy = table.Column<int>(type: "int", nullable: true),
                    DeletedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    DeletedBy = table.Column<int>(type: "int", nullable: true),
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PayComponents", x => x.Id);
                }
            );

            migrationBuilder.CreateTable(
                name: "Permissions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false).Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    Resource = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Action = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    CreatedBy = table.Column<int>(type: "int", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    UpdatedBy = table.Column<int>(type: "int", nullable: true),
                    DeletedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    DeletedBy = table.Column<int>(type: "int", nullable: true),
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Permissions", x => x.Id);
                }
            );

            migrationBuilder.CreateTable(
                name: "Roles",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false).Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    CreatedBy = table.Column<int>(type: "int", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    UpdatedBy = table.Column<int>(type: "int", nullable: true),
                    DeletedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    DeletedBy = table.Column<int>(type: "int", nullable: true),
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Roles", x => x.Id);
                }
            );

            migrationBuilder.CreateTable(
                name: "StateEmploymentPrograms",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false).Annotation("SqlServer:Identity", "1, 1"),
                    Code = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    IsIrExempt = table.Column<bool>(type: "bit", nullable: false),
                    IsCnssEmployeeExempt = table.Column<bool>(type: "bit", nullable: false),
                    IsCnssEmployerExempt = table.Column<bool>(type: "bit", nullable: false),
                    MaxDurationMonths = table.Column<int>(type: "int", nullable: true),
                    SalaryCeiling = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    CreatedBy = table.Column<int>(type: "int", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    UpdatedBy = table.Column<int>(type: "int", nullable: true),
                    DeletedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    DeletedBy = table.Column<int>(type: "int", nullable: true),
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StateEmploymentPrograms", x => x.Id);
                }
            );

            migrationBuilder.CreateTable(
                name: "Statuses",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false).Annotation("SqlServer:Identity", "1, 1"),
                    Code = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    NameFr = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    NameAr = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    NameEn = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    AffectsAccess = table.Column<bool>(type: "bit", nullable: false),
                    AffectsPayroll = table.Column<bool>(type: "bit", nullable: false),
                    AffectsAttendance = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    CreatedBy = table.Column<int>(type: "int", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    UpdatedBy = table.Column<int>(type: "int", nullable: true),
                    DeletedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    DeletedBy = table.Column<int>(type: "int", nullable: true),
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Statuses", x => x.Id);
                }
            );

            migrationBuilder.CreateTable(
                name: "Cities",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false).Annotation("SqlServer:Identity", "1, 1"),
                    CityName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    CountryId = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    CreatedBy = table.Column<int>(type: "int", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    UpdatedBy = table.Column<int>(type: "int", nullable: true),
                    DeletedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    DeletedBy = table.Column<int>(type: "int", nullable: true),
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Cities", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Cities_Countries_CountryId",
                        column: x => x.CountryId,
                        principalTable: "Countries",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict
                    );
                }
            );

            migrationBuilder.CreateTable(
                name: "ReferentielElements",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false).Annotation("SqlServer:Identity", "1, 1"),
                    Code = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    CategoryId = table.Column<int>(type: "int", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    DefaultFrequency = table.Column<int>(type: "int", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    HasConvergence = table.Column<bool>(type: "bit", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    CreatedBy = table.Column<int>(type: "int", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    UpdatedBy = table.Column<int>(type: "int", nullable: true),
                    DeletedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    DeletedBy = table.Column<int>(type: "int", nullable: true),
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ReferentielElements", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ReferentielElements_ElementCategories_CategoryId",
                        column: x => x.CategoryId,
                        principalTable: "ElementCategories",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict
                    );
                }
            );

            migrationBuilder.CreateTable(
                name: "RolesPermissions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false).Annotation("SqlServer:Identity", "1, 1"),
                    RoleId = table.Column<int>(type: "int", nullable: false),
                    PermissionId = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    CreatedBy = table.Column<int>(type: "int", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    UpdatedBy = table.Column<int>(type: "int", nullable: true),
                    DeletedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    DeletedBy = table.Column<int>(type: "int", nullable: true),
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RolesPermissions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RolesPermissions_Permissions_PermissionId",
                        column: x => x.PermissionId,
                        principalTable: "Permissions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict
                    );
                    table.ForeignKey(
                        name: "FK_RolesPermissions_Roles_RoleId",
                        column: x => x.RoleId,
                        principalTable: "Roles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict
                    );
                }
            );

            migrationBuilder.CreateTable(
                name: "Companies",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false).Annotation("SqlServer:Identity", "1, 1"),
                    CompanyName = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    Email = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    PhoneNumber = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    CountryPhoneCode = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: true),
                    CompanyAddress = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    CityId = table.Column<int>(type: "int", nullable: false),
                    CountryId = table.Column<int>(type: "int", nullable: false),
                    CnssNumber = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    IsCabinetExpert = table.Column<bool>(type: "bit", nullable: false),
                    IceNumber = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    IfNumber = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    RcNumber = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    PatenteNumber = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    RibNumber = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    LegalForm = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    SignatoryName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    SignatoryTitle = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    FoundingDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    WebsiteUrl = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Currency = table.Column<string>(
                        type: "nvarchar(10)",
                        maxLength: 10,
                        nullable: false,
                        defaultValue: "MAD"
                    ),
                    PayrollPeriodicity = table.Column<string>(
                        type: "nvarchar(50)",
                        maxLength: 50,
                        nullable: false,
                        defaultValue: "Mensuelle"
                    ),
                    FiscalYearStartMonth = table.Column<int>(type: "int", nullable: false),
                    BusinessSector = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    PaymentMethod = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    ManagedByCompanyId = table.Column<int>(type: "int", nullable: true),
                    isActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    CreatedBy = table.Column<int>(type: "int", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    UpdatedBy = table.Column<int>(type: "int", nullable: true),
                    DeletedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    DeletedBy = table.Column<int>(type: "int", nullable: true),
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Companies", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Companies_Cities_CityId",
                        column: x => x.CityId,
                        principalTable: "Cities",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict
                    );
                    table.ForeignKey(
                        name: "FK_Companies_Companies_ManagedByCompanyId",
                        column: x => x.ManagedByCompanyId,
                        principalTable: "Companies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict
                    );
                    table.ForeignKey(
                        name: "FK_Companies_Countries_CountryId",
                        column: x => x.CountryId,
                        principalTable: "Countries",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict
                    );
                }
            );

            migrationBuilder.CreateTable(
                name: "ElementRules",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false).Annotation("SqlServer:Identity", "1, 1"),
                    ElementId = table.Column<int>(type: "int", nullable: false),
                    AuthorityId = table.Column<int>(type: "int", nullable: false),
                    ExemptionType = table.Column<int>(type: "int", nullable: false),
                    RuleDetails = table.Column<string>(
                        type: "nvarchar(2000)",
                        maxLength: 2000,
                        nullable: false,
                        defaultValue: "{}"
                    ),
                    SourceRef = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    EffectiveFrom = table.Column<DateOnly>(type: "date", nullable: false),
                    EffectiveTo = table.Column<DateOnly>(type: "date", nullable: true),
                    Status = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    CreatedBy = table.Column<int>(type: "int", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    UpdatedBy = table.Column<int>(type: "int", nullable: true),
                    DeletedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    DeletedBy = table.Column<int>(type: "int", nullable: true),
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ElementRules", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ElementRules_Authorities_AuthorityId",
                        column: x => x.AuthorityId,
                        principalTable: "Authorities",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict
                    );
                    table.ForeignKey(
                        name: "FK_ElementRules_ReferentielElements_ElementId",
                        column: x => x.ElementId,
                        principalTable: "ReferentielElements",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict
                    );
                }
            );

            migrationBuilder.CreateTable(
                name: "AncienneteRateSets",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false).Annotation("SqlServer:Identity", "1, 1"),
                    CompanyId = table.Column<int>(type: "int", nullable: true),
                    ClonedFromId = table.Column<int>(type: "int", nullable: true),
                    Code = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    IsLegalDefault = table.Column<bool>(type: "bit", nullable: false),
                    Source = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    EffectiveFrom = table.Column<DateOnly>(type: "date", nullable: false),
                    EffectiveTo = table.Column<DateOnly>(type: "date", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    CreatedBy = table.Column<int>(type: "int", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    UpdatedBy = table.Column<int>(type: "int", nullable: true),
                    DeletedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    DeletedBy = table.Column<int>(type: "int", nullable: true),
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AncienneteRateSets", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AncienneteRateSets_AncienneteRateSets_ClonedFromId",
                        column: x => x.ClonedFromId,
                        principalTable: "AncienneteRateSets",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict
                    );
                    table.ForeignKey(
                        name: "FK_AncienneteRateSets_Companies_CompanyId",
                        column: x => x.CompanyId,
                        principalTable: "Companies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict
                    );
                }
            );

            migrationBuilder.CreateTable(
                name: "CompanyDocuments",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false).Annotation("SqlServer:Identity", "1, 1"),
                    CompanyId = table.Column<int>(type: "int", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    FilePath = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    DocumentType = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    CreatedBy = table.Column<int>(type: "int", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    UpdatedBy = table.Column<int>(type: "int", nullable: true),
                    DeletedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    DeletedBy = table.Column<int>(type: "int", nullable: true),
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CompanyDocuments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CompanyDocuments_Companies_CompanyId",
                        column: x => x.CompanyId,
                        principalTable: "Companies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict
                    );
                }
            );

            migrationBuilder.CreateTable(
                name: "ContractTypes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false).Annotation("SqlServer:Identity", "1, 1"),
                    ContractTypeName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    CompanyId = table.Column<int>(type: "int", nullable: false),
                    LegalContractTypeId = table.Column<int>(type: "int", nullable: true),
                    StateEmploymentProgramId = table.Column<int>(type: "int", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    CreatedBy = table.Column<int>(type: "int", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    UpdatedBy = table.Column<int>(type: "int", nullable: true),
                    DeletedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    DeletedBy = table.Column<int>(type: "int", nullable: true),
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ContractTypes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ContractTypes_Companies_CompanyId",
                        column: x => x.CompanyId,
                        principalTable: "Companies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict
                    );
                    table.ForeignKey(
                        name: "FK_ContractTypes_LegalContractTypes_LegalContractTypeId",
                        column: x => x.LegalContractTypeId,
                        principalTable: "LegalContractTypes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull
                    );
                    table.ForeignKey(
                        name: "FK_ContractTypes_StateEmploymentPrograms_StateEmploymentProgramId",
                        column: x => x.StateEmploymentProgramId,
                        principalTable: "StateEmploymentPrograms",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull
                    );
                }
            );

            migrationBuilder.CreateTable(
                name: "Departements",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false).Annotation("SqlServer:Identity", "1, 1"),
                    DepartementName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    CompanyId = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    CreatedBy = table.Column<int>(type: "int", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    UpdatedBy = table.Column<int>(type: "int", nullable: true),
                    DeletedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    DeletedBy = table.Column<int>(type: "int", nullable: true),
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Departements", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Departements_Companies_CompanyId",
                        column: x => x.CompanyId,
                        principalTable: "Companies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict
                    );
                }
            );

            migrationBuilder.CreateTable(
                name: "EmployeeCategories",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false).Annotation("SqlServer:Identity", "1, 1"),
                    CompanyId = table.Column<int>(type: "int", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Mode = table.Column<int>(type: "int", nullable: false),
                    PayrollPeriodicity = table.Column<string>(
                        type: "nvarchar(50)",
                        maxLength: 50,
                        nullable: false,
                        defaultValue: "Mensuelle"
                    ),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    CreatedBy = table.Column<int>(type: "int", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    UpdatedBy = table.Column<int>(type: "int", nullable: true),
                    DeletedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    DeletedBy = table.Column<int>(type: "int", nullable: true),
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EmployeeCategories", x => x.Id);
                    table.ForeignKey(
                        name: "FK_EmployeeCategories_Companies_CompanyId",
                        column: x => x.CompanyId,
                        principalTable: "Companies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict
                    );
                }
            );

            migrationBuilder.CreateTable(
                name: "Holidays",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false).Annotation("SqlServer:Identity", "1, 1"),
                    NameFr = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    NameAr = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    NameEn = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    HolidayDate = table.Column<DateOnly>(type: "date", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CompanyId = table.Column<int>(type: "int", nullable: true),
                    CountryId = table.Column<int>(type: "int", nullable: false),
                    Scope = table.Column<int>(type: "int", nullable: false),
                    HolidayType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    IsMandatory = table.Column<bool>(type: "bit", nullable: false),
                    IsPaid = table.Column<bool>(type: "bit", nullable: false),
                    IsRecurring = table.Column<bool>(type: "bit", nullable: false),
                    RecurrenceRule = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    Year = table.Column<int>(type: "int", nullable: true),
                    AffectPayroll = table.Column<bool>(type: "bit", nullable: false),
                    AffectAttendance = table.Column<bool>(type: "bit", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    CreatedBy = table.Column<int>(type: "int", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    UpdatedBy = table.Column<int>(type: "int", nullable: true),
                    DeletedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    DeletedBy = table.Column<int>(type: "int", nullable: true),
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Holidays", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Holidays_Companies_CompanyId",
                        column: x => x.CompanyId,
                        principalTable: "Companies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict
                    );
                    table.ForeignKey(
                        name: "FK_Holidays_Countries_CountryId",
                        column: x => x.CountryId,
                        principalTable: "Countries",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict
                    );
                }
            );

            migrationBuilder.CreateTable(
                name: "JobPositions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false).Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    CompanyId = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    CreatedBy = table.Column<int>(type: "int", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    UpdatedBy = table.Column<int>(type: "int", nullable: true),
                    DeletedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    DeletedBy = table.Column<int>(type: "int", nullable: true),
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_JobPositions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_JobPositions_Companies_CompanyId",
                        column: x => x.CompanyId,
                        principalTable: "Companies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict
                    );
                }
            );

            migrationBuilder.CreateTable(
                name: "LeaveTypes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false).Annotation("SqlServer:Identity", "1, 1"),
                    LeaveCode = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    LeaveNameAr = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    LeaveNameEn = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    LeaveNameFr = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    LeaveDescription = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    Scope = table.Column<int>(type: "int", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CompanyId = table.Column<int>(type: "int", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    CreatedBy = table.Column<int>(type: "int", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    UpdatedBy = table.Column<int>(type: "int", nullable: true),
                    DeletedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    DeletedBy = table.Column<int>(type: "int", nullable: true),
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LeaveTypes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_LeaveTypes_Companies_CompanyId",
                        column: x => x.CompanyId,
                        principalTable: "Companies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict
                    );
                }
            );

            migrationBuilder.CreateTable(
                name: "SalaryPackages",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false).Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Category = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    BaseSalary = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    CompanyId = table.Column<int>(type: "int", nullable: true),
                    BusinessSectorId = table.Column<int>(type: "int", nullable: false),
                    TemplateType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    RegulationVersion = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    AutoRulesJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CimrConfigJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    OriginType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    SourceTemplateNameSnapshot = table.Column<string>(
                        type: "nvarchar(200)",
                        maxLength: 200,
                        nullable: true
                    ),
                    CopiedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CimrRate = table.Column<decimal>(type: "decimal(5,4)", nullable: true),
                    HasPrivateInsurance = table.Column<bool>(type: "bit", nullable: false),
                    Version = table.Column<int>(type: "int", nullable: false),
                    SourceTemplateId = table.Column<int>(type: "int", nullable: true),
                    SourceTemplateVersion = table.Column<int>(type: "int", nullable: true),
                    ValidFrom = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ValidTo = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsLocked = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    CreatedBy = table.Column<int>(type: "int", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    UpdatedBy = table.Column<int>(type: "int", nullable: true),
                    DeletedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    DeletedBy = table.Column<int>(type: "int", nullable: true),
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SalaryPackages", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SalaryPackages_BusinessSectors_BusinessSectorId",
                        column: x => x.BusinessSectorId,
                        principalTable: "BusinessSectors",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict
                    );
                    table.ForeignKey(
                        name: "FK_SalaryPackages_Companies_CompanyId",
                        column: x => x.CompanyId,
                        principalTable: "Companies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict
                    );
                }
            );

            migrationBuilder.CreateTable(
                name: "WorkingCalendars",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false).Annotation("SqlServer:Identity", "1, 1"),
                    CompanyId = table.Column<int>(type: "int", nullable: false),
                    DayOfWeek = table.Column<int>(type: "int", nullable: false),
                    IsWorkingDay = table.Column<bool>(type: "bit", nullable: false),
                    StartTime = table.Column<TimeSpan>(type: "time", nullable: true),
                    EndTime = table.Column<TimeSpan>(type: "time", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    CreatedBy = table.Column<int>(type: "int", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    UpdatedBy = table.Column<int>(type: "int", nullable: true),
                    DeletedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    DeletedBy = table.Column<int>(type: "int", nullable: true),
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WorkingCalendars", x => x.Id);
                    table.ForeignKey(
                        name: "FK_WorkingCalendars_Companies_CompanyId",
                        column: x => x.CompanyId,
                        principalTable: "Companies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict
                    );
                }
            );

            migrationBuilder.CreateTable(
                name: "RuleCaps",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false).Annotation("SqlServer:Identity", "1, 1"),
                    RuleId = table.Column<int>(type: "int", nullable: false),
                    CapAmount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    CapUnit = table.Column<int>(type: "int", nullable: false),
                    MinAmount = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    CreatedBy = table.Column<int>(type: "int", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    UpdatedBy = table.Column<int>(type: "int", nullable: true),
                    DeletedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    DeletedBy = table.Column<int>(type: "int", nullable: true),
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RuleCaps", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RuleCaps_ElementRules_RuleId",
                        column: x => x.RuleId,
                        principalTable: "ElementRules",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade
                    );
                }
            );

            migrationBuilder.CreateTable(
                name: "RuleDualCaps",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false).Annotation("SqlServer:Identity", "1, 1"),
                    RuleId = table.Column<int>(type: "int", nullable: false),
                    FixedCapAmount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    FixedCapUnit = table.Column<int>(type: "int", nullable: false),
                    PercentageCap = table.Column<decimal>(type: "decimal(5,2)", nullable: false),
                    BaseReference = table.Column<int>(type: "int", nullable: false),
                    Logic = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    CreatedBy = table.Column<int>(type: "int", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    UpdatedBy = table.Column<int>(type: "int", nullable: true),
                    DeletedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    DeletedBy = table.Column<int>(type: "int", nullable: true),
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RuleDualCaps", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RuleDualCaps_ElementRules_RuleId",
                        column: x => x.RuleId,
                        principalTable: "ElementRules",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade
                    );
                }
            );

            migrationBuilder.CreateTable(
                name: "RuleFormulas",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false).Annotation("SqlServer:Identity", "1, 1"),
                    RuleId = table.Column<int>(type: "int", nullable: false),
                    Multiplier = table.Column<decimal>(type: "decimal(10,4)", nullable: false),
                    ParameterId = table.Column<int>(type: "int", nullable: false),
                    ResultUnit = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    CreatedBy = table.Column<int>(type: "int", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    UpdatedBy = table.Column<int>(type: "int", nullable: true),
                    DeletedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    DeletedBy = table.Column<int>(type: "int", nullable: true),
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RuleFormulas", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RuleFormulas_ElementRules_RuleId",
                        column: x => x.RuleId,
                        principalTable: "ElementRules",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade
                    );
                    table.ForeignKey(
                        name: "FK_RuleFormulas_LegalParameters_ParameterId",
                        column: x => x.ParameterId,
                        principalTable: "LegalParameters",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict
                    );
                }
            );

            migrationBuilder.CreateTable(
                name: "RulePercentages",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false).Annotation("SqlServer:Identity", "1, 1"),
                    RuleId = table.Column<int>(type: "int", nullable: false),
                    Percentage = table.Column<decimal>(type: "decimal(5,2)", nullable: false),
                    BaseReference = table.Column<int>(type: "int", nullable: false),
                    EligibilityId = table.Column<int>(type: "int", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    CreatedBy = table.Column<int>(type: "int", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    UpdatedBy = table.Column<int>(type: "int", nullable: true),
                    DeletedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    DeletedBy = table.Column<int>(type: "int", nullable: true),
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RulePercentages", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RulePercentages_ElementRules_RuleId",
                        column: x => x.RuleId,
                        principalTable: "ElementRules",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade
                    );
                    table.ForeignKey(
                        name: "FK_RulePercentages_EligibilityCriteria_EligibilityId",
                        column: x => x.EligibilityId,
                        principalTable: "EligibilityCriteria",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull
                    );
                }
            );

            migrationBuilder.CreateTable(
                name: "RuleTiers",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false).Annotation("SqlServer:Identity", "1, 1"),
                    RuleId = table.Column<int>(type: "int", nullable: false),
                    TierOrder = table.Column<int>(type: "int", nullable: false),
                    FromAmount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    ToAmount = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    ExemptPercent = table.Column<decimal>(type: "decimal(5,2)", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    CreatedBy = table.Column<int>(type: "int", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    UpdatedBy = table.Column<int>(type: "int", nullable: true),
                    DeletedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    DeletedBy = table.Column<int>(type: "int", nullable: true),
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RuleTiers", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RuleTiers_ElementRules_RuleId",
                        column: x => x.RuleId,
                        principalTable: "ElementRules",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict
                    );
                }
            );

            migrationBuilder.CreateTable(
                name: "RuleVariants",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false).Annotation("SqlServer:Identity", "1, 1"),
                    RuleId = table.Column<int>(type: "int", nullable: false),
                    VariantType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    VariantKey = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    VariantLabel = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    OverrideCap = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    OverridePercentage = table.Column<decimal>(type: "decimal(5,2)", nullable: true),
                    EligibilityId = table.Column<int>(type: "int", nullable: true),
                    SortOrder = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    CreatedBy = table.Column<int>(type: "int", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    UpdatedBy = table.Column<int>(type: "int", nullable: true),
                    DeletedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    DeletedBy = table.Column<int>(type: "int", nullable: true),
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RuleVariants", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RuleVariants_ElementRules_RuleId",
                        column: x => x.RuleId,
                        principalTable: "ElementRules",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict
                    );
                    table.ForeignKey(
                        name: "FK_RuleVariants_EligibilityCriteria_EligibilityId",
                        column: x => x.EligibilityId,
                        principalTable: "EligibilityCriteria",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull
                    );
                }
            );

            migrationBuilder.CreateTable(
                name: "AncienneteRates",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false).Annotation("SqlServer:Identity", "1, 1"),
                    RateSetId = table.Column<int>(type: "int", nullable: false),
                    MinYears = table.Column<int>(type: "int", nullable: false),
                    MaxYears = table.Column<int>(type: "int", nullable: true),
                    Rate = table.Column<decimal>(type: "decimal(5,4)", nullable: false),
                    SortOrder = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    CreatedBy = table.Column<int>(type: "int", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    UpdatedBy = table.Column<int>(type: "int", nullable: true),
                    DeletedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    DeletedBy = table.Column<int>(type: "int", nullable: true),
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AncienneteRates", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AncienneteRates_AncienneteRateSets_RateSetId",
                        column: x => x.RateSetId,
                        principalTable: "AncienneteRateSets",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict
                    );
                }
            );

            migrationBuilder.CreateTable(
                name: "Employees",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false).Annotation("SqlServer:Identity", "1, 1"),
                    Matricule = table.Column<int>(type: "int", nullable: true),
                    FirstName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    LastName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    CinNumber = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    DateOfBirth = table.Column<DateOnly>(type: "date", nullable: false),
                    Phone = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Email = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    PersonalEmail = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    CompanyId = table.Column<int>(type: "int", nullable: false),
                    ManagerId = table.Column<int>(type: "int", nullable: true),
                    DepartementId = table.Column<int>(type: "int", nullable: true),
                    StatusId = table.Column<int>(type: "int", nullable: true),
                    GenderId = table.Column<int>(type: "int", nullable: true),
                    NationalityId = table.Column<int>(type: "int", nullable: true),
                    EducationLevelId = table.Column<int>(type: "int", nullable: true),
                    MaritalStatusId = table.Column<int>(type: "int", nullable: true),
                    CnssNumber = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    CimrNumber = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    CimrEmployeeRate = table.Column<decimal>(type: "decimal(5,4)", nullable: true),
                    CimrCompanyRate = table.Column<decimal>(type: "decimal(5,4)", nullable: true),
                    HasPrivateInsurance = table.Column<bool>(type: "bit", nullable: false),
                    PrivateInsuranceNumber = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    PrivateInsuranceRate = table.Column<decimal>(type: "decimal(5,4)", nullable: true),
                    DisableAmo = table.Column<bool>(type: "bit", nullable: false),
                    PaymentMethod = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    CategoryId = table.Column<int>(type: "int", nullable: true),
                    CountryId = table.Column<int>(type: "int", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    CreatedBy = table.Column<int>(type: "int", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    UpdatedBy = table.Column<int>(type: "int", nullable: true),
                    DeletedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    DeletedBy = table.Column<int>(type: "int", nullable: true),
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Employees", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Employees_Companies_CompanyId",
                        column: x => x.CompanyId,
                        principalTable: "Companies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict
                    );
                    table.ForeignKey(
                        name: "FK_Employees_Countries_CountryId",
                        column: x => x.CountryId,
                        principalTable: "Countries",
                        principalColumn: "Id"
                    );
                    table.ForeignKey(
                        name: "FK_Employees_Departements_DepartementId",
                        column: x => x.DepartementId,
                        principalTable: "Departements",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull
                    );
                    table.ForeignKey(
                        name: "FK_Employees_EducationLevels_EducationLevelId",
                        column: x => x.EducationLevelId,
                        principalTable: "EducationLevels",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull
                    );
                    table.ForeignKey(
                        name: "FK_Employees_EmployeeCategories_CategoryId",
                        column: x => x.CategoryId,
                        principalTable: "EmployeeCategories",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull
                    );
                    table.ForeignKey(
                        name: "FK_Employees_Employees_ManagerId",
                        column: x => x.ManagerId,
                        principalTable: "Employees",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict
                    );
                    table.ForeignKey(
                        name: "FK_Employees_Genders_GenderId",
                        column: x => x.GenderId,
                        principalTable: "Genders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull
                    );
                    table.ForeignKey(
                        name: "FK_Employees_MaritalStatuses_MaritalStatusId",
                        column: x => x.MaritalStatusId,
                        principalTable: "MaritalStatuses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull
                    );
                    table.ForeignKey(
                        name: "FK_Employees_Nationalities_NationalityId",
                        column: x => x.NationalityId,
                        principalTable: "Nationalities",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull
                    );
                    table.ForeignKey(
                        name: "FK_Employees_Statuses_StatusId",
                        column: x => x.StatusId,
                        principalTable: "Statuses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull
                    );
                }
            );

            migrationBuilder.CreateTable(
                name: "LeaveTypeLegalRules",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false).Annotation("SqlServer:Identity", "1, 1"),
                    LeaveTypeId = table.Column<int>(type: "int", nullable: false),
                    EventCaseCode = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: false),
                    DaysGranted = table.Column<int>(type: "int", nullable: false),
                    LegalArticle = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    CanBeDiscountinuous = table.Column<bool>(type: "bit", nullable: false),
                    MustBeUsedWithinDays = table.Column<int>(type: "int", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    CreatedBy = table.Column<int>(type: "int", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    UpdatedBy = table.Column<int>(type: "int", nullable: true),
                    DeletedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    DeletedBy = table.Column<int>(type: "int", nullable: true),
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LeaveTypeLegalRules", x => x.Id);
                    table.ForeignKey(
                        name: "FK_LeaveTypeLegalRules_LeaveTypes_LeaveTypeId",
                        column: x => x.LeaveTypeId,
                        principalTable: "LeaveTypes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict
                    );
                }
            );

            migrationBuilder.CreateTable(
                name: "LeaveTypePolicies",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false).Annotation("SqlServer:Identity", "1, 1"),
                    CompanyId = table.Column<int>(type: "int", nullable: true),
                    LeaveTypeId = table.Column<int>(type: "int", nullable: false),
                    IsEnabled = table.Column<bool>(type: "bit", nullable: false),
                    RequiresBalance = table.Column<bool>(type: "bit", nullable: false),
                    AllowNegativeBalance = table.Column<bool>(type: "bit", nullable: false),
                    IsPaid = table.Column<bool>(type: "bit", nullable: false),
                    RequiresEligibility6Months = table.Column<bool>(type: "bit", nullable: false),
                    AccrualMethod = table.Column<int>(type: "int", nullable: false),
                    DaysPerMonthAdult = table.Column<decimal>(type: "decimal(5,2)", nullable: false),
                    DaysPerMonthMinor = table.Column<decimal>(type: "decimal(5,2)", nullable: false),
                    BonusDaysPerYearAfter5Years = table.Column<decimal>(type: "decimal(5,2)", nullable: false),
                    AnnualCapDays = table.Column<int>(type: "int", nullable: false),
                    AllowCarryover = table.Column<bool>(type: "bit", nullable: false),
                    MaxCarryoverYears = table.Column<int>(type: "int", nullable: false),
                    MinConsecutiveDays = table.Column<int>(type: "int", nullable: false),
                    UseWorkingCalendar = table.Column<bool>(type: "bit", nullable: false),
                    EffectiveFrom = table.Column<DateOnly>(type: "date", nullable: true),
                    EffectiveTo = table.Column<DateOnly>(type: "date", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    CreatedBy = table.Column<int>(type: "int", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    UpdatedBy = table.Column<int>(type: "int", nullable: true),
                    DeletedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    DeletedBy = table.Column<int>(type: "int", nullable: true),
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LeaveTypePolicies", x => x.Id);
                    table.ForeignKey(
                        name: "FK_LeaveTypePolicies_Companies_CompanyId",
                        column: x => x.CompanyId,
                        principalTable: "Companies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict
                    );
                    table.ForeignKey(
                        name: "FK_LeaveTypePolicies_LeaveTypes_LeaveTypeId",
                        column: x => x.LeaveTypeId,
                        principalTable: "LeaveTypes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict
                    );
                }
            );

            migrationBuilder.CreateTable(
                name: "SalaryPackageItems",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false).Annotation("SqlServer:Identity", "1, 1"),
                    SalaryPackageId = table.Column<int>(type: "int", nullable: false),
                    PayComponentId = table.Column<int>(type: "int", nullable: true),
                    ReferentielElementId = table.Column<int>(type: "int", nullable: true),
                    Label = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    DefaultValue = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    SortOrder = table.Column<int>(type: "int", nullable: false),
                    Type = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    IsTaxable = table.Column<bool>(type: "bit", nullable: false),
                    IsSocial = table.Column<bool>(type: "bit", nullable: false),
                    IsCIMR = table.Column<bool>(type: "bit", nullable: false),
                    IsVariable = table.Column<bool>(type: "bit", nullable: false),
                    ExemptionLimit = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    CreatedBy = table.Column<int>(type: "int", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    UpdatedBy = table.Column<int>(type: "int", nullable: true),
                    DeletedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    DeletedBy = table.Column<int>(type: "int", nullable: true),
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SalaryPackageItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SalaryPackageItems_PayComponents_PayComponentId",
                        column: x => x.PayComponentId,
                        principalTable: "PayComponents",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull
                    );
                    table.ForeignKey(
                        name: "FK_SalaryPackageItems_ReferentielElements_ReferentielElementId",
                        column: x => x.ReferentielElementId,
                        principalTable: "ReferentielElements",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull
                    );
                    table.ForeignKey(
                        name: "FK_SalaryPackageItems_SalaryPackages_SalaryPackageId",
                        column: x => x.SalaryPackageId,
                        principalTable: "SalaryPackages",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict
                    );
                }
            );

            migrationBuilder.CreateTable(
                name: "EmployeeAbsences",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false).Annotation("SqlServer:Identity", "1, 1"),
                    EmployeeId = table.Column<int>(type: "int", nullable: false),
                    AbsenceDate = table.Column<DateOnly>(type: "date", nullable: false),
                    DurationType = table.Column<int>(type: "int", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    DecisionAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    DecisionBy = table.Column<int>(type: "int", nullable: true),
                    DecisionComment = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    IsMorning = table.Column<bool>(type: "bit", nullable: true),
                    StartTime = table.Column<TimeOnly>(type: "time", nullable: true),
                    EndTime = table.Column<TimeOnly>(type: "time", nullable: true),
                    AbsenceType = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Reason = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    CreatedBy = table.Column<int>(type: "int", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    UpdatedBy = table.Column<int>(type: "int", nullable: true),
                    DeletedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    DeletedBy = table.Column<int>(type: "int", nullable: true),
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EmployeeAbsences", x => x.Id);
                    table.ForeignKey(
                        name: "FK_EmployeeAbsences_Employees_EmployeeId",
                        column: x => x.EmployeeId,
                        principalTable: "Employees",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict
                    );
                }
            );

            migrationBuilder.CreateTable(
                name: "EmployeeAddresses",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false).Annotation("SqlServer:Identity", "1, 1"),
                    EmployeeId = table.Column<int>(type: "int", nullable: false),
                    AddressLine1 = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    AddressLine2 = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    ZipCode = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    CityId = table.Column<int>(type: "int", nullable: false),
                    CountryId = table.Column<int>(type: "int", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    CreatedBy = table.Column<int>(type: "int", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    UpdatedBy = table.Column<int>(type: "int", nullable: true),
                    DeletedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    DeletedBy = table.Column<int>(type: "int", nullable: true),
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EmployeeAddresses", x => x.Id);
                    table.ForeignKey(
                        name: "FK_EmployeeAddresses_Cities_CityId",
                        column: x => x.CityId,
                        principalTable: "Cities",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict
                    );
                    table.ForeignKey(
                        name: "FK_EmployeeAddresses_Countries_CountryId",
                        column: x => x.CountryId,
                        principalTable: "Countries",
                        principalColumn: "Id"
                    );
                    table.ForeignKey(
                        name: "FK_EmployeeAddresses_Employees_EmployeeId",
                        column: x => x.EmployeeId,
                        principalTable: "Employees",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict
                    );
                }
            );

            migrationBuilder.CreateTable(
                name: "EmployeeAttendances",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false).Annotation("SqlServer:Identity", "1, 1"),
                    EmployeeId = table.Column<int>(type: "int", nullable: false),
                    WorkDate = table.Column<DateOnly>(type: "date", nullable: false),
                    CheckIn = table.Column<TimeOnly>(type: "time", nullable: true),
                    CheckOut = table.Column<TimeOnly>(type: "time", nullable: true),
                    BreakMinutesApplied = table.Column<int>(type: "int", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    Source = table.Column<int>(type: "int", nullable: false),
                    WorkedHours = table.Column<decimal>(type: "decimal(5,2)", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    CreatedBy = table.Column<int>(type: "int", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    UpdatedBy = table.Column<int>(type: "int", nullable: true),
                    DeletedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    DeletedBy = table.Column<int>(type: "int", nullable: true),
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EmployeeAttendances", x => x.Id);
                    table.ForeignKey(
                        name: "FK_EmployeeAttendances_Employees_EmployeeId",
                        column: x => x.EmployeeId,
                        principalTable: "Employees",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict
                    );
                }
            );

            migrationBuilder.CreateTable(
                name: "EmployeeChildren",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false).Annotation("SqlServer:Identity", "1, 1"),
                    EmployeeId = table.Column<int>(type: "int", nullable: false),
                    FirstName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    LastName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    DateOfBirth = table.Column<DateTime>(type: "datetime2", nullable: false),
                    GenderId = table.Column<int>(type: "int", nullable: true),
                    IsDependent = table.Column<bool>(type: "bit", nullable: false),
                    IsStudent = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    CreatedBy = table.Column<int>(type: "int", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    UpdatedBy = table.Column<int>(type: "int", nullable: true),
                    DeletedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    DeletedBy = table.Column<int>(type: "int", nullable: true),
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EmployeeChildren", x => x.Id);
                    table.ForeignKey(
                        name: "FK_EmployeeChildren_Employees_EmployeeId",
                        column: x => x.EmployeeId,
                        principalTable: "Employees",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict
                    );
                    table.ForeignKey(
                        name: "FK_EmployeeChildren_Genders_GenderId",
                        column: x => x.GenderId,
                        principalTable: "Genders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull
                    );
                }
            );

            migrationBuilder.CreateTable(
                name: "EmployeeContracts",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false).Annotation("SqlServer:Identity", "1, 1"),
                    EmployeeId = table.Column<int>(type: "int", nullable: false),
                    CompanyId = table.Column<int>(type: "int", nullable: false),
                    JobPositionId = table.Column<int>(type: "int", nullable: false),
                    ContractTypeId = table.Column<int>(type: "int", nullable: false),
                    StartDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    EndDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ExonerationEndDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    CreatedBy = table.Column<int>(type: "int", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    UpdatedBy = table.Column<int>(type: "int", nullable: true),
                    DeletedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    DeletedBy = table.Column<int>(type: "int", nullable: true),
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EmployeeContracts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_EmployeeContracts_Companies_CompanyId",
                        column: x => x.CompanyId,
                        principalTable: "Companies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict
                    );
                    table.ForeignKey(
                        name: "FK_EmployeeContracts_ContractTypes_ContractTypeId",
                        column: x => x.ContractTypeId,
                        principalTable: "ContractTypes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict
                    );
                    table.ForeignKey(
                        name: "FK_EmployeeContracts_Employees_EmployeeId",
                        column: x => x.EmployeeId,
                        principalTable: "Employees",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict
                    );
                    table.ForeignKey(
                        name: "FK_EmployeeContracts_JobPositions_JobPositionId",
                        column: x => x.JobPositionId,
                        principalTable: "JobPositions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict
                    );
                }
            );

            migrationBuilder.CreateTable(
                name: "EmployeeDocuments",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false).Annotation("SqlServer:Identity", "1, 1"),
                    EmployeeId = table.Column<int>(type: "int", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    FilePath = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    ExpirationDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DocumentType = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    CreatedBy = table.Column<int>(type: "int", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    UpdatedBy = table.Column<int>(type: "int", nullable: true),
                    DeletedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    DeletedBy = table.Column<int>(type: "int", nullable: true),
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EmployeeDocuments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_EmployeeDocuments_Employees_EmployeeId",
                        column: x => x.EmployeeId,
                        principalTable: "Employees",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict
                    );
                }
            );

            migrationBuilder.CreateTable(
                name: "EmployeeOvertimes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false).Annotation("SqlServer:Identity", "1, 1"),
                    EmployeeId = table.Column<int>(type: "int", nullable: false),
                    OverTimeType = table.Column<int>(type: "int", nullable: false),
                    EntryMode = table.Column<int>(type: "int", nullable: false),
                    HolidayId = table.Column<int>(type: "int", nullable: true),
                    OvertimeDate = table.Column<DateOnly>(type: "date", nullable: false),
                    StartTime = table.Column<TimeOnly>(type: "time", nullable: true),
                    EndTime = table.Column<TimeOnly>(type: "time", nullable: true),
                    CrossesMidnight = table.Column<bool>(type: "bit", nullable: false),
                    DurationInHours = table.Column<decimal>(type: "decimal(5,2)", nullable: false),
                    StandardDayHours = table.Column<decimal>(type: "decimal(5,2)", nullable: true),
                    RateRuleId = table.Column<int>(type: "int", nullable: true),
                    RateRuleCodeApplied = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    RateRuleNameApplied = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    RateMultiplierApplied = table.Column<decimal>(type: "decimal(5,2)", nullable: false),
                    MultiplierCalculationDetails = table.Column<string>(
                        type: "nvarchar(1000)",
                        maxLength: 1000,
                        nullable: true
                    ),
                    SplitBatchId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    SplitSequence = table.Column<int>(type: "int", nullable: true),
                    SplitTotalSegments = table.Column<int>(type: "int", nullable: true),
                    Status = table.Column<int>(type: "int", nullable: false),
                    EmployeeComment = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    ManagerComment = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    ApprovedBy = table.Column<int>(type: "int", nullable: true),
                    ApprovedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    IsProcessedInPayroll = table.Column<bool>(type: "bit", nullable: false),
                    PayrollBatchId = table.Column<int>(type: "int", nullable: true),
                    ProcessedInPayrollAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    CreatedBy = table.Column<int>(type: "int", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    UpdatedBy = table.Column<int>(type: "int", nullable: true),
                    DeletedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    DeletedBy = table.Column<int>(type: "int", nullable: true),
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EmployeeOvertimes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_EmployeeOvertimes_Employees_EmployeeId",
                        column: x => x.EmployeeId,
                        principalTable: "Employees",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict
                    );
                    table.ForeignKey(
                        name: "FK_EmployeeOvertimes_Holidays_HolidayId",
                        column: x => x.HolidayId,
                        principalTable: "Holidays",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull
                    );
                    table.ForeignKey(
                        name: "FK_EmployeeOvertimes_OvertimeRateRules_RateRuleId",
                        column: x => x.RateRuleId,
                        principalTable: "OvertimeRateRules",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull
                    );
                }
            );

            migrationBuilder.CreateTable(
                name: "EmployeeSpouses",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false).Annotation("SqlServer:Identity", "1, 1"),
                    EmployeeId = table.Column<int>(type: "int", nullable: false),
                    FirstName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    LastName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    DateOfBirth = table.Column<DateTime>(type: "datetime2", nullable: false),
                    GenderId = table.Column<int>(type: "int", nullable: true),
                    CinNumber = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    IsDependent = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    CreatedBy = table.Column<int>(type: "int", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    UpdatedBy = table.Column<int>(type: "int", nullable: true),
                    DeletedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    DeletedBy = table.Column<int>(type: "int", nullable: true),
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EmployeeSpouses", x => x.Id);
                    table.ForeignKey(
                        name: "FK_EmployeeSpouses_Employees_EmployeeId",
                        column: x => x.EmployeeId,
                        principalTable: "Employees",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict
                    );
                    table.ForeignKey(
                        name: "FK_EmployeeSpouses_Genders_GenderId",
                        column: x => x.GenderId,
                        principalTable: "Genders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull
                    );
                }
            );

            migrationBuilder.CreateTable(
                name: "LeaveBalances",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false).Annotation("SqlServer:Identity", "1, 1"),
                    EmployeeId = table.Column<int>(type: "int", nullable: false),
                    CompanyId = table.Column<int>(type: "int", nullable: false),
                    LeaveTypeId = table.Column<int>(type: "int", nullable: false),
                    Year = table.Column<int>(type: "int", nullable: false),
                    Month = table.Column<int>(type: "int", nullable: false),
                    OpeningDays = table.Column<decimal>(type: "decimal(10,2)", nullable: false),
                    AccruedDays = table.Column<decimal>(type: "decimal(10,2)", nullable: false),
                    UsedDays = table.Column<decimal>(type: "decimal(10,2)", nullable: false),
                    CarryInDays = table.Column<decimal>(type: "decimal(10,2)", nullable: false),
                    CarryOutDays = table.Column<decimal>(type: "decimal(10,2)", nullable: false),
                    ClosingDays = table.Column<decimal>(type: "decimal(10,2)", nullable: false),
                    CarryoverExpiresOn = table.Column<DateOnly>(type: "date", nullable: true),
                    LastRecalculatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    CreatedBy = table.Column<int>(type: "int", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    UpdatedBy = table.Column<int>(type: "int", nullable: true),
                    DeletedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    DeletedBy = table.Column<int>(type: "int", nullable: true),
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LeaveBalances", x => x.Id);
                    table.ForeignKey(
                        name: "FK_LeaveBalances_Companies_CompanyId",
                        column: x => x.CompanyId,
                        principalTable: "Companies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict
                    );
                    table.ForeignKey(
                        name: "FK_LeaveBalances_Employees_EmployeeId",
                        column: x => x.EmployeeId,
                        principalTable: "Employees",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict
                    );
                    table.ForeignKey(
                        name: "FK_LeaveBalances_LeaveTypes_LeaveTypeId",
                        column: x => x.LeaveTypeId,
                        principalTable: "LeaveTypes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict
                    );
                }
            );

            migrationBuilder.CreateTable(
                name: "LeaveCarryOverAgreements",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false).Annotation("SqlServer:Identity", "1, 1"),
                    EmployeeId = table.Column<int>(type: "int", nullable: false),
                    CompanyId = table.Column<int>(type: "int", nullable: false),
                    LeaveTypeId = table.Column<int>(type: "int", nullable: false),
                    FromYear = table.Column<int>(type: "int", nullable: false),
                    ToYear = table.Column<int>(type: "int", nullable: false),
                    AgreementDate = table.Column<DateOnly>(type: "date", nullable: false),
                    AgreementDocRef = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    CreatedBy = table.Column<int>(type: "int", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    UpdatedBy = table.Column<int>(type: "int", nullable: true),
                    DeletedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    DeletedBy = table.Column<int>(type: "int", nullable: true),
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LeaveCarryOverAgreements", x => x.Id);
                    table.ForeignKey(
                        name: "FK_LeaveCarryOverAgreements_Companies_CompanyId",
                        column: x => x.CompanyId,
                        principalTable: "Companies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict
                    );
                    table.ForeignKey(
                        name: "FK_LeaveCarryOverAgreements_Employees_EmployeeId",
                        column: x => x.EmployeeId,
                        principalTable: "Employees",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict
                    );
                    table.ForeignKey(
                        name: "FK_LeaveCarryOverAgreements_LeaveTypes_LeaveTypeId",
                        column: x => x.LeaveTypeId,
                        principalTable: "LeaveTypes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict
                    );
                }
            );

            migrationBuilder.CreateTable(
                name: "PayrollResults",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false).Annotation("SqlServer:Identity", "1, 1"),
                    EmployeeId = table.Column<int>(type: "int", nullable: false),
                    CompanyId = table.Column<int>(type: "int", nullable: false),
                    Month = table.Column<int>(type: "int", nullable: false),
                    Year = table.Column<int>(type: "int", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    ErrorMessage = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    ResultatJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    SalaireBase = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    HeuresSupp25 = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    HeuresSupp50 = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    HeuresSupp100 = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    Conges = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    JoursFeries = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    PrimeAnciennete = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    PrimeAnciennteRate = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    PrimeImposable1 = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    PrimeImposable2 = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    PrimeImposable3 = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    TotalPrimesImposables = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    TotalBrut = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    FraisProfessionnels = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    IndemniteRepresentation = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    PrimeTransport = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    PrimePanier = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    IndemniteDeplacement = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    IndemniteCaisse = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    PrimeSalissure = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    GratificationsFamilial = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    PrimeVoyageMecque = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    IndemniteLicenciement = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    IndemniteKilometrique = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    PrimeTourne = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    PrimeOutillage = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    AideMedicale = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    AutresPrimesNonImposable = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    TotalIndemnites = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    TotalNiExcedentImposable = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    CnssPartSalariale = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    CnssBase = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    CimrPartSalariale = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    CimrBase = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    AmoPartSalariale = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    AmoBase = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    MutuellePartSalariale = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    MutuelleBase = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    TotalCotisationsSalariales = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    CnssPartPatronale = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    CimrPartPatronale = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    AmoPartPatronale = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    MutuellePartPatronale = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    TotalCotisationsPatronales = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    ImpotRevenu = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    IrTaux = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    Arrondi = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    AvanceSurSalaire = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    InteretSurLogement = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    BrutImposable = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    NetImposable = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    TotalGains = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    TotalRetenues = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    NetAPayer = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    TotalNet = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    TotalNet2 = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    ClaudeModel = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    TokensUsed = table.Column<int>(type: "int", nullable: true),
                    ProcessedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    CreatedBy = table.Column<int>(type: "int", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    UpdatedBy = table.Column<int>(type: "int", nullable: true),
                    DeletedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    DeletedBy = table.Column<int>(type: "int", nullable: true),
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PayrollResults", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PayrollResults_Companies_CompanyId",
                        column: x => x.CompanyId,
                        principalTable: "Companies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict
                    );
                    table.ForeignKey(
                        name: "FK_PayrollResults_Employees_EmployeeId",
                        column: x => x.EmployeeId,
                        principalTable: "Employees",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict
                    );
                }
            );

            migrationBuilder.CreateTable(
                name: "Users",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false).Annotation("SqlServer:Identity", "1, 1"),
                    EmployeeId = table.Column<int>(type: "int", nullable: true),
                    Username = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Email = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    EmailPersonal = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    PasswordHash = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    CreatedBy = table.Column<int>(type: "int", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    UpdatedBy = table.Column<int>(type: "int", nullable: true),
                    DeletedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    DeletedBy = table.Column<int>(type: "int", nullable: true),
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Users", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Users_Employees_EmployeeId",
                        column: x => x.EmployeeId,
                        principalTable: "Employees",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull
                    );
                }
            );

            migrationBuilder.CreateTable(
                name: "LeaveRequests",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false).Annotation("SqlServer:Identity", "1, 1"),
                    EmployeeId = table.Column<int>(type: "int", nullable: false),
                    CompanyId = table.Column<int>(type: "int", nullable: false),
                    LeaveTypeId = table.Column<int>(type: "int", nullable: false),
                    LegalRuleId = table.Column<int>(type: "int", nullable: true),
                    PolicyId = table.Column<int>(type: "int", nullable: true),
                    StartDate = table.Column<DateOnly>(type: "date", nullable: false),
                    EndDate = table.Column<DateOnly>(type: "date", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    RequestedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    SubmittedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    DecisionAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    DecisionBy = table.Column<int>(type: "int", nullable: true),
                    DecisionComment = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    CalendarDays = table.Column<int>(type: "int", nullable: false),
                    WorkingDaysDeducted = table.Column<decimal>(type: "decimal(10,2)", nullable: false),
                    HasMinConsecutiveBlock = table.Column<bool>(type: "bit", nullable: false),
                    ComputationVersion = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    EmployeeNote = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    ManagerNote = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    IsRenounced = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    CreatedBy = table.Column<int>(type: "int", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    UpdatedBy = table.Column<int>(type: "int", nullable: true),
                    DeletedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    DeletedBy = table.Column<int>(type: "int", nullable: true),
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LeaveRequests", x => x.Id);
                    table.ForeignKey(
                        name: "FK_LeaveRequests_Companies_CompanyId",
                        column: x => x.CompanyId,
                        principalTable: "Companies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict
                    );
                    table.ForeignKey(
                        name: "FK_LeaveRequests_Employees_EmployeeId",
                        column: x => x.EmployeeId,
                        principalTable: "Employees",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict
                    );
                    table.ForeignKey(
                        name: "FK_LeaveRequests_LeaveTypeLegalRules_LegalRuleId",
                        column: x => x.LegalRuleId,
                        principalTable: "LeaveTypeLegalRules",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull
                    );
                    table.ForeignKey(
                        name: "FK_LeaveRequests_LeaveTypePolicies_PolicyId",
                        column: x => x.PolicyId,
                        principalTable: "LeaveTypePolicies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull
                    );
                    table.ForeignKey(
                        name: "FK_LeaveRequests_LeaveTypes_LeaveTypeId",
                        column: x => x.LeaveTypeId,
                        principalTable: "LeaveTypes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict
                    );
                }
            );

            migrationBuilder.CreateTable(
                name: "EmployeeAttendanceBreaks",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false).Annotation("SqlServer:Identity", "1, 1"),
                    EmployeeAttendanceId = table.Column<int>(type: "int", nullable: false),
                    BreakStart = table.Column<TimeOnly>(type: "time", nullable: false),
                    BreakEnd = table.Column<TimeOnly>(type: "time", nullable: true),
                    BreakType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    CreatedBy = table.Column<int>(type: "int", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    UpdatedBy = table.Column<int>(type: "int", nullable: true),
                    DeletedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    DeletedBy = table.Column<int>(type: "int", nullable: true),
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EmployeeAttendanceBreaks", x => x.Id);
                    table.ForeignKey(
                        name: "FK_EmployeeAttendanceBreaks_EmployeeAttendances_EmployeeAttendanceId",
                        column: x => x.EmployeeAttendanceId,
                        principalTable: "EmployeeAttendances",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict
                    );
                }
            );

            migrationBuilder.CreateTable(
                name: "EmployeeSalaries",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false).Annotation("SqlServer:Identity", "1, 1"),
                    EmployeeId = table.Column<int>(type: "int", nullable: false),
                    ContractId = table.Column<int>(type: "int", nullable: false),
                    BaseSalary = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    BaseSalaryHourly = table.Column<decimal>(type: "decimal(18,4)", nullable: true),
                    EffectiveDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    EndDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    CreatedBy = table.Column<int>(type: "int", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    UpdatedBy = table.Column<int>(type: "int", nullable: true),
                    DeletedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    DeletedBy = table.Column<int>(type: "int", nullable: true),
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EmployeeSalaries", x => x.Id);
                    table.ForeignKey(
                        name: "FK_EmployeeSalaries_EmployeeContracts_ContractId",
                        column: x => x.ContractId,
                        principalTable: "EmployeeContracts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict
                    );
                    table.ForeignKey(
                        name: "FK_EmployeeSalaries_Employees_EmployeeId",
                        column: x => x.EmployeeId,
                        principalTable: "Employees",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict
                    );
                }
            );

            migrationBuilder.CreateTable(
                name: "PayrollCalculationAuditSteps",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false).Annotation("SqlServer:Identity", "1, 1"),
                    PayrollResultId = table.Column<int>(type: "int", nullable: false),
                    StepOrder = table.Column<int>(type: "int", nullable: false),
                    ModuleName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    FormulaDescription = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    InputsJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    OutputsJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    CreatedBy = table.Column<int>(type: "int", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    UpdatedBy = table.Column<int>(type: "int", nullable: true),
                    DeletedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    DeletedBy = table.Column<int>(type: "int", nullable: true),
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PayrollCalculationAuditSteps", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PayrollCalculationAuditSteps_PayrollResults_PayrollResultId",
                        column: x => x.PayrollResultId,
                        principalTable: "PayrollResults",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict
                    );
                }
            );

            migrationBuilder.CreateTable(
                name: "PayrollResultPrimes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false).Annotation("SqlServer:Identity", "1, 1"),
                    PayrollResultId = table.Column<int>(type: "int", nullable: false),
                    Label = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Montant = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Ordre = table.Column<int>(type: "int", nullable: false),
                    IsTaxable = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    CreatedBy = table.Column<int>(type: "int", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    UpdatedBy = table.Column<int>(type: "int", nullable: true),
                    DeletedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    DeletedBy = table.Column<int>(type: "int", nullable: true),
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PayrollResultPrimes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PayrollResultPrimes_PayrollResults_PayrollResultId",
                        column: x => x.PayrollResultId,
                        principalTable: "PayrollResults",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict
                    );
                }
            );

            migrationBuilder.CreateTable(
                name: "UsersRoles",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false).Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<int>(type: "int", nullable: false),
                    RoleId = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    CreatedBy = table.Column<int>(type: "int", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    UpdatedBy = table.Column<int>(type: "int", nullable: true),
                    DeletedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    DeletedBy = table.Column<int>(type: "int", nullable: true),
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UsersRoles", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UsersRoles_Roles_RoleId",
                        column: x => x.RoleId,
                        principalTable: "Roles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict
                    );
                    table.ForeignKey(
                        name: "FK_UsersRoles_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict
                    );
                }
            );

            migrationBuilder.CreateTable(
                name: "LeaveAuditLogs",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false).Annotation("SqlServer:Identity", "1, 1"),
                    CompanyId = table.Column<int>(type: "int", nullable: false),
                    EmployeeId = table.Column<int>(type: "int", nullable: true),
                    LeaveRequestId = table.Column<int>(type: "int", nullable: true),
                    EventName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    OldValue = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    NewValue = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    CreatedBy = table.Column<int>(type: "int", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    UpdatedBy = table.Column<int>(type: "int", nullable: true),
                    DeletedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    DeletedBy = table.Column<int>(type: "int", nullable: true),
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LeaveAuditLogs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_LeaveAuditLogs_Companies_CompanyId",
                        column: x => x.CompanyId,
                        principalTable: "Companies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict
                    );
                    table.ForeignKey(
                        name: "FK_LeaveAuditLogs_Employees_EmployeeId",
                        column: x => x.EmployeeId,
                        principalTable: "Employees",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull
                    );
                    table.ForeignKey(
                        name: "FK_LeaveAuditLogs_LeaveRequests_LeaveRequestId",
                        column: x => x.LeaveRequestId,
                        principalTable: "LeaveRequests",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull
                    );
                }
            );

            migrationBuilder.CreateTable(
                name: "LeaveRequestApprovalHistories",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false).Annotation("SqlServer:Identity", "1, 1"),
                    LeaveRequestId = table.Column<int>(type: "int", nullable: false),
                    Action = table.Column<int>(type: "int", nullable: false),
                    ActionAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    ActionBy = table.Column<int>(type: "int", nullable: false),
                    Comment = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    CreatedBy = table.Column<int>(type: "int", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    UpdatedBy = table.Column<int>(type: "int", nullable: true),
                    DeletedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    DeletedBy = table.Column<int>(type: "int", nullable: true),
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LeaveRequestApprovalHistories", x => x.Id);
                    table.ForeignKey(
                        name: "FK_LeaveRequestApprovalHistories_LeaveRequests_LeaveRequestId",
                        column: x => x.LeaveRequestId,
                        principalTable: "LeaveRequests",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict
                    );
                }
            );

            migrationBuilder.CreateTable(
                name: "LeaveRequestAttachments",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false).Annotation("SqlServer:Identity", "1, 1"),
                    LeaveRequestId = table.Column<int>(type: "int", nullable: false),
                    FileName = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    FilePath = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    FileType = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    CreatedBy = table.Column<int>(type: "int", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    UpdatedBy = table.Column<int>(type: "int", nullable: true),
                    DeletedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    DeletedBy = table.Column<int>(type: "int", nullable: true),
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LeaveRequestAttachments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_LeaveRequestAttachments_LeaveRequests_LeaveRequestId",
                        column: x => x.LeaveRequestId,
                        principalTable: "LeaveRequests",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict
                    );
                }
            );

            migrationBuilder.CreateTable(
                name: "LeaveRequestExemptions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false).Annotation("SqlServer:Identity", "1, 1"),
                    LeaveRequestId = table.Column<int>(type: "int", nullable: false),
                    ExemptionDate = table.Column<DateOnly>(type: "date", nullable: false),
                    ReasonType = table.Column<int>(type: "int", nullable: false),
                    CountsAsLeaveDay = table.Column<bool>(type: "bit", nullable: false),
                    HolidayId = table.Column<int>(type: "int", nullable: true),
                    EmployeeAbsenceId = table.Column<int>(type: "int", nullable: true),
                    Note = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    CreatedBy = table.Column<int>(type: "int", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    UpdatedBy = table.Column<int>(type: "int", nullable: true),
                    DeletedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    DeletedBy = table.Column<int>(type: "int", nullable: true),
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LeaveRequestExemptions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_LeaveRequestExemptions_EmployeeAbsences_EmployeeAbsenceId",
                        column: x => x.EmployeeAbsenceId,
                        principalTable: "EmployeeAbsences",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull
                    );
                    table.ForeignKey(
                        name: "FK_LeaveRequestExemptions_Holidays_HolidayId",
                        column: x => x.HolidayId,
                        principalTable: "Holidays",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull
                    );
                    table.ForeignKey(
                        name: "FK_LeaveRequestExemptions_LeaveRequests_LeaveRequestId",
                        column: x => x.LeaveRequestId,
                        principalTable: "LeaveRequests",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict
                    );
                }
            );

            migrationBuilder.CreateTable(
                name: "EmployeeSalaryComponents",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false).Annotation("SqlServer:Identity", "1, 1"),
                    EmployeeSalaryId = table.Column<int>(type: "int", nullable: false),
                    ComponentType = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    IsTaxable = table.Column<bool>(type: "bit", nullable: false),
                    IsSocial = table.Column<bool>(type: "bit", nullable: false),
                    IsCIMR = table.Column<bool>(type: "bit", nullable: false),
                    Amount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    EffectiveDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    EndDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    CreatedBy = table.Column<int>(type: "int", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    UpdatedBy = table.Column<int>(type: "int", nullable: true),
                    DeletedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    DeletedBy = table.Column<int>(type: "int", nullable: true),
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EmployeeSalaryComponents", x => x.Id);
                    table.ForeignKey(
                        name: "FK_EmployeeSalaryComponents_EmployeeSalaries_EmployeeSalaryId",
                        column: x => x.EmployeeSalaryId,
                        principalTable: "EmployeeSalaries",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict
                    );
                }
            );

            migrationBuilder.CreateTable(
                name: "SalaryPackageAssignments",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false).Annotation("SqlServer:Identity", "1, 1"),
                    SalaryPackageId = table.Column<int>(type: "int", nullable: false),
                    EmployeeId = table.Column<int>(type: "int", nullable: false),
                    ContractId = table.Column<int>(type: "int", nullable: false),
                    EmployeeSalaryId = table.Column<int>(type: "int", nullable: false),
                    EffectiveDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    EndDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    PackageVersion = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    CreatedBy = table.Column<int>(type: "int", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    UpdatedBy = table.Column<int>(type: "int", nullable: true),
                    DeletedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    DeletedBy = table.Column<int>(type: "int", nullable: true),
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SalaryPackageAssignments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SalaryPackageAssignments_EmployeeContracts_ContractId",
                        column: x => x.ContractId,
                        principalTable: "EmployeeContracts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict
                    );
                    table.ForeignKey(
                        name: "FK_SalaryPackageAssignments_EmployeeSalaries_EmployeeSalaryId",
                        column: x => x.EmployeeSalaryId,
                        principalTable: "EmployeeSalaries",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict
                    );
                    table.ForeignKey(
                        name: "FK_SalaryPackageAssignments_Employees_EmployeeId",
                        column: x => x.EmployeeId,
                        principalTable: "Employees",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict
                    );
                    table.ForeignKey(
                        name: "FK_SalaryPackageAssignments_SalaryPackages_SalaryPackageId",
                        column: x => x.SalaryPackageId,
                        principalTable: "SalaryPackages",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict
                    );
                }
            );

            migrationBuilder.CreateIndex(
                name: "IX_AncienneteRates_RateSetId_SortOrder",
                table: "AncienneteRates",
                columns: new[] { "RateSetId", "SortOrder" },
                unique: true
            );

            migrationBuilder.CreateIndex(
                name: "IX_AncienneteRateSets_ClonedFromId",
                table: "AncienneteRateSets",
                column: "ClonedFromId"
            );

            migrationBuilder.CreateIndex(
                name: "IX_AncienneteRateSets_CompanyId_EffectiveFrom",
                table: "AncienneteRateSets",
                columns: new[] { "CompanyId", "EffectiveFrom" },
                unique: true,
                filter: "[DeletedAt] IS NULL"
            );

            migrationBuilder.CreateIndex(
                name: "IX_Authorities_Code",
                table: "Authorities",
                column: "Code",
                unique: true,
                filter: "[DeletedAt] IS NULL"
            );

            migrationBuilder.CreateIndex(
                name: "IX_BusinessSectors_Code",
                table: "BusinessSectors",
                column: "Code",
                unique: true,
                filter: "[DeletedAt] IS NULL"
            );

            migrationBuilder.CreateIndex(name: "IX_Cities_CountryId", table: "Cities", column: "CountryId");

            migrationBuilder.CreateIndex(name: "IX_Companies_CityId", table: "Companies", column: "CityId");

            migrationBuilder.CreateIndex(name: "IX_Companies_CountryId", table: "Companies", column: "CountryId");

            migrationBuilder.CreateIndex(
                name: "IX_Companies_ManagedByCompanyId",
                table: "Companies",
                column: "ManagedByCompanyId"
            );

            migrationBuilder.CreateIndex(
                name: "IX_CompanyDocuments_CompanyId",
                table: "CompanyDocuments",
                column: "CompanyId"
            );

            migrationBuilder.CreateIndex(
                name: "IX_ContractTypes_CompanyId",
                table: "ContractTypes",
                column: "CompanyId"
            );

            migrationBuilder.CreateIndex(
                name: "IX_ContractTypes_LegalContractTypeId",
                table: "ContractTypes",
                column: "LegalContractTypeId"
            );

            migrationBuilder.CreateIndex(
                name: "IX_ContractTypes_StateEmploymentProgramId",
                table: "ContractTypes",
                column: "StateEmploymentProgramId"
            );

            migrationBuilder.CreateIndex(
                name: "IX_Countries_CountryCode",
                table: "Countries",
                column: "CountryCode",
                unique: true
            );

            migrationBuilder.CreateIndex(name: "IX_Departements_CompanyId", table: "Departements", column: "CompanyId");

            migrationBuilder.CreateIndex(
                name: "IX_EducationLevels_Code",
                table: "EducationLevels",
                column: "Code",
                unique: true
            );

            migrationBuilder.CreateIndex(
                name: "IX_ElementRules_AuthorityId",
                table: "ElementRules",
                column: "AuthorityId"
            );

            migrationBuilder.CreateIndex(name: "IX_ElementRules_ElementId", table: "ElementRules", column: "ElementId");

            migrationBuilder.CreateIndex(
                name: "IX_EligibilityCriteria_Code",
                table: "EligibilityCriteria",
                column: "Code",
                unique: true
            );

            migrationBuilder.CreateIndex(
                name: "IX_EmployeeAbsences_EmployeeId",
                table: "EmployeeAbsences",
                column: "EmployeeId"
            );

            migrationBuilder.CreateIndex(
                name: "IX_EmployeeAddresses_CityId",
                table: "EmployeeAddresses",
                column: "CityId"
            );

            migrationBuilder.CreateIndex(
                name: "IX_EmployeeAddresses_CountryId",
                table: "EmployeeAddresses",
                column: "CountryId"
            );

            migrationBuilder.CreateIndex(
                name: "IX_EmployeeAddresses_EmployeeId",
                table: "EmployeeAddresses",
                column: "EmployeeId"
            );

            migrationBuilder.CreateIndex(
                name: "IX_EmployeeAttendanceBreaks_EmployeeAttendanceId",
                table: "EmployeeAttendanceBreaks",
                column: "EmployeeAttendanceId"
            );

            migrationBuilder.CreateIndex(
                name: "IX_EmployeeAttendances_EmployeeId",
                table: "EmployeeAttendances",
                column: "EmployeeId"
            );

            migrationBuilder.CreateIndex(
                name: "IX_EmployeeCategories_CompanyId",
                table: "EmployeeCategories",
                column: "CompanyId"
            );

            migrationBuilder.CreateIndex(
                name: "IX_EmployeeChildren_EmployeeId",
                table: "EmployeeChildren",
                column: "EmployeeId"
            );

            migrationBuilder.CreateIndex(
                name: "IX_EmployeeChildren_GenderId",
                table: "EmployeeChildren",
                column: "GenderId"
            );

            migrationBuilder.CreateIndex(
                name: "IX_EmployeeContracts_CompanyId",
                table: "EmployeeContracts",
                column: "CompanyId"
            );

            migrationBuilder.CreateIndex(
                name: "IX_EmployeeContracts_ContractTypeId",
                table: "EmployeeContracts",
                column: "ContractTypeId"
            );

            migrationBuilder.CreateIndex(
                name: "IX_EmployeeContracts_EmployeeId",
                table: "EmployeeContracts",
                column: "EmployeeId"
            );

            migrationBuilder.CreateIndex(
                name: "IX_EmployeeContracts_JobPositionId",
                table: "EmployeeContracts",
                column: "JobPositionId"
            );

            migrationBuilder.CreateIndex(
                name: "IX_EmployeeDocuments_EmployeeId",
                table: "EmployeeDocuments",
                column: "EmployeeId"
            );

            migrationBuilder.CreateIndex(
                name: "IX_EmployeeOvertimes_EmployeeId",
                table: "EmployeeOvertimes",
                column: "EmployeeId"
            );

            migrationBuilder.CreateIndex(
                name: "IX_EmployeeOvertimes_HolidayId",
                table: "EmployeeOvertimes",
                column: "HolidayId"
            );

            migrationBuilder.CreateIndex(
                name: "IX_EmployeeOvertimes_RateRuleId",
                table: "EmployeeOvertimes",
                column: "RateRuleId"
            );

            migrationBuilder.CreateIndex(name: "IX_Employees_CategoryId", table: "Employees", column: "CategoryId");

            migrationBuilder.CreateIndex(name: "IX_Employees_CompanyId", table: "Employees", column: "CompanyId");

            migrationBuilder.CreateIndex(name: "IX_Employees_CountryId", table: "Employees", column: "CountryId");

            migrationBuilder.CreateIndex(
                name: "IX_Employees_DepartementId",
                table: "Employees",
                column: "DepartementId"
            );

            migrationBuilder.CreateIndex(
                name: "IX_Employees_EducationLevelId",
                table: "Employees",
                column: "EducationLevelId"
            );

            migrationBuilder.CreateIndex(name: "IX_Employees_GenderId", table: "Employees", column: "GenderId");

            migrationBuilder.CreateIndex(name: "IX_Employees_ManagerId", table: "Employees", column: "ManagerId");

            migrationBuilder.CreateIndex(
                name: "IX_Employees_MaritalStatusId",
                table: "Employees",
                column: "MaritalStatusId"
            );

            migrationBuilder.CreateIndex(
                name: "IX_Employees_NationalityId",
                table: "Employees",
                column: "NationalityId"
            );

            migrationBuilder.CreateIndex(name: "IX_Employees_StatusId", table: "Employees", column: "StatusId");

            migrationBuilder.CreateIndex(
                name: "IX_EmployeeSalaries_ContractId",
                table: "EmployeeSalaries",
                column: "ContractId"
            );

            migrationBuilder.CreateIndex(
                name: "IX_EmployeeSalaries_EmployeeId",
                table: "EmployeeSalaries",
                column: "EmployeeId"
            );

            migrationBuilder.CreateIndex(
                name: "IX_EmployeeSalaryComponents_EmployeeSalaryId",
                table: "EmployeeSalaryComponents",
                column: "EmployeeSalaryId"
            );

            migrationBuilder.CreateIndex(
                name: "IX_EmployeeSpouses_EmployeeId",
                table: "EmployeeSpouses",
                column: "EmployeeId"
            );

            migrationBuilder.CreateIndex(
                name: "IX_EmployeeSpouses_GenderId",
                table: "EmployeeSpouses",
                column: "GenderId"
            );

            migrationBuilder.CreateIndex(name: "IX_Genders_Code", table: "Genders", column: "Code", unique: true);

            migrationBuilder.CreateIndex(name: "IX_Holidays_CompanyId", table: "Holidays", column: "CompanyId");

            migrationBuilder.CreateIndex(name: "IX_Holidays_CountryId", table: "Holidays", column: "CountryId");

            migrationBuilder.CreateIndex(name: "IX_JobPositions_CompanyId", table: "JobPositions", column: "CompanyId");

            migrationBuilder.CreateIndex(
                name: "IX_LeaveAuditLogs_CompanyId",
                table: "LeaveAuditLogs",
                column: "CompanyId"
            );

            migrationBuilder.CreateIndex(
                name: "IX_LeaveAuditLogs_EmployeeId",
                table: "LeaveAuditLogs",
                column: "EmployeeId"
            );

            migrationBuilder.CreateIndex(
                name: "IX_LeaveAuditLogs_LeaveRequestId",
                table: "LeaveAuditLogs",
                column: "LeaveRequestId"
            );

            migrationBuilder.CreateIndex(
                name: "IX_LeaveBalances_CompanyId",
                table: "LeaveBalances",
                column: "CompanyId"
            );

            migrationBuilder.CreateIndex(
                name: "IX_LeaveBalances_EmployeeId_LeaveTypeId_Year_Month",
                table: "LeaveBalances",
                columns: new[] { "EmployeeId", "LeaveTypeId", "Year", "Month" },
                unique: true,
                filter: "[DeletedAt] IS NULL"
            );

            migrationBuilder.CreateIndex(
                name: "IX_LeaveBalances_LeaveTypeId",
                table: "LeaveBalances",
                column: "LeaveTypeId"
            );

            migrationBuilder.CreateIndex(
                name: "IX_LeaveCarryOverAgreements_CompanyId",
                table: "LeaveCarryOverAgreements",
                column: "CompanyId"
            );

            migrationBuilder.CreateIndex(
                name: "IX_LeaveCarryOverAgreements_EmployeeId",
                table: "LeaveCarryOverAgreements",
                column: "EmployeeId"
            );

            migrationBuilder.CreateIndex(
                name: "IX_LeaveCarryOverAgreements_LeaveTypeId",
                table: "LeaveCarryOverAgreements",
                column: "LeaveTypeId"
            );

            migrationBuilder.CreateIndex(
                name: "IX_LeaveRequestApprovalHistories_LeaveRequestId",
                table: "LeaveRequestApprovalHistories",
                column: "LeaveRequestId"
            );

            migrationBuilder.CreateIndex(
                name: "IX_LeaveRequestAttachments_LeaveRequestId",
                table: "LeaveRequestAttachments",
                column: "LeaveRequestId"
            );

            migrationBuilder.CreateIndex(
                name: "IX_LeaveRequestExemptions_EmployeeAbsenceId",
                table: "LeaveRequestExemptions",
                column: "EmployeeAbsenceId"
            );

            migrationBuilder.CreateIndex(
                name: "IX_LeaveRequestExemptions_HolidayId",
                table: "LeaveRequestExemptions",
                column: "HolidayId"
            );

            migrationBuilder.CreateIndex(
                name: "IX_LeaveRequestExemptions_LeaveRequestId",
                table: "LeaveRequestExemptions",
                column: "LeaveRequestId"
            );

            migrationBuilder.CreateIndex(
                name: "IX_LeaveRequests_CompanyId",
                table: "LeaveRequests",
                column: "CompanyId"
            );

            migrationBuilder.CreateIndex(
                name: "IX_LeaveRequests_EmployeeId",
                table: "LeaveRequests",
                column: "EmployeeId"
            );

            migrationBuilder.CreateIndex(
                name: "IX_LeaveRequests_LeaveTypeId",
                table: "LeaveRequests",
                column: "LeaveTypeId"
            );

            migrationBuilder.CreateIndex(
                name: "IX_LeaveRequests_LegalRuleId",
                table: "LeaveRequests",
                column: "LegalRuleId"
            );

            migrationBuilder.CreateIndex(name: "IX_LeaveRequests_PolicyId", table: "LeaveRequests", column: "PolicyId");

            migrationBuilder.CreateIndex(
                name: "IX_LeaveTypeLegalRules_LeaveTypeId",
                table: "LeaveTypeLegalRules",
                column: "LeaveTypeId"
            );

            migrationBuilder.CreateIndex(
                name: "IX_LeaveTypePolicies_CompanyId",
                table: "LeaveTypePolicies",
                column: "CompanyId"
            );

            migrationBuilder.CreateIndex(
                name: "IX_LeaveTypePolicies_LeaveTypeId",
                table: "LeaveTypePolicies",
                column: "LeaveTypeId"
            );

            migrationBuilder.CreateIndex(name: "IX_LeaveTypes_CompanyId", table: "LeaveTypes", column: "CompanyId");

            migrationBuilder.CreateIndex(
                name: "IX_LeaveTypes_LeaveCode",
                table: "LeaveTypes",
                column: "LeaveCode",
                unique: true,
                filter: "[DeletedAt] IS NULL"
            );

            migrationBuilder.CreateIndex(
                name: "IX_LegalContractTypes_Code",
                table: "LegalContractTypes",
                column: "Code",
                unique: true
            );

            migrationBuilder.CreateIndex(
                name: "IX_LegalParameters_Code_EffectiveFrom",
                table: "LegalParameters",
                columns: new[] { "Code", "EffectiveFrom" },
                unique: true
            );

            migrationBuilder.CreateIndex(
                name: "IX_MaritalStatuses_Code",
                table: "MaritalStatuses",
                column: "Code",
                unique: true
            );

            migrationBuilder.CreateIndex(
                name: "IX_OvertimeRateRules_Code",
                table: "OvertimeRateRules",
                column: "Code",
                unique: true,
                filter: "[DeletedAt] IS NULL"
            );

            migrationBuilder.CreateIndex(
                name: "IX_PayComponents_Code",
                table: "PayComponents",
                column: "Code",
                unique: true,
                filter: "[DeletedAt] IS NULL"
            );

            migrationBuilder.CreateIndex(
                name: "IX_PayrollCalculationAuditSteps_PayrollResultId",
                table: "PayrollCalculationAuditSteps",
                column: "PayrollResultId"
            );

            migrationBuilder.CreateIndex(
                name: "IX_PayrollResultPrimes_PayrollResultId",
                table: "PayrollResultPrimes",
                column: "PayrollResultId"
            );

            migrationBuilder.CreateIndex(
                name: "IX_PayrollResults_CompanyId",
                table: "PayrollResults",
                column: "CompanyId"
            );

            migrationBuilder.CreateIndex(
                name: "IX_PayrollResults_EmployeeId_Year_Month",
                table: "PayrollResults",
                columns: new[] { "EmployeeId", "Year", "Month" },
                filter: "[DeletedAt] IS NULL"
            );

            migrationBuilder.CreateIndex(
                name: "IX_Permissions_Name",
                table: "Permissions",
                column: "Name",
                unique: true,
                filter: "[DeletedAt] IS NULL"
            );

            migrationBuilder.CreateIndex(
                name: "IX_ReferentielElements_CategoryId",
                table: "ReferentielElements",
                column: "CategoryId"
            );

            migrationBuilder.CreateIndex(
                name: "IX_Roles_Name",
                table: "Roles",
                column: "Name",
                unique: true,
                filter: "[DeletedAt] IS NULL"
            );

            migrationBuilder.CreateIndex(
                name: "IX_RolesPermissions_PermissionId",
                table: "RolesPermissions",
                column: "PermissionId"
            );

            migrationBuilder.CreateIndex(
                name: "IX_RolesPermissions_RoleId_PermissionId",
                table: "RolesPermissions",
                columns: new[] { "RoleId", "PermissionId" },
                unique: true,
                filter: "[DeletedAt] IS NULL"
            );

            migrationBuilder.CreateIndex(name: "IX_RuleCaps_RuleId", table: "RuleCaps", column: "RuleId", unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_RuleDualCaps_RuleId",
                table: "RuleDualCaps",
                column: "RuleId",
                unique: true
            );

            migrationBuilder.CreateIndex(
                name: "IX_RuleFormulas_ParameterId",
                table: "RuleFormulas",
                column: "ParameterId"
            );

            migrationBuilder.CreateIndex(
                name: "IX_RuleFormulas_RuleId",
                table: "RuleFormulas",
                column: "RuleId",
                unique: true
            );

            migrationBuilder.CreateIndex(
                name: "IX_RulePercentages_EligibilityId",
                table: "RulePercentages",
                column: "EligibilityId"
            );

            migrationBuilder.CreateIndex(
                name: "IX_RulePercentages_RuleId",
                table: "RulePercentages",
                column: "RuleId",
                unique: true
            );

            migrationBuilder.CreateIndex(name: "IX_RuleTiers_RuleId", table: "RuleTiers", column: "RuleId");

            migrationBuilder.CreateIndex(
                name: "IX_RuleVariants_EligibilityId",
                table: "RuleVariants",
                column: "EligibilityId"
            );

            migrationBuilder.CreateIndex(name: "IX_RuleVariants_RuleId", table: "RuleVariants", column: "RuleId");

            migrationBuilder.CreateIndex(
                name: "IX_SalaryPackageAssignments_ContractId",
                table: "SalaryPackageAssignments",
                column: "ContractId"
            );

            migrationBuilder.CreateIndex(
                name: "IX_SalaryPackageAssignments_EmployeeId",
                table: "SalaryPackageAssignments",
                column: "EmployeeId"
            );

            migrationBuilder.CreateIndex(
                name: "IX_SalaryPackageAssignments_EmployeeSalaryId",
                table: "SalaryPackageAssignments",
                column: "EmployeeSalaryId"
            );

            migrationBuilder.CreateIndex(
                name: "IX_SalaryPackageAssignments_SalaryPackageId",
                table: "SalaryPackageAssignments",
                column: "SalaryPackageId"
            );

            migrationBuilder.CreateIndex(
                name: "IX_SalaryPackageItems_PayComponentId",
                table: "SalaryPackageItems",
                column: "PayComponentId"
            );

            migrationBuilder.CreateIndex(
                name: "IX_SalaryPackageItems_ReferentielElementId",
                table: "SalaryPackageItems",
                column: "ReferentielElementId"
            );

            migrationBuilder.CreateIndex(
                name: "IX_SalaryPackageItems_SalaryPackageId",
                table: "SalaryPackageItems",
                column: "SalaryPackageId"
            );

            migrationBuilder.CreateIndex(
                name: "IX_SalaryPackages_BusinessSectorId",
                table: "SalaryPackages",
                column: "BusinessSectorId"
            );

            migrationBuilder.CreateIndex(
                name: "IX_SalaryPackages_CompanyId",
                table: "SalaryPackages",
                column: "CompanyId"
            );

            migrationBuilder.CreateIndex(
                name: "IX_StateEmploymentPrograms_Code",
                table: "StateEmploymentPrograms",
                column: "Code",
                unique: true
            );

            migrationBuilder.CreateIndex(name: "IX_Statuses_Code", table: "Statuses", column: "Code", unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Users_Email",
                table: "Users",
                column: "Email",
                unique: true,
                filter: "[DeletedAt] IS NULL"
            );

            migrationBuilder.CreateIndex(name: "IX_Users_EmployeeId", table: "Users", column: "EmployeeId");

            migrationBuilder.CreateIndex(
                name: "IX_Users_Username",
                table: "Users",
                column: "Username",
                unique: true,
                filter: "[DeletedAt] IS NULL"
            );

            migrationBuilder.CreateIndex(name: "IX_UsersRoles_RoleId", table: "UsersRoles", column: "RoleId");

            migrationBuilder.CreateIndex(
                name: "IX_UsersRoles_UserId_RoleId",
                table: "UsersRoles",
                columns: new[] { "UserId", "RoleId" },
                unique: true,
                filter: "[DeletedAt] IS NULL"
            );

            migrationBuilder.CreateIndex(
                name: "IX_WorkingCalendars_CompanyId",
                table: "WorkingCalendars",
                column: "CompanyId"
            );
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "AncienneteRates");

            migrationBuilder.DropTable(name: "CompanyDocuments");

            migrationBuilder.DropTable(name: "CompanyEventLogs");

            migrationBuilder.DropTable(name: "EmployeeAddresses");

            migrationBuilder.DropTable(name: "EmployeeAttendanceBreaks");

            migrationBuilder.DropTable(name: "EmployeeChildren");

            migrationBuilder.DropTable(name: "EmployeeDocuments");

            migrationBuilder.DropTable(name: "EmployeeEventLogs");

            migrationBuilder.DropTable(name: "EmployeeOvertimes");

            migrationBuilder.DropTable(name: "EmployeeSalaryComponents");

            migrationBuilder.DropTable(name: "EmployeeSpouses");

            migrationBuilder.DropTable(name: "LeaveAuditLogs");

            migrationBuilder.DropTable(name: "LeaveBalances");

            migrationBuilder.DropTable(name: "LeaveCarryOverAgreements");

            migrationBuilder.DropTable(name: "LeaveRequestApprovalHistories");

            migrationBuilder.DropTable(name: "LeaveRequestAttachments");

            migrationBuilder.DropTable(name: "LeaveRequestExemptions");

            migrationBuilder.DropTable(name: "PayrollCalculationAuditSteps");

            migrationBuilder.DropTable(name: "PayrollResultPrimes");

            migrationBuilder.DropTable(name: "RolesPermissions");

            migrationBuilder.DropTable(name: "RuleCaps");

            migrationBuilder.DropTable(name: "RuleDualCaps");

            migrationBuilder.DropTable(name: "RuleFormulas");

            migrationBuilder.DropTable(name: "RulePercentages");

            migrationBuilder.DropTable(name: "RuleTiers");

            migrationBuilder.DropTable(name: "RuleVariants");

            migrationBuilder.DropTable(name: "SalaryPackageAssignments");

            migrationBuilder.DropTable(name: "SalaryPackageItems");

            migrationBuilder.DropTable(name: "UsersRoles");

            migrationBuilder.DropTable(name: "WorkingCalendars");

            migrationBuilder.DropTable(name: "AncienneteRateSets");

            migrationBuilder.DropTable(name: "EmployeeAttendances");

            migrationBuilder.DropTable(name: "OvertimeRateRules");

            migrationBuilder.DropTable(name: "EmployeeAbsences");

            migrationBuilder.DropTable(name: "Holidays");

            migrationBuilder.DropTable(name: "LeaveRequests");

            migrationBuilder.DropTable(name: "PayrollResults");

            migrationBuilder.DropTable(name: "Permissions");

            migrationBuilder.DropTable(name: "LegalParameters");

            migrationBuilder.DropTable(name: "ElementRules");

            migrationBuilder.DropTable(name: "EligibilityCriteria");

            migrationBuilder.DropTable(name: "EmployeeSalaries");

            migrationBuilder.DropTable(name: "PayComponents");

            migrationBuilder.DropTable(name: "SalaryPackages");

            migrationBuilder.DropTable(name: "Roles");

            migrationBuilder.DropTable(name: "Users");

            migrationBuilder.DropTable(name: "LeaveTypeLegalRules");

            migrationBuilder.DropTable(name: "LeaveTypePolicies");

            migrationBuilder.DropTable(name: "Authorities");

            migrationBuilder.DropTable(name: "ReferentielElements");

            migrationBuilder.DropTable(name: "EmployeeContracts");

            migrationBuilder.DropTable(name: "BusinessSectors");

            migrationBuilder.DropTable(name: "LeaveTypes");

            migrationBuilder.DropTable(name: "ElementCategories");

            migrationBuilder.DropTable(name: "ContractTypes");

            migrationBuilder.DropTable(name: "Employees");

            migrationBuilder.DropTable(name: "JobPositions");

            migrationBuilder.DropTable(name: "LegalContractTypes");

            migrationBuilder.DropTable(name: "StateEmploymentPrograms");

            migrationBuilder.DropTable(name: "Departements");

            migrationBuilder.DropTable(name: "EducationLevels");

            migrationBuilder.DropTable(name: "EmployeeCategories");

            migrationBuilder.DropTable(name: "Genders");

            migrationBuilder.DropTable(name: "MaritalStatuses");

            migrationBuilder.DropTable(name: "Nationalities");

            migrationBuilder.DropTable(name: "Statuses");

            migrationBuilder.DropTable(name: "Companies");

            migrationBuilder.DropTable(name: "Cities");

            migrationBuilder.DropTable(name: "Countries");
        }
    }
}
