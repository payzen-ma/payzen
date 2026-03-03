IF OBJECT_ID(N'[__EFMigrationsHistory]') IS NULL
BEGIN
    CREATE TABLE [__EFMigrationsHistory] (
        [MigrationId] nvarchar(150) NOT NULL,
        [ProductVersion] nvarchar(32) NOT NULL,
        CONSTRAINT [PK___EFMigrationsHistory] PRIMARY KEY ([MigrationId])
    );
END;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251124093659_SecondMigration'
)
BEGIN
    CREATE TABLE [Users] (
        [Id] int NOT NULL IDENTITY,
        [Username] nvarchar(450) NOT NULL,
        [Email] nvarchar(450) NOT NULL,
        [PasswordHash] nvarchar(max) NOT NULL,
        [IsActive] bit NOT NULL,
        [CreatedAt] datetimeoffset NOT NULL,
        [UpdatedAt] datetimeoffset NULL,
        [DeletedAT] datetimeoffset NULL,
        [CreatedBy] int NOT NULL,
        [UpdatedBy] int NULL,
        [DeletedBy] int NULL,
        CONSTRAINT [PK_Users] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251124093659_SecondMigration'
)
BEGIN
    CREATE INDEX [IX_Users_Email] ON [Users] ([Email]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251124093659_SecondMigration'
)
BEGIN
    CREATE UNIQUE INDEX [IX_Users_Username] ON [Users] ([Username]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251124093659_SecondMigration'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20251124093659_SecondMigration', N'9.0.0');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251125082201_AddCompanyId_AddPermissionTables'
)
BEGIN
    EXEC sp_rename N'[Users].[DeletedAT]', N'DeletedAt', 'COLUMN';
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251125082201_AddCompanyId_AddPermissionTables'
)
BEGIN
    ALTER TABLE [Users] ADD [CompanyId] int NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251125082201_AddCompanyId_AddPermissionTables'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20251125082201_AddCompanyId_AddPermissionTables', N'9.0.0');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251125082626_AddCompanyIdAndReorganizeModels'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20251125082626_AddCompanyIdAndReorganizeModels', N'9.0.0');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251125084534_AddCompanyIdAndPermissionsSystem'
)
BEGIN
    DROP INDEX [IX_Users_Username] ON [Users];
    DECLARE @var0 sysname;
    SELECT @var0 = [d].[name]
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Users]') AND [c].[name] = N'Username');
    IF @var0 IS NOT NULL EXEC(N'ALTER TABLE [Users] DROP CONSTRAINT [' + @var0 + '];');
    ALTER TABLE [Users] ALTER COLUMN [Username] nvarchar(50) NOT NULL;
    CREATE UNIQUE INDEX [IX_Users_Username] ON [Users] ([Username]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251125084534_AddCompanyIdAndPermissionsSystem'
)
BEGIN
    DROP INDEX [IX_Users_Email] ON [Users];
    DECLARE @var1 sysname;
    SELECT @var1 = [d].[name]
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Users]') AND [c].[name] = N'Email');
    IF @var1 IS NOT NULL EXEC(N'ALTER TABLE [Users] DROP CONSTRAINT [' + @var1 + '];');
    ALTER TABLE [Users] ALTER COLUMN [Email] nvarchar(100) NOT NULL;
    CREATE INDEX [IX_Users_Email] ON [Users] ([Email]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251125084534_AddCompanyIdAndPermissionsSystem'
)
BEGIN
    CREATE TABLE [Permissions] (
        [Id] int NOT NULL IDENTITY,
        [Name] nvarchar(100) NOT NULL,
        [Description] nvarchar(500) NOT NULL,
        [CreatedAt] datetimeoffset NOT NULL,
        [UpdatedAt] datetimeoffset NULL,
        [DeletedAt] datetimeoffset NULL,
        [CreatedBy] int NOT NULL,
        [UpdatedBy] int NULL,
        [DeletedBy] int NULL,
        CONSTRAINT [PK_Permissions] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251125084534_AddCompanyIdAndPermissionsSystem'
)
BEGIN
    CREATE TABLE [Roles] (
        [Id] int NOT NULL IDENTITY,
        [Name] nvarchar(50) NOT NULL,
        [Description] nvarchar(500) NOT NULL,
        [CreatedAt] datetimeoffset NOT NULL,
        [UpdatedAt] datetimeoffset NULL,
        [DeletedAt] datetimeoffset NULL,
        [CreatedBy] int NOT NULL,
        [UpdatedBy] int NULL,
        [DeletedBy] int NULL,
        CONSTRAINT [PK_Roles] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251125084534_AddCompanyIdAndPermissionsSystem'
)
BEGIN
    CREATE TABLE [RolesPermissions] (
        [Id] int NOT NULL IDENTITY,
        [RoleId] int NOT NULL,
        [PermissionId] int NOT NULL,
        [CreatedAt] datetimeoffset NOT NULL,
        [UpdatedAt] datetimeoffset NULL,
        [DeletedAt] datetimeoffset NULL,
        [CreatedBy] int NOT NULL,
        [UpdatedBy] int NULL,
        [DeletedBy] int NULL,
        CONSTRAINT [PK_RolesPermissions] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_RolesPermissions_Permissions_PermissionId] FOREIGN KEY ([PermissionId]) REFERENCES [Permissions] ([Id]) ON DELETE CASCADE,
        CONSTRAINT [FK_RolesPermissions_Roles_RoleId] FOREIGN KEY ([RoleId]) REFERENCES [Roles] ([Id]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251125084534_AddCompanyIdAndPermissionsSystem'
)
BEGIN
    CREATE TABLE [UsersRoles] (
        [Id] int NOT NULL IDENTITY,
        [UserId] int NOT NULL,
        [RoleId] int NOT NULL,
        [CreatedAt] datetimeoffset NOT NULL,
        [UpdatedAt] datetimeoffset NULL,
        [DeletedAt] datetimeoffset NULL,
        [CreatedBy] int NOT NULL,
        [UpdatedBy] int NULL,
        [DeletedBy] int NULL,
        CONSTRAINT [PK_UsersRoles] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_UsersRoles_Roles_RoleId] FOREIGN KEY ([RoleId]) REFERENCES [Roles] ([Id]) ON DELETE CASCADE,
        CONSTRAINT [FK_UsersRoles_Users_UserId] FOREIGN KEY ([UserId]) REFERENCES [Users] ([Id]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251125084534_AddCompanyIdAndPermissionsSystem'
)
BEGIN
    CREATE UNIQUE INDEX [IX_Permissions_Name] ON [Permissions] ([Name]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251125084534_AddCompanyIdAndPermissionsSystem'
)
BEGIN
    CREATE UNIQUE INDEX [IX_Roles_Name] ON [Roles] ([Name]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251125084534_AddCompanyIdAndPermissionsSystem'
)
BEGIN
    CREATE INDEX [IX_RolesPermissions_PermissionId] ON [RolesPermissions] ([PermissionId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251125084534_AddCompanyIdAndPermissionsSystem'
)
BEGIN
    CREATE UNIQUE INDEX [IX_RolesPermissions_RoleId_PermissionId] ON [RolesPermissions] ([RoleId], [PermissionId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251125084534_AddCompanyIdAndPermissionsSystem'
)
BEGIN
    CREATE INDEX [IX_UsersRoles_RoleId] ON [UsersRoles] ([RoleId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251125084534_AddCompanyIdAndPermissionsSystem'
)
BEGIN
    CREATE UNIQUE INDEX [IX_UsersRoles_UserId_RoleId] ON [UsersRoles] ([UserId], [RoleId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251125084534_AddCompanyIdAndPermissionsSystem'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20251125084534_AddCompanyIdAndPermissionsSystem', N'9.0.0');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251125154756_AddFilteredUniqueIndexes'
)
BEGIN
    DROP INDEX [IX_UsersRoles_UserId_RoleId] ON [UsersRoles];
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251125154756_AddFilteredUniqueIndexes'
)
BEGIN
    DROP INDEX [IX_RolesPermissions_RoleId_PermissionId] ON [RolesPermissions];
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251125154756_AddFilteredUniqueIndexes'
)
BEGIN
    DROP INDEX [IX_Roles_Name] ON [Roles];
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251125154756_AddFilteredUniqueIndexes'
)
BEGIN
    DROP INDEX [IX_Permissions_Name] ON [Permissions];
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251125154756_AddFilteredUniqueIndexes'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [IX_UsersRoles_UserId_RoleId] ON [UsersRoles] ([UserId], [RoleId]) WHERE [DeletedAt] IS NULL');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251125154756_AddFilteredUniqueIndexes'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [IX_RolesPermissions_RoleId_PermissionId] ON [RolesPermissions] ([RoleId], [PermissionId]) WHERE [DeletedAt] IS NULL');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251125154756_AddFilteredUniqueIndexes'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [IX_Roles_Name] ON [Roles] ([Name]) WHERE [DeletedAt] IS NULL');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251125154756_AddFilteredUniqueIndexes'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [IX_Permissions_Name] ON [Permissions] ([Name]) WHERE [DeletedAt] IS NULL');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251125154756_AddFilteredUniqueIndexes'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20251125154756_AddFilteredUniqueIndexes', N'9.0.0');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251127092020_Add_Company_Model_Employee_Model_Change_User_Login_Response_to_include_role_in_the_token'
)
BEGIN
    EXEC sp_rename N'[Users].[CompanyId]', N'EmployeeId', 'COLUMN';
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251127092020_Add_Company_Model_Employee_Model_Change_User_Login_Response_to_include_role_in_the_token'
)
BEGIN
    CREATE TABLE [Company] (
        [Id] int NOT NULL IDENTITY,
        [company_name] nvarchar(500) NOT NULL,
        [company_address] nvarchar(500) NOT NULL,
        [city_id] int NULL,
        [country_id] int NULL,
        [ice_number] nvarchar(500) NOT NULL,
        [cnss_number] nvarchar(500) NOT NULL,
        [if_number] nvarchar(500) NOT NULL,
        [rc_number] nvarchar(500) NOT NULL,
        [rib_number] nvarchar(500) NOT NULL,
        [phone_number] int NOT NULL,
        [email] nvarchar(500) NOT NULL,
        [managedby_company_id] int NULL,
        [is_cabinet_expert] bit NOT NULL DEFAULT CAST(0 AS bit),
        [created_at] datetimeoffset NOT NULL,
        [created_by] int NOT NULL,
        [modified_at] datetimeoffset NULL,
        [modified_by] int NULL,
        [deleted_at] datetimeoffset NULL,
        [deleted_by] int NULL,
        CONSTRAINT [PK_Company] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_Company_Company_managedby_company_id] FOREIGN KEY ([managedby_company_id]) REFERENCES [Company] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251127092020_Add_Company_Model_Employee_Model_Change_User_Login_Response_to_include_role_in_the_token'
)
BEGIN
    CREATE TABLE [Employee] (
        [Id] int NOT NULL IDENTITY,
        [first_name] nvarchar(500) NOT NULL,
        [last_name] nvarchar(500) NOT NULL,
        [cin_number] nvarchar(500) NOT NULL,
        [date_of_birth] datetime2 NOT NULL,
        [phone] int NOT NULL,
        [email] nvarchar(500) NOT NULL,
        [company_id] int NOT NULL,
        [manager_id] int NULL,
        [status_id] int NULL,
        [gender_id] int NULL,
        [nationality_id] int NULL,
        [education_level_id] int NULL,
        [marital_status_id] int NULL,
        [created_at] datetimeoffset NOT NULL,
        [created_by] int NOT NULL,
        [modified_at] datetimeoffset NULL,
        [modified_by] int NULL,
        [deleted_at] datetimeoffset NULL,
        [deleted_by] int NULL,
        CONSTRAINT [PK_Employee] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_Employee_Company_company_id] FOREIGN KEY ([company_id]) REFERENCES [Company] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_Employee_Employee_manager_id] FOREIGN KEY ([manager_id]) REFERENCES [Employee] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251127092020_Add_Company_Model_Employee_Model_Change_User_Login_Response_to_include_role_in_the_token'
)
BEGIN
    CREATE INDEX [IX_Users_EmployeeId] ON [Users] ([EmployeeId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251127092020_Add_Company_Model_Employee_Model_Change_User_Login_Response_to_include_role_in_the_token'
)
BEGIN
    CREATE INDEX [IX_Company_managedby_company_id] ON [Company] ([managedby_company_id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251127092020_Add_Company_Model_Employee_Model_Change_User_Login_Response_to_include_role_in_the_token'
)
BEGIN
    CREATE INDEX [IX_Employee_company_id] ON [Employee] ([company_id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251127092020_Add_Company_Model_Employee_Model_Change_User_Login_Response_to_include_role_in_the_token'
)
BEGIN
    CREATE INDEX [IX_Employee_manager_id] ON [Employee] ([manager_id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251127092020_Add_Company_Model_Employee_Model_Change_User_Login_Response_to_include_role_in_the_token'
)
BEGIN
    ALTER TABLE [Users] ADD CONSTRAINT [FK_Users_Employee_EmployeeId] FOREIGN KEY ([EmployeeId]) REFERENCES [Employee] ([Id]) ON DELETE NO ACTION;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251127092020_Add_Company_Model_Employee_Model_Change_User_Login_Response_to_include_role_in_the_token'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20251127092020_Add_Company_Model_Employee_Model_Change_User_Login_Response_to_include_role_in_the_token', N'9.0.0');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251201081249_Implement_Departement_Tables'
)
BEGIN
    ALTER TABLE [Employee] ADD [DepartementId] int NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251201081249_Implement_Departement_Tables'
)
BEGIN
    CREATE TABLE [Departement] (
        [Id] int NOT NULL IDENTITY,
        [DepartementName] nvarchar(100) NOT NULL,
        [CompanyId] int NOT NULL,
        [CreatedAt] datetimeoffset NOT NULL,
        [UpdatedAt] datetimeoffset NULL,
        [DeletedAt] datetimeoffset NULL,
        [CreatedBy] int NOT NULL,
        [UpdatedBy] int NULL,
        [DeletedBy] int NULL,
        CONSTRAINT [PK_Departement] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_Departement_Company_CompanyId] FOREIGN KEY ([CompanyId]) REFERENCES [Company] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251201081249_Implement_Departement_Tables'
)
BEGIN
    CREATE INDEX [IX_Employee_DepartementId] ON [Employee] ([DepartementId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251201081249_Implement_Departement_Tables'
)
BEGIN
    CREATE INDEX [IX_Departement_CompanyId] ON [Departement] ([CompanyId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251201081249_Implement_Departement_Tables'
)
BEGIN
    ALTER TABLE [Employee] ADD CONSTRAINT [FK_Employee_Departement_DepartementId] FOREIGN KEY ([DepartementId]) REFERENCES [Departement] ([Id]) ON DELETE NO ACTION;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251201081249_Implement_Departement_Tables'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20251201081249_Implement_Departement_Tables', N'9.0.0');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251202143115_InitialCreate'
)
BEGIN
    ALTER TABLE [Employee] DROP CONSTRAINT [FK_Employee_Departement_DepartementId];
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251202143115_InitialCreate'
)
BEGIN
    DROP INDEX [IX_Users_Username] ON [Users];
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251202143115_InitialCreate'
)
BEGIN
    EXEC sp_rename N'[Employee].[DepartementId]', N'departement_id', 'COLUMN';
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251202143115_InitialCreate'
)
BEGIN
    EXEC sp_rename N'[Employee].[IX_Employee_DepartementId]', N'IX_Employee_departement_id', 'INDEX';
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251202143115_InitialCreate'
)
BEGIN
    CREATE TABLE [ContractType] (
        [Id] int NOT NULL IDENTITY,
        [ContractTypeName] nvarchar(100) NOT NULL,
        [CompanyId] int NOT NULL,
        [CreatedAt] datetimeoffset NOT NULL,
        [UpdatedAt] datetimeoffset NULL,
        [DeletedAt] datetimeoffset NULL,
        [CreatedBy] int NOT NULL,
        [UpdatedBy] int NULL,
        [DeletedBy] int NULL,
        CONSTRAINT [PK_ContractType] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_ContractType_Company_CompanyId] FOREIGN KEY ([CompanyId]) REFERENCES [Company] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251202143115_InitialCreate'
)
BEGIN
    CREATE TABLE [Country] (
        [Id] int NOT NULL IDENTITY,
        [CountryName] nvarchar(500) NOT NULL,
        [CountryNameAr] nvarchar(500) NULL,
        [CountryCode] nvarchar(3) NOT NULL,
        [CountryPhoneCode] nvarchar(10) NOT NULL,
        [Nationality] nvarchar(500) NOT NULL,
        [CreatedAt] datetimeoffset NOT NULL,
        [CreatedBy] int NOT NULL,
        [ModifiedAt] datetimeoffset NULL,
        [ModifiedBy] int NULL,
        [DeletedAt] datetimeoffset NULL,
        [DeletedBy] int NULL,
        CONSTRAINT [PK_Country] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251202143115_InitialCreate'
)
BEGIN
    CREATE TABLE [EducationLevel] (
        [Id] int NOT NULL IDENTITY,
        [Name] nvarchar(100) NOT NULL,
        [CreatedAt] datetimeoffset NOT NULL,
        [CreatedBy] int NOT NULL,
        [ModifiedAt] datetimeoffset NULL,
        [ModifiedBy] int NULL,
        [DeletedAt] datetimeoffset NULL,
        [DeletedBy] int NULL,
        CONSTRAINT [PK_EducationLevel] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251202143115_InitialCreate'
)
BEGIN
    CREATE TABLE [EmployeeDocument] (
        [Id] int NOT NULL IDENTITY,
        [EmployeeId] int NOT NULL,
        [Name] nvarchar(500) NOT NULL,
        [FilePath] nvarchar(1000) NOT NULL,
        [ExpirationDate] datetime2 NULL,
        [DocumentType] nvarchar(100) NOT NULL,
        [CreatedAt] datetimeoffset NOT NULL,
        [CreatedBy] int NOT NULL,
        [ModifiedAt] datetimeoffset NULL,
        [ModifiedBy] int NULL,
        [DeletedAt] datetimeoffset NULL,
        [DeletedBy] int NULL,
        CONSTRAINT [PK_EmployeeDocument] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_EmployeeDocument_Employee_EmployeeId] FOREIGN KEY ([EmployeeId]) REFERENCES [Employee] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251202143115_InitialCreate'
)
BEGIN
    CREATE TABLE [Gender] (
        [Id] int NOT NULL IDENTITY,
        [Name] nvarchar(50) NOT NULL,
        [CreatedAt] datetimeoffset NOT NULL,
        [CreatedBy] int NOT NULL,
        [ModifiedAt] datetimeoffset NULL,
        [ModifiedBy] int NULL,
        [DeletedAt] datetimeoffset NULL,
        [DeletedBy] int NULL,
        CONSTRAINT [PK_Gender] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251202143115_InitialCreate'
)
BEGIN
    CREATE TABLE [JobPosition] (
        [Id] int NOT NULL IDENTITY,
        [Name] nvarchar(200) NOT NULL,
        [CompanyId] int NOT NULL,
        [CreatedAt] datetimeoffset NOT NULL,
        [CreatedBy] int NOT NULL,
        [ModifiedAt] datetimeoffset NULL,
        [ModifiedBy] int NULL,
        [DeletedAt] datetimeoffset NULL,
        [DeletedBy] int NULL,
        CONSTRAINT [PK_JobPosition] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_JobPosition_Company_CompanyId] FOREIGN KEY ([CompanyId]) REFERENCES [Company] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251202143115_InitialCreate'
)
BEGIN
    CREATE TABLE [MaritalStatus] (
        [Id] int NOT NULL IDENTITY,
        [Name] nvarchar(50) NOT NULL,
        [CreatedAt] datetimeoffset NOT NULL,
        [CreatedBy] int NOT NULL,
        [ModifiedAt] datetimeoffset NULL,
        [ModifiedBy] int NULL,
        [DeletedAt] datetimeoffset NULL,
        [DeletedBy] int NULL,
        CONSTRAINT [PK_MaritalStatus] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251202143115_InitialCreate'
)
BEGIN
    CREATE TABLE [Status] (
        [Id] int NOT NULL IDENTITY,
        [Name] nvarchar(100) NOT NULL,
        [CreatedAt] datetimeoffset NOT NULL,
        [CreatedBy] int NOT NULL,
        [ModifiedAt] datetimeoffset NULL,
        [ModifiedBy] int NULL,
        [DeletedAt] datetimeoffset NULL,
        [DeletedBy] int NULL,
        CONSTRAINT [PK_Status] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251202143115_InitialCreate'
)
BEGIN
    CREATE TABLE [WorkingCalendar] (
        [Id] int NOT NULL IDENTITY,
        [CompanyId] int NOT NULL,
        [DayOfWeek] int NOT NULL,
        [IsWorkingDay] bit NOT NULL,
        [StartTime] time NULL,
        [EndTime] time NULL,
        [CreatedAt] datetimeoffset NOT NULL,
        [CreatedBy] int NOT NULL,
        [ModifiedAt] datetimeoffset NULL,
        [ModifiedBy] int NULL,
        [DeletedAt] datetimeoffset NULL,
        [DeletedBy] int NULL,
        CONSTRAINT [PK_WorkingCalendar] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_WorkingCalendar_Company_CompanyId] FOREIGN KEY ([CompanyId]) REFERENCES [Company] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251202143115_InitialCreate'
)
BEGIN
    CREATE TABLE [City] (
        [Id] int NOT NULL IDENTITY,
        [CityName] nvarchar(500) NOT NULL,
        [CountryId] int NOT NULL,
        [CreatedAt] datetimeoffset NOT NULL,
        [CreatedBy] int NOT NULL,
        [ModifiedAt] datetimeoffset NULL,
        [ModifiedBy] int NULL,
        [DeletedAt] datetimeoffset NULL,
        [DeletedBy] int NULL,
        CONSTRAINT [PK_City] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_City_Country_CountryId] FOREIGN KEY ([CountryId]) REFERENCES [Country] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251202143115_InitialCreate'
)
BEGIN
    CREATE TABLE [Holiday] (
        [Id] int NOT NULL IDENTITY,
        [CompanyId] int NOT NULL,
        [CountryId] int NOT NULL,
        [HolidayDate] date NOT NULL,
        [Name] nvarchar(500) NOT NULL,
        [IsFixedAnnually] bit NOT NULL,
        [CreatedAt] datetimeoffset NOT NULL,
        [CreatedBy] int NOT NULL,
        [ModifiedAt] datetimeoffset NULL,
        [ModifiedBy] int NULL,
        [DeletedAt] datetimeoffset NULL,
        [DeletedBy] int NULL,
        CONSTRAINT [PK_Holiday] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_Holiday_Company_CompanyId] FOREIGN KEY ([CompanyId]) REFERENCES [Company] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_Holiday_Country_CountryId] FOREIGN KEY ([CountryId]) REFERENCES [Country] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251202143115_InitialCreate'
)
BEGIN
    CREATE TABLE [EmployeeContract] (
        [Id] int NOT NULL IDENTITY,
        [EmployeeId] int NOT NULL,
        [CompanyId] int NOT NULL,
        [JobPositionId] int NOT NULL,
        [ContractTypeId] int NOT NULL,
        [StartDate] datetime2 NOT NULL,
        [EndDate] datetime2 NULL,
        [CreatedAt] datetimeoffset NOT NULL,
        [CreatedBy] int NOT NULL,
        [ModifiedAt] datetimeoffset NULL,
        [ModifiedBy] int NULL,
        [DeletedAt] datetimeoffset NULL,
        [DeletedBy] int NULL,
        CONSTRAINT [PK_EmployeeContract] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_EmployeeContract_Company_CompanyId] FOREIGN KEY ([CompanyId]) REFERENCES [Company] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_EmployeeContract_ContractType_ContractTypeId] FOREIGN KEY ([ContractTypeId]) REFERENCES [ContractType] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_EmployeeContract_Employee_EmployeeId] FOREIGN KEY ([EmployeeId]) REFERENCES [Employee] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_EmployeeContract_JobPosition_JobPositionId] FOREIGN KEY ([JobPositionId]) REFERENCES [JobPosition] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251202143115_InitialCreate'
)
BEGIN
    CREATE TABLE [EmployeeAddress] (
        [Id] int NOT NULL IDENTITY,
        [EmployeeId] int NOT NULL,
        [AddressLine1] nvarchar(500) NOT NULL,
        [AddressLine2] nvarchar(500) NULL,
        [ZipCode] nvarchar(20) NOT NULL,
        [CityId] int NOT NULL,
        [CountryId] int NOT NULL,
        [CreatedAt] datetimeoffset NOT NULL,
        [CreatedBy] int NOT NULL,
        [ModifiedAt] datetimeoffset NULL,
        [ModifiedBy] int NULL,
        [DeletedAt] datetimeoffset NULL,
        [DeletedBy] int NULL,
        CONSTRAINT [PK_EmployeeAddress] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_EmployeeAddress_City_CityId] FOREIGN KEY ([CityId]) REFERENCES [City] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_EmployeeAddress_Country_CountryId] FOREIGN KEY ([CountryId]) REFERENCES [Country] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_EmployeeAddress_Employee_EmployeeId] FOREIGN KEY ([EmployeeId]) REFERENCES [Employee] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251202143115_InitialCreate'
)
BEGIN
    CREATE TABLE [EmployeeSalary] (
        [Id] int NOT NULL IDENTITY,
        [EmployeeId] int NOT NULL,
        [ContractId] int NOT NULL,
        [BaseSalary] decimal(18,2) NOT NULL,
        [EffectiveDate] datetime2 NOT NULL,
        [EndDate] datetime2 NULL,
        [CreatedAt] datetimeoffset NOT NULL,
        [CreatedBy] int NOT NULL,
        [ModifiedAt] datetimeoffset NULL,
        [ModifiedBy] int NULL,
        [DeletedAt] datetimeoffset NULL,
        [DeletedBy] int NULL,
        CONSTRAINT [PK_EmployeeSalary] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_EmployeeSalary_EmployeeContract_ContractId] FOREIGN KEY ([ContractId]) REFERENCES [EmployeeContract] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_EmployeeSalary_Employee_EmployeeId] FOREIGN KEY ([EmployeeId]) REFERENCES [Employee] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251202143115_InitialCreate'
)
BEGIN
    CREATE TABLE [EmployeeSalaryComponent] (
        [Id] int NOT NULL IDENTITY,
        [EmployeeSalaryId] int NOT NULL,
        [ComponentType] nvarchar(100) NOT NULL,
        [Amount] decimal(18,2) NOT NULL,
        [EffectiveDate] datetime2 NOT NULL,
        [EndDate] datetime2 NULL,
        [CreatedAt] datetimeoffset NOT NULL,
        [CreatedBy] int NOT NULL,
        [ModifiedAt] datetimeoffset NULL,
        [ModifiedBy] int NULL,
        [DeletedAt] datetimeoffset NULL,
        [DeletedBy] int NULL,
        CONSTRAINT [PK_EmployeeSalaryComponent] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_EmployeeSalaryComponent_EmployeeSalary_EmployeeSalaryId] FOREIGN KEY ([EmployeeSalaryId]) REFERENCES [EmployeeSalary] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251202143115_InitialCreate'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [IX_Users_Username] ON [Users] ([Username]) WHERE [DeletedAt] IS NULL');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251202143115_InitialCreate'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [IX_Employee_cin_number] ON [Employee] ([cin_number]) WHERE [deleted_at] IS NULL');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251202143115_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_Employee_education_level_id] ON [Employee] ([education_level_id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251202143115_InitialCreate'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [IX_Employee_email] ON [Employee] ([email]) WHERE [deleted_at] IS NULL');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251202143115_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_Employee_gender_id] ON [Employee] ([gender_id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251202143115_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_Employee_marital_status_id] ON [Employee] ([marital_status_id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251202143115_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_Employee_nationality_id] ON [Employee] ([nationality_id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251202143115_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_Employee_status_id] ON [Employee] ([status_id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251202143115_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_Company_city_id] ON [Company] ([city_id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251202143115_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_Company_country_id] ON [Company] ([country_id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251202143115_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_City_CountryId] ON [City] ([CountryId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251202143115_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_ContractType_CompanyId] ON [ContractType] ([CompanyId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251202143115_InitialCreate'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [IX_Country_CountryCode] ON [Country] ([CountryCode]) WHERE [DeletedAt] IS NULL');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251202143115_InitialCreate'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [IX_EducationLevel_Name] ON [EducationLevel] ([Name]) WHERE [DeletedAt] IS NULL');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251202143115_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_EmployeeAddress_CityId] ON [EmployeeAddress] ([CityId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251202143115_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_EmployeeAddress_CountryId] ON [EmployeeAddress] ([CountryId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251202143115_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_EmployeeAddress_EmployeeId] ON [EmployeeAddress] ([EmployeeId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251202143115_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_EmployeeContract_CompanyId] ON [EmployeeContract] ([CompanyId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251202143115_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_EmployeeContract_ContractTypeId] ON [EmployeeContract] ([ContractTypeId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251202143115_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_EmployeeContract_EmployeeId] ON [EmployeeContract] ([EmployeeId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251202143115_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_EmployeeContract_JobPositionId] ON [EmployeeContract] ([JobPositionId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251202143115_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_EmployeeDocument_EmployeeId] ON [EmployeeDocument] ([EmployeeId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251202143115_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_EmployeeSalary_ContractId] ON [EmployeeSalary] ([ContractId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251202143115_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_EmployeeSalary_EmployeeId] ON [EmployeeSalary] ([EmployeeId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251202143115_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_EmployeeSalaryComponent_EmployeeSalaryId] ON [EmployeeSalaryComponent] ([EmployeeSalaryId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251202143115_InitialCreate'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [IX_Gender_Name] ON [Gender] ([Name]) WHERE [DeletedAt] IS NULL');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251202143115_InitialCreate'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [IX_Holiday_CompanyId_HolidayDate] ON [Holiday] ([CompanyId], [HolidayDate]) WHERE [DeletedAt] IS NULL');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251202143115_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_Holiday_CountryId] ON [Holiday] ([CountryId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251202143115_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_JobPosition_CompanyId] ON [JobPosition] ([CompanyId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251202143115_InitialCreate'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [IX_MaritalStatus_Name] ON [MaritalStatus] ([Name]) WHERE [DeletedAt] IS NULL');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251202143115_InitialCreate'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [IX_Status_Name] ON [Status] ([Name]) WHERE [DeletedAt] IS NULL');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251202143115_InitialCreate'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [IX_WorkingCalendar_CompanyId_DayOfWeek] ON [WorkingCalendar] ([CompanyId], [DayOfWeek]) WHERE [DeletedAt] IS NULL');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251202143115_InitialCreate'
)
BEGIN
    ALTER TABLE [Company] ADD CONSTRAINT [FK_Company_City_city_id] FOREIGN KEY ([city_id]) REFERENCES [City] ([Id]) ON DELETE NO ACTION;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251202143115_InitialCreate'
)
BEGIN
    ALTER TABLE [Company] ADD CONSTRAINT [FK_Company_Country_country_id] FOREIGN KEY ([country_id]) REFERENCES [Country] ([Id]) ON DELETE NO ACTION;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251202143115_InitialCreate'
)
BEGIN
    ALTER TABLE [Employee] ADD CONSTRAINT [FK_Employee_Country_nationality_id] FOREIGN KEY ([nationality_id]) REFERENCES [Country] ([Id]) ON DELETE NO ACTION;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251202143115_InitialCreate'
)
BEGIN
    ALTER TABLE [Employee] ADD CONSTRAINT [FK_Employee_Departement_departement_id] FOREIGN KEY ([departement_id]) REFERENCES [Departement] ([Id]) ON DELETE NO ACTION;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251202143115_InitialCreate'
)
BEGIN
    ALTER TABLE [Employee] ADD CONSTRAINT [FK_Employee_EducationLevel_education_level_id] FOREIGN KEY ([education_level_id]) REFERENCES [EducationLevel] ([Id]) ON DELETE NO ACTION;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251202143115_InitialCreate'
)
BEGIN
    ALTER TABLE [Employee] ADD CONSTRAINT [FK_Employee_Gender_gender_id] FOREIGN KEY ([gender_id]) REFERENCES [Gender] ([Id]) ON DELETE NO ACTION;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251202143115_InitialCreate'
)
BEGIN
    ALTER TABLE [Employee] ADD CONSTRAINT [FK_Employee_MaritalStatus_marital_status_id] FOREIGN KEY ([marital_status_id]) REFERENCES [MaritalStatus] ([Id]) ON DELETE NO ACTION;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251202143115_InitialCreate'
)
BEGIN
    ALTER TABLE [Employee] ADD CONSTRAINT [FK_Employee_Status_status_id] FOREIGN KEY ([status_id]) REFERENCES [Status] ([Id]) ON DELETE NO ACTION;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251202143115_InitialCreate'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20251202143115_InitialCreate', N'9.0.0');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251208102259_migration1'
)
BEGIN
    ALTER TABLE [Employee] DROP CONSTRAINT [FK_Employee_Country_nationality_id];
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251208102259_migration1'
)
BEGIN
    ALTER TABLE [EmployeeAddress] DROP CONSTRAINT [FK_EmployeeAddress_Country_CountryId];
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251208102259_migration1'
)
BEGIN
    DECLARE @var2 sysname;
    SELECT @var2 = [d].[name]
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Country]') AND [c].[name] = N'Nationality');
    IF @var2 IS NOT NULL EXEC(N'ALTER TABLE [Country] DROP CONSTRAINT [' + @var2 + '];');
    ALTER TABLE [Country] DROP COLUMN [Nationality];
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251208102259_migration1'
)
BEGIN
    EXEC sp_rename N'[Employee].[nationality_id]', N'CountryId', 'COLUMN';
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251208102259_migration1'
)
BEGIN
    EXEC sp_rename N'[Employee].[IX_Employee_nationality_id]', N'IX_Employee_CountryId', 'INDEX';
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251208102259_migration1'
)
BEGIN
    DECLARE @var3 sysname;
    SELECT @var3 = [d].[name]
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[EmployeeAddress]') AND [c].[name] = N'CountryId');
    IF @var3 IS NOT NULL EXEC(N'ALTER TABLE [EmployeeAddress] DROP CONSTRAINT [' + @var3 + '];');
    ALTER TABLE [EmployeeAddress] ALTER COLUMN [CountryId] int NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251208102259_migration1'
)
BEGIN
    ALTER TABLE [Employee] ADD CONSTRAINT [FK_Employee_Country_CountryId] FOREIGN KEY ([CountryId]) REFERENCES [Country] ([Id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251208102259_migration1'
)
BEGIN
    ALTER TABLE [EmployeeAddress] ADD CONSTRAINT [FK_EmployeeAddress_Country_CountryId] FOREIGN KEY ([CountryId]) REFERENCES [Country] ([Id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251208102259_migration1'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20251208102259_migration1', N'9.0.0');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251208104432_Event'
)
BEGIN
    CREATE TABLE [EventType] (
        [Id] int NOT NULL IDENTITY,
        [Name] nvarchar(100) NOT NULL,
        [Description] nvarchar(500) NULL,
        CONSTRAINT [PK_EventType] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251208104432_Event'
)
BEGIN
    CREATE TABLE [EventsEmployee] (
        [Id] int NOT NULL IDENTITY,
        [EmployeeId] int NOT NULL,
        [EventTypeId] int NOT NULL,
        [EventTime] datetime2 NOT NULL,
        [CreatedAt] datetimeoffset NOT NULL,
        [CreatedBy] int NOT NULL,
        [ModifiedAt] datetimeoffset NULL,
        [ModifiedBy] int NULL,
        [DeletedAt] datetimeoffset NULL,
        [DeletedBy] int NULL,
        CONSTRAINT [PK_EventsEmployee] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_EventsEmployee_Employee_EmployeeId] FOREIGN KEY ([EmployeeId]) REFERENCES [Employee] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_EventsEmployee_EventType_EventTypeId] FOREIGN KEY ([EventTypeId]) REFERENCES [EventType] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251208104432_Event'
)
BEGIN
    CREATE INDEX [IX_EventsEmployee_EmployeeId] ON [EventsEmployee] ([EmployeeId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251208104432_Event'
)
BEGIN
    CREATE INDEX [IX_EventsEmployee_EventTypeId] ON [EventsEmployee] ([EventTypeId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251208104432_Event'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20251208104432_Event', N'9.0.0');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251209131608_Update_Employee_Table'
)
BEGIN
    ALTER TABLE [Employee] ADD [cimr_number] int NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251209131608_Update_Employee_Table'
)
BEGIN
    ALTER TABLE [Employee] ADD [cnss_number] int NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251209131608_Update_Employee_Table'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20251209131608_Update_Employee_Table', N'9.0.0');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251209133420_add_nationality_tables'
)
BEGIN
    ALTER TABLE [Employee] ADD [NationalityId] int NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251209133420_add_nationality_tables'
)
BEGIN
    CREATE TABLE [Nationality] (
        [Id] int NOT NULL IDENTITY,
        [Name] nvarchar(450) NOT NULL,
        [CreatedAt] datetimeoffset NOT NULL,
        [CreatedBy] int NOT NULL,
        [ModifiedAt] datetimeoffset NULL,
        [ModifiedBy] int NULL,
        [DeletedAt] datetimeoffset NULL,
        [DeletedBy] int NULL,
        CONSTRAINT [PK_Nationality] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251209133420_add_nationality_tables'
)
BEGIN
    CREATE INDEX [IX_Employee_NationalityId] ON [Employee] ([NationalityId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251209133420_add_nationality_tables'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [IX_Nationality_Name] ON [Nationality] ([Name]) WHERE [DeletedAt] IS NULL');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251209133420_add_nationality_tables'
)
BEGIN
    ALTER TABLE [Employee] ADD CONSTRAINT [FK_Employee_Nationality_NationalityId] FOREIGN KEY ([NationalityId]) REFERENCES [Nationality] ([Id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251209133420_add_nationality_tables'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20251209133420_add_nationality_tables', N'9.0.0');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251210085555_Change_Number_to_string'
)
BEGIN
    DECLARE @var4 sysname;
    SELECT @var4 = [d].[name]
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Employee]') AND [c].[name] = N'cnss_number');
    IF @var4 IS NOT NULL EXEC(N'ALTER TABLE [Employee] DROP CONSTRAINT [' + @var4 + '];');
    ALTER TABLE [Employee] ALTER COLUMN [cnss_number] nvarchar(max) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251210085555_Change_Number_to_string'
)
BEGIN
    DECLARE @var5 sysname;
    SELECT @var5 = [d].[name]
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Employee]') AND [c].[name] = N'cimr_number');
    IF @var5 IS NOT NULL EXEC(N'ALTER TABLE [Employee] DROP CONSTRAINT [' + @var5 + '];');
    ALTER TABLE [Employee] ALTER COLUMN [cimr_number] nvarchar(max) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251210085555_Change_Number_to_string'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20251210085555_Change_Number_to_string', N'9.0.0');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251210100918_Change_Phone_to_string'
)
BEGIN
    DECLARE @var6 sysname;
    SELECT @var6 = [d].[name]
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Employee]') AND [c].[name] = N'phone');
    IF @var6 IS NOT NULL EXEC(N'ALTER TABLE [Employee] DROP CONSTRAINT [' + @var6 + '];');
    ALTER TABLE [Employee] ALTER COLUMN [phone] nvarchar(max) NOT NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251210100918_Change_Phone_to_string'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20251210100918_Change_Phone_to_string', N'9.0.0');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251211131034_delete_event_tables'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20251211131034_delete_event_tables', N'9.0.0');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251211134527_eventlog'
)
BEGIN
    CREATE TABLE [EmployeeEventLog] (
        [Id] int NOT NULL IDENTITY,
        [employeeId] int NOT NULL,
        [eventName] nvarchar(200) NOT NULL,
        [oldValue] nvarchar(1000) NULL,
        [oldValueId] int NULL,
        [newValue] nvarchar(1000) NULL,
        [newValueId] int NULL,
        [createdAt] datetimeoffset NOT NULL,
        [createdBy] int NOT NULL,
        CONSTRAINT [PK_EmployeeEventLog] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_EmployeeEventLog_Employee_employeeId] FOREIGN KEY ([employeeId]) REFERENCES [Employee] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_EmployeeEventLog_Users_createdBy] FOREIGN KEY ([createdBy]) REFERENCES [Users] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251211134527_eventlog'
)
BEGIN
    CREATE INDEX [IX_EmployeeEventLog_createdBy] ON [EmployeeEventLog] ([createdBy]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251211134527_eventlog'
)
BEGIN
    CREATE INDEX [IX_EmployeeEventLog_employeeId] ON [EmployeeEventLog] ([employeeId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251211134527_eventlog'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20251211134527_eventlog', N'9.0.0');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251215111424_users'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20251215111424_users', N'9.0.0');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251215145246_addpersonalEmailToEmployee'
)
BEGIN
    ALTER TABLE [Employee] ADD [personal_email] nvarchar(500) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251215145246_addpersonalEmailToEmployee'
)
BEGIN
    EXEC(N'CREATE INDEX [IX_Employee_personal_email] ON [Employee] ([personal_email]) WHERE [deleted_at] IS NULL AND [personal_email] IS NOT NULL');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251215145246_addpersonalEmailToEmployee'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20251215145246_addpersonalEmailToEmployee', N'9.0.0');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251215145932_addpersonalEmailTouser'
)
BEGIN
    ALTER TABLE [Users] ADD [EmailPersonal] nvarchar(450) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251215145932_addpersonalEmailTouser'
)
BEGIN
    EXEC(N'CREATE INDEX [IX_Users_EmailPersonal] ON [Users] ([EmailPersonal]) WHERE [DeletedAt] IS NULL AND [EmailPersonal] IS NOT NULL');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251215145932_addpersonalEmailTouser'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20251215145932_addpersonalEmailTouser', N'9.0.0');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251217155714_UpdateCompanyModel_AddPayrollFiel'
)
BEGIN
    DECLARE @var7 sysname;
    SELECT @var7 = [d].[name]
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Company]') AND [c].[name] = N'rib_number');
    IF @var7 IS NOT NULL EXEC(N'ALTER TABLE [Company] DROP CONSTRAINT [' + @var7 + '];');
    ALTER TABLE [Company] ALTER COLUMN [rib_number] nvarchar(100) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251217155714_UpdateCompanyModel_AddPayrollFiel'
)
BEGIN
    DECLARE @var8 sysname;
    SELECT @var8 = [d].[name]
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Company]') AND [c].[name] = N'rc_number');
    IF @var8 IS NOT NULL EXEC(N'ALTER TABLE [Company] DROP CONSTRAINT [' + @var8 + '];');
    ALTER TABLE [Company] ALTER COLUMN [rc_number] nvarchar(100) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251217155714_UpdateCompanyModel_AddPayrollFiel'
)
BEGIN
    DECLARE @var9 sysname;
    SELECT @var9 = [d].[name]
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Company]') AND [c].[name] = N'phone_number');
    IF @var9 IS NOT NULL EXEC(N'ALTER TABLE [Company] DROP CONSTRAINT [' + @var9 + '];');
    ALTER TABLE [Company] ALTER COLUMN [phone_number] nvarchar(20) NOT NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251217155714_UpdateCompanyModel_AddPayrollFiel'
)
BEGIN
    DECLARE @var10 sysname;
    SELECT @var10 = [d].[name]
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Company]') AND [c].[name] = N'if_number');
    IF @var10 IS NOT NULL EXEC(N'ALTER TABLE [Company] DROP CONSTRAINT [' + @var10 + '];');
    ALTER TABLE [Company] ALTER COLUMN [if_number] nvarchar(100) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251217155714_UpdateCompanyModel_AddPayrollFiel'
)
BEGIN
    DECLARE @var11 sysname;
    SELECT @var11 = [d].[name]
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Company]') AND [c].[name] = N'ice_number');
    IF @var11 IS NOT NULL EXEC(N'ALTER TABLE [Company] DROP CONSTRAINT [' + @var11 + '];');
    ALTER TABLE [Company] ALTER COLUMN [ice_number] nvarchar(100) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251217155714_UpdateCompanyModel_AddPayrollFiel'
)
BEGIN
    DROP INDEX [IX_Company_country_id] ON [Company];
    DECLARE @var12 sysname;
    SELECT @var12 = [d].[name]
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Company]') AND [c].[name] = N'country_id');
    IF @var12 IS NOT NULL EXEC(N'ALTER TABLE [Company] DROP CONSTRAINT [' + @var12 + '];');
    EXEC(N'UPDATE [Company] SET [country_id] = 0 WHERE [country_id] IS NULL');
    ALTER TABLE [Company] ALTER COLUMN [country_id] int NOT NULL;
    ALTER TABLE [Company] ADD DEFAULT 0 FOR [country_id];
    CREATE INDEX [IX_Company_country_id] ON [Company] ([country_id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251217155714_UpdateCompanyModel_AddPayrollFiel'
)
BEGIN
    DECLARE @var13 sysname;
    SELECT @var13 = [d].[name]
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Company]') AND [c].[name] = N'company_address');
    IF @var13 IS NOT NULL EXEC(N'ALTER TABLE [Company] DROP CONSTRAINT [' + @var13 + '];');
    ALTER TABLE [Company] ALTER COLUMN [company_address] nvarchar(1000) NOT NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251217155714_UpdateCompanyModel_AddPayrollFiel'
)
BEGIN
    DECLARE @var14 sysname;
    SELECT @var14 = [d].[name]
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Company]') AND [c].[name] = N'cnss_number');
    IF @var14 IS NOT NULL EXEC(N'ALTER TABLE [Company] DROP CONSTRAINT [' + @var14 + '];');
    ALTER TABLE [Company] ALTER COLUMN [cnss_number] nvarchar(100) NOT NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251217155714_UpdateCompanyModel_AddPayrollFiel'
)
BEGIN
    DROP INDEX [IX_Company_city_id] ON [Company];
    DECLARE @var15 sysname;
    SELECT @var15 = [d].[name]
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Company]') AND [c].[name] = N'city_id');
    IF @var15 IS NOT NULL EXEC(N'ALTER TABLE [Company] DROP CONSTRAINT [' + @var15 + '];');
    EXEC(N'UPDATE [Company] SET [city_id] = 0 WHERE [city_id] IS NULL');
    ALTER TABLE [Company] ALTER COLUMN [city_id] int NOT NULL;
    ALTER TABLE [Company] ADD DEFAULT 0 FOR [city_id];
    CREATE INDEX [IX_Company_city_id] ON [Company] ([city_id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251217155714_UpdateCompanyModel_AddPayrollFiel'
)
BEGIN
    ALTER TABLE [Company] ADD [business_sector] nvarchar(200) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251217155714_UpdateCompanyModel_AddPayrollFiel'
)
BEGIN
    ALTER TABLE [Company] ADD [country_phone_code] nvarchar(10) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251217155714_UpdateCompanyModel_AddPayrollFiel'
)
BEGIN
    ALTER TABLE [Company] ADD [currency] nvarchar(10) NOT NULL DEFAULT N'MAD';
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251217155714_UpdateCompanyModel_AddPayrollFiel'
)
BEGIN
    ALTER TABLE [Company] ADD [fiscal_year_start_month] int NOT NULL DEFAULT 1;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251217155714_UpdateCompanyModel_AddPayrollFiel'
)
BEGIN
    ALTER TABLE [Company] ADD [founding_date] date NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251217155714_UpdateCompanyModel_AddPayrollFiel'
)
BEGIN
    ALTER TABLE [Company] ADD [legal_form] nvarchar(50) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251217155714_UpdateCompanyModel_AddPayrollFiel'
)
BEGIN
    ALTER TABLE [Company] ADD [payment_method] nvarchar(100) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251217155714_UpdateCompanyModel_AddPayrollFiel'
)
BEGIN
    ALTER TABLE [Company] ADD [payroll_periodicity] nvarchar(50) NOT NULL DEFAULT N'Mensuelle';
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251217155714_UpdateCompanyModel_AddPayrollFiel'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20251217155714_UpdateCompanyModel_AddPayrollFiel', N'9.0.0');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251222132551_add_CompanyEventLog'
)
BEGIN
    CREATE TABLE [CompanyEventLog] (
        [Id] int NOT NULL IDENTITY,
        [employeeId] int NOT NULL,
        [eventName] nvarchar(200) NOT NULL,
        [oldValue] nvarchar(1000) NULL,
        [oldValueId] int NULL,
        [newValue] nvarchar(1000) NULL,
        [newValueId] int NULL,
        [createdAt] datetimeoffset NOT NULL,
        [createdBy] int NOT NULL,
        [companyId] int NOT NULL,
        CONSTRAINT [PK_CompanyEventLog] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_CompanyEventLog_Company_companyId] FOREIGN KEY ([companyId]) REFERENCES [Company] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_CompanyEventLog_Users_createdBy] FOREIGN KEY ([createdBy]) REFERENCES [Users] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251222132551_add_CompanyEventLog'
)
BEGIN
    CREATE INDEX [IX_CompanyEventLog_companyId] ON [CompanyEventLog] ([companyId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251222132551_add_CompanyEventLog'
)
BEGIN
    CREATE INDEX [IX_CompanyEventLog_createdBy] ON [CompanyEventLog] ([createdBy]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251222132551_add_CompanyEventLog'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20251222132551_add_CompanyEventLog', N'9.0.0');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251224145715_add_Resource_to_Permission_table'
)
BEGIN
    ALTER TABLE [Permissions] ADD [Resource] nvarchar(max) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251224145715_add_Resource_to_Permission_table'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20251224145715_add_Resource_to_Permission_table', N'9.0.0');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251224151832_add_Action_to_Permission_table'
)
BEGIN
    ALTER TABLE [Permissions] ADD [Action] nvarchar(max) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251224151832_add_Action_to_Permission_table'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20251224151832_add_Action_to_Permission_table', N'9.0.0');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251230133647_Changement_table_gender'
)
BEGIN
    DROP INDEX [IX_Gender_Name] ON [Gender];
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251230133647_Changement_table_gender'
)
BEGIN
    DECLARE @var16 sysname;
    SELECT @var16 = [d].[name]
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Gender]') AND [c].[name] = N'DeletedAt');
    IF @var16 IS NOT NULL EXEC(N'ALTER TABLE [Gender] DROP CONSTRAINT [' + @var16 + '];');
    ALTER TABLE [Gender] DROP COLUMN [DeletedAt];
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251230133647_Changement_table_gender'
)
BEGIN
    DECLARE @var17 sysname;
    SELECT @var17 = [d].[name]
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Gender]') AND [c].[name] = N'DeletedBy');
    IF @var17 IS NOT NULL EXEC(N'ALTER TABLE [Gender] DROP CONSTRAINT [' + @var17 + '];');
    ALTER TABLE [Gender] DROP COLUMN [DeletedBy];
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251230133647_Changement_table_gender'
)
BEGIN
    EXEC sp_rename N'[Gender].[Name]', N'Code', 'COLUMN';
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251230133647_Changement_table_gender'
)
BEGIN
    ALTER TABLE [Gender] ADD [IsActive] bit NOT NULL DEFAULT CAST(1 AS bit);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251230133647_Changement_table_gender'
)
BEGIN
    ALTER TABLE [Gender] ADD [NameAr] nvarchar(100) NOT NULL DEFAULT N'';
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251230133647_Changement_table_gender'
)
BEGIN
    ALTER TABLE [Gender] ADD [NameEn] nvarchar(100) NOT NULL DEFAULT N'';
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251230133647_Changement_table_gender'
)
BEGIN
    ALTER TABLE [Gender] ADD [NameFr] nvarchar(100) NOT NULL DEFAULT N'';
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251230133647_Changement_table_gender'
)
BEGIN
    CREATE UNIQUE INDEX [IX_Gender_Code] ON [Gender] ([Code]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251230133647_Changement_table_gender'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20251230133647_Changement_table_gender', N'9.0.0');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251230141454_Changement_table_education_level'
)
BEGIN
    DROP INDEX [IX_EducationLevel_Name] ON [EducationLevel];
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251230141454_Changement_table_education_level'
)
BEGIN
    DECLARE @var18 sysname;
    SELECT @var18 = [d].[name]
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[EducationLevel]') AND [c].[name] = N'DeletedAt');
    IF @var18 IS NOT NULL EXEC(N'ALTER TABLE [EducationLevel] DROP CONSTRAINT [' + @var18 + '];');
    ALTER TABLE [EducationLevel] DROP COLUMN [DeletedAt];
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251230141454_Changement_table_education_level'
)
BEGIN
    DECLARE @var19 sysname;
    SELECT @var19 = [d].[name]
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[EducationLevel]') AND [c].[name] = N'DeletedBy');
    IF @var19 IS NOT NULL EXEC(N'ALTER TABLE [EducationLevel] DROP CONSTRAINT [' + @var19 + '];');
    ALTER TABLE [EducationLevel] DROP COLUMN [DeletedBy];
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251230141454_Changement_table_education_level'
)
BEGIN
    EXEC sp_rename N'[EducationLevel].[Name]', N'NameFr', 'COLUMN';
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251230141454_Changement_table_education_level'
)
BEGIN
    ALTER TABLE [EducationLevel] ADD [Code] nvarchar(50) NOT NULL DEFAULT N'';
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251230141454_Changement_table_education_level'
)
BEGIN
    ALTER TABLE [EducationLevel] ADD [IsActive] bit NOT NULL DEFAULT CAST(1 AS bit);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251230141454_Changement_table_education_level'
)
BEGIN
    ALTER TABLE [EducationLevel] ADD [LevelOrder] int NOT NULL DEFAULT 0;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251230141454_Changement_table_education_level'
)
BEGIN
    ALTER TABLE [EducationLevel] ADD [NameAr] nvarchar(100) NOT NULL DEFAULT N'';
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251230141454_Changement_table_education_level'
)
BEGIN
    ALTER TABLE [EducationLevel] ADD [NameEn] nvarchar(100) NOT NULL DEFAULT N'';
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251230141454_Changement_table_education_level'
)
BEGIN
    CREATE UNIQUE INDEX [IX_EducationLevel_Code] ON [EducationLevel] ([Code]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251230141454_Changement_table_education_level'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20251230141454_Changement_table_education_level', N'9.0.0');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251230144322_Changement_table_marital_status'
)
BEGIN
    DROP INDEX [IX_MaritalStatus_Name] ON [MaritalStatus];
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251230144322_Changement_table_marital_status'
)
BEGIN
    DECLARE @var20 sysname;
    SELECT @var20 = [d].[name]
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[MaritalStatus]') AND [c].[name] = N'DeletedAt');
    IF @var20 IS NOT NULL EXEC(N'ALTER TABLE [MaritalStatus] DROP CONSTRAINT [' + @var20 + '];');
    ALTER TABLE [MaritalStatus] DROP COLUMN [DeletedAt];
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251230144322_Changement_table_marital_status'
)
BEGIN
    DECLARE @var21 sysname;
    SELECT @var21 = [d].[name]
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[MaritalStatus]') AND [c].[name] = N'DeletedBy');
    IF @var21 IS NOT NULL EXEC(N'ALTER TABLE [MaritalStatus] DROP CONSTRAINT [' + @var21 + '];');
    ALTER TABLE [MaritalStatus] DROP COLUMN [DeletedBy];
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251230144322_Changement_table_marital_status'
)
BEGIN
    EXEC sp_rename N'[MaritalStatus].[Name]', N'Code', 'COLUMN';
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251230144322_Changement_table_marital_status'
)
BEGIN
    ALTER TABLE [MaritalStatus] ADD [IsActive] bit NOT NULL DEFAULT CAST(1 AS bit);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251230144322_Changement_table_marital_status'
)
BEGIN
    ALTER TABLE [MaritalStatus] ADD [NameAr] nvarchar(100) NOT NULL DEFAULT N'';
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251230144322_Changement_table_marital_status'
)
BEGIN
    ALTER TABLE [MaritalStatus] ADD [NameEn] nvarchar(100) NOT NULL DEFAULT N'';
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251230144322_Changement_table_marital_status'
)
BEGIN
    ALTER TABLE [MaritalStatus] ADD [NameFr] nvarchar(100) NOT NULL DEFAULT N'';
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251230144322_Changement_table_marital_status'
)
BEGIN
    CREATE UNIQUE INDEX [IX_MaritalStatus_Code] ON [MaritalStatus] ([Code]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251230144322_Changement_table_marital_status'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20251230144322_Changement_table_marital_status', N'9.0.0');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251230151913_Changement_table_statues'
)
BEGIN
    DROP INDEX [IX_Status_Name] ON [Status];
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251230151913_Changement_table_statues'
)
BEGIN
    DECLARE @var22 sysname;
    SELECT @var22 = [d].[name]
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Status]') AND [c].[name] = N'DeletedAt');
    IF @var22 IS NOT NULL EXEC(N'ALTER TABLE [Status] DROP CONSTRAINT [' + @var22 + '];');
    ALTER TABLE [Status] DROP COLUMN [DeletedAt];
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251230151913_Changement_table_statues'
)
BEGIN
    DECLARE @var23 sysname;
    SELECT @var23 = [d].[name]
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Status]') AND [c].[name] = N'DeletedBy');
    IF @var23 IS NOT NULL EXEC(N'ALTER TABLE [Status] DROP CONSTRAINT [' + @var23 + '];');
    ALTER TABLE [Status] DROP COLUMN [DeletedBy];
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251230151913_Changement_table_statues'
)
BEGIN
    EXEC sp_rename N'[Status].[Name]', N'NameFr', 'COLUMN';
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251230151913_Changement_table_statues'
)
BEGIN
    ALTER TABLE [Status] ADD [AffectsAccess] bit NOT NULL DEFAULT CAST(0 AS bit);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251230151913_Changement_table_statues'
)
BEGIN
    ALTER TABLE [Status] ADD [AffectsAttendance] bit NOT NULL DEFAULT CAST(0 AS bit);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251230151913_Changement_table_statues'
)
BEGIN
    ALTER TABLE [Status] ADD [AffectsPayroll] bit NOT NULL DEFAULT CAST(0 AS bit);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251230151913_Changement_table_statues'
)
BEGIN
    ALTER TABLE [Status] ADD [Code] nvarchar(50) NOT NULL DEFAULT N'';
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251230151913_Changement_table_statues'
)
BEGIN
    ALTER TABLE [Status] ADD [IsActive] bit NOT NULL DEFAULT CAST(1 AS bit);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251230151913_Changement_table_statues'
)
BEGIN
    ALTER TABLE [Status] ADD [NameAr] nvarchar(100) NOT NULL DEFAULT N'';
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251230151913_Changement_table_statues'
)
BEGIN
    ALTER TABLE [Status] ADD [NameEn] nvarchar(100) NOT NULL DEFAULT N'';
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251230151913_Changement_table_statues'
)
BEGIN
    CREATE UNIQUE INDEX [IX_Status_Code] ON [Status] ([Code]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251230151913_Changement_table_statues'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20251230151913_Changement_table_statues', N'9.0.0');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260109230511_AddSalaryPackages'
)
BEGIN
    CREATE TABLE [SalaryPackage] (
        [Id] int NOT NULL IDENTITY,
        [Name] nvarchar(200) NOT NULL,
        [Category] nvarchar(100) NOT NULL,
        [Description] nvarchar(1000) NULL,
        [BaseSalary] decimal(18,2) NOT NULL,
        [Status] nvarchar(20) NOT NULL DEFAULT N'draft',
        [CompanyId] int NULL,
        [CreatedAt] datetimeoffset NOT NULL,
        [CreatedBy] int NOT NULL,
        [ModifiedAt] datetimeoffset NULL,
        [ModifiedBy] int NULL,
        [DeletedAt] datetimeoffset NULL,
        [DeletedBy] int NULL,
        CONSTRAINT [PK_SalaryPackage] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_SalaryPackage_Company_CompanyId] FOREIGN KEY ([CompanyId]) REFERENCES [Company] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260109230511_AddSalaryPackages'
)
BEGIN
    CREATE TABLE [SalaryPackageAssignment] (
        [Id] int NOT NULL IDENTITY,
        [SalaryPackageId] int NOT NULL,
        [EmployeeId] int NOT NULL,
        [ContractId] int NOT NULL,
        [EmployeeSalaryId] int NOT NULL,
        [EffectiveDate] datetime2 NOT NULL,
        [EndDate] datetime2 NULL,
        [CreatedAt] datetimeoffset NOT NULL,
        [CreatedBy] int NOT NULL,
        [ModifiedAt] datetimeoffset NULL,
        [ModifiedBy] int NULL,
        [DeletedAt] datetimeoffset NULL,
        [DeletedBy] int NULL,
        CONSTRAINT [PK_SalaryPackageAssignment] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_SalaryPackageAssignment_EmployeeContract_ContractId] FOREIGN KEY ([ContractId]) REFERENCES [EmployeeContract] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_SalaryPackageAssignment_EmployeeSalary_EmployeeSalaryId] FOREIGN KEY ([EmployeeSalaryId]) REFERENCES [EmployeeSalary] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_SalaryPackageAssignment_Employee_EmployeeId] FOREIGN KEY ([EmployeeId]) REFERENCES [Employee] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_SalaryPackageAssignment_SalaryPackage_SalaryPackageId] FOREIGN KEY ([SalaryPackageId]) REFERENCES [SalaryPackage] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260109230511_AddSalaryPackages'
)
BEGIN
    CREATE TABLE [SalaryPackageItem] (
        [Id] int NOT NULL IDENTITY,
        [SalaryPackageId] int NOT NULL,
        [Label] nvarchar(200) NOT NULL,
        [DefaultValue] decimal(18,2) NOT NULL,
        [SortOrder] int NOT NULL,
        [CreatedAt] datetimeoffset NOT NULL,
        [CreatedBy] int NOT NULL,
        [ModifiedAt] datetimeoffset NULL,
        [ModifiedBy] int NULL,
        [DeletedAt] datetimeoffset NULL,
        [DeletedBy] int NULL,
        CONSTRAINT [PK_SalaryPackageItem] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_SalaryPackageItem_SalaryPackage_SalaryPackageId] FOREIGN KEY ([SalaryPackageId]) REFERENCES [SalaryPackage] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260109230511_AddSalaryPackages'
)
BEGIN
    CREATE INDEX [IX_SalaryPackage_CompanyId] ON [SalaryPackage] ([CompanyId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260109230511_AddSalaryPackages'
)
BEGIN
    CREATE INDEX [IX_SalaryPackageAssignment_ContractId] ON [SalaryPackageAssignment] ([ContractId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260109230511_AddSalaryPackages'
)
BEGIN
    CREATE INDEX [IX_SalaryPackageAssignment_EmployeeId] ON [SalaryPackageAssignment] ([EmployeeId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260109230511_AddSalaryPackages'
)
BEGIN
    CREATE INDEX [IX_SalaryPackageAssignment_EmployeeSalaryId] ON [SalaryPackageAssignment] ([EmployeeSalaryId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260109230511_AddSalaryPackages'
)
BEGIN
    CREATE INDEX [IX_SalaryPackageAssignment_SalaryPackageId] ON [SalaryPackageAssignment] ([SalaryPackageId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260109230511_AddSalaryPackages'
)
BEGIN
    CREATE INDEX [IX_SalaryPackageItem_SalaryPackageId] ON [SalaryPackageItem] ([SalaryPackageId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260109230511_AddSalaryPackages'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260109230511_AddSalaryPackages', N'9.0.0');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260119174350_AddMoroccanPayrollCompliance'
)
BEGIN

                    IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'SalaryPackageItem') AND name = 'ExemptionLimit')
                    ALTER TABLE [SalaryPackageItem] ADD [ExemptionLimit] decimal(18,2) NULL;
                
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260119174350_AddMoroccanPayrollCompliance'
)
BEGIN

                    IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'SalaryPackageItem') AND name = 'IsCIMR')
                    ALTER TABLE [SalaryPackageItem] ADD [IsCIMR] bit NOT NULL DEFAULT 0;
                
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260119174350_AddMoroccanPayrollCompliance'
)
BEGIN

                    IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'SalaryPackageItem') AND name = 'IsSocial')
                    ALTER TABLE [SalaryPackageItem] ADD [IsSocial] bit NOT NULL DEFAULT 1;
                
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260119174350_AddMoroccanPayrollCompliance'
)
BEGIN

                    IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'SalaryPackageItem') AND name = 'IsTaxable')
                    ALTER TABLE [SalaryPackageItem] ADD [IsTaxable] bit NOT NULL DEFAULT 1;
                
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260119174350_AddMoroccanPayrollCompliance'
)
BEGIN

                    IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'SalaryPackageItem') AND name = 'IsVariable')
                    ALTER TABLE [SalaryPackageItem] ADD [IsVariable] bit NOT NULL DEFAULT 0;
                
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260119174350_AddMoroccanPayrollCompliance'
)
BEGIN

                    IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'SalaryPackageItem') AND name = 'PayComponentId')
                    ALTER TABLE [SalaryPackageItem] ADD [PayComponentId] int NULL;
                
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260119174350_AddMoroccanPayrollCompliance'
)
BEGIN

                    IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'SalaryPackageItem') AND name = 'Type')
                    ALTER TABLE [SalaryPackageItem] ADD [Type] nvarchar(50) NOT NULL DEFAULT 'allowance';
                
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260119174350_AddMoroccanPayrollCompliance'
)
BEGIN

                    IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'SalaryPackageAssignment') AND name = 'PackageVersion')
                    ALTER TABLE [SalaryPackageAssignment] ADD [PackageVersion] int NOT NULL DEFAULT 1;
                
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260119174350_AddMoroccanPayrollCompliance'
)
BEGIN

                    IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'SalaryPackage') AND name = 'CimrRate')
                    ALTER TABLE [SalaryPackage] ADD [CimrRate] decimal(5,4) NULL;
                
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260119174350_AddMoroccanPayrollCompliance'
)
BEGIN

                    IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'SalaryPackage') AND name = 'HasPrivateInsurance')
                    ALTER TABLE [SalaryPackage] ADD [HasPrivateInsurance] bit NOT NULL DEFAULT 0;
                
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260119174350_AddMoroccanPayrollCompliance'
)
BEGIN

                    IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'SalaryPackage') AND name = 'IsLocked')
                    ALTER TABLE [SalaryPackage] ADD [IsLocked] bit NOT NULL DEFAULT 0;
                
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260119174350_AddMoroccanPayrollCompliance'
)
BEGIN

                    IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'SalaryPackage') AND name = 'SourceTemplateId')
                    ALTER TABLE [SalaryPackage] ADD [SourceTemplateId] int NULL;
                
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260119174350_AddMoroccanPayrollCompliance'
)
BEGIN

                    IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'SalaryPackage') AND name = 'SourceTemplateVersion')
                    ALTER TABLE [SalaryPackage] ADD [SourceTemplateVersion] int NULL;
                
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260119174350_AddMoroccanPayrollCompliance'
)
BEGIN

                    IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'SalaryPackage') AND name = 'ValidFrom')
                    ALTER TABLE [SalaryPackage] ADD [ValidFrom] datetime2 NULL;
                
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260119174350_AddMoroccanPayrollCompliance'
)
BEGIN

                    IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'SalaryPackage') AND name = 'ValidTo')
                    ALTER TABLE [SalaryPackage] ADD [ValidTo] datetime2 NULL;
                
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260119174350_AddMoroccanPayrollCompliance'
)
BEGIN

                    IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'SalaryPackage') AND name = 'Version')
                    ALTER TABLE [SalaryPackage] ADD [Version] int NOT NULL DEFAULT 1;
                
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260119174350_AddMoroccanPayrollCompliance'
)
BEGIN
    CREATE TABLE [PayComponent] (
        [Id] int NOT NULL IDENTITY,
        [Code] nvarchar(50) NOT NULL,
        [NameFr] nvarchar(200) NOT NULL,
        [NameAr] nvarchar(200) NULL,
        [NameEn] nvarchar(200) NULL,
        [Type] nvarchar(50) NOT NULL,
        [IsTaxable] bit NOT NULL,
        [IsSocial] bit NOT NULL,
        [IsCIMR] bit NOT NULL,
        [ExemptionLimit] decimal(18,2) NULL,
        [ExemptionRule] nvarchar(100) NULL,
        [DefaultAmount] decimal(18,2) NULL,
        [Version] int NOT NULL DEFAULT 1,
        [ValidFrom] datetime2 NOT NULL,
        [ValidTo] datetime2 NULL,
        [IsRegulated] bit NOT NULL DEFAULT CAST(0 AS bit),
        [IsActive] bit NOT NULL DEFAULT CAST(1 AS bit),
        [SortOrder] int NOT NULL DEFAULT 0,
        [CreatedAt] datetimeoffset NOT NULL,
        [CreatedBy] int NOT NULL,
        [ModifiedAt] datetimeoffset NULL,
        [ModifiedBy] int NULL,
        [DeletedAt] datetimeoffset NULL,
        [DeletedBy] int NULL,
        CONSTRAINT [PK_PayComponent] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260119174350_AddMoroccanPayrollCompliance'
)
BEGIN

                    IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_SalaryPackageItem_PayComponentId' AND object_id = OBJECT_ID('SalaryPackageItem'))
                    CREATE INDEX [IX_SalaryPackageItem_PayComponentId] ON [SalaryPackageItem] ([PayComponentId]);
                
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260119174350_AddMoroccanPayrollCompliance'
)
BEGIN

                    IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_SalaryPackage_SourceTemplateId' AND object_id = OBJECT_ID('SalaryPackage'))
                    CREATE INDEX [IX_SalaryPackage_SourceTemplateId] ON [SalaryPackage] ([SourceTemplateId]);
                
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260119174350_AddMoroccanPayrollCompliance'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [IX_PayComponent_Code_Version] ON [PayComponent] ([Code], [Version]) WHERE [DeletedAt] IS NULL');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260119174350_AddMoroccanPayrollCompliance'
)
BEGIN

                    IF NOT EXISTS (SELECT * FROM sys.foreign_keys WHERE name = 'FK_SalaryPackage_SalaryPackage_SourceTemplateId')
                    ALTER TABLE [SalaryPackage] ADD CONSTRAINT [FK_SalaryPackage_SalaryPackage_SourceTemplateId] 
                    FOREIGN KEY ([SourceTemplateId]) REFERENCES [SalaryPackage] ([Id]) ON DELETE NO ACTION;
                
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260119174350_AddMoroccanPayrollCompliance'
)
BEGIN

                    IF NOT EXISTS (SELECT * FROM sys.foreign_keys WHERE name = 'FK_SalaryPackageItem_PayComponent_PayComponentId')
                    ALTER TABLE [SalaryPackageItem] ADD CONSTRAINT [FK_SalaryPackageItem_PayComponent_PayComponentId] 
                    FOREIGN KEY ([PayComponentId]) REFERENCES [PayComponent] ([Id]) ON DELETE NO ACTION;
                
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260119174350_AddMoroccanPayrollCompliance'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260119174350_AddMoroccanPayrollCompliance', N'9.0.0');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260122124028_AddTemplateFieldsToSalaryPackage'
)
BEGIN
    ALTER TABLE [SalaryPackage] ADD [AutoRulesJson] nvarchar(max) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260122124028_AddTemplateFieldsToSalaryPackage'
)
BEGIN
    ALTER TABLE [SalaryPackage] ADD [CimrConfigJson] nvarchar(max) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260122124028_AddTemplateFieldsToSalaryPackage'
)
BEGIN
    ALTER TABLE [SalaryPackage] ADD [CopiedAt] datetime2 NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260122124028_AddTemplateFieldsToSalaryPackage'
)
BEGIN
    ALTER TABLE [SalaryPackage] ADD [OriginType] nvarchar(30) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260122124028_AddTemplateFieldsToSalaryPackage'
)
BEGIN
    ALTER TABLE [SalaryPackage] ADD [RegulationVersion] nvarchar(20) NOT NULL DEFAULT N'MA_2025';
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260122124028_AddTemplateFieldsToSalaryPackage'
)
BEGIN
    ALTER TABLE [SalaryPackage] ADD [SourceTemplateNameSnapshot] nvarchar(200) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260122124028_AddTemplateFieldsToSalaryPackage'
)
BEGIN
    ALTER TABLE [SalaryPackage] ADD [TemplateType] nvarchar(20) NOT NULL DEFAULT N'OFFICIAL';
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260122124028_AddTemplateFieldsToSalaryPackage'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260122124028_AddTemplateFieldsToSalaryPackage', N'9.0.0');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260129140456_AddReferentielSchema'
)
BEGIN
    CREATE TABLE [AncienneteRateSets] (
        [Id] int NOT NULL IDENTITY,
        [Code] nvarchar(50) NOT NULL,
        [Name] nvarchar(255) NOT NULL,
        [IsLegalDefault] bit NOT NULL DEFAULT CAST(0 AS bit),
        [Source] nvarchar(500) NULL,
        [EffectiveFrom] date NOT NULL,
        [EffectiveTo] date NULL,
        [CreatedAt] datetimeoffset NOT NULL DEFAULT (GETUTCDATE()),
        [CreatedBy] int NOT NULL,
        [ModifiedAt] datetimeoffset NULL,
        [ModifiedBy] int NULL,
        [DeletedAt] datetimeoffset NULL,
        [DeletedBy] int NULL,
        CONSTRAINT [PK_AncienneteRateSets] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260129140456_AddReferentielSchema'
)
BEGIN
    CREATE TABLE [Authorities] (
        [Id] int NOT NULL IDENTITY,
        [Code] nvarchar(50) NOT NULL,
        [Name] nvarchar(255) NOT NULL,
        [Description] nvarchar(max) NULL,
        [SortOrder] int NOT NULL DEFAULT 0,
        [CreatedAt] datetimeoffset NOT NULL DEFAULT (GETUTCDATE()),
        [CreatedBy] int NOT NULL,
        [ModifiedAt] datetimeoffset NULL,
        [ModifiedBy] int NULL,
        [DeletedAt] datetimeoffset NULL,
        [DeletedBy] int NULL,
        CONSTRAINT [PK_Authorities] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260129140456_AddReferentielSchema'
)
BEGIN
    CREATE TABLE [ElementCategories] (
        [Id] int NOT NULL IDENTITY,
        [Code] nvarchar(50) NOT NULL,
        [Name] nvarchar(255) NOT NULL,
        [Description] nvarchar(max) NULL,
        [SortOrder] int NOT NULL DEFAULT 0,
        [CreatedAt] datetimeoffset NOT NULL DEFAULT (GETUTCDATE()),
        [CreatedBy] int NOT NULL,
        [ModifiedAt] datetimeoffset NULL,
        [ModifiedBy] int NULL,
        [DeletedAt] datetimeoffset NULL,
        [DeletedBy] int NULL,
        CONSTRAINT [PK_ElementCategories] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260129140456_AddReferentielSchema'
)
BEGIN
    CREATE TABLE [EligibilityCriteria] (
        [Id] int NOT NULL IDENTITY,
        [Code] nvarchar(50) NOT NULL,
        [Name] nvarchar(255) NOT NULL,
        [Description] nvarchar(max) NULL,
        [CreatedAt] datetimeoffset NOT NULL DEFAULT (GETUTCDATE()),
        [CreatedBy] int NOT NULL,
        [ModifiedAt] datetimeoffset NULL,
        [ModifiedBy] int NULL,
        [DeletedAt] datetimeoffset NULL,
        [DeletedBy] int NULL,
        CONSTRAINT [PK_EligibilityCriteria] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260129140456_AddReferentielSchema'
)
BEGIN
    CREATE TABLE [LegalParameters] (
        [Id] int NOT NULL IDENTITY,
        [Code] nvarchar(50) NOT NULL,
        [Label] nvarchar(255) NOT NULL,
        [Value] decimal(18,4) NOT NULL,
        [Unit] nvarchar(50) NOT NULL,
        [Source] nvarchar(500) NULL,
        [EffectiveFrom] date NOT NULL,
        [EffectiveTo] date NULL,
        [CreatedAt] datetimeoffset NOT NULL DEFAULT (GETUTCDATE()),
        [CreatedBy] int NOT NULL,
        [ModifiedAt] datetimeoffset NULL,
        [ModifiedBy] int NULL,
        [DeletedAt] datetimeoffset NULL,
        [DeletedBy] int NULL,
        CONSTRAINT [PK_LegalParameters] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260129140456_AddReferentielSchema'
)
BEGIN
    CREATE TABLE [AncienneteRates] (
        [Id] int NOT NULL IDENTITY,
        [RateSetId] int NOT NULL,
        [MinYears] int NOT NULL,
        [MaxYears] int NULL,
        [Rate] decimal(5,4) NOT NULL,
        [SortOrder] int NOT NULL,
        CONSTRAINT [PK_AncienneteRates] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_AncienneteRates_AncienneteRateSets_RateSetId] FOREIGN KEY ([RateSetId]) REFERENCES [AncienneteRateSets] ([Id]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260129140456_AddReferentielSchema'
)
BEGIN
    CREATE TABLE [ReferentielElements] (
        [Id] int NOT NULL IDENTITY,
        [Code] nvarchar(50) NOT NULL,
        [Name] nvarchar(255) NOT NULL,
        [CategoryId] int NOT NULL,
        [Description] nvarchar(max) NULL,
        [DefaultFrequency] nvarchar(20) NOT NULL,
        [IsActive] bit NOT NULL DEFAULT CAST(1 AS bit),
        [CreatedAt] datetimeoffset NOT NULL DEFAULT (GETUTCDATE()),
        [CreatedBy] int NOT NULL,
        [ModifiedAt] datetimeoffset NULL,
        [ModifiedBy] int NULL,
        [DeletedAt] datetimeoffset NULL,
        [DeletedBy] int NULL,
        CONSTRAINT [PK_ReferentielElements] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_ReferentielElements_ElementCategories_CategoryId] FOREIGN KEY ([CategoryId]) REFERENCES [ElementCategories] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260129140456_AddReferentielSchema'
)
BEGIN
    CREATE TABLE [ElementRules] (
        [Id] int NOT NULL IDENTITY,
        [ElementId] int NOT NULL,
        [AuthorityId] int NOT NULL,
        [ExemptionType] nvarchar(30) NOT NULL,
        [SourceRef] nvarchar(500) NULL,
        [EffectiveFrom] date NOT NULL,
        [EffectiveTo] date NULL,
        [CreatedAt] datetimeoffset NOT NULL DEFAULT (GETUTCDATE()),
        [CreatedBy] int NOT NULL,
        [ModifiedAt] datetimeoffset NULL,
        [ModifiedBy] int NULL,
        [DeletedAt] datetimeoffset NULL,
        [DeletedBy] int NULL,
        CONSTRAINT [PK_ElementRules] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_ElementRules_Authorities_AuthorityId] FOREIGN KEY ([AuthorityId]) REFERENCES [Authorities] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_ElementRules_ReferentielElements_ElementId] FOREIGN KEY ([ElementId]) REFERENCES [ReferentielElements] ([Id]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260129140456_AddReferentielSchema'
)
BEGIN
    CREATE TABLE [RuleCaps] (
        [Id] int NOT NULL IDENTITY,
        [RuleId] int NOT NULL,
        [CapAmount] decimal(18,4) NOT NULL,
        [CapUnit] nvarchar(20) NOT NULL,
        [MinAmount] decimal(18,4) NULL,
        CONSTRAINT [PK_RuleCaps] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_RuleCaps_ElementRules_RuleId] FOREIGN KEY ([RuleId]) REFERENCES [ElementRules] ([Id]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260129140456_AddReferentielSchema'
)
BEGIN
    CREATE TABLE [RuleFormulas] (
        [Id] int NOT NULL IDENTITY,
        [RuleId] int NOT NULL,
        [Multiplier] decimal(10,4) NOT NULL,
        [ParameterId] int NOT NULL,
        [ResultUnit] nvarchar(20) NOT NULL,
        CONSTRAINT [PK_RuleFormulas] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_RuleFormulas_ElementRules_RuleId] FOREIGN KEY ([RuleId]) REFERENCES [ElementRules] ([Id]) ON DELETE CASCADE,
        CONSTRAINT [FK_RuleFormulas_LegalParameters_ParameterId] FOREIGN KEY ([ParameterId]) REFERENCES [LegalParameters] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260129140456_AddReferentielSchema'
)
BEGIN
    CREATE TABLE [RulePercentages] (
        [Id] int NOT NULL IDENTITY,
        [RuleId] int NOT NULL,
        [Percentage] decimal(5,4) NOT NULL,
        [BaseReference] nvarchar(30) NOT NULL,
        [EligibilityId] int NULL,
        CONSTRAINT [PK_RulePercentages] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_RulePercentages_ElementRules_RuleId] FOREIGN KEY ([RuleId]) REFERENCES [ElementRules] ([Id]) ON DELETE CASCADE,
        CONSTRAINT [FK_RulePercentages_EligibilityCriteria_EligibilityId] FOREIGN KEY ([EligibilityId]) REFERENCES [EligibilityCriteria] ([Id]) ON DELETE SET NULL
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260129140456_AddReferentielSchema'
)
BEGIN
    CREATE TABLE [RuleTiers] (
        [Id] int NOT NULL IDENTITY,
        [RuleId] int NOT NULL,
        [TierOrder] int NOT NULL,
        [FromAmount] decimal(18,4) NOT NULL,
        [ToAmount] decimal(18,4) NULL,
        [ExemptPercent] decimal(5,4) NOT NULL,
        CONSTRAINT [PK_RuleTiers] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_RuleTiers_ElementRules_RuleId] FOREIGN KEY ([RuleId]) REFERENCES [ElementRules] ([Id]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260129140456_AddReferentielSchema'
)
BEGIN
    CREATE TABLE [RuleVariants] (
        [Id] int NOT NULL IDENTITY,
        [RuleId] int NOT NULL,
        [VariantType] nvarchar(50) NOT NULL,
        [VariantKey] nvarchar(50) NOT NULL,
        [VariantLabel] nvarchar(255) NOT NULL,
        [OverrideCap] decimal(18,4) NULL,
        [OverridePercentage] decimal(5,4) NULL,
        [EligibilityId] int NULL,
        [SortOrder] int NOT NULL DEFAULT 0,
        CONSTRAINT [PK_RuleVariants] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_RuleVariants_ElementRules_RuleId] FOREIGN KEY ([RuleId]) REFERENCES [ElementRules] ([Id]) ON DELETE CASCADE,
        CONSTRAINT [FK_RuleVariants_EligibilityCriteria_EligibilityId] FOREIGN KEY ([EligibilityId]) REFERENCES [EligibilityCriteria] ([Id]) ON DELETE SET NULL
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260129140456_AddReferentielSchema'
)
BEGIN
    CREATE UNIQUE INDEX [IX_AncienneteRates_RateSetId_SortOrder] ON [AncienneteRates] ([RateSetId], [SortOrder]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260129140456_AddReferentielSchema'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [IX_AncienneteRateSets_Code] ON [AncienneteRateSets] ([Code]) WHERE [DeletedAt] IS NULL');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260129140456_AddReferentielSchema'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [IX_Authorities_Code] ON [Authorities] ([Code]) WHERE [DeletedAt] IS NULL');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260129140456_AddReferentielSchema'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [IX_ElementCategories_Code] ON [ElementCategories] ([Code]) WHERE [DeletedAt] IS NULL');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260129140456_AddReferentielSchema'
)
BEGIN
    CREATE INDEX [IX_ElementRules_AuthorityId] ON [ElementRules] ([AuthorityId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260129140456_AddReferentielSchema'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [IX_ElementRules_ElementId_AuthorityId_EffectiveFrom] ON [ElementRules] ([ElementId], [AuthorityId], [EffectiveFrom]) WHERE [DeletedAt] IS NULL');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260129140456_AddReferentielSchema'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [IX_EligibilityCriteria_Code] ON [EligibilityCriteria] ([Code]) WHERE [DeletedAt] IS NULL');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260129140456_AddReferentielSchema'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [IX_LegalParameters_Code_EffectiveFrom] ON [LegalParameters] ([Code], [EffectiveFrom]) WHERE [DeletedAt] IS NULL');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260129140456_AddReferentielSchema'
)
BEGIN
    CREATE INDEX [IX_ReferentielElements_CategoryId] ON [ReferentielElements] ([CategoryId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260129140456_AddReferentielSchema'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [IX_ReferentielElements_Code] ON [ReferentielElements] ([Code]) WHERE [DeletedAt] IS NULL');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260129140456_AddReferentielSchema'
)
BEGIN
    CREATE UNIQUE INDEX [IX_RuleCaps_RuleId] ON [RuleCaps] ([RuleId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260129140456_AddReferentielSchema'
)
BEGIN
    CREATE INDEX [IX_RuleFormulas_ParameterId] ON [RuleFormulas] ([ParameterId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260129140456_AddReferentielSchema'
)
BEGIN
    CREATE UNIQUE INDEX [IX_RuleFormulas_RuleId] ON [RuleFormulas] ([RuleId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260129140456_AddReferentielSchema'
)
BEGIN
    CREATE INDEX [IX_RulePercentages_EligibilityId] ON [RulePercentages] ([EligibilityId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260129140456_AddReferentielSchema'
)
BEGIN
    CREATE UNIQUE INDEX [IX_RulePercentages_RuleId] ON [RulePercentages] ([RuleId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260129140456_AddReferentielSchema'
)
BEGIN
    CREATE UNIQUE INDEX [IX_RuleTiers_RuleId_TierOrder] ON [RuleTiers] ([RuleId], [TierOrder]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260129140456_AddReferentielSchema'
)
BEGIN
    CREATE INDEX [IX_RuleVariants_EligibilityId] ON [RuleVariants] ([EligibilityId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260129140456_AddReferentielSchema'
)
BEGIN
    CREATE UNIQUE INDEX [IX_RuleVariants_RuleId_VariantType_VariantKey] ON [RuleVariants] ([RuleId], [VariantType], [VariantKey]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260129140456_AddReferentielSchema'
)
BEGIN
    IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'Code', N'Name', N'Description', N'SortOrder', N'CreatedBy') AND [object_id] = OBJECT_ID(N'[Authorities]'))
        SET IDENTITY_INSERT [Authorities] ON;
    EXEC(N'INSERT INTO [Authorities] ([Code], [Name], [Description], [SortOrder], [CreatedBy])
    VALUES (N''CNSS'', N''Caisse Nationale de Sécurité Sociale'', N''Cotisations sociales obligatoires'', 1, 1),
    (N''IR'', N''Impôt sur le Revenu'', N''Direction Générale des Impôts - Retenue à la source'', 2, 1),
    (N''AMO'', N''Assurance Maladie Obligatoire'', N''Couverture médicale obligatoire'', 3, 1),
    (N''CIMR'', N''Caisse Interprofessionnelle Marocaine de Retraites'', N''Retraite complémentaire'', 4, 1)');
    IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'Code', N'Name', N'Description', N'SortOrder', N'CreatedBy') AND [object_id] = OBJECT_ID(N'[Authorities]'))
        SET IDENTITY_INSERT [Authorities] OFF;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260129140456_AddReferentielSchema'
)
BEGIN
    IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'Code', N'Name', N'Description', N'SortOrder', N'CreatedBy') AND [object_id] = OBJECT_ID(N'[ElementCategories]'))
        SET IDENTITY_INSERT [ElementCategories] ON;
    EXEC(N'INSERT INTO [ElementCategories] ([Code], [Name], [Description], [SortOrder], [CreatedBy])
    VALUES (N''IND_PRO'', N''Indemnités Professionnelles'', N''Indemnités liées à l''''exercice de la fonction'', 1, 1),
    (N''IND_SOCIAL'', N''Indemnités Sociales'', N''Indemnités à caractère social'', 2, 1),
    (N''PRIME_SPEC'', N''Primes Spécifiques'', N''Primes liées à des conditions particulières'', 3, 1),
    (N''AVANTAGE'', N''Avantages en Nature'', N''Avantages non monétaires'', 4, 1)');
    IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'Code', N'Name', N'Description', N'SortOrder', N'CreatedBy') AND [object_id] = OBJECT_ID(N'[ElementCategories]'))
        SET IDENTITY_INSERT [ElementCategories] OFF;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260129140456_AddReferentielSchema'
)
BEGIN
    IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'Code', N'Name', N'Description', N'CreatedBy') AND [object_id] = OBJECT_ID(N'[EligibilityCriteria]'))
        SET IDENTITY_INSERT [EligibilityCriteria] ON;
    EXEC(N'INSERT INTO [EligibilityCriteria] ([Code], [Name], [Description], [CreatedBy])
    VALUES (N''ALL'', N''Tous les salariés'', N''Applicable à l''''ensemble des salariés'', 1),
    (N''CADRES_SUP'', N''Cadres Supérieurs'', N''Cadres de direction et assimilés'', 1),
    (N''PDG_DG'', N''PDG et Directeurs Généraux'', N''Dirigeants de l''''entreprise uniquement'', 1),
    (N''NON_CADRES'', N''Non Cadres'', N''Personnel non cadre'', 1),
    (N''COMMERCIAUX'', N''Commerciaux'', N''Personnel commercial itinérant'', 1)');
    IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'Code', N'Name', N'Description', N'CreatedBy') AND [object_id] = OBJECT_ID(N'[EligibilityCriteria]'))
        SET IDENTITY_INSERT [EligibilityCriteria] OFF;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260129140456_AddReferentielSchema'
)
BEGIN
    IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'Code', N'Label', N'Value', N'Unit', N'Source', N'EffectiveFrom', N'CreatedBy') AND [object_id] = OBJECT_ID(N'[LegalParameters]'))
        SET IDENTITY_INSERT [LegalParameters] ON;
    EXEC(N'INSERT INTO [LegalParameters] ([Code], [Label], [Value], [Unit], [Source], [EffectiveFrom], [CreatedBy])
    VALUES (N''SMIG_HORAIRE'', N''SMIG Horaire'', 17.1, N''MAD/heure'', N''Décret n° 2-24-145 du 1er janvier 2025'', ''2025-01-01'', 1),
    (N''SMIG_MENSUEL'', N''SMIG Mensuel (191h)'', 3258.9, N''MAD/mois'', N''Calculé: 17.10 × 191 heures'', ''2025-01-01'', 1),
    (N''SMAG_JOURNALIER'', N''SMAG Journalier'', 93.0, N''MAD/jour'', N''Décret n° 2-24-145 du 1er janvier 2025'', ''2025-01-01'', 1)');
    IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'Code', N'Label', N'Value', N'Unit', N'Source', N'EffectiveFrom', N'CreatedBy') AND [object_id] = OBJECT_ID(N'[LegalParameters]'))
        SET IDENTITY_INSERT [LegalParameters] OFF;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260129140456_AddReferentielSchema'
)
BEGIN
    IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'Code', N'Name', N'IsLegalDefault', N'Source', N'EffectiveFrom', N'CreatedBy') AND [object_id] = OBJECT_ID(N'[AncienneteRateSets]'))
        SET IDENTITY_INSERT [AncienneteRateSets] ON;
    EXEC(N'INSERT INTO [AncienneteRateSets] ([Code], [Name], [IsLegalDefault], [Source], [EffectiveFrom], [CreatedBy])
    VALUES (N''LEGAL_4TIER'', N''Barème légal 4 paliers'', CAST(1 AS bit), N''Code du Travail - Article 350'', ''2004-06-08'', 1)');
    IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'Code', N'Name', N'IsLegalDefault', N'Source', N'EffectiveFrom', N'CreatedBy') AND [object_id] = OBJECT_ID(N'[AncienneteRateSets]'))
        SET IDENTITY_INSERT [AncienneteRateSets] OFF;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260129140456_AddReferentielSchema'
)
BEGIN

                    DECLARE @RateSetId INT = (SELECT Id FROM AncienneteRateSets WHERE Code = 'LEGAL_4TIER');
                    INSERT INTO AncienneteRates (RateSetId, MinYears, MaxYears, Rate, SortOrder)
                    VALUES 
                        (@RateSetId, 2, 4, 0.05, 1),
                        (@RateSetId, 5, 11, 0.10, 2),
                        (@RateSetId, 12, 19, 0.15, 3),
                        (@RateSetId, 20, NULL, 0.20, 4);
                
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260129140456_AddReferentielSchema'
)
BEGIN

                    DECLARE @CatIndPro INT = (SELECT Id FROM ElementCategories WHERE Code = 'IND_PRO');
                    INSERT INTO ReferentielElements (Code, Name, CategoryId, Description, DefaultFrequency, IsActive, CreatedBy)
                    VALUES 
                        ('IND_TRANSPORT', 'Indemnité de Transport', @CatIndPro, 'Indemnité forfaitaire de déplacement domicile-travail', 'MONTHLY', 1, 1),
                        ('PRIME_PANIER', 'Prime de Panier', @CatIndPro, 'Indemnité de repas pour travail posté ou éloigné', 'DAILY', 1, 1),
                        ('IND_REPRESENT', 'Indemnité de Représentation', @CatIndPro, 'Frais de représentation pour cadres dirigeants', 'MONTHLY', 1, 1);
                
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260129140456_AddReferentielSchema'
)
BEGIN

                    DECLARE @ElemTransport INT = (SELECT Id FROM ReferentielElements WHERE Code = 'IND_TRANSPORT');
                    DECLARE @AuthCNSS INT = (SELECT Id FROM Authorities WHERE Code = 'CNSS');
                    DECLARE @AuthIR INT = (SELECT Id FROM Authorities WHERE Code = 'IR');
                    
                    INSERT INTO ElementRules (ElementId, AuthorityId, ExemptionType, SourceRef, EffectiveFrom, CreatedBy)
                    VALUES 
                        (@ElemTransport, @AuthCNSS, 'CAPPED', 'Arrêté du Ministre des Finances', '2020-01-01', 1),
                        (@ElemTransport, @AuthIR, 'CAPPED', 'CGI Article 57', '2020-01-01', 1);
                
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260129140456_AddReferentielSchema'
)
BEGIN

                    DECLARE @RuleCNSSTransport INT = (SELECT er.Id FROM ElementRules er 
                        INNER JOIN ReferentielElements re ON er.ElementId = re.Id 
                        INNER JOIN Authorities a ON er.AuthorityId = a.Id 
                        WHERE re.Code = 'IND_TRANSPORT' AND a.Code = 'CNSS');
                    DECLARE @RuleIRTransport INT = (SELECT er.Id FROM ElementRules er 
                        INNER JOIN ReferentielElements re ON er.ElementId = re.Id 
                        INNER JOIN Authorities a ON er.AuthorityId = a.Id 
                        WHERE re.Code = 'IND_TRANSPORT' AND a.Code = 'IR');
                    
                    INSERT INTO RuleCaps (RuleId, CapAmount, CapUnit) VALUES (@RuleCNSSTransport, 500.0000, 'PER_MONTH');
                    INSERT INTO RuleCaps (RuleId, CapAmount, CapUnit) VALUES (@RuleIRTransport, 500.0000, 'PER_MONTH');
                    
                    INSERT INTO RuleVariants (RuleId, VariantType, VariantKey, VariantLabel, OverrideCap, SortOrder)
                    VALUES 
                        (@RuleCNSSTransport, 'ZONE', 'URBAN', 'Zone Urbaine', 500.0000, 1),
                        (@RuleCNSSTransport, 'ZONE', 'HORS_URBAN', 'Hors Zone Urbaine', 750.0000, 2),
                        (@RuleIRTransport, 'ZONE', 'URBAN', 'Zone Urbaine', 500.0000, 1),
                        (@RuleIRTransport, 'ZONE', 'HORS_URBAN', 'Hors Zone Urbaine', 750.0000, 2);
                
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260129140456_AddReferentielSchema'
)
BEGIN

                    DECLARE @ElemPanier INT = (SELECT Id FROM ReferentielElements WHERE Code = 'PRIME_PANIER');
                    DECLARE @AuthCNSS INT = (SELECT Id FROM Authorities WHERE Code = 'CNSS');
                    DECLARE @AuthIR INT = (SELECT Id FROM Authorities WHERE Code = 'IR');
                    
                    INSERT INTO ElementRules (ElementId, AuthorityId, ExemptionType, SourceRef, EffectiveFrom, CreatedBy)
                    VALUES 
                        (@ElemPanier, @AuthCNSS, 'FORMULA', 'Circulaire CNSS', '2020-01-01', 1),
                        (@ElemPanier, @AuthIR, 'FORMULA', 'CGI Article 57', '2020-01-01', 1);
                
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260129140456_AddReferentielSchema'
)
BEGIN

                    DECLARE @RuleCNSSPanier INT = (SELECT er.Id FROM ElementRules er 
                        INNER JOIN ReferentielElements re ON er.ElementId = re.Id 
                        INNER JOIN Authorities a ON er.AuthorityId = a.Id 
                        WHERE re.Code = 'PRIME_PANIER' AND a.Code = 'CNSS');
                    DECLARE @RuleIRPanier INT = (SELECT er.Id FROM ElementRules er 
                        INNER JOIN ReferentielElements re ON er.ElementId = re.Id 
                        INNER JOIN Authorities a ON er.AuthorityId = a.Id 
                        WHERE re.Code = 'PRIME_PANIER' AND a.Code = 'IR');
                    DECLARE @ParamSMIG INT = (SELECT Id FROM LegalParameters WHERE Code = 'SMIG_HORAIRE');
                    
                    INSERT INTO RuleFormulas (RuleId, Multiplier, ParameterId, ResultUnit) VALUES (@RuleCNSSPanier, 2.0000, @ParamSMIG, 'PER_DAY');
                    INSERT INTO RuleFormulas (RuleId, Multiplier, ParameterId, ResultUnit) VALUES (@RuleIRPanier, 2.0000, @ParamSMIG, 'PER_DAY');
                
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260129140456_AddReferentielSchema'
)
BEGIN

                    DECLARE @ElemRepresent INT = (SELECT Id FROM ReferentielElements WHERE Code = 'IND_REPRESENT');
                    DECLARE @AuthCNSS INT = (SELECT Id FROM Authorities WHERE Code = 'CNSS');
                    DECLARE @AuthIR INT = (SELECT Id FROM Authorities WHERE Code = 'IR');
                    
                    INSERT INTO ElementRules (ElementId, AuthorityId, ExemptionType, SourceRef, EffectiveFrom, CreatedBy)
                    VALUES 
                        (@ElemRepresent, @AuthCNSS, 'PERCENTAGE', 'Circulaire CNSS', '2020-01-01', 1),
                        (@ElemRepresent, @AuthIR, 'PERCENTAGE', 'CGI Article 57-7', '2020-01-01', 1);
                
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260129140456_AddReferentielSchema'
)
BEGIN

                    DECLARE @RuleCNSSRepresent INT = (SELECT er.Id FROM ElementRules er 
                        INNER JOIN ReferentielElements re ON er.ElementId = re.Id 
                        INNER JOIN Authorities a ON er.AuthorityId = a.Id 
                        WHERE re.Code = 'IND_REPRESENT' AND a.Code = 'CNSS');
                    DECLARE @RuleIRRepresent INT = (SELECT er.Id FROM ElementRules er 
                        INNER JOIN ReferentielElements re ON er.ElementId = re.Id 
                        INNER JOIN Authorities a ON er.AuthorityId = a.Id 
                        WHERE re.Code = 'IND_REPRESENT' AND a.Code = 'IR');
                    DECLARE @EligCadresSup INT = (SELECT Id FROM EligibilityCriteria WHERE Code = 'CADRES_SUP');
                    DECLARE @EligPDG INT = (SELECT Id FROM EligibilityCriteria WHERE Code = 'PDG_DG');
                    
                    INSERT INTO RulePercentages (RuleId, Percentage, BaseReference, EligibilityId) 
                    VALUES 
                        (@RuleCNSSRepresent, 0.10, 'BASE_SALARY', @EligCadresSup),
                        (@RuleIRRepresent, 0.10, 'BASE_SALARY', @EligPDG);
                
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260129140456_AddReferentielSchema'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260129140456_AddReferentielSchema', N'9.0.0');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260131151141_SyncModelWithSnapshot'
)
BEGIN
    DROP INDEX [IX_ElementCategories_Code] ON [ElementCategories];
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260131151141_SyncModelWithSnapshot'
)
BEGIN
    DECLARE @var24 sysname;
    SELECT @var24 = [d].[name]
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[ElementCategories]') AND [c].[name] = N'Code');
    IF @var24 IS NOT NULL EXEC(N'ALTER TABLE [ElementCategories] DROP CONSTRAINT [' + @var24 + '];');
    ALTER TABLE [ElementCategories] DROP COLUMN [Code];
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260131151141_SyncModelWithSnapshot'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260131151141_SyncModelWithSnapshot', N'9.0.0');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260131175222_RemoveCodeFromLegalParameters'
)
BEGIN
    DROP INDEX [IX_LegalParameters_Code_EffectiveFrom] ON [LegalParameters];
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260131175222_RemoveCodeFromLegalParameters'
)
BEGIN
    DECLARE @var25 sysname;
    SELECT @var25 = [d].[name]
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[LegalParameters]') AND [c].[name] = N'Code');
    IF @var25 IS NOT NULL EXEC(N'ALTER TABLE [LegalParameters] DROP CONSTRAINT [' + @var25 + '];');
    ALTER TABLE [LegalParameters] DROP COLUMN [Code];
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260131175222_RemoveCodeFromLegalParameters'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [IX_LegalParameters_Label_EffectiveFrom] ON [LegalParameters] ([Label], [EffectiveFrom]) WHERE [DeletedAt] IS NULL');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260131175222_RemoveCodeFromLegalParameters'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260131175222_RemoveCodeFromLegalParameters', N'9.0.0');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260201175951_ApplyPendingModelChanges'
)
BEGIN
    ALTER TABLE [AncienneteRateSets] ADD [Code] nvarchar(max) NOT NULL DEFAULT N'';
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260201175951_ApplyPendingModelChanges'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260201175951_ApplyPendingModelChanges', N'9.0.0');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260203105323_SyncModelChanges'
)
BEGIN
    CREATE TABLE [RuleDualCaps] (
        [Id] int NOT NULL IDENTITY,
        [RuleId] int NOT NULL,
        [FixedCapAmount] decimal(18,4) NOT NULL,
        [FixedCapUnit] nvarchar(20) NOT NULL,
        [PercentageCap] decimal(5,4) NOT NULL,
        [BaseReference] nvarchar(30) NOT NULL,
        [Logic] nvarchar(10) NOT NULL,
        CONSTRAINT [PK_RuleDualCaps] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_RuleDualCaps_ElementRules_RuleId] FOREIGN KEY ([RuleId]) REFERENCES [ElementRules] ([Id]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260203105323_SyncModelChanges'
)
BEGIN
    CREATE UNIQUE INDEX [IX_RuleDualCaps_RuleId] ON [RuleDualCaps] ([RuleId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260203105323_SyncModelChanges'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260203105323_SyncModelChanges', N'9.0.0');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260203113158_AddReferencesRuleId'
)
BEGIN
    ALTER TABLE [ElementRules] ADD [ReferencesRuleId] int NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260203113158_AddReferencesRuleId'
)
BEGIN
    CREATE INDEX [IX_ElementRules_ReferencesRuleId] ON [ElementRules] ([ReferencesRuleId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260203113158_AddReferencesRuleId'
)
BEGIN
    ALTER TABLE [ElementRules] ADD CONSTRAINT [FK_ElementRules_ElementRules_ReferencesRuleId] FOREIGN KEY ([ReferencesRuleId]) REFERENCES [ElementRules] ([Id]) ON DELETE NO ACTION;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260203113158_AddReferencesRuleId'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260203113158_AddReferencesRuleId', N'9.0.0');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260203121247_CaptureLatestModelChanges'
)
BEGIN
    ALTER TABLE [ElementRules] DROP CONSTRAINT [FK_ElementRules_ElementRules_ReferencesRuleId];
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260203121247_CaptureLatestModelChanges'
)
BEGIN
    DROP INDEX [IX_ElementRules_ReferencesRuleId] ON [ElementRules];
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260203121247_CaptureLatestModelChanges'
)
BEGIN
    DECLARE @var26 sysname;
    SELECT @var26 = [d].[name]
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[ElementRules]') AND [c].[name] = N'ReferencesRuleId');
    IF @var26 IS NOT NULL EXEC(N'ALTER TABLE [ElementRules] DROP CONSTRAINT [' + @var26 + '];');
    ALTER TABLE [ElementRules] DROP COLUMN [ReferencesRuleId];
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260203121247_CaptureLatestModelChanges'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260203121247_CaptureLatestModelChanges', N'9.0.0');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260205181817_AddReferentielElementIdToSalaryPackageItem'
)
BEGIN
    ALTER TABLE [SalaryPackageItem] ADD [ReferentielElementId] int NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260205181817_AddReferentielElementIdToSalaryPackageItem'
)
BEGIN
    CREATE INDEX [IX_SalaryPackageItem_ReferentielElementId] ON [SalaryPackageItem] ([ReferentielElementId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260205181817_AddReferentielElementIdToSalaryPackageItem'
)
BEGIN
    ALTER TABLE [SalaryPackageItem] ADD CONSTRAINT [FK_SalaryPackageItem_ReferentielElements_ReferentielElementId] FOREIGN KEY ([ReferentielElementId]) REFERENCES [ReferentielElements] ([Id]) ON DELETE NO ACTION;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260205181817_AddReferentielElementIdToSalaryPackageItem'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260205181817_AddReferentielElementIdToSalaryPackageItem', N'9.0.0');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260205182624_SeedPhase2LegalParameters'
)
BEGIN

                    INSERT INTO LegalParameters (Label, Value, Unit, Source, EffectiveFrom, CreatedBy)
                    VALUES
                        ('SMIG_HORAIRE', 17.10, 'MAD/heure', 'Décret 2025', '2025-01-01', 0),
                        ('CNSS_PLAFOND', 6000, 'MAD/mois', 'Plafond CNSS 2025', '2025-01-01', 0),
                        ('CNSS_PS_EMPLOYEE_RATE', 0.0448, 'ratio', 'Prestations sociales', '2025-01-01', 0),
                        ('CNSS_PS_EMPLOYER_RATE', 0.0898, 'ratio', 'Prestations sociales', '2025-01-01', 0),
                        ('CNSS_AMO_EMPLOYEE_RATE', 0.0226, 'ratio', 'AMO', '2025-01-01', 0),
                        ('CNSS_AMO_EMPLOYER_RATE', 0.0411, 'ratio', 'AMO', '2025-01-01', 0),
                        ('CNSS_AF_EMPLOYER_RATE', 0.0640, 'ratio', 'Allocations familiales', '2025-01-01', 0),
                        ('CNSS_FP_EMPLOYER_RATE', 0.0160, 'ratio', 'Formation professionnelle', '2025-01-01', 0),
                        ('PROF_EXP_THRESHOLD_MONTHLY', 6500, 'MAD/mois', 'LF 2023 Art.59', '2023-01-01', 0),
                        ('PROF_EXP_RATE_LOW', 0.35, 'ratio', 'Revenu ≤ 78k/an', '2023-01-01', 0),
                        ('PROF_EXP_RATE_HIGH', 0.25, 'ratio', 'Revenu ≥ 78k/an', '2023-01-01', 0),
                        ('PROF_EXP_CAP_LOW_MONTHLY', 2916.67, 'MAD/mois', '35 000/12', '2023-01-01', 0),
                        ('PROF_EXP_CAP_HIGH_MONTHLY', 2500, 'MAD/mois', '30 000/12', '2023-01-01', 0);
                
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260205182624_SeedPhase2LegalParameters'
)
BEGIN

                    INSERT INTO LegalParameters (Label, Value, Unit, Source, EffectiveFrom, CreatedBy)
                    VALUES
                        ('IR_2025_B0_MAX', 3333.33, 'MAD', 'LF 2025', '2025-01-01', 0),
                        ('IR_2025_B0_RATE', 0, 'ratio', 'LF 2025', '2025-01-01', 0),
                        ('IR_2025_B0_DEDUCTION', 0, 'MAD', 'LF 2025', '2025-01-01', 0),
                        ('IR_2025_B1_MAX', 5000, 'MAD', 'LF 2025', '2025-01-01', 0),
                        ('IR_2025_B1_RATE', 0.10, 'ratio', 'LF 2025', '2025-01-01', 0),
                        ('IR_2025_B1_DEDUCTION', 333.33, 'MAD', 'LF 2025', '2025-01-01', 0),
                        ('IR_2025_B2_MAX', 6666.67, 'MAD', 'LF 2025', '2025-01-01', 0),
                        ('IR_2025_B2_RATE', 0.20, 'ratio', 'LF 2025', '2025-01-01', 0),
                        ('IR_2025_B2_DEDUCTION', 833.33, 'MAD', 'LF 2025', '2025-01-01', 0),
                        ('IR_2025_B3_MAX', 8333.33, 'MAD', 'LF 2025', '2025-01-01', 0),
                        ('IR_2025_B3_RATE', 0.30, 'ratio', 'LF 2025', '2025-01-01', 0),
                        ('IR_2025_B3_DEDUCTION', 1500, 'MAD', 'LF 2025', '2025-01-01', 0),
                        ('IR_2025_B4_MAX', 15000, 'MAD', 'LF 2025', '2025-01-01', 0),
                        ('IR_2025_B4_RATE', 0.34, 'ratio', 'LF 2025', '2025-01-01', 0),
                        ('IR_2025_B4_DEDUCTION', 1833.33, 'MAD', 'LF 2025', '2025-01-01', 0),
                        ('IR_2025_B5_MAX', 999999999, 'MAD', 'LF 2025', '2025-01-01', 0),
                        ('IR_2025_B5_RATE', 0.37, 'ratio', 'LF 2025', '2025-01-01', 0),
                        ('IR_2025_B5_DEDUCTION', 2283.33, 'MAD', 'LF 2025', '2025-01-01', 0);
                
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260205182624_SeedPhase2LegalParameters'
)
BEGIN

                    INSERT INTO LegalParameters (Label, Value, Unit, Source, EffectiveFrom, EffectiveTo, CreatedBy)
                    VALUES
                        ('IR_PRE2025_B0_MAX', 2500, 'MAD', 'Pre-LF 2025', '2020-01-01', '2024-12-31', 0),
                        ('IR_PRE2025_B0_RATE', 0, 'ratio', 'Pre-LF 2025', '2020-01-01', '2024-12-31', 0),
                        ('IR_PRE2025_B0_DEDUCTION', 0, 'MAD', 'Pre-LF 2025', '2020-01-01', '2024-12-31', 0),
                        ('IR_PRE2025_B1_MAX', 4166.67, 'MAD', 'Pre-LF 2025', '2020-01-01', '2024-12-31', 0),
                        ('IR_PRE2025_B1_RATE', 0.10, 'ratio', 'Pre-LF 2025', '2020-01-01', '2024-12-31', 0),
                        ('IR_PRE2025_B1_DEDUCTION', 250, 'MAD', 'Pre-LF 2025', '2020-01-01', '2024-12-31', 0),
                        ('IR_PRE2025_B2_MAX', 5000, 'MAD', 'Pre-LF 2025', '2020-01-01', '2024-12-31', 0),
                        ('IR_PRE2025_B2_RATE', 0.20, 'ratio', 'Pre-LF 2025', '2020-01-01', '2024-12-31', 0),
                        ('IR_PRE2025_B2_DEDUCTION', 666.67, 'MAD', 'Pre-LF 2025', '2020-01-01', '2024-12-31', 0),
                        ('IR_PRE2025_B3_MAX', 6666.67, 'MAD', 'Pre-LF 2025', '2020-01-01', '2024-12-31', 0),
                        ('IR_PRE2025_B3_RATE', 0.30, 'ratio', 'Pre-LF 2025', '2020-01-01', '2024-12-31', 0),
                        ('IR_PRE2025_B3_DEDUCTION', 1166.67, 'MAD', 'Pre-LF 2025', '2020-01-01', '2024-12-31', 0),
                        ('IR_PRE2025_B4_MAX', 15000, 'MAD', 'Pre-LF 2025', '2020-01-01', '2024-12-31', 0),
                        ('IR_PRE2025_B4_RATE', 0.34, 'ratio', 'Pre-LF 2025', '2020-01-01', '2024-12-31', 0),
                        ('IR_PRE2025_B4_DEDUCTION', 1433.33, 'MAD', 'Pre-LF 2025', '2020-01-01', '2024-12-31', 0),
                        ('IR_PRE2025_B5_MAX', 999999999, 'MAD', 'Pre-LF 2025', '2020-01-01', '2024-12-31', 0),
                        ('IR_PRE2025_B5_RATE', 0.38, 'ratio', 'Pre-LF 2025', '2020-01-01', '2024-12-31', 0),
                        ('IR_PRE2025_B5_DEDUCTION', 2033.33, 'MAD', 'Pre-LF 2025', '2020-01-01', '2024-12-31', 0);
                
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260205182624_SeedPhase2LegalParameters'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260205182624_SeedPhase2LegalParameters', N'9.0.0');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260205194824_SeedElementCategories'
)
BEGIN

                    IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('ElementCategories') AND name = 'IsActive')
                    BEGIN
                        ALTER TABLE ElementCategories ADD IsActive bit NOT NULL DEFAULT 1;
                    END
                    
                    IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('Authorities') AND name = 'IsActive')
                    BEGIN
                        ALTER TABLE Authorities ADD IsActive bit NOT NULL DEFAULT 1;
                    END
                
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260205194824_SeedElementCategories'
)
BEGIN

                    SET IDENTITY_INSERT ElementCategories ON;
                    
                    MERGE INTO ElementCategories AS Target
                    USING (VALUES
                        (1, 'Indemnités à caractère professionnel', 'Indemnités liées à l''exercice de la profession (transport, représentation, etc.)', 1, 1, GETUTCDATE(), 0),
                        (2, 'Indemnités sociales', 'Indemnités de nature sociale (panier, téléphone, etc.)', 2, 1, GETUTCDATE(), 0),
                        (3, 'Primes spéciales', 'Primes exceptionnelles ou périodiques', 3, 1, GETUTCDATE(), 0),
                        (4, 'Avantages en nature', 'Avantages fournis en nature (logement, voiture, etc.)', 4, 1, GETUTCDATE(), 0)
                    ) AS Source (Id, Name, Description, SortOrder, IsActive, CreatedAt, CreatedBy)
                    ON Target.Id = Source.Id
                    WHEN NOT MATCHED THEN
                        INSERT (Id, Name, Description, SortOrder, IsActive, CreatedAt, CreatedBy)
                        VALUES (Source.Id, Source.Name, Source.Description, Source.SortOrder, Source.IsActive, Source.CreatedAt, Source.CreatedBy);
                    
                    SET IDENTITY_INSERT ElementCategories OFF;
                
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260205194824_SeedElementCategories'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260205194824_SeedElementCategories', N'9.0.0');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260205230150_AddStatusAndConvergenceFieldsFixed'
)
BEGIN
    DECLARE @var27 sysname;
    SELECT @var27 = [d].[name]
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[ReferentielElements]') AND [c].[name] = N'Code');
    IF @var27 IS NOT NULL EXEC(N'ALTER TABLE [ReferentielElements] DROP CONSTRAINT [' + @var27 + '];');
    ALTER TABLE [ReferentielElements] ALTER COLUMN [Code] nvarchar(100) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260205230150_AddStatusAndConvergenceFieldsFixed'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260205230150_AddStatusAndConvergenceFieldsFixed', N'9.0.0');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260206014422_FinalPayrollReferentielSchema'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260206014422_FinalPayrollReferentielSchema', N'9.0.0');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260206020704_AddMissingColumns'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260206020704_AddMissingColumns', N'9.0.0');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260206064028_AddMissingNationalityIdColumn'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260206064028_AddMissingNationalityIdColumn', N'9.0.0');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260206123000_FixPayrollMissingColumns'
)
BEGIN

    IF OBJECT_ID(N'[SalaryPackage]', N'U') IS NOT NULL AND COL_LENGTH('SalaryPackage', 'Status') IS NULL
    BEGIN
        ALTER TABLE [SalaryPackage]
            ADD [Status] nvarchar(20) NOT NULL
            CONSTRAINT [DF_SalaryPackage_Status] DEFAULT ('draft');
    END

END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260206123000_FixPayrollMissingColumns'
)
BEGIN

    IF OBJECT_ID(N'[SalaryPackageItem]', N'U') IS NOT NULL AND COL_LENGTH('SalaryPackageItem', 'ReferentielElementId') IS NULL
    BEGIN
        ALTER TABLE [SalaryPackageItem]
            ADD [ReferentielElementId] int NULL;
    END

END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260206123000_FixPayrollMissingColumns'
)
BEGIN

    IF OBJECT_ID(N'[ReferentielElements]', N'U') IS NOT NULL AND COL_LENGTH('ReferentielElements', 'Status') IS NULL
    BEGIN
        ALTER TABLE [ReferentielElements]
            ADD [Status] int NOT NULL
            CONSTRAINT [DF_ReferentielElements_Status] DEFAULT (0);
    END

END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260206123000_FixPayrollMissingColumns'
)
BEGIN

    IF OBJECT_ID(N'[ReferentielElements]', N'U') IS NOT NULL AND COL_LENGTH('ReferentielElements', 'HasConvergence') IS NULL
    BEGIN
        ALTER TABLE [ReferentielElements]
            ADD [HasConvergence] bit NOT NULL
            CONSTRAINT [DF_ReferentielElements_HasConvergence] DEFAULT (0);
    END

END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260206123000_FixPayrollMissingColumns'
)
BEGIN

    IF OBJECT_ID(N'[ReferentielElements]', N'U') IS NOT NULL AND COL_LENGTH('ReferentielElements', 'IsActive') IS NULL
    BEGIN
        ALTER TABLE [ReferentielElements]
            ADD [IsActive] bit NOT NULL
            CONSTRAINT [DF_ReferentielElements_IsActive] DEFAULT (1);
    END

END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260206123000_FixPayrollMissingColumns'
)
BEGIN

    IF OBJECT_ID(N'[ElementRules]', N'U') IS NOT NULL AND COL_LENGTH('ElementRules', 'Status') IS NULL
    BEGIN
        ALTER TABLE [ElementRules]
            ADD [Status] int NOT NULL
            CONSTRAINT [DF_ElementRules_Status] DEFAULT (0);
    END

END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260206123000_FixPayrollMissingColumns'
)
BEGIN

    IF OBJECT_ID(N'[ElementRules]', N'U') IS NOT NULL AND COL_LENGTH('ElementRules', 'RuleDetails') IS NULL
    BEGIN
        ALTER TABLE [ElementRules]
            ADD [RuleDetails] nvarchar(max) NOT NULL
            CONSTRAINT [DF_ElementRules_RuleDetails] DEFAULT ('{}');
    END

END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260206123000_FixPayrollMissingColumns'
)
BEGIN

    IF OBJECT_ID(N'[ElementCategories]', N'U') IS NOT NULL AND COL_LENGTH('ElementCategories', 'IsActive') IS NULL
    BEGIN
        ALTER TABLE [ElementCategories]
            ADD [IsActive] bit NOT NULL
            CONSTRAINT [DF_ElementCategories_IsActive] DEFAULT (1);
    END

END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260206123000_FixPayrollMissingColumns'
)
BEGIN

    IF OBJECT_ID(N'[Authorities]', N'U') IS NOT NULL AND COL_LENGTH('Authorities', 'IsActive') IS NULL
    BEGIN
        ALTER TABLE [Authorities]
            ADD [IsActive] bit NOT NULL
            CONSTRAINT [DF_Authorities_IsActive] DEFAULT (1);
    END

END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260206123000_FixPayrollMissingColumns'
)
BEGIN

    IF OBJECT_ID(N'[SalaryPackageItem]', N'U') IS NOT NULL
        AND COL_LENGTH('SalaryPackageItem', 'ReferentielElementId') IS NOT NULL
        AND NOT EXISTS (
            SELECT 1
            FROM sys.indexes
            WHERE name = 'IX_SalaryPackageItem_ReferentielElementId'
              AND object_id = OBJECT_ID('SalaryPackageItem')
        )
    BEGIN
        CREATE INDEX [IX_SalaryPackageItem_ReferentielElementId]
            ON [SalaryPackageItem]([ReferentielElementId]);
    END

END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260206123000_FixPayrollMissingColumns'
)
BEGIN

    IF OBJECT_ID(N'[SalaryPackageItem]', N'U') IS NOT NULL
        AND OBJECT_ID(N'[ReferentielElements]', N'U') IS NOT NULL
        AND COL_LENGTH('SalaryPackageItem', 'ReferentielElementId') IS NOT NULL
        AND NOT EXISTS (
            SELECT 1
            FROM sys.foreign_keys
            WHERE name = 'FK_SalaryPackageItem_ReferentielElements_ReferentielElementId'
        )
    BEGIN
        ALTER TABLE [SalaryPackageItem]
            ADD CONSTRAINT [FK_SalaryPackageItem_ReferentielElements_ReferentielElementId]
            FOREIGN KEY ([ReferentielElementId]) REFERENCES [ReferentielElements] ([Id]) ON DELETE NO ACTION;
    END

END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260206123000_FixPayrollMissingColumns'
)
BEGIN

    IF OBJECT_ID(N'[RuleDualCaps]', N'U') IS NULL
    BEGIN
        CREATE TABLE [RuleDualCaps] (
            [Id] int NOT NULL IDENTITY(1,1),
            [RuleId] int NOT NULL,
            [FixedCapAmount] decimal(18,4) NOT NULL,
            [FixedCapUnit] nvarchar(20) NOT NULL,
            [PercentageCap] decimal(5,4) NOT NULL,
            [BaseReference] nvarchar(30) NOT NULL,
            [Logic] nvarchar(10) NOT NULL DEFAULT 'MIN',
            CONSTRAINT [PK_RuleDualCaps] PRIMARY KEY ([Id]),
            CONSTRAINT [FK_RuleDualCaps_ElementRules_RuleId] FOREIGN KEY ([RuleId])
                REFERENCES [ElementRules] ([Id]) ON DELETE CASCADE
        );

        CREATE UNIQUE INDEX [IX_RuleDualCaps_RuleId] ON [RuleDualCaps]([RuleId]);
    END

END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260206123000_FixPayrollMissingColumns'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260206123000_FixPayrollMissingColumns', N'9.0.0');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260208225139__DetectPendingModelChanges'
)
BEGIN

    UPDATE [SalaryPackage]
    SET [TemplateType] = 'COMPANY'
    WHERE [DeletedAt] IS NULL
      AND [CompanyId] IS NOT NULL
      AND [TemplateType] <> 'COMPANY';

END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260208225139__DetectPendingModelChanges'
)
BEGIN

    UPDATE [SalaryPackage]
    SET [TemplateType] = 'OFFICIAL'
    WHERE [DeletedAt] IS NULL
      AND [CompanyId] IS NULL
      AND [TemplateType] <> 'OFFICIAL';

END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260208225139__DetectPendingModelChanges'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260208225139__DetectPendingModelChanges', N'9.0.0');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260210101731_AddBusinessSectorsTable'
)
BEGIN
    CREATE TABLE [BusinessSectors] (
        [Id] int NOT NULL IDENTITY,
        [Code] nvarchar(50) NOT NULL,
        [Name] nvarchar(200) NOT NULL,
        [IsStandard] bit NOT NULL DEFAULT CAST(0 AS bit),
        [SortOrder] int NOT NULL DEFAULT 0,
        [CreatedAt] datetimeoffset NOT NULL DEFAULT (GETUTCDATE()),
        [CreatedBy] int NOT NULL,
        [ModifiedAt] datetimeoffset NULL,
        [ModifiedBy] int NULL,
        [DeletedAt] datetimeoffset NULL,
        [DeletedBy] int NULL,
        CONSTRAINT [PK_BusinessSectors] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260210101731_AddBusinessSectorsTable'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [IX_BusinessSectors_Code] ON [BusinessSectors] ([Code]) WHERE [DeletedAt] IS NULL');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260210101731_AddBusinessSectorsTable'
)
BEGIN

                    SET IDENTITY_INSERT BusinessSectors ON;

                    INSERT INTO BusinessSectors (Id, Code, Name, IsStandard, SortOrder, CreatedAt, CreatedBy)
                    VALUES
                        (1, 'IND', 'Industrie', 1, 1, GETUTCDATE(), 1),
                        (2, 'COM', 'Commerce', 1, 2, GETUTCDATE(), 1),
                        (3, 'SRV', 'Services', 1, 3, GETUTCDATE(), 1),
                        (4, 'BTP', 'BTP', 1, 4, GETUTCDATE(), 1),
                        (5, 'AGR', 'Agriculture et Pêche', 1, 5, GETUTCDATE(), 1),
                        (6, 'ART', 'Artisanat', 1, 6, GETUTCDATE(), 1),
                        (7, 'LIB', 'Professions libérales', 1, 7, GETUTCDATE(), 1),
                        (8, 'TOU', 'Tourisme et Hôtellerie', 1, 8, GETUTCDATE(), 1),
                        (9, 'TRA', 'Transport et Logistique', 1, 9, GETUTCDATE(), 1),
                        (10, 'EDU', 'Éducation et Formation', 1, 10, GETUTCDATE(), 1),
                        (11, 'SAN', 'Santé', 1, 11, GETUTCDATE(), 1),
                        (12, 'IMM', 'Immobilier', 1, 12, GETUTCDATE(), 1),
                        (13, 'TEC', 'Technologies et Télécommunications', 1, 13, GETUTCDATE(), 1),
                        (14, 'ASS', 'Associations et Coopératives', 1, 14, GETUTCDATE(), 1);

                    SET IDENTITY_INSERT BusinessSectors OFF;
                
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260210101731_AddBusinessSectorsTable'
)
BEGIN
    ALTER TABLE [SalaryPackage] ADD [BusinessSectorId] int NOT NULL DEFAULT 1;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260210101731_AddBusinessSectorsTable'
)
BEGIN
    ALTER TABLE [SalaryPackage] ADD CONSTRAINT [FK_SalaryPackage_BusinessSectors_BusinessSectorId] FOREIGN KEY ([BusinessSectorId]) REFERENCES [BusinessSectors] ([Id]) ON DELETE NO ACTION;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260210101731_AddBusinessSectorsTable'
)
BEGIN
    CREATE INDEX [IX_SalaryPackage_BusinessSectorId] ON [SalaryPackage] ([BusinessSectorId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260210101731_AddBusinessSectorsTable'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260210101731_AddBusinessSectorsTable', N'9.0.0');
END;

COMMIT;
GO

