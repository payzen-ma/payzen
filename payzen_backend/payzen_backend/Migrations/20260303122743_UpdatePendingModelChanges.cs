using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace payzen_backend.Migrations
{
    /// <inheritdoc />
    public partial class UpdatePendingModelChanges : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Authorities",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Code = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    SortOrder = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                    IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false, defaultValueSql: "GETUTCDATE()"),
                    CreatedBy = table.Column<int>(type: "int", nullable: false),
                    ModifiedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    ModifiedBy = table.Column<int>(type: "int", nullable: true),
                    DeletedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    DeletedBy = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Authorities", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "BusinessSectors",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Code = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    IsStandard = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    SortOrder = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false, defaultValueSql: "GETUTCDATE()"),
                    CreatedBy = table.Column<int>(type: "int", nullable: false),
                    ModifiedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    ModifiedBy = table.Column<int>(type: "int", nullable: true),
                    DeletedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    DeletedBy = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BusinessSectors", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Country",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CountryName = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    CountryNameAr = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    CountryCode = table.Column<string>(type: "nvarchar(3)", maxLength: 3, nullable: false),
                    CountryPhoneCode = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    CreatedBy = table.Column<int>(type: "int", nullable: false),
                    ModifiedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    ModifiedBy = table.Column<int>(type: "int", nullable: true),
                    DeletedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    DeletedBy = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Country", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "EducationLevel",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Code = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    NameFr = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    NameAr = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    NameEn = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    LevelOrder = table.Column<int>(type: "int", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    CreatedBy = table.Column<int>(type: "int", nullable: false),
                    ModifiedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    ModifiedBy = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EducationLevel", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ElementCategories",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    SortOrder = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                    IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false, defaultValueSql: "GETUTCDATE()"),
                    CreatedBy = table.Column<int>(type: "int", nullable: false),
                    ModifiedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    ModifiedBy = table.Column<int>(type: "int", nullable: true),
                    DeletedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    DeletedBy = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ElementCategories", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "EligibilityCriteria",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Code = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false, defaultValueSql: "GETUTCDATE()"),
                    CreatedBy = table.Column<int>(type: "int", nullable: false),
                    ModifiedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    ModifiedBy = table.Column<int>(type: "int", nullable: true),
                    DeletedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    DeletedBy = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EligibilityCriteria", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Gender",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Code = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    NameFr = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    NameAr = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    NameEn = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    CreatedBy = table.Column<int>(type: "int", nullable: false),
                    ModifiedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    ModifiedBy = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Gender", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "LegalContractType",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Code = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    DeletedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    CreatedBy = table.Column<int>(type: "int", nullable: false),
                    UpdatedBy = table.Column<int>(type: "int", nullable: true),
                    DeletedBy = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LegalContractType", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "LegalParameters",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Code = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Label = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    Value = table.Column<decimal>(type: "decimal(18,4)", nullable: false),
                    Unit = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Source = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    EffectiveFrom = table.Column<DateOnly>(type: "date", nullable: false),
                    EffectiveTo = table.Column<DateOnly>(type: "date", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false, defaultValueSql: "GETUTCDATE()"),
                    CreatedBy = table.Column<int>(type: "int", nullable: false),
                    ModifiedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    ModifiedBy = table.Column<int>(type: "int", nullable: true),
                    DeletedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    DeletedBy = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LegalParameters", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "MaritalStatus",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Code = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    NameFr = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    NameAr = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    NameEn = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    CreatedBy = table.Column<int>(type: "int", nullable: false),
                    ModifiedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    ModifiedBy = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MaritalStatus", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Nationality",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    CreatedBy = table.Column<int>(type: "int", nullable: false),
                    ModifiedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    ModifiedBy = table.Column<int>(type: "int", nullable: true),
                    DeletedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    DeletedBy = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Nationality", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "OvertimeRateRules",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Code = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    NameAr = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    NameFr = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    NameEn = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    AppliesTo = table.Column<int>(type: "int", nullable: false),
                    TimeRangeType = table.Column<int>(type: "int", nullable: false),
                    StartTime = table.Column<TimeOnly>(type: "time", nullable: true),
                    EndTime = table.Column<TimeOnly>(type: "time", nullable: true),
                    ApplicableDaysOfWeek = table.Column<int>(type: "int", nullable: true),
                    Multiplier = table.Column<decimal>(type: "decimal(5,2)", precision: 5, scale: 2, nullable: false),
                    CumulationStrategy = table.Column<int>(type: "int", nullable: false),
                    Priority = table.Column<int>(type: "int", nullable: false),
                    Category = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    EffectiveFrom = table.Column<DateOnly>(type: "date", nullable: true),
                    EffectiveTo = table.Column<DateOnly>(type: "date", nullable: true),
                    MinimumDurationHours = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    MaximumDurationHours = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    RequiresSuperiorApproval = table.Column<bool>(type: "bit", nullable: false),
                    LegalReference = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    DocumentationUrl = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    CreatedBy = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    ModifiedBy = table.Column<int>(type: "int", nullable: true),
                    ModifiedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    DeletedBy = table.Column<int>(type: "int", nullable: true),
                    DeletedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OvertimeRateRules", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "PayComponent",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Code = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    NameFr = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    NameAr = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    NameEn = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    Type = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    IsTaxable = table.Column<bool>(type: "bit", nullable: false),
                    IsSocial = table.Column<bool>(type: "bit", nullable: false),
                    IsCIMR = table.Column<bool>(type: "bit", nullable: false),
                    ExemptionLimit = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    ExemptionRule = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    DefaultAmount = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    Version = table.Column<int>(type: "int", nullable: false, defaultValue: 1),
                    ValidFrom = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ValidTo = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsRegulated = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    SortOrder = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    CreatedBy = table.Column<int>(type: "int", nullable: false),
                    ModifiedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    ModifiedBy = table.Column<int>(type: "int", nullable: true),
                    DeletedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    DeletedBy = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PayComponent", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Permissions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    Resource = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Action = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    DeletedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    CreatedBy = table.Column<int>(type: "int", nullable: false),
                    UpdatedBy = table.Column<int>(type: "int", nullable: true),
                    DeletedBy = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Permissions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Roles",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    DeletedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    CreatedBy = table.Column<int>(type: "int", nullable: false),
                    UpdatedBy = table.Column<int>(type: "int", nullable: true),
                    DeletedBy = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Roles", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "StateEmploymentProgram",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Code = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    IsIrExempt = table.Column<bool>(type: "bit", nullable: false),
                    IsCnssEmployeeExempt = table.Column<bool>(type: "bit", nullable: false),
                    IsCnssEmployerExempt = table.Column<bool>(type: "bit", nullable: false),
                    MaxDurationMonths = table.Column<int>(type: "int", nullable: true),
                    SalaryCeiling = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    DeletedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    CreatedBy = table.Column<int>(type: "int", nullable: false),
                    UpdatedBy = table.Column<int>(type: "int", nullable: true),
                    DeletedBy = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StateEmploymentProgram", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Status",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Code = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    NameFr = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    NameAr = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    NameEn = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    AffectsAccess = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    AffectsPayroll = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    AffectsAttendance = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    CreatedBy = table.Column<int>(type: "int", nullable: false),
                    ModifiedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    ModifiedBy = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Status", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "City",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CityName = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    CountryId = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    CreatedBy = table.Column<int>(type: "int", nullable: false),
                    ModifiedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    ModifiedBy = table.Column<int>(type: "int", nullable: true),
                    DeletedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    DeletedBy = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_City", x => x.Id);
                    table.ForeignKey(
                        name: "FK_City_Country_CountryId",
                        column: x => x.CountryId,
                        principalTable: "Country",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ReferentielElements",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Code = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Name = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    CategoryId = table.Column<int>(type: "int", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    DefaultFrequency = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    HasConvergence = table.Column<bool>(type: "bit", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false, defaultValueSql: "GETUTCDATE()"),
                    CreatedBy = table.Column<int>(type: "int", nullable: false),
                    ModifiedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    ModifiedBy = table.Column<int>(type: "int", nullable: true),
                    DeletedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    DeletedBy = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ReferentielElements", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ReferentielElements_ElementCategories_CategoryId",
                        column: x => x.CategoryId,
                        principalTable: "ElementCategories",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "RolesPermissions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    RoleId = table.Column<int>(type: "int", nullable: false),
                    PermissionId = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    DeletedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    CreatedBy = table.Column<int>(type: "int", nullable: false),
                    UpdatedBy = table.Column<int>(type: "int", nullable: true),
                    DeletedBy = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RolesPermissions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RolesPermissions_Permissions_PermissionId",
                        column: x => x.PermissionId,
                        principalTable: "Permissions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_RolesPermissions_Roles_RoleId",
                        column: x => x.RoleId,
                        principalTable: "Roles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Company",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    company_name = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    email = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    phone_number = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    country_phone_code = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: true),
                    company_address = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    city_id = table.Column<int>(type: "int", nullable: false),
                    country_id = table.Column<int>(type: "int", nullable: false),
                    cnss_number = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    is_cabinet_expert = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    ice_number = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    if_number = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    rc_number = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    patente_number = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    rib_number = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    legal_form = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    founding_date = table.Column<DateTime>(type: "date", nullable: true),
                    website_url = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    currency = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false, defaultValue: "MAD"),
                    payroll_periodicity = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false, defaultValue: "Mensuelle"),
                    fiscal_year_start_month = table.Column<int>(type: "int", nullable: false, defaultValue: 1),
                    business_sector = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    payment_method = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    managedby_company_id = table.Column<int>(type: "int", nullable: true),
                    isActive = table.Column<bool>(type: "bit", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    created_by = table.Column<int>(type: "int", nullable: false),
                    modified_at = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    modified_by = table.Column<int>(type: "int", nullable: true),
                    deleted_at = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    deleted_by = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Company", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Company_City_city_id",
                        column: x => x.city_id,
                        principalTable: "City",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Company_Company_managedby_company_id",
                        column: x => x.managedby_company_id,
                        principalTable: "Company",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Company_Country_country_id",
                        column: x => x.country_id,
                        principalTable: "Country",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ElementRules",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ElementId = table.Column<int>(type: "int", nullable: false),
                    AuthorityId = table.Column<int>(type: "int", nullable: false),
                    ExemptionType = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    RuleDetails = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    SourceRef = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    EffectiveFrom = table.Column<DateOnly>(type: "date", nullable: false),
                    EffectiveTo = table.Column<DateOnly>(type: "date", nullable: true),
                    Status = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false, defaultValueSql: "GETUTCDATE()"),
                    CreatedBy = table.Column<int>(type: "int", nullable: false),
                    ModifiedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    ModifiedBy = table.Column<int>(type: "int", nullable: true),
                    DeletedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    DeletedBy = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ElementRules", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ElementRules_Authorities_AuthorityId",
                        column: x => x.AuthorityId,
                        principalTable: "Authorities",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ElementRules_ReferentielElements_ElementId",
                        column: x => x.ElementId,
                        principalTable: "ReferentielElements",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AncienneteRateSets",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CompanyId = table.Column<int>(type: "int", nullable: true),
                    ClonedFromId = table.Column<int>(type: "int", nullable: true),
                    Code = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    IsLegalDefault = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    Source = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    EffectiveFrom = table.Column<DateOnly>(type: "date", nullable: false),
                    EffectiveTo = table.Column<DateOnly>(type: "date", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false, defaultValueSql: "GETUTCDATE()"),
                    CreatedBy = table.Column<int>(type: "int", nullable: false),
                    ModifiedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    ModifiedBy = table.Column<int>(type: "int", nullable: true),
                    DeletedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    DeletedBy = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AncienneteRateSets", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AncienneteRateSets_AncienneteRateSets_ClonedFromId",
                        column: x => x.ClonedFromId,
                        principalTable: "AncienneteRateSets",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_AncienneteRateSets_Company_CompanyId",
                        column: x => x.CompanyId,
                        principalTable: "Company",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "CompanyDocument",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CompanyId = table.Column<int>(type: "int", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    FilePath = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    DocumentType = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    created_by = table.Column<int>(type: "int", nullable: false),
                    deleted_at = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    deleted_by = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CompanyDocument", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CompanyDocument_Company_CompanyId",
                        column: x => x.CompanyId,
                        principalTable: "Company",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ContractType",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ContractTypeName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    CompanyId = table.Column<int>(type: "int", nullable: false),
                    LegalContractTypeId = table.Column<int>(type: "int", nullable: true),
                    StateEmploymentProgramId = table.Column<int>(type: "int", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    DeletedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    CreatedBy = table.Column<int>(type: "int", nullable: false),
                    UpdatedBy = table.Column<int>(type: "int", nullable: true),
                    DeletedBy = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ContractType", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ContractType_Company_CompanyId",
                        column: x => x.CompanyId,
                        principalTable: "Company",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ContractType_LegalContractType_LegalContractTypeId",
                        column: x => x.LegalContractTypeId,
                        principalTable: "LegalContractType",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ContractType_StateEmploymentProgram_StateEmploymentProgramId",
                        column: x => x.StateEmploymentProgramId,
                        principalTable: "StateEmploymentProgram",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Departement",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    DepartementName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    CompanyId = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    DeletedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    CreatedBy = table.Column<int>(type: "int", nullable: false),
                    UpdatedBy = table.Column<int>(type: "int", nullable: true),
                    DeletedBy = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Departement", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Departement_Company_CompanyId",
                        column: x => x.CompanyId,
                        principalTable: "Company",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "EmployeeCategory",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CompanyId = table.Column<int>(type: "int", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    Mode = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    CreatedBy = table.Column<int>(type: "int", nullable: false),
                    ModifiedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    ModifiedBy = table.Column<int>(type: "int", nullable: true),
                    DeletedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    DeletedBy = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EmployeeCategory", x => x.Id);
                    table.ForeignKey(
                        name: "FK_EmployeeCategory_Company_CompanyId",
                        column: x => x.CompanyId,
                        principalTable: "Company",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Holiday",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    NameFr = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    NameAr = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    NameEn = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    HolidayDate = table.Column<DateOnly>(type: "date", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    CompanyId = table.Column<int>(type: "int", nullable: true),
                    CountryId = table.Column<int>(type: "int", nullable: false),
                    Scope = table.Column<int>(type: "int", nullable: false),
                    HolidayType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    IsMandatory = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    IsPaid = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    IsRecurring = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    RecurrenceRule = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    Year = table.Column<int>(type: "int", nullable: true),
                    AffectPayroll = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    AffectAttendance = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    ModifiedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    DeletedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    CreatedBy = table.Column<int>(type: "int", nullable: false),
                    ModifiedBy = table.Column<int>(type: "int", nullable: true),
                    DeletedBy = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Holiday", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Holiday_Company_CompanyId",
                        column: x => x.CompanyId,
                        principalTable: "Company",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Holiday_Country_CountryId",
                        column: x => x.CountryId,
                        principalTable: "Country",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "JobPosition",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    CompanyId = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    CreatedBy = table.Column<int>(type: "int", nullable: false),
                    ModifiedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    ModifiedBy = table.Column<int>(type: "int", nullable: true),
                    DeletedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    DeletedBy = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_JobPosition", x => x.Id);
                    table.ForeignKey(
                        name: "FK_JobPosition_Company_CompanyId",
                        column: x => x.CompanyId,
                        principalTable: "Company",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "LeaveType",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    LeaveCode = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    LeaveNameAr = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    LeaveNameEn = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    LeaveNameFr = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    LeaveDescription = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    Scope = table.Column<int>(type: "int", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CompanyId = table.Column<int>(type: "int", nullable: true),
                    CreatedBy = table.Column<int>(type: "int", nullable: false),
                    UpdatedBy = table.Column<int>(type: "int", nullable: true),
                    DeletedBy = table.Column<int>(type: "int", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    DeletedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LeaveType", x => x.Id);
                    table.ForeignKey(
                        name: "FK_LeaveType_Company_CompanyId",
                        column: x => x.CompanyId,
                        principalTable: "Company",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "SalaryPackage",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Category = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    BaseSalary = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false, defaultValue: "draft"),
                    CompanyId = table.Column<int>(type: "int", nullable: true),
                    BusinessSectorId = table.Column<int>(type: "int", nullable: false),
                    TemplateType = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false, defaultValue: "OFFICIAL"),
                    RegulationVersion = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false, defaultValue: "MA_2025"),
                    AutoRulesJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CimrConfigJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    OriginType = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: true),
                    SourceTemplateNameSnapshot = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    CopiedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CimrRate = table.Column<decimal>(type: "decimal(5,4)", nullable: true),
                    HasPrivateInsurance = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    Version = table.Column<int>(type: "int", nullable: false, defaultValue: 1),
                    SourceTemplateId = table.Column<int>(type: "int", nullable: true),
                    SourceTemplateVersion = table.Column<int>(type: "int", nullable: true),
                    ValidFrom = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ValidTo = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsLocked = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    CreatedBy = table.Column<int>(type: "int", nullable: false),
                    ModifiedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    ModifiedBy = table.Column<int>(type: "int", nullable: true),
                    DeletedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    DeletedBy = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SalaryPackage", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SalaryPackage_BusinessSectors_BusinessSectorId",
                        column: x => x.BusinessSectorId,
                        principalTable: "BusinessSectors",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_SalaryPackage_Company_CompanyId",
                        column: x => x.CompanyId,
                        principalTable: "Company",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SalaryPackage_SalaryPackage_SourceTemplateId",
                        column: x => x.SourceTemplateId,
                        principalTable: "SalaryPackage",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "WorkingCalendar",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CompanyId = table.Column<int>(type: "int", nullable: false),
                    DayOfWeek = table.Column<int>(type: "int", nullable: false),
                    IsWorkingDay = table.Column<bool>(type: "bit", nullable: false),
                    StartTime = table.Column<TimeSpan>(type: "time", nullable: true),
                    EndTime = table.Column<TimeSpan>(type: "time", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    CreatedBy = table.Column<int>(type: "int", nullable: false),
                    ModifiedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    ModifiedBy = table.Column<int>(type: "int", nullable: true),
                    DeletedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    DeletedBy = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WorkingCalendar", x => x.Id);
                    table.ForeignKey(
                        name: "FK_WorkingCalendar_Company_CompanyId",
                        column: x => x.CompanyId,
                        principalTable: "Company",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "RuleCaps",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    RuleId = table.Column<int>(type: "int", nullable: false),
                    CapAmount = table.Column<decimal>(type: "decimal(18,4)", nullable: false),
                    CapUnit = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    MinAmount = table.Column<decimal>(type: "decimal(18,4)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RuleCaps", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RuleCaps_ElementRules_RuleId",
                        column: x => x.RuleId,
                        principalTable: "ElementRules",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "RuleDualCaps",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    RuleId = table.Column<int>(type: "int", nullable: false),
                    FixedCapAmount = table.Column<decimal>(type: "decimal(18,4)", nullable: false),
                    FixedCapUnit = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    PercentageCap = table.Column<decimal>(type: "decimal(5,4)", nullable: false),
                    BaseReference = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    Logic = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RuleDualCaps", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RuleDualCaps_ElementRules_RuleId",
                        column: x => x.RuleId,
                        principalTable: "ElementRules",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "RuleFormulas",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    RuleId = table.Column<int>(type: "int", nullable: false),
                    Multiplier = table.Column<decimal>(type: "decimal(10,4)", nullable: false),
                    ParameterId = table.Column<int>(type: "int", nullable: false),
                    ResultUnit = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RuleFormulas", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RuleFormulas_ElementRules_RuleId",
                        column: x => x.RuleId,
                        principalTable: "ElementRules",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_RuleFormulas_LegalParameters_ParameterId",
                        column: x => x.ParameterId,
                        principalTable: "LegalParameters",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "RulePercentages",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    RuleId = table.Column<int>(type: "int", nullable: false),
                    Percentage = table.Column<decimal>(type: "decimal(5,4)", nullable: false),
                    BaseReference = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    EligibilityId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RulePercentages", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RulePercentages_ElementRules_RuleId",
                        column: x => x.RuleId,
                        principalTable: "ElementRules",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_RulePercentages_EligibilityCriteria_EligibilityId",
                        column: x => x.EligibilityId,
                        principalTable: "EligibilityCriteria",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "RuleTiers",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    RuleId = table.Column<int>(type: "int", nullable: false),
                    TierOrder = table.Column<int>(type: "int", nullable: false),
                    FromAmount = table.Column<decimal>(type: "decimal(18,4)", nullable: false),
                    ToAmount = table.Column<decimal>(type: "decimal(18,4)", nullable: true),
                    ExemptPercent = table.Column<decimal>(type: "decimal(5,4)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RuleTiers", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RuleTiers_ElementRules_RuleId",
                        column: x => x.RuleId,
                        principalTable: "ElementRules",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "RuleVariants",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    RuleId = table.Column<int>(type: "int", nullable: false),
                    VariantType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    VariantKey = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    VariantLabel = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    OverrideCap = table.Column<decimal>(type: "decimal(18,4)", nullable: true),
                    OverridePercentage = table.Column<decimal>(type: "decimal(5,4)", nullable: true),
                    EligibilityId = table.Column<int>(type: "int", nullable: true),
                    SortOrder = table.Column<int>(type: "int", nullable: false, defaultValue: 0)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RuleVariants", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RuleVariants_ElementRules_RuleId",
                        column: x => x.RuleId,
                        principalTable: "ElementRules",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_RuleVariants_EligibilityCriteria_EligibilityId",
                        column: x => x.EligibilityId,
                        principalTable: "EligibilityCriteria",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "AncienneteRates",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    RateSetId = table.Column<int>(type: "int", nullable: false),
                    MinYears = table.Column<int>(type: "int", nullable: false),
                    MaxYears = table.Column<int>(type: "int", nullable: true),
                    Rate = table.Column<decimal>(type: "decimal(5,4)", nullable: false),
                    SortOrder = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AncienneteRates", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AncienneteRates_AncienneteRateSets_RateSetId",
                        column: x => x.RateSetId,
                        principalTable: "AncienneteRateSets",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Employee",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    first_name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    last_name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    cin_number = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    date_of_birth = table.Column<DateOnly>(type: "date", nullable: false),
                    phone = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    email = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    personal_email = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    company_id = table.Column<int>(type: "int", nullable: false),
                    manager_id = table.Column<int>(type: "int", nullable: true),
                    departement_id = table.Column<int>(type: "int", nullable: true),
                    status_id = table.Column<int>(type: "int", nullable: true),
                    gender_id = table.Column<int>(type: "int", nullable: true),
                    nationality_id = table.Column<int>(type: "int", nullable: true),
                    education_level_id = table.Column<int>(type: "int", nullable: true),
                    marital_status_id = table.Column<int>(type: "int", nullable: true),
                    cnss_number = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    cimr_number = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    cimr_employee_rate = table.Column<decimal>(type: "decimal(5,2)", nullable: true),
                    cimr_company_rate = table.Column<decimal>(type: "decimal(5,2)", nullable: true),
                    has_private_insurance = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    private_insurance_number = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    private_insurance_rate = table.Column<decimal>(type: "decimal(5,2)", nullable: true),
                    disable_amo = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    created_at = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    created_by = table.Column<int>(type: "int", nullable: false),
                    modified_at = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    modified_by = table.Column<int>(type: "int", nullable: true),
                    deleted_at = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    deleted_by = table.Column<int>(type: "int", nullable: true),
                    employee_category_id = table.Column<int>(type: "int", nullable: true),
                    CountryId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Employee", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Employee_Company_company_id",
                        column: x => x.company_id,
                        principalTable: "Company",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Employee_Country_CountryId",
                        column: x => x.CountryId,
                        principalTable: "Country",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Employee_Departement_departement_id",
                        column: x => x.departement_id,
                        principalTable: "Departement",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Employee_EducationLevel_education_level_id",
                        column: x => x.education_level_id,
                        principalTable: "EducationLevel",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Employee_EmployeeCategory_employee_category_id",
                        column: x => x.employee_category_id,
                        principalTable: "EmployeeCategory",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Employee_Employee_manager_id",
                        column: x => x.manager_id,
                        principalTable: "Employee",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Employee_Gender_gender_id",
                        column: x => x.gender_id,
                        principalTable: "Gender",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Employee_MaritalStatus_marital_status_id",
                        column: x => x.marital_status_id,
                        principalTable: "MaritalStatus",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Employee_Nationality_nationality_id",
                        column: x => x.nationality_id,
                        principalTable: "Nationality",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Employee_Status_status_id",
                        column: x => x.status_id,
                        principalTable: "Status",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "LeaveTypeLegalRule",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    LeaveTypeId = table.Column<int>(type: "int", nullable: false),
                    EventCaseCode = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: false),
                    DaysGranted = table.Column<int>(type: "int", nullable: false),
                    LegalArticle = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    CanBeDiscountinuous = table.Column<bool>(type: "bit", nullable: false),
                    MustBeUsedWithinDays = table.Column<int>(type: "int", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    CreatedBy = table.Column<int>(type: "int", nullable: false),
                    ModifiedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    ModifiedBy = table.Column<int>(type: "int", nullable: true),
                    DeletedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    DeletedBy = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LeaveTypeLegalRule", x => x.Id);
                    table.ForeignKey(
                        name: "FK_LeaveTypeLegalRule_LeaveType_LeaveTypeId",
                        column: x => x.LeaveTypeId,
                        principalTable: "LeaveType",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "LeaveTypePolicy",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
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
                    ModifiedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    ModifiedBy = table.Column<int>(type: "int", nullable: true),
                    DeletedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    DeletedBy = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LeaveTypePolicy", x => x.Id);
                    table.ForeignKey(
                        name: "FK_LeaveTypePolicy_Company_CompanyId",
                        column: x => x.CompanyId,
                        principalTable: "Company",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_LeaveTypePolicy_LeaveType_LeaveTypeId",
                        column: x => x.LeaveTypeId,
                        principalTable: "LeaveType",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SalaryPackageItem",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    SalaryPackageId = table.Column<int>(type: "int", nullable: false),
                    PayComponentId = table.Column<int>(type: "int", nullable: true),
                    ReferentielElementId = table.Column<int>(type: "int", nullable: true),
                    Label = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    DefaultValue = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    SortOrder = table.Column<int>(type: "int", nullable: false),
                    Type = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false, defaultValue: "allowance"),
                    IsTaxable = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    IsSocial = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    IsCIMR = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    IsVariable = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    ExemptionLimit = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    CreatedBy = table.Column<int>(type: "int", nullable: false),
                    ModifiedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    ModifiedBy = table.Column<int>(type: "int", nullable: true),
                    DeletedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    DeletedBy = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SalaryPackageItem", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SalaryPackageItem_PayComponent_PayComponentId",
                        column: x => x.PayComponentId,
                        principalTable: "PayComponent",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SalaryPackageItem_ReferentielElements_ReferentielElementId",
                        column: x => x.ReferentielElementId,
                        principalTable: "ReferentielElements",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SalaryPackageItem_SalaryPackage_SalaryPackageId",
                        column: x => x.SalaryPackageId,
                        principalTable: "SalaryPackage",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "EmployeeAbsence",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    employee_id = table.Column<int>(type: "int", nullable: false),
                    absence_date = table.Column<DateOnly>(type: "date", nullable: false),
                    duration_type = table.Column<int>(type: "int", nullable: false),
                    status = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                    decision_at = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    decision_by = table.Column<int>(type: "int", nullable: true),
                    decision_comment = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    is_morning = table.Column<bool>(type: "bit", nullable: true),
                    start_time = table.Column<TimeOnly>(type: "time", nullable: true),
                    end_time = table.Column<TimeOnly>(type: "time", nullable: true),
                    absence_type = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    reason = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    created_by = table.Column<int>(type: "int", nullable: false),
                    ModifiedBy = table.Column<int>(type: "int", nullable: true),
                    ModifiedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    DeletedBy = table.Column<int>(type: "int", nullable: true),
                    DeletedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EmployeeAbsence", x => x.Id);
                    table.ForeignKey(
                        name: "FK_EmployeeAbsence_Employee",
                        column: x => x.employee_id,
                        principalTable: "Employee",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "EmployeeAddress",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    EmployeeId = table.Column<int>(type: "int", nullable: false),
                    AddressLine1 = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    AddressLine2 = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    ZipCode = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    CityId = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    CreatedBy = table.Column<int>(type: "int", nullable: false),
                    ModifiedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    ModifiedBy = table.Column<int>(type: "int", nullable: true),
                    DeletedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    DeletedBy = table.Column<int>(type: "int", nullable: true),
                    CountryId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EmployeeAddress", x => x.Id);
                    table.ForeignKey(
                        name: "FK_EmployeeAddress_City_CityId",
                        column: x => x.CityId,
                        principalTable: "City",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_EmployeeAddress_Country_CountryId",
                        column: x => x.CountryId,
                        principalTable: "Country",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_EmployeeAddress_Employee_EmployeeId",
                        column: x => x.EmployeeId,
                        principalTable: "Employee",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "EmployeeAttendance",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    employee_id = table.Column<int>(type: "int", nullable: false),
                    work_date = table.Column<DateOnly>(type: "date", nullable: false),
                    check_in = table.Column<TimeOnly>(type: "time", nullable: true),
                    check_out = table.Column<TimeOnly>(type: "time", nullable: true),
                    break_minutes_applied = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                    status = table.Column<int>(type: "int", nullable: false),
                    source = table.Column<int>(type: "int", nullable: false),
                    worked_hours = table.Column<decimal>(type: "decimal(5,2)", nullable: false, defaultValue: 0m),
                    created_at = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    created_by = table.Column<int>(type: "int", nullable: false),
                    modified_at = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    modified_by = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EmployeeAttendance", x => x.Id);
                    table.ForeignKey(
                        name: "FK_EmployeeAttendance_Employee",
                        column: x => x.employee_id,
                        principalTable: "Employee",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "EmployeeChild",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    employee_id = table.Column<int>(type: "int", nullable: false),
                    first_name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    last_name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    date_of_birth = table.Column<DateTime>(type: "date", nullable: false),
                    gender_id = table.Column<int>(type: "int", nullable: true),
                    is_dependent = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    is_student = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    created_at = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    created_by = table.Column<int>(type: "int", nullable: false),
                    modified_at = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    modified_by = table.Column<int>(type: "int", nullable: true),
                    deleted_at = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    deleted_by = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EmployeeChild", x => x.Id);
                    table.ForeignKey(
                        name: "FK_EmployeeChild_Employee",
                        column: x => x.employee_id,
                        principalTable: "Employee",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_EmployeeChild_Gender",
                        column: x => x.gender_id,
                        principalTable: "Gender",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "EmployeeContract",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    EmployeeId = table.Column<int>(type: "int", nullable: false),
                    CompanyId = table.Column<int>(type: "int", nullable: false),
                    JobPositionId = table.Column<int>(type: "int", nullable: false),
                    ContractTypeId = table.Column<int>(type: "int", nullable: false),
                    StartDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    EndDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ExonerationEndDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    CreatedBy = table.Column<int>(type: "int", nullable: false),
                    ModifiedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    ModifiedBy = table.Column<int>(type: "int", nullable: true),
                    DeletedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    DeletedBy = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EmployeeContract", x => x.Id);
                    table.ForeignKey(
                        name: "FK_EmployeeContract_Company_CompanyId",
                        column: x => x.CompanyId,
                        principalTable: "Company",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_EmployeeContract_ContractType_ContractTypeId",
                        column: x => x.ContractTypeId,
                        principalTable: "ContractType",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_EmployeeContract_Employee_EmployeeId",
                        column: x => x.EmployeeId,
                        principalTable: "Employee",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_EmployeeContract_JobPosition_JobPositionId",
                        column: x => x.JobPositionId,
                        principalTable: "JobPosition",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "EmployeeDocument",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    EmployeeId = table.Column<int>(type: "int", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    FilePath = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    ExpirationDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DocumentType = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    CreatedBy = table.Column<int>(type: "int", nullable: false),
                    ModifiedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    ModifiedBy = table.Column<int>(type: "int", nullable: true),
                    DeletedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    DeletedBy = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EmployeeDocument", x => x.Id);
                    table.ForeignKey(
                        name: "FK_EmployeeDocument_Employee_EmployeeId",
                        column: x => x.EmployeeId,
                        principalTable: "Employee",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "EmployeeOvertimes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    EmployeeId = table.Column<int>(type: "int", nullable: false),
                    OverTimeType = table.Column<int>(type: "int", nullable: false),
                    EntryMode = table.Column<int>(type: "int", nullable: false),
                    HolidayId = table.Column<int>(type: "int", nullable: true),
                    OvertimeDate = table.Column<DateOnly>(type: "date", nullable: false),
                    StartTime = table.Column<TimeOnly>(type: "time", nullable: true),
                    EndTime = table.Column<TimeOnly>(type: "time", nullable: true),
                    CrossesMidnight = table.Column<bool>(type: "bit", nullable: false),
                    DurationInHours = table.Column<decimal>(type: "decimal(5,2)", precision: 5, scale: 2, nullable: false),
                    StandardDayHours = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    RateRuleId = table.Column<int>(type: "int", nullable: true),
                    RateRuleCodeApplied = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    RateRuleNameApplied = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    RateMultiplierApplied = table.Column<decimal>(type: "decimal(5,2)", precision: 5, scale: 2, nullable: false),
                    MultiplierCalculationDetails = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
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
                    CreatedBy = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    ModifiedBy = table.Column<int>(type: "int", nullable: true),
                    ModifiedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    DeletedBy = table.Column<int>(type: "int", nullable: true),
                    DeletedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EmployeeOvertimes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_EmployeeOvertimes_Employee_EmployeeId",
                        column: x => x.EmployeeId,
                        principalTable: "Employee",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_EmployeeOvertimes_Holiday_HolidayId",
                        column: x => x.HolidayId,
                        principalTable: "Holiday",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_EmployeeOvertimes_OvertimeRateRules_RateRuleId",
                        column: x => x.RateRuleId,
                        principalTable: "OvertimeRateRules",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "EmployeeSpouse",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    employee_id = table.Column<int>(type: "int", nullable: false),
                    first_name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    last_name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    date_of_birth = table.Column<DateTime>(type: "date", nullable: false),
                    gender_id = table.Column<int>(type: "int", nullable: true),
                    cin_number = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    is_dependent = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    created_at = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    created_by = table.Column<int>(type: "int", nullable: false),
                    modified_at = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    modified_by = table.Column<int>(type: "int", nullable: true),
                    deleted_at = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    deleted_by = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EmployeeSpouse", x => x.Id);
                    table.ForeignKey(
                        name: "FK_EmployeeSpouse_Employee",
                        column: x => x.employee_id,
                        principalTable: "Employee",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_EmployeeSpouse_Gender",
                        column: x => x.gender_id,
                        principalTable: "Gender",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "LeaveBalance",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
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
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    CreatedBy = table.Column<int>(type: "int", nullable: true),
                    ModifiedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    ModifiedBy = table.Column<int>(type: "int", nullable: true),
                    DeletedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    DeletedBy = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LeaveBalance", x => x.Id);
                    table.ForeignKey(
                        name: "FK_LeaveBalance_Company_CompanyId",
                        column: x => x.CompanyId,
                        principalTable: "Company",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_LeaveBalance_Employee_EmployeeId",
                        column: x => x.EmployeeId,
                        principalTable: "Employee",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_LeaveBalance_LeaveType_LeaveTypeId",
                        column: x => x.LeaveTypeId,
                        principalTable: "LeaveType",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "LeaveCarryOverAgreements",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    EmployeeId = table.Column<int>(type: "int", nullable: false),
                    CompanyId = table.Column<int>(type: "int", nullable: false),
                    LeaveTypeId = table.Column<int>(type: "int", nullable: false),
                    FromYear = table.Column<int>(type: "int", nullable: false),
                    ToYear = table.Column<int>(type: "int", nullable: false),
                    AgreementDate = table.Column<DateOnly>(type: "date", nullable: false),
                    AgreementDocRef = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    CreatedBy = table.Column<int>(type: "int", nullable: true),
                    DeletedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    DeletedBy = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LeaveCarryOverAgreements", x => x.Id);
                    table.ForeignKey(
                        name: "FK_LeaveCarryOverAgreements_Company_CompanyId",
                        column: x => x.CompanyId,
                        principalTable: "Company",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_LeaveCarryOverAgreements_Employee_EmployeeId",
                        column: x => x.EmployeeId,
                        principalTable: "Employee",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_LeaveCarryOverAgreements_LeaveType_LeaveTypeId",
                        column: x => x.LeaveTypeId,
                        principalTable: "LeaveType",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "PayrollResults",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
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
                    CimrPartSalariale = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    AmoPartSalariale = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    MutuellePartSalariale = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    TotalCotisationsSalariales = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    CnssPartPatronale = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    CimrPartPatronale = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    AmoPartPatronale = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    MutuellePartPatronale = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    TotalCotisationsPatronales = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    ImpotRevenu = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
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
                    ModifiedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    ModifiedBy = table.Column<int>(type: "int", nullable: true),
                    DeletedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    DeletedBy = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PayrollResults", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PayrollResults_Company_CompanyId",
                        column: x => x.CompanyId,
                        principalTable: "Company",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PayrollResults_Employee_EmployeeId",
                        column: x => x.EmployeeId,
                        principalTable: "Employee",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Users",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    EmployeeId = table.Column<int>(type: "int", nullable: true),
                    Username = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Email = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    EmailPersonal = table.Column<string>(type: "nvarchar(450)", nullable: true),
                    PasswordHash = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    DeletedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    CreatedBy = table.Column<int>(type: "int", nullable: false),
                    UpdatedBy = table.Column<int>(type: "int", nullable: true),
                    DeletedBy = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Users", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Users_Employee_EmployeeId",
                        column: x => x.EmployeeId,
                        principalTable: "Employee",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "LeaveRequests",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
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
                    IsRenounced = table.Column<bool>(type: "bit", nullable: false),
                    EmployeeNote = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    ManagerNote = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    CreatedBy = table.Column<int>(type: "int", nullable: true),
                    ModifiedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    ModifiedBy = table.Column<int>(type: "int", nullable: true),
                    DeletedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    DeletedBy = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LeaveRequests", x => x.Id);
                    table.ForeignKey(
                        name: "FK_LeaveRequests_Company_CompanyId",
                        column: x => x.CompanyId,
                        principalTable: "Company",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_LeaveRequests_Employee_EmployeeId",
                        column: x => x.EmployeeId,
                        principalTable: "Employee",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_LeaveRequests_LeaveTypeLegalRule_LegalRuleId",
                        column: x => x.LegalRuleId,
                        principalTable: "LeaveTypeLegalRule",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_LeaveRequests_LeaveTypePolicy_PolicyId",
                        column: x => x.PolicyId,
                        principalTable: "LeaveTypePolicy",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_LeaveRequests_LeaveType_LeaveTypeId",
                        column: x => x.LeaveTypeId,
                        principalTable: "LeaveType",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "EmployeeAttendanceBreak",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    employee_attendance_id = table.Column<int>(type: "int", nullable: false),
                    break_start = table.Column<TimeOnly>(type: "time", nullable: false),
                    break_end = table.Column<TimeOnly>(type: "time", nullable: true),
                    break_type = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    created_by = table.Column<int>(type: "int", nullable: false),
                    modified_at = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    modified_by = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EmployeeAttendanceBreak", x => x.Id);
                    table.ForeignKey(
                        name: "FK_EmployeeAttendanceBreak_EmployeeAttendance",
                        column: x => x.employee_attendance_id,
                        principalTable: "EmployeeAttendance",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "EmployeeSalary",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    EmployeeId = table.Column<int>(type: "int", nullable: false),
                    ContractId = table.Column<int>(type: "int", nullable: false),
                    BaseSalary = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    EffectiveDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    EndDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    CreatedBy = table.Column<int>(type: "int", nullable: false),
                    ModifiedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    ModifiedBy = table.Column<int>(type: "int", nullable: true),
                    DeletedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    DeletedBy = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EmployeeSalary", x => x.Id);
                    table.ForeignKey(
                        name: "FK_EmployeeSalary_EmployeeContract_ContractId",
                        column: x => x.ContractId,
                        principalTable: "EmployeeContract",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_EmployeeSalary_Employee_EmployeeId",
                        column: x => x.EmployeeId,
                        principalTable: "Employee",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "PayrollCalculationAuditSteps",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PayrollResultId = table.Column<int>(type: "int", nullable: false),
                    StepOrder = table.Column<int>(type: "int", nullable: false),
                    ModuleName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    FormulaDescription = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    InputsJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    OutputsJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PayrollCalculationAuditSteps", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PayrollCalculationAuditSteps_PayrollResults_PayrollResultId",
                        column: x => x.PayrollResultId,
                        principalTable: "PayrollResults",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PayrollResultPrimes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PayrollResultId = table.Column<int>(type: "int", nullable: false),
                    Label = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Montant = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Ordre = table.Column<int>(type: "int", nullable: false),
                    IsTaxable = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PayrollResultPrimes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PayrollResultPrimes_PayrollResults_PayrollResultId",
                        column: x => x.PayrollResultId,
                        principalTable: "PayrollResults",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "CompanyEventLog",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    employeeId = table.Column<int>(type: "int", nullable: false),
                    eventName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    oldValue = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    oldValueId = table.Column<int>(type: "int", nullable: true),
                    newValue = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    newValueId = table.Column<int>(type: "int", nullable: true),
                    createdAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    createdBy = table.Column<int>(type: "int", nullable: false),
                    companyId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CompanyEventLog", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CompanyEventLog_Company_companyId",
                        column: x => x.companyId,
                        principalTable: "Company",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CompanyEventLog_Users_createdBy",
                        column: x => x.createdBy,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "EmployeeEventLog",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    employeeId = table.Column<int>(type: "int", nullable: false),
                    eventName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    oldValue = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    oldValueId = table.Column<int>(type: "int", nullable: true),
                    newValue = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    newValueId = table.Column<int>(type: "int", nullable: true),
                    createdAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    createdBy = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EmployeeEventLog", x => x.Id);
                    table.ForeignKey(
                        name: "FK_EmployeeEventLog_Employee_employeeId",
                        column: x => x.employeeId,
                        principalTable: "Employee",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_EmployeeEventLog_Users_createdBy",
                        column: x => x.createdBy,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "UsersRoles",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<int>(type: "int", nullable: false),
                    RoleId = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    DeletedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    CreatedBy = table.Column<int>(type: "int", nullable: false),
                    UpdatedBy = table.Column<int>(type: "int", nullable: true),
                    DeletedBy = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UsersRoles", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UsersRoles_Roles_RoleId",
                        column: x => x.RoleId,
                        principalTable: "Roles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_UsersRoles_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "LeaveAuditLog",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CompanyId = table.Column<int>(type: "int", nullable: false),
                    EmployeeId = table.Column<int>(type: "int", nullable: true),
                    LeaveRequestId = table.Column<int>(type: "int", nullable: true),
                    EventName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    OldValue = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    NewValue = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    CreatedBy = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LeaveAuditLog", x => x.Id);
                    table.ForeignKey(
                        name: "FK_LeaveAuditLog_Company_CompanyId",
                        column: x => x.CompanyId,
                        principalTable: "Company",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_LeaveAuditLog_Employee_EmployeeId",
                        column: x => x.EmployeeId,
                        principalTable: "Employee",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_LeaveAuditLog_LeaveRequests_LeaveRequestId",
                        column: x => x.LeaveRequestId,
                        principalTable: "LeaveRequests",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "LeaveRequestApprovalHistory",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    LeaveRequestId = table.Column<int>(type: "int", nullable: false),
                    Action = table.Column<int>(type: "int", nullable: false),
                    ActionAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    ActionBy = table.Column<int>(type: "int", nullable: false),
                    Comment = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LeaveRequestApprovalHistory", x => x.Id);
                    table.ForeignKey(
                        name: "FK_LeaveRequestApprovalHistory_LeaveRequests_LeaveRequestId",
                        column: x => x.LeaveRequestId,
                        principalTable: "LeaveRequests",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "LeaveRequestAttachments",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    LeaveRequestId = table.Column<int>(type: "int", nullable: false),
                    FileName = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    FilePath = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    FileType = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    UploadedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    UploadedBy = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LeaveRequestAttachments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_LeaveRequestAttachments_LeaveRequests_LeaveRequestId",
                        column: x => x.LeaveRequestId,
                        principalTable: "LeaveRequests",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "LeaveRequestExemptions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    LeaveRequestId = table.Column<int>(type: "int", nullable: false),
                    ExemptionDate = table.Column<DateOnly>(type: "date", nullable: false),
                    ReasonType = table.Column<int>(type: "int", nullable: false),
                    CountsAsLeaveDay = table.Column<bool>(type: "bit", nullable: false),
                    HolidayId = table.Column<int>(type: "int", nullable: true),
                    EmployeeAbsenceId = table.Column<int>(type: "int", nullable: true),
                    Note = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    CreatedBy = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LeaveRequestExemptions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_LeaveRequestExemptions_EmployeeAbsence_EmployeeAbsenceId",
                        column: x => x.EmployeeAbsenceId,
                        principalTable: "EmployeeAbsence",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_LeaveRequestExemptions_Holiday_HolidayId",
                        column: x => x.HolidayId,
                        principalTable: "Holiday",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_LeaveRequestExemptions_LeaveRequests_LeaveRequestId",
                        column: x => x.LeaveRequestId,
                        principalTable: "LeaveRequests",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "EmployeeSalaryComponent",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    EmployeeSalaryId = table.Column<int>(type: "int", nullable: false),
                    ComponentType = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Istaxable = table.Column<bool>(type: "bit", nullable: false),
                    IsSocial = table.Column<bool>(type: "bit", nullable: false),
                    IsCIMR = table.Column<bool>(type: "bit", nullable: false),
                    Amount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    EffectiveDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    EndDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    CreatedBy = table.Column<int>(type: "int", nullable: false),
                    ModifiedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    ModifiedBy = table.Column<int>(type: "int", nullable: true),
                    DeletedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    DeletedBy = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EmployeeSalaryComponent", x => x.Id);
                    table.ForeignKey(
                        name: "FK_EmployeeSalaryComponent_EmployeeSalary_EmployeeSalaryId",
                        column: x => x.EmployeeSalaryId,
                        principalTable: "EmployeeSalary",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "SalaryPackageAssignment",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    SalaryPackageId = table.Column<int>(type: "int", nullable: false),
                    EmployeeId = table.Column<int>(type: "int", nullable: false),
                    ContractId = table.Column<int>(type: "int", nullable: false),
                    EmployeeSalaryId = table.Column<int>(type: "int", nullable: false),
                    EffectiveDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    EndDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    PackageVersion = table.Column<int>(type: "int", nullable: false, defaultValue: 1),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    CreatedBy = table.Column<int>(type: "int", nullable: false),
                    ModifiedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    ModifiedBy = table.Column<int>(type: "int", nullable: true),
                    DeletedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    DeletedBy = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SalaryPackageAssignment", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SalaryPackageAssignment_EmployeeContract_ContractId",
                        column: x => x.ContractId,
                        principalTable: "EmployeeContract",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SalaryPackageAssignment_EmployeeSalary_EmployeeSalaryId",
                        column: x => x.EmployeeSalaryId,
                        principalTable: "EmployeeSalary",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SalaryPackageAssignment_Employee_EmployeeId",
                        column: x => x.EmployeeId,
                        principalTable: "Employee",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SalaryPackageAssignment_SalaryPackage_SalaryPackageId",
                        column: x => x.SalaryPackageId,
                        principalTable: "SalaryPackage",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AncienneteRates_RateSetId_SortOrder",
                table: "AncienneteRates",
                columns: new[] { "RateSetId", "SortOrder" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AncienneteRateSets_ClonedFromId",
                table: "AncienneteRateSets",
                column: "ClonedFromId");

            migrationBuilder.CreateIndex(
                name: "IX_AncienneteRateSets_CompanyId_EffectiveFrom",
                table: "AncienneteRateSets",
                columns: new[] { "CompanyId", "EffectiveFrom" },
                unique: true,
                filter: "[DeletedAt] IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Authorities_Code",
                table: "Authorities",
                column: "Code",
                unique: true,
                filter: "[DeletedAt] IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_BusinessSectors_Code",
                table: "BusinessSectors",
                column: "Code",
                unique: true,
                filter: "[DeletedAt] IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_City_CountryId",
                table: "City",
                column: "CountryId");

            migrationBuilder.CreateIndex(
                name: "IX_Company_city_id",
                table: "Company",
                column: "city_id");

            migrationBuilder.CreateIndex(
                name: "IX_Company_country_id",
                table: "Company",
                column: "country_id");

            migrationBuilder.CreateIndex(
                name: "IX_Company_managedby_company_id",
                table: "Company",
                column: "managedby_company_id");

            migrationBuilder.CreateIndex(
                name: "IX_CompanyDocument_CompanyId",
                table: "CompanyDocument",
                column: "CompanyId");

            migrationBuilder.CreateIndex(
                name: "IX_CompanyEventLog_companyId",
                table: "CompanyEventLog",
                column: "companyId");

            migrationBuilder.CreateIndex(
                name: "IX_CompanyEventLog_createdBy",
                table: "CompanyEventLog",
                column: "createdBy");

            migrationBuilder.CreateIndex(
                name: "IX_ContractType_CompanyId",
                table: "ContractType",
                column: "CompanyId");

            migrationBuilder.CreateIndex(
                name: "IX_ContractType_LegalContractTypeId",
                table: "ContractType",
                column: "LegalContractTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_ContractType_StateEmploymentProgramId",
                table: "ContractType",
                column: "StateEmploymentProgramId");

            migrationBuilder.CreateIndex(
                name: "IX_Country_CountryCode",
                table: "Country",
                column: "CountryCode",
                unique: true,
                filter: "[DeletedAt] IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Departement_CompanyId",
                table: "Departement",
                column: "CompanyId");

            migrationBuilder.CreateIndex(
                name: "IX_EducationLevel_Code",
                table: "EducationLevel",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ElementRules_AuthorityId",
                table: "ElementRules",
                column: "AuthorityId");

            migrationBuilder.CreateIndex(
                name: "IX_ElementRules_ElementId_AuthorityId_EffectiveFrom",
                table: "ElementRules",
                columns: new[] { "ElementId", "AuthorityId", "EffectiveFrom" },
                unique: true,
                filter: "[DeletedAt] IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_EligibilityCriteria_Code",
                table: "EligibilityCriteria",
                column: "Code",
                unique: true,
                filter: "[DeletedAt] IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Employee_cin_number",
                table: "Employee",
                column: "cin_number",
                unique: true,
                filter: "[deleted_at] IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Employee_company_id",
                table: "Employee",
                column: "company_id");

            migrationBuilder.CreateIndex(
                name: "IX_Employee_CountryId",
                table: "Employee",
                column: "CountryId");

            migrationBuilder.CreateIndex(
                name: "IX_Employee_departement_id",
                table: "Employee",
                column: "departement_id");

            migrationBuilder.CreateIndex(
                name: "IX_Employee_education_level_id",
                table: "Employee",
                column: "education_level_id");

            migrationBuilder.CreateIndex(
                name: "IX_Employee_email",
                table: "Employee",
                column: "email",
                unique: true,
                filter: "[deleted_at] IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Employee_employee_category_id",
                table: "Employee",
                column: "employee_category_id");

            migrationBuilder.CreateIndex(
                name: "IX_Employee_gender_id",
                table: "Employee",
                column: "gender_id");

            migrationBuilder.CreateIndex(
                name: "IX_Employee_manager_id",
                table: "Employee",
                column: "manager_id");

            migrationBuilder.CreateIndex(
                name: "IX_Employee_marital_status_id",
                table: "Employee",
                column: "marital_status_id");

            migrationBuilder.CreateIndex(
                name: "IX_Employee_nationality_id",
                table: "Employee",
                column: "nationality_id");

            migrationBuilder.CreateIndex(
                name: "IX_Employee_personal_email",
                table: "Employee",
                column: "personal_email",
                filter: "[deleted_at] IS NULL AND [personal_email] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Employee_status_id",
                table: "Employee",
                column: "status_id");

            migrationBuilder.CreateIndex(
                name: "IX_EmployeeAbsence_Date",
                table: "EmployeeAbsence",
                column: "absence_date");

            migrationBuilder.CreateIndex(
                name: "IX_EmployeeAbsence_Employee_Date",
                table: "EmployeeAbsence",
                columns: new[] { "employee_id", "absence_date" });

            migrationBuilder.CreateIndex(
                name: "IX_EmployeeAbsence_EmployeeId",
                table: "EmployeeAbsence",
                column: "employee_id");

            migrationBuilder.CreateIndex(
                name: "IX_EmployeeAbsence_Type",
                table: "EmployeeAbsence",
                column: "absence_type");

            migrationBuilder.CreateIndex(
                name: "IX_EmployeeAddress_CityId",
                table: "EmployeeAddress",
                column: "CityId");

            migrationBuilder.CreateIndex(
                name: "IX_EmployeeAddress_CountryId",
                table: "EmployeeAddress",
                column: "CountryId");

            migrationBuilder.CreateIndex(
                name: "IX_EmployeeAddress_EmployeeId",
                table: "EmployeeAddress",
                column: "EmployeeId");

            migrationBuilder.CreateIndex(
                name: "IX_EmployeeAttendance_Employee_WorkDate",
                table: "EmployeeAttendance",
                columns: new[] { "employee_id", "work_date" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_EmployeeAttendance_EmployeeId",
                table: "EmployeeAttendance",
                column: "employee_id");

            migrationBuilder.CreateIndex(
                name: "IX_EmployeeAttendance_WorkDate",
                table: "EmployeeAttendance",
                column: "work_date");

            migrationBuilder.CreateIndex(
                name: "IX_EmployeeAttendanceBreak_AttendanceId",
                table: "EmployeeAttendanceBreak",
                column: "employee_attendance_id");

            migrationBuilder.CreateIndex(
                name: "IX_EmployeeCategory_CompanyId_Name",
                table: "EmployeeCategory",
                columns: new[] { "CompanyId", "Name" },
                unique: true,
                filter: "[DeletedAt] IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_EmployeeChild_DeletedAt",
                table: "EmployeeChild",
                column: "deleted_at",
                filter: "[deleted_at] IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_EmployeeChild_EmployeeId",
                table: "EmployeeChild",
                column: "employee_id");

            migrationBuilder.CreateIndex(
                name: "IX_EmployeeChild_gender_id",
                table: "EmployeeChild",
                column: "gender_id");

            migrationBuilder.CreateIndex(
                name: "IX_EmployeeContract_CompanyId",
                table: "EmployeeContract",
                column: "CompanyId");

            migrationBuilder.CreateIndex(
                name: "IX_EmployeeContract_ContractTypeId",
                table: "EmployeeContract",
                column: "ContractTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_EmployeeContract_EmployeeId",
                table: "EmployeeContract",
                column: "EmployeeId");

            migrationBuilder.CreateIndex(
                name: "IX_EmployeeContract_JobPositionId",
                table: "EmployeeContract",
                column: "JobPositionId");

            migrationBuilder.CreateIndex(
                name: "IX_EmployeeDocument_EmployeeId",
                table: "EmployeeDocument",
                column: "EmployeeId");

            migrationBuilder.CreateIndex(
                name: "IX_EmployeeEventLog_createdBy",
                table: "EmployeeEventLog",
                column: "createdBy");

            migrationBuilder.CreateIndex(
                name: "IX_EmployeeEventLog_employeeId",
                table: "EmployeeEventLog",
                column: "employeeId");

            migrationBuilder.CreateIndex(
                name: "IX_EmployeeOvertime_DeletedAt",
                table: "EmployeeOvertimes",
                column: "DeletedAt");

            migrationBuilder.CreateIndex(
                name: "IX_EmployeeOvertime_EmployeeDate",
                table: "EmployeeOvertimes",
                columns: new[] { "EmployeeId", "OvertimeDate", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_EmployeeOvertime_SplitBatchId",
                table: "EmployeeOvertimes",
                column: "SplitBatchId");

            migrationBuilder.CreateIndex(
                name: "IX_EmployeeOvertime_StatusDate",
                table: "EmployeeOvertimes",
                columns: new[] { "Status", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_EmployeeOvertimes_HolidayId",
                table: "EmployeeOvertimes",
                column: "HolidayId");

            migrationBuilder.CreateIndex(
                name: "IX_EmployeeOvertimes_RateRuleId",
                table: "EmployeeOvertimes",
                column: "RateRuleId");

            migrationBuilder.CreateIndex(
                name: "IX_EmployeeSalary_ContractId",
                table: "EmployeeSalary",
                column: "ContractId");

            migrationBuilder.CreateIndex(
                name: "IX_EmployeeSalary_EmployeeId",
                table: "EmployeeSalary",
                column: "EmployeeId");

            migrationBuilder.CreateIndex(
                name: "IX_EmployeeSalaryComponent_EmployeeSalaryId",
                table: "EmployeeSalaryComponent",
                column: "EmployeeSalaryId");

            migrationBuilder.CreateIndex(
                name: "IX_EmployeeSpouse_CinNumber",
                table: "EmployeeSpouse",
                column: "cin_number",
                unique: true,
                filter: "[deleted_at] IS NULL AND [cin_number] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_EmployeeSpouse_DeletedAt",
                table: "EmployeeSpouse",
                column: "deleted_at",
                filter: "[deleted_at] IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_EmployeeSpouse_EmployeeId",
                table: "EmployeeSpouse",
                column: "employee_id");

            migrationBuilder.CreateIndex(
                name: "IX_EmployeeSpouse_gender_id",
                table: "EmployeeSpouse",
                column: "gender_id");

            migrationBuilder.CreateIndex(
                name: "IX_Gender_Code",
                table: "Gender",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Holiday_CompanyId_HolidayDate",
                table: "Holiday",
                columns: new[] { "CompanyId", "HolidayDate" },
                unique: true,
                filter: "[DeletedAt] IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Holiday_CountryId",
                table: "Holiday",
                column: "CountryId");

            migrationBuilder.CreateIndex(
                name: "IX_JobPosition_CompanyId",
                table: "JobPosition",
                column: "CompanyId");

            migrationBuilder.CreateIndex(
                name: "idx_LeaveAuditLog_Company_CreatedAt",
                table: "LeaveAuditLog",
                columns: new[] { "CompanyId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "idx_LeaveAuditLog_LeaveRequestId",
                table: "LeaveAuditLog",
                column: "LeaveRequestId");

            migrationBuilder.CreateIndex(
                name: "IX_LeaveAuditLog_EmployeeId",
                table: "LeaveAuditLog",
                column: "EmployeeId");

            migrationBuilder.CreateIndex(
                name: "idx_LeaveBalance_Company_Year_Month",
                table: "LeaveBalance",
                columns: new[] { "CompanyId", "Year", "Month" });

            migrationBuilder.CreateIndex(
                name: "IX_LeaveBalance_EmployeeId",
                table: "LeaveBalance",
                column: "EmployeeId");

            migrationBuilder.CreateIndex(
                name: "IX_LeaveBalance_LeaveTypeId",
                table: "LeaveBalance",
                column: "LeaveTypeId");

            migrationBuilder.CreateIndex(
                name: "idx_LeaveCarryOverAgreements_CompanyId",
                table: "LeaveCarryOverAgreements",
                column: "CompanyId");

            migrationBuilder.CreateIndex(
                name: "IX_LeaveCarryOverAgreements_LeaveTypeId",
                table: "LeaveCarryOverAgreements",
                column: "LeaveTypeId");

            migrationBuilder.CreateIndex(
                name: "ux_LeaveCarryOverAgreements_Emp_Type_Years",
                table: "LeaveCarryOverAgreements",
                columns: new[] { "EmployeeId", "LeaveTypeId", "FromYear", "ToYear" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "idx_LeaveRequestApprovalHistory_RequestId",
                table: "LeaveRequestApprovalHistory",
                column: "LeaveRequestId");

            migrationBuilder.CreateIndex(
                name: "IX_LeaveRequestAttachments_LeaveRequestId",
                table: "LeaveRequestAttachments",
                column: "LeaveRequestId");

            migrationBuilder.CreateIndex(
                name: "idx_LeaveRequestExemptions_Date",
                table: "LeaveRequestExemptions",
                column: "ExemptionDate");

            migrationBuilder.CreateIndex(
                name: "IX_LeaveRequestExemptions_EmployeeAbsenceId",
                table: "LeaveRequestExemptions",
                column: "EmployeeAbsenceId");

            migrationBuilder.CreateIndex(
                name: "IX_LeaveRequestExemptions_HolidayId",
                table: "LeaveRequestExemptions",
                column: "HolidayId");

            migrationBuilder.CreateIndex(
                name: "ux_LeaveRequestExemptions_Request_Date",
                table: "LeaveRequestExemptions",
                columns: new[] { "LeaveRequestId", "ExemptionDate" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "idx_LeaveRequests_Company_StartDate",
                table: "LeaveRequests",
                columns: new[] { "CompanyId", "StartDate" });

            migrationBuilder.CreateIndex(
                name: "idx_LeaveRequests_Company_Status",
                table: "LeaveRequests",
                columns: new[] { "CompanyId", "Status" });

            migrationBuilder.CreateIndex(
                name: "idx_LeaveRequests_EmployeeId",
                table: "LeaveRequests",
                column: "EmployeeId");

            migrationBuilder.CreateIndex(
                name: "IX_LeaveRequests_LeaveTypeId",
                table: "LeaveRequests",
                column: "LeaveTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_LeaveRequests_LegalRuleId",
                table: "LeaveRequests",
                column: "LegalRuleId");

            migrationBuilder.CreateIndex(
                name: "IX_LeaveRequests_PolicyId",
                table: "LeaveRequests",
                column: "PolicyId");

            migrationBuilder.CreateIndex(
                name: "idx_LeaveType_LeaveCode",
                table: "LeaveType",
                column: "LeaveCode");

            migrationBuilder.CreateIndex(
                name: "ux_LeaveType_Company_LeaveCode",
                table: "LeaveType",
                columns: new[] { "CompanyId", "LeaveCode" },
                unique: true,
                filter: "[CompanyId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "ux_LegalRule_Type_Case",
                table: "LeaveTypeLegalRule",
                columns: new[] { "LeaveTypeId", "EventCaseCode" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "idx_LeaveTypePolicy_CompanyId",
                table: "LeaveTypePolicy",
                column: "CompanyId");

            migrationBuilder.CreateIndex(
                name: "IX_LeaveTypePolicy_LeaveTypeId",
                table: "LeaveTypePolicy",
                column: "LeaveTypeId");

            migrationBuilder.CreateIndex(
                name: "ux_LeaveTypePolicy_Company_LeaveType",
                table: "LeaveTypePolicy",
                columns: new[] { "CompanyId", "LeaveTypeId" },
                unique: true,
                filter: "[CompanyId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_LegalContractType_Code",
                table: "LegalContractType",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_LegalContractType_Name",
                table: "LegalContractType",
                column: "Name",
                unique: true,
                filter: "[DeletedAt] IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_LegalParameters_Code_EffectiveFrom",
                table: "LegalParameters",
                columns: new[] { "Code", "EffectiveFrom" },
                unique: true,
                filter: "[DeletedAt] IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_MaritalStatus_Code",
                table: "MaritalStatus",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Nationality_Name",
                table: "Nationality",
                column: "Name",
                unique: true,
                filter: "[DeletedAt] IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_OvertimeRateRule_Code",
                table: "OvertimeRateRules",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_OvertimeRateRule_EffectiveDates",
                table: "OvertimeRateRules",
                columns: new[] { "EffectiveFrom", "EffectiveTo" });

            migrationBuilder.CreateIndex(
                name: "IX_OvertimeRateRule_Lookup",
                table: "OvertimeRateRules",
                columns: new[] { "AppliesTo", "IsActive", "Priority" });

            migrationBuilder.CreateIndex(
                name: "IX_PayComponent_Code_Version",
                table: "PayComponent",
                columns: new[] { "Code", "Version" },
                unique: true,
                filter: "[DeletedAt] IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_PayrollCalculationAuditSteps_PayrollResultId",
                table: "PayrollCalculationAuditSteps",
                column: "PayrollResultId");

            migrationBuilder.CreateIndex(
                name: "IX_PayrollResultPrimes_PayrollResultId",
                table: "PayrollResultPrimes",
                column: "PayrollResultId");

            migrationBuilder.CreateIndex(
                name: "IX_PayrollResult_Company_Period",
                table: "PayrollResults",
                columns: new[] { "CompanyId", "Year", "Month" });

            migrationBuilder.CreateIndex(
                name: "IX_PayrollResult_Status",
                table: "PayrollResults",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "UX_PayrollResult_Employee_Period",
                table: "PayrollResults",
                columns: new[] { "EmployeeId", "Month", "Year" },
                unique: true,
                filter: "[DeletedAt] IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Permissions_Name",
                table: "Permissions",
                column: "Name",
                unique: true,
                filter: "[DeletedAt] IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_ReferentielElements_CategoryId",
                table: "ReferentielElements",
                column: "CategoryId");

            migrationBuilder.CreateIndex(
                name: "IX_ReferentielElements_Name_CategoryId",
                table: "ReferentielElements",
                columns: new[] { "Name", "CategoryId" },
                unique: true,
                filter: "[DeletedAt] IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Roles_Name",
                table: "Roles",
                column: "Name",
                unique: true,
                filter: "[DeletedAt] IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_RolesPermissions_PermissionId",
                table: "RolesPermissions",
                column: "PermissionId");

            migrationBuilder.CreateIndex(
                name: "IX_RolesPermissions_RoleId_PermissionId",
                table: "RolesPermissions",
                columns: new[] { "RoleId", "PermissionId" },
                unique: true,
                filter: "[DeletedAt] IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_RuleCaps_RuleId",
                table: "RuleCaps",
                column: "RuleId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_RuleDualCaps_RuleId",
                table: "RuleDualCaps",
                column: "RuleId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_RuleFormulas_ParameterId",
                table: "RuleFormulas",
                column: "ParameterId");

            migrationBuilder.CreateIndex(
                name: "IX_RuleFormulas_RuleId",
                table: "RuleFormulas",
                column: "RuleId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_RulePercentages_EligibilityId",
                table: "RulePercentages",
                column: "EligibilityId");

            migrationBuilder.CreateIndex(
                name: "IX_RulePercentages_RuleId",
                table: "RulePercentages",
                column: "RuleId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_RuleTiers_RuleId_TierOrder",
                table: "RuleTiers",
                columns: new[] { "RuleId", "TierOrder" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_RuleVariants_EligibilityId",
                table: "RuleVariants",
                column: "EligibilityId");

            migrationBuilder.CreateIndex(
                name: "IX_RuleVariants_RuleId_VariantType_VariantKey",
                table: "RuleVariants",
                columns: new[] { "RuleId", "VariantType", "VariantKey" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SalaryPackage_BusinessSectorId",
                table: "SalaryPackage",
                column: "BusinessSectorId");

            migrationBuilder.CreateIndex(
                name: "IX_SalaryPackage_CompanyId",
                table: "SalaryPackage",
                column: "CompanyId");

            migrationBuilder.CreateIndex(
                name: "IX_SalaryPackage_SourceTemplateId",
                table: "SalaryPackage",
                column: "SourceTemplateId");

            migrationBuilder.CreateIndex(
                name: "IX_SalaryPackageAssignment_ContractId",
                table: "SalaryPackageAssignment",
                column: "ContractId");

            migrationBuilder.CreateIndex(
                name: "IX_SalaryPackageAssignment_EmployeeId",
                table: "SalaryPackageAssignment",
                column: "EmployeeId");

            migrationBuilder.CreateIndex(
                name: "IX_SalaryPackageAssignment_EmployeeSalaryId",
                table: "SalaryPackageAssignment",
                column: "EmployeeSalaryId");

            migrationBuilder.CreateIndex(
                name: "IX_SalaryPackageAssignment_SalaryPackageId",
                table: "SalaryPackageAssignment",
                column: "SalaryPackageId");

            migrationBuilder.CreateIndex(
                name: "IX_SalaryPackageItem_PayComponentId",
                table: "SalaryPackageItem",
                column: "PayComponentId");

            migrationBuilder.CreateIndex(
                name: "IX_SalaryPackageItem_ReferentielElementId",
                table: "SalaryPackageItem",
                column: "ReferentielElementId");

            migrationBuilder.CreateIndex(
                name: "IX_SalaryPackageItem_SalaryPackageId",
                table: "SalaryPackageItem",
                column: "SalaryPackageId");

            migrationBuilder.CreateIndex(
                name: "IX_StateEmploymentProgram_Code",
                table: "StateEmploymentProgram",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Status_Code",
                table: "Status",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Users_Email",
                table: "Users",
                column: "Email");

            migrationBuilder.CreateIndex(
                name: "IX_Users_EmailPersonal",
                table: "Users",
                column: "EmailPersonal",
                filter: "[DeletedAt] IS NULL AND [EmailPersonal] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Users_EmployeeId",
                table: "Users",
                column: "EmployeeId");

            migrationBuilder.CreateIndex(
                name: "IX_Users_Username",
                table: "Users",
                column: "Username",
                unique: true,
                filter: "[DeletedAt] IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_UsersRoles_RoleId",
                table: "UsersRoles",
                column: "RoleId");

            migrationBuilder.CreateIndex(
                name: "IX_UsersRoles_UserId_RoleId",
                table: "UsersRoles",
                columns: new[] { "UserId", "RoleId" },
                unique: true,
                filter: "[DeletedAt] IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_WorkingCalendar_CompanyId_DayOfWeek",
                table: "WorkingCalendar",
                columns: new[] { "CompanyId", "DayOfWeek" },
                unique: true,
                filter: "[DeletedAt] IS NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AncienneteRates");

            migrationBuilder.DropTable(
                name: "CompanyDocument");

            migrationBuilder.DropTable(
                name: "CompanyEventLog");

            migrationBuilder.DropTable(
                name: "EmployeeAddress");

            migrationBuilder.DropTable(
                name: "EmployeeAttendanceBreak");

            migrationBuilder.DropTable(
                name: "EmployeeChild");

            migrationBuilder.DropTable(
                name: "EmployeeDocument");

            migrationBuilder.DropTable(
                name: "EmployeeEventLog");

            migrationBuilder.DropTable(
                name: "EmployeeOvertimes");

            migrationBuilder.DropTable(
                name: "EmployeeSalaryComponent");

            migrationBuilder.DropTable(
                name: "EmployeeSpouse");

            migrationBuilder.DropTable(
                name: "LeaveAuditLog");

            migrationBuilder.DropTable(
                name: "LeaveBalance");

            migrationBuilder.DropTable(
                name: "LeaveCarryOverAgreements");

            migrationBuilder.DropTable(
                name: "LeaveRequestApprovalHistory");

            migrationBuilder.DropTable(
                name: "LeaveRequestAttachments");

            migrationBuilder.DropTable(
                name: "LeaveRequestExemptions");

            migrationBuilder.DropTable(
                name: "PayrollCalculationAuditSteps");

            migrationBuilder.DropTable(
                name: "PayrollResultPrimes");

            migrationBuilder.DropTable(
                name: "RolesPermissions");

            migrationBuilder.DropTable(
                name: "RuleCaps");

            migrationBuilder.DropTable(
                name: "RuleDualCaps");

            migrationBuilder.DropTable(
                name: "RuleFormulas");

            migrationBuilder.DropTable(
                name: "RulePercentages");

            migrationBuilder.DropTable(
                name: "RuleTiers");

            migrationBuilder.DropTable(
                name: "RuleVariants");

            migrationBuilder.DropTable(
                name: "SalaryPackageAssignment");

            migrationBuilder.DropTable(
                name: "SalaryPackageItem");

            migrationBuilder.DropTable(
                name: "UsersRoles");

            migrationBuilder.DropTable(
                name: "WorkingCalendar");

            migrationBuilder.DropTable(
                name: "AncienneteRateSets");

            migrationBuilder.DropTable(
                name: "EmployeeAttendance");

            migrationBuilder.DropTable(
                name: "OvertimeRateRules");

            migrationBuilder.DropTable(
                name: "EmployeeAbsence");

            migrationBuilder.DropTable(
                name: "Holiday");

            migrationBuilder.DropTable(
                name: "LeaveRequests");

            migrationBuilder.DropTable(
                name: "PayrollResults");

            migrationBuilder.DropTable(
                name: "Permissions");

            migrationBuilder.DropTable(
                name: "LegalParameters");

            migrationBuilder.DropTable(
                name: "ElementRules");

            migrationBuilder.DropTable(
                name: "EligibilityCriteria");

            migrationBuilder.DropTable(
                name: "EmployeeSalary");

            migrationBuilder.DropTable(
                name: "PayComponent");

            migrationBuilder.DropTable(
                name: "SalaryPackage");

            migrationBuilder.DropTable(
                name: "Roles");

            migrationBuilder.DropTable(
                name: "Users");

            migrationBuilder.DropTable(
                name: "LeaveTypeLegalRule");

            migrationBuilder.DropTable(
                name: "LeaveTypePolicy");

            migrationBuilder.DropTable(
                name: "Authorities");

            migrationBuilder.DropTable(
                name: "ReferentielElements");

            migrationBuilder.DropTable(
                name: "EmployeeContract");

            migrationBuilder.DropTable(
                name: "BusinessSectors");

            migrationBuilder.DropTable(
                name: "LeaveType");

            migrationBuilder.DropTable(
                name: "ElementCategories");

            migrationBuilder.DropTable(
                name: "ContractType");

            migrationBuilder.DropTable(
                name: "Employee");

            migrationBuilder.DropTable(
                name: "JobPosition");

            migrationBuilder.DropTable(
                name: "LegalContractType");

            migrationBuilder.DropTable(
                name: "StateEmploymentProgram");

            migrationBuilder.DropTable(
                name: "Departement");

            migrationBuilder.DropTable(
                name: "EducationLevel");

            migrationBuilder.DropTable(
                name: "EmployeeCategory");

            migrationBuilder.DropTable(
                name: "Gender");

            migrationBuilder.DropTable(
                name: "MaritalStatus");

            migrationBuilder.DropTable(
                name: "Nationality");

            migrationBuilder.DropTable(
                name: "Status");

            migrationBuilder.DropTable(
                name: "Company");

            migrationBuilder.DropTable(
                name: "City");

            migrationBuilder.DropTable(
                name: "Country");
        }
    }
}
