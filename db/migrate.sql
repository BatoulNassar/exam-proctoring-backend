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
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260730181823_LocalMigration'
)
BEGIN
    CREATE TABLE [Permission] (
        [id] int NOT NULL IDENTITY,
        [name] nvarchar(100) NOT NULL,
        [description] nvarchar(255) NOT NULL,
        [created_at] datetime2 NOT NULL,
        [created_by] int NULL,
        [updated_at] datetime2 NULL,
        [updated_by] int NULL,
        [is_deleted] bit NOT NULL DEFAULT CAST(0 AS bit),
        [deleted_at] datetime2 NULL,
        [deleted_by] int NULL,
        CONSTRAINT [PK_Permission] PRIMARY KEY ([id])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260730181823_LocalMigration'
)
BEGIN
    CREATE TABLE [Role] (
        [id] int NOT NULL IDENTITY,
        [name] nvarchar(50) NOT NULL,
        [created_at] datetime2 NOT NULL,
        [created_by] int NULL,
        [updated_at] datetime2 NULL,
        [updated_by] int NULL,
        [is_deleted] bit NOT NULL DEFAULT CAST(0 AS bit),
        [deleted_at] datetime2 NULL,
        [deleted_by] int NULL,
        CONSTRAINT [PK_Role] PRIMARY KEY ([id])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260730181823_LocalMigration'
)
BEGIN
    CREATE TABLE [Student] (
        [id] int NOT NULL IDENTITY,
        [user_name] nvarchar(50) NOT NULL,
        [password] nvarchar(255) NOT NULL,
        [email] nvarchar(100) NOT NULL,
        [phone_number] nvarchar(20) NOT NULL,
        [first_name] nvarchar(50) NOT NULL,
        [middle_name] nvarchar(50) NULL,
        [last_name] nvarchar(50) NOT NULL,
        [university_number] nvarchar(20) NOT NULL,
        [face_id] nvarchar(500) NOT NULL,
        [created_at] datetime2 NOT NULL,
        [created_by] int NULL,
        [updated_at] datetime2 NULL,
        [updated_by] int NULL,
        [is_deleted] bit NOT NULL DEFAULT CAST(0 AS bit),
        [deleted_at] datetime2 NULL,
        [deleted_by] int NULL,
        CONSTRAINT [PK_Student] PRIMARY KEY ([id])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260730181823_LocalMigration'
)
BEGIN
    CREATE TABLE [User] (
        [id] int NOT NULL IDENTITY,
        [user_name] nvarchar(50) NULL,
        [email] nvarchar(100) NOT NULL,
        [phone_number] nvarchar(20) NULL,
        [full_name] nvarchar(100) NOT NULL,
        [password_hash] nvarchar(max) NOT NULL,
        [is_active] bit NOT NULL,
        [created_at] datetime2 NOT NULL,
        [created_by] int NULL,
        [updated_at] datetime2 NULL,
        [updated_by] int NULL,
        [is_deleted] bit NOT NULL DEFAULT CAST(0 AS bit),
        [deleted_at] datetime2 NULL,
        [deleted_by] int NULL,
        CONSTRAINT [PK_User] PRIMARY KEY ([id])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260730181823_LocalMigration'
)
BEGIN
    CREATE TABLE [Permission_Role] (
        [id] int NOT NULL IDENTITY,
        [permission_id] int NOT NULL,
        [role_id] int NOT NULL,
        [created_at] datetime2 NOT NULL,
        [created_by] int NULL,
        [updated_at] datetime2 NULL,
        [updated_by] int NULL,
        [is_deleted] bit NOT NULL DEFAULT CAST(0 AS bit),
        [deleted_at] datetime2 NULL,
        [deleted_by] int NULL,
        CONSTRAINT [PK_Permission_Role] PRIMARY KEY ([id]),
        CONSTRAINT [FK_Permission_Role_Permission_permission_id] FOREIGN KEY ([permission_id]) REFERENCES [Permission] ([id]) ON DELETE CASCADE,
        CONSTRAINT [FK_Permission_Role_Role_role_id] FOREIGN KEY ([role_id]) REFERENCES [Role] ([id]) ON DELETE CASCADE
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260730181823_LocalMigration'
)
BEGIN
    CREATE TABLE [QuestionBank] (
        [id] int NOT NULL IDENTITY,
        [title] nvarchar(100) NOT NULL,
        [course_code] nvarchar(10) NOT NULL,
        [status] nvarchar(max) NOT NULL,
        [version] nvarchar(10) NOT NULL,
        [authored_by_admin_id] int NOT NULL,
        [locked_at] datetime2 NULL,
        [randomization] bit NOT NULL,
        [option_shuffle] bit NOT NULL,
        [created_at] datetime2 NOT NULL,
        [created_by] int NULL,
        [updated_at] datetime2 NULL,
        [updated_by] int NULL,
        [is_deleted] bit NOT NULL DEFAULT CAST(0 AS bit),
        [deleted_at] datetime2 NULL,
        [deleted_by] int NULL,
        CONSTRAINT [PK_QuestionBank] PRIMARY KEY ([id]),
        CONSTRAINT [FK_QuestionBank_User_authored_by_admin_id] FOREIGN KEY ([authored_by_admin_id]) REFERENCES [User] ([id]) ON DELETE NO ACTION
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260730181823_LocalMigration'
)
BEGIN
    CREATE TABLE [RefreshToken] (
        [id] int NOT NULL IDENTITY,
        [user_id] int NOT NULL,
        [token] nvarchar(500) NOT NULL,
        [expires_at] datetime2 NOT NULL,
        [revoked_at] datetime2 NULL,
        [replaced_by_token] nvarchar(500) NULL,
        [created_at] datetime2 NOT NULL,
        [created_by] int NULL,
        [updated_at] datetime2 NULL,
        [updated_by] int NULL,
        [is_deleted] bit NOT NULL,
        [deleted_at] datetime2 NULL,
        [deleted_by] int NULL,
        CONSTRAINT [PK_RefreshToken] PRIMARY KEY ([id]),
        CONSTRAINT [FK_RefreshToken_User_user_id] FOREIGN KEY ([user_id]) REFERENCES [User] ([id]) ON DELETE CASCADE
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260730181823_LocalMigration'
)
BEGIN
    CREATE TABLE [User_Roles] (
        [id] int NOT NULL IDENTITY,
        [user_id] int NOT NULL,
        [role_id] int NOT NULL,
        [created_at] datetime2 NOT NULL,
        [created_by] int NULL,
        [updated_at] datetime2 NULL,
        [updated_by] int NULL,
        [is_deleted] bit NOT NULL DEFAULT CAST(0 AS bit),
        [deleted_at] datetime2 NULL,
        [deleted_by] int NULL,
        CONSTRAINT [PK_User_Roles] PRIMARY KEY ([id]),
        CONSTRAINT [FK_User_Roles_Role_role_id] FOREIGN KEY ([role_id]) REFERENCES [Role] ([id]) ON DELETE CASCADE,
        CONSTRAINT [FK_User_Roles_User_user_id] FOREIGN KEY ([user_id]) REFERENCES [User] ([id]) ON DELETE CASCADE
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260730181823_LocalMigration'
)
BEGIN
    CREATE TABLE [ExamSession] (
        [id] int NOT NULL IDENTITY,
        [title] nvarchar(150) NOT NULL,
        [course_tag] nvarchar(50) NOT NULL,
        [status] nvarchar(max) NOT NULL,
        [start_time] datetime2 NOT NULL,
        [duration_minutes] int NOT NULL,
        [question_bank_id] int NOT NULL,
        [scheduled_at] datetime2 NULL,
        [locked_at] datetime2 NULL,
        [active_at] datetime2 NULL,
        [grace_period_ended_at] datetime2 NULL,
        [grace_period_minutes] int NOT NULL,
        [extended_by_minutes] int NOT NULL,
        [login_window_minutes] int NOT NULL,
        [eye_gaze_threshold_sec] int NOT NULL,
        [face_alert_sensitivity] nvarchar(10) NOT NULL,
        [created_by_admin_id] int NOT NULL,
        [closed_at] datetime2 NULL,
        [archived_at] datetime2 NULL,
        [created_at] datetime2 NOT NULL,
        [created_by] int NULL,
        [updated_at] datetime2 NULL,
        [updated_by] int NULL,
        [is_deleted] bit NOT NULL DEFAULT CAST(0 AS bit),
        [deleted_at] datetime2 NULL,
        [deleted_by] int NULL,
        CONSTRAINT [PK_ExamSession] PRIMARY KEY ([id]),
        CONSTRAINT [FK_ExamSession_QuestionBank_question_bank_id] FOREIGN KEY ([question_bank_id]) REFERENCES [QuestionBank] ([id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_ExamSession_User_created_by_admin_id] FOREIGN KEY ([created_by_admin_id]) REFERENCES [User] ([id]) ON DELETE NO ACTION
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260730181823_LocalMigration'
)
BEGIN
    CREATE TABLE [Question] (
        [id] int NOT NULL IDENTITY,
        [question_bank_id] int NOT NULL,
        [type] nvarchar(max) NOT NULL,
        [question_text] nvarchar(2000) NOT NULL,
        [option_a] nvarchar(1000) NULL,
        [option_b] nvarchar(1000) NULL,
        [option_c] nvarchar(1000) NULL,
        [option_d] nvarchar(1000) NULL,
        [option_e] nvarchar(1000) NULL,
        [correct_answer] nvarchar(255) NOT NULL,
        [marks] int NOT NULL,
        [created_at] datetime2 NOT NULL,
        [created_by] int NULL,
        [updated_at] datetime2 NULL,
        [updated_by] int NULL,
        [is_deleted] bit NOT NULL DEFAULT CAST(0 AS bit),
        [deleted_at] datetime2 NULL,
        [deleted_by] int NULL,
        CONSTRAINT [PK_Question] PRIMARY KEY ([id]),
        CONSTRAINT [FK_Question_QuestionBank_question_bank_id] FOREIGN KEY ([question_bank_id]) REFERENCES [QuestionBank] ([id]) ON DELETE NO ACTION
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260730181823_LocalMigration'
)
BEGIN
    CREATE TABLE [AuditLog] (
        [id] int NOT NULL IDENTITY,
        [exam_session_id] int NOT NULL,
        [actor_id] int NOT NULL,
        [actor_type] nvarchar(50) NOT NULL,
        [action] nvarchar(100) NOT NULL,
        [entity_id] int NOT NULL,
        [entity_type] nvarchar(50) NOT NULL,
        [details] nvarchar(1000) NOT NULL,
        [occurred_at] datetime2 NOT NULL,
        [created_at] datetime2 NOT NULL,
        [created_by] int NULL,
        [updated_at] datetime2 NULL,
        [updated_by] int NULL,
        [is_deleted] bit NOT NULL DEFAULT CAST(0 AS bit),
        [deleted_at] datetime2 NULL,
        [deleted_by] int NULL,
        CONSTRAINT [PK_AuditLog] PRIMARY KEY ([id]),
        CONSTRAINT [FK_AuditLog_ExamSession_exam_session_id] FOREIGN KEY ([exam_session_id]) REFERENCES [ExamSession] ([id]) ON DELETE CASCADE
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260730181823_LocalMigration'
)
BEGIN
    CREATE TABLE [GradingExport] (
        [id] int NOT NULL IDENTITY,
        [exam_session_id] int NOT NULL,
        [format] nvarchar(max) NOT NULL,
        [file_path] nvarchar(500) NOT NULL,
        [generated_at] datetime2 NOT NULL,
        [created_at] datetime2 NOT NULL,
        [created_by] int NULL,
        [updated_at] datetime2 NULL,
        [updated_by] int NULL,
        [is_deleted] bit NOT NULL DEFAULT CAST(0 AS bit),
        [deleted_at] datetime2 NULL,
        [deleted_by] int NULL,
        CONSTRAINT [PK_GradingExport] PRIMARY KEY ([id]),
        CONSTRAINT [FK_GradingExport_ExamSession_exam_session_id] FOREIGN KEY ([exam_session_id]) REFERENCES [ExamSession] ([id]) ON DELETE CASCADE
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260730181823_LocalMigration'
)
BEGIN
    CREATE TABLE [ProctorSession] (
        [id] int NOT NULL IDENTITY,
        [exam_session_id] int NOT NULL,
        [proctor_id] int NOT NULL,
        [created_at] datetime2 NOT NULL,
        [created_by] int NULL,
        [updated_at] datetime2 NULL,
        [updated_by] int NULL,
        [is_deleted] bit NOT NULL DEFAULT CAST(0 AS bit),
        [deleted_at] datetime2 NULL,
        [deleted_by] int NULL,
        CONSTRAINT [PK_ProctorSession] PRIMARY KEY ([id]),
        CONSTRAINT [FK_ProctorSession_ExamSession_exam_session_id] FOREIGN KEY ([exam_session_id]) REFERENCES [ExamSession] ([id]) ON DELETE CASCADE,
        CONSTRAINT [FK_ProctorSession_User_proctor_id] FOREIGN KEY ([proctor_id]) REFERENCES [User] ([id]) ON DELETE NO ACTION
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260730181823_LocalMigration'
)
BEGIN
    CREATE TABLE [StudentSession] (
        [id] int NOT NULL IDENTITY,
        [exam_session_id] int NOT NULL,
        [student_id] int NOT NULL,
        [status] nvarchar(20) NOT NULL,
        [login_at] datetime2 NULL,
        [verified_at] datetime2 NULL,
        [liveness_passed] bit NOT NULL,
        [face_match_passed] bit NOT NULL,
        [failed_auth_attempts] int NOT NULL,
        [submitted_at] datetime2 NULL,
        [awarded_marks] int NULL,
        [created_at] datetime2 NOT NULL,
        [created_by] int NULL,
        [updated_at] datetime2 NULL,
        [updated_by] int NULL,
        [is_deleted] bit NOT NULL DEFAULT CAST(0 AS bit),
        [deleted_at] datetime2 NULL,
        [deleted_by] int NULL,
        CONSTRAINT [PK_StudentSession] PRIMARY KEY ([id]),
        CONSTRAINT [FK_StudentSession_ExamSession_exam_session_id] FOREIGN KEY ([exam_session_id]) REFERENCES [ExamSession] ([id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_StudentSession_Student_student_id] FOREIGN KEY ([student_id]) REFERENCES [Student] ([id]) ON DELETE NO ACTION
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260730181823_LocalMigration'
)
BEGIN
    CREATE TABLE [AutoScore] (
        [id] int NOT NULL IDENTITY,
        [student_session_id] int NOT NULL,
        [question_id] int NOT NULL,
        [student_answer] nvarchar(10) NOT NULL,
        [correct_answer] nvarchar(10) NOT NULL,
        [marks_awarded] int NOT NULL,
        [max_marks] int NOT NULL,
        [created_at] datetime2 NOT NULL,
        [created_by] int NULL,
        [updated_at] datetime2 NULL,
        [updated_by] int NULL,
        [is_deleted] bit NOT NULL DEFAULT CAST(0 AS bit),
        [deleted_at] datetime2 NULL,
        [deleted_by] int NULL,
        CONSTRAINT [PK_AutoScore] PRIMARY KEY ([id]),
        CONSTRAINT [FK_AutoScore_Question_question_id] FOREIGN KEY ([question_id]) REFERENCES [Question] ([id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_AutoScore_StudentSession_student_session_id] FOREIGN KEY ([student_session_id]) REFERENCES [StudentSession] ([id]) ON DELETE CASCADE
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260730181823_LocalMigration'
)
BEGIN
    CREATE TABLE [ConnectivityBuffer] (
        [id] int NOT NULL IDENTITY,
        [student_session_id] int NOT NULL,
        [buffer_type] nvarchar(50) NOT NULL,
        [encrypted_payload] nvarchar(max) NOT NULL,
        [action] nvarchar(50) NOT NULL,
        [buffered_at] datetime2 NOT NULL,
        [synced_at] datetime2 NULL,
        [created_at] datetime2 NOT NULL,
        [created_by] int NULL,
        [updated_at] datetime2 NULL,
        [updated_by] int NULL,
        [is_deleted] bit NOT NULL DEFAULT CAST(0 AS bit),
        [deleted_at] datetime2 NULL,
        [deleted_by] int NULL,
        CONSTRAINT [PK_ConnectivityBuffer] PRIMARY KEY ([id]),
        CONSTRAINT [FK_ConnectivityBuffer_StudentSession_student_session_id] FOREIGN KEY ([student_session_id]) REFERENCES [StudentSession] ([id]) ON DELETE CASCADE
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260730181823_LocalMigration'
)
BEGIN
    CREATE TABLE [MonitoringEvent] (
        [id] int NOT NULL IDENTITY,
        [student_session_id] int NOT NULL,
        [event_type] nvarchar(50) NOT NULL,
        [event_details] nvarchar(1000) NOT NULL,
        [occured_at] datetime2 NOT NULL,
        [created_at] datetime2 NOT NULL,
        [created_by] int NULL,
        [updated_at] datetime2 NULL,
        [updated_by] int NULL,
        [is_deleted] bit NOT NULL DEFAULT CAST(0 AS bit),
        [deleted_at] datetime2 NULL,
        [deleted_by] int NULL,
        CONSTRAINT [PK_MonitoringEvent] PRIMARY KEY ([id]),
        CONSTRAINT [FK_MonitoringEvent_StudentSession_student_session_id] FOREIGN KEY ([student_session_id]) REFERENCES [StudentSession] ([id]) ON DELETE CASCADE
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260730181823_LocalMigration'
)
BEGIN
    CREATE TABLE [StudentAnswer] (
        [id] int NOT NULL IDENTITY,
        [student_session_id] int NOT NULL,
        [question_id] int NOT NULL,
        [student_response] nvarchar(max) NOT NULL,
        [saved_at] datetime2 NOT NULL,
        [created_at] datetime2 NOT NULL,
        [created_by] int NULL,
        [updated_at] datetime2 NULL,
        [updated_by] int NULL,
        [is_deleted] bit NOT NULL DEFAULT CAST(0 AS bit),
        [deleted_at] datetime2 NULL,
        [deleted_by] int NULL,
        CONSTRAINT [PK_StudentAnswer] PRIMARY KEY ([id]),
        CONSTRAINT [FK_StudentAnswer_Question_question_id] FOREIGN KEY ([question_id]) REFERENCES [Question] ([id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_StudentAnswer_StudentSession_student_session_id] FOREIGN KEY ([student_session_id]) REFERENCES [StudentSession] ([id]) ON DELETE CASCADE
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260730181823_LocalMigration'
)
BEGIN
    CREATE TABLE [AlertEvent] (
        [id] int NOT NULL IDENTITY,
        [student_session_id] int NOT NULL,
        [monitoring_event_id] int NOT NULL,
        [alert_type] nvarchar(50) NOT NULL,
        [triggered_at] datetime2 NOT NULL,
        [delivered_at] datetime2 NULL,
        [created_at] datetime2 NOT NULL,
        [created_by] int NULL,
        [updated_at] datetime2 NULL,
        [updated_by] int NULL,
        [is_deleted] bit NOT NULL DEFAULT CAST(0 AS bit),
        [deleted_at] datetime2 NULL,
        [deleted_by] int NULL,
        CONSTRAINT [PK_AlertEvent] PRIMARY KEY ([id]),
        CONSTRAINT [FK_AlertEvent_MonitoringEvent_monitoring_event_id] FOREIGN KEY ([monitoring_event_id]) REFERENCES [MonitoringEvent] ([id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_AlertEvent_StudentSession_student_session_id] FOREIGN KEY ([student_session_id]) REFERENCES [StudentSession] ([id]) ON DELETE CASCADE
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260730181823_LocalMigration'
)
BEGIN
    CREATE TABLE [ProctorAction] (
        [id] int NOT NULL IDENTITY,
        [alert_event_id] int NOT NULL,
        [admin_id] int NOT NULL,
        [action_type] nvarchar(50) NOT NULL,
        [action_note] nvarchar(500) NOT NULL,
        [acted_at] datetime2 NOT NULL,
        [created_at] datetime2 NOT NULL,
        [created_by] int NULL,
        [updated_at] datetime2 NULL,
        [updated_by] int NULL,
        [is_deleted] bit NOT NULL DEFAULT CAST(0 AS bit),
        [deleted_at] datetime2 NULL,
        [deleted_by] int NULL,
        CONSTRAINT [PK_ProctorAction] PRIMARY KEY ([id]),
        CONSTRAINT [FK_ProctorAction_AlertEvent_alert_event_id] FOREIGN KEY ([alert_event_id]) REFERENCES [AlertEvent] ([id]) ON DELETE CASCADE,
        CONSTRAINT [FK_ProctorAction_User_admin_id] FOREIGN KEY ([admin_id]) REFERENCES [User] ([id]) ON DELETE NO ACTION
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260730181823_LocalMigration'
)
BEGIN
    CREATE TABLE [WarningMessage] (
        [id] int NOT NULL IDENTITY,
        [proctor_action_id] int NOT NULL,
        [student_session_id] int NOT NULL,
        [message_text] nvarchar(1000) NOT NULL,
        [sent_at] datetime2 NOT NULL,
        [acknowledged_at] datetime2 NULL,
        [created_at] datetime2 NOT NULL,
        [created_by] int NULL,
        [updated_at] datetime2 NULL,
        [updated_by] int NULL,
        [is_deleted] bit NOT NULL DEFAULT CAST(0 AS bit),
        [deleted_at] datetime2 NULL,
        [deleted_by] int NULL,
        CONSTRAINT [PK_WarningMessage] PRIMARY KEY ([id]),
        CONSTRAINT [FK_WarningMessage_ProctorAction_proctor_action_id] FOREIGN KEY ([proctor_action_id]) REFERENCES [ProctorAction] ([id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_WarningMessage_StudentSession_student_session_id] FOREIGN KEY ([student_session_id]) REFERENCES [StudentSession] ([id]) ON DELETE NO ACTION
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260730181823_LocalMigration'
)
BEGIN
    CREATE INDEX [IX_AlertEvent_monitoring_event_id] ON [AlertEvent] ([monitoring_event_id]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260730181823_LocalMigration'
)
BEGIN
    CREATE INDEX [IX_AlertEvent_student_session_id] ON [AlertEvent] ([student_session_id]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260730181823_LocalMigration'
)
BEGIN
    CREATE INDEX [IX_AuditLog_exam_session_id] ON [AuditLog] ([exam_session_id]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260730181823_LocalMigration'
)
BEGIN
    CREATE INDEX [IX_AutoScore_question_id] ON [AutoScore] ([question_id]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260730181823_LocalMigration'
)
BEGIN
    CREATE INDEX [IX_AutoScore_student_session_id] ON [AutoScore] ([student_session_id]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260730181823_LocalMigration'
)
BEGIN
    CREATE INDEX [IX_ConnectivityBuffer_student_session_id] ON [ConnectivityBuffer] ([student_session_id]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260730181823_LocalMigration'
)
BEGIN
    CREATE INDEX [IX_ExamSession_created_by_admin_id] ON [ExamSession] ([created_by_admin_id]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260730181823_LocalMigration'
)
BEGIN
    CREATE INDEX [IX_ExamSession_question_bank_id] ON [ExamSession] ([question_bank_id]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260730181823_LocalMigration'
)
BEGIN
    CREATE INDEX [IX_GradingExport_exam_session_id] ON [GradingExport] ([exam_session_id]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260730181823_LocalMigration'
)
BEGIN
    CREATE INDEX [IX_MonitoringEvent_student_session_id] ON [MonitoringEvent] ([student_session_id]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260730181823_LocalMigration'
)
BEGIN
    CREATE UNIQUE INDEX [IX_Permission_name] ON [Permission] ([name]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260730181823_LocalMigration'
)
BEGIN
    CREATE INDEX [IX_Permission_Role_permission_id] ON [Permission_Role] ([permission_id]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260730181823_LocalMigration'
)
BEGIN
    CREATE INDEX [IX_Permission_Role_role_id] ON [Permission_Role] ([role_id]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260730181823_LocalMigration'
)
BEGIN
    CREATE INDEX [IX_ProctorAction_admin_id] ON [ProctorAction] ([admin_id]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260730181823_LocalMigration'
)
BEGIN
    CREATE INDEX [IX_ProctorAction_alert_event_id] ON [ProctorAction] ([alert_event_id]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260730181823_LocalMigration'
)
BEGIN
    CREATE INDEX [IX_ProctorSession_proctor_id] ON [ProctorSession] ([proctor_id]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260730181823_LocalMigration'
)
BEGIN
    CREATE UNIQUE INDEX [IX_ProctorSession_Unique] ON [ProctorSession] ([exam_session_id], [proctor_id]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260730181823_LocalMigration'
)
BEGIN
    CREATE INDEX [IX_Question_question_bank_id] ON [Question] ([question_bank_id]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260730181823_LocalMigration'
)
BEGIN
    CREATE INDEX [IX_QuestionBank_authored_by_admin_id] ON [QuestionBank] ([authored_by_admin_id]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260730181823_LocalMigration'
)
BEGIN
    CREATE UNIQUE INDEX [IX_RefreshToken_token] ON [RefreshToken] ([token]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260730181823_LocalMigration'
)
BEGIN
    CREATE INDEX [IX_RefreshToken_user_id] ON [RefreshToken] ([user_id]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260730181823_LocalMigration'
)
BEGIN
    CREATE UNIQUE INDEX [IX_Role_name] ON [Role] ([name]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260730181823_LocalMigration'
)
BEGIN
    CREATE UNIQUE INDEX [IX_Student_email] ON [Student] ([email]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260730181823_LocalMigration'
)
BEGIN
    CREATE UNIQUE INDEX [IX_Student_university_number] ON [Student] ([university_number]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260730181823_LocalMigration'
)
BEGIN
    CREATE UNIQUE INDEX [IX_Student_user_name] ON [Student] ([user_name]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260730181823_LocalMigration'
)
BEGIN
    CREATE INDEX [IX_StudentAnswer_question_id] ON [StudentAnswer] ([question_id]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260730181823_LocalMigration'
)
BEGIN
    CREATE UNIQUE INDEX [IX_StudentAnswer_student_session_id_question_id] ON [StudentAnswer] ([student_session_id], [question_id]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260730181823_LocalMigration'
)
BEGIN
    CREATE UNIQUE INDEX [IX_StudentSession_exam_session_id_student_id] ON [StudentSession] ([exam_session_id], [student_id]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260730181823_LocalMigration'
)
BEGIN
    CREATE INDEX [IX_StudentSession_student_id] ON [StudentSession] ([student_id]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260730181823_LocalMigration'
)
BEGIN
    CREATE UNIQUE INDEX [IX_User_email] ON [User] ([email]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260730181823_LocalMigration'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [IX_User_user_name] ON [User] ([user_name]) WHERE [user_name] IS NOT NULL');
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260730181823_LocalMigration'
)
BEGIN
    CREATE INDEX [IX_User_Roles_role_id] ON [User_Roles] ([role_id]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260730181823_LocalMigration'
)
BEGIN
    CREATE INDEX [IX_User_Roles_user_id] ON [User_Roles] ([user_id]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260730181823_LocalMigration'
)
BEGIN
    CREATE INDEX [IX_WarningMessage_proctor_action_id] ON [WarningMessage] ([proctor_action_id]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260730181823_LocalMigration'
)
BEGIN
    CREATE INDEX [IX_WarningMessage_student_session_id] ON [WarningMessage] ([student_session_id]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260730181823_LocalMigration'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260730181823_LocalMigration', N'8.0.14');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260730220834_ServerMigration'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260730220834_ServerMigration', N'8.0.14');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260731151411_AddAlertSeverityAndStatus'
)
BEGIN
    ALTER TABLE [Student] ADD [photo_raw] varbinary(max) NOT NULL DEFAULT 0x;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260731151411_AddAlertSeverityAndStatus'
)
BEGIN
    ALTER TABLE [AlertEvent] ADD [severity] nvarchar(20) NOT NULL DEFAULT N'';
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260731151411_AddAlertSeverityAndStatus'
)
BEGIN
    ALTER TABLE [AlertEvent] ADD [status] nvarchar(20) NOT NULL DEFAULT N'';
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260731151411_AddAlertSeverityAndStatus'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260731151411_AddAlertSeverityAndStatus', N'8.0.14');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260731171439_AddSystemSettings'
)
BEGIN
    CREATE TABLE [SystemSettings] (
        [id] int NOT NULL IDENTITY,
        [gaze_alert_threshold_sec] int NOT NULL,
        [face_sensitivity] nvarchar(10) NOT NULL,
        [ambient_audio_monitoring] bit NOT NULL,
        [grace_period_minutes] int NOT NULL,
        [login_window_minutes] int NOT NULL,
        [max_liveness_attempts] int NOT NULL,
        [face_match_threshold] int NOT NULL,
        [question_randomisation] bit NOT NULL,
        [option_shuffle] bit NOT NULL,
        [created_at] datetime2 NOT NULL,
        [created_by] int NULL,
        [updated_at] datetime2 NULL,
        [updated_by] int NULL,
        [is_deleted] bit NOT NULL DEFAULT CAST(0 AS bit),
        [deleted_at] datetime2 NULL,
        [deleted_by] int NULL,
        CONSTRAINT [PK_SystemSettings] PRIMARY KEY ([id])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260731171439_AddSystemSettings'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260731171439_AddSystemSettings', N'8.0.14');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260803104257_AddStudentAuthenticationState'
)
BEGIN
    ALTER TABLE [Student] ADD [failed_login_attempts] int NOT NULL DEFAULT 0;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260803104257_AddStudentAuthenticationState'
)
BEGIN
    ALTER TABLE [Student] ADD [is_active] bit NOT NULL DEFAULT CAST(1 AS bit);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260803104257_AddStudentAuthenticationState'
)
BEGIN
    ALTER TABLE [Student] ADD [lockout_end_utc] datetime2 NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260803104257_AddStudentAuthenticationState'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260803104257_AddStudentAuthenticationState', N'8.0.14');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260803115733_AddStudentLoginAttemptTracking'
)
BEGIN
    CREATE TABLE [StudentLoginAttempt] (
        [id] int NOT NULL IDENTITY,
        [identifier_hash] nvarchar(64) NOT NULL,
        [failed_attempts] int NOT NULL DEFAULT 0,
        [lockout_end_utc] datetime2 NULL,
        [created_at] datetime2 NOT NULL,
        [created_by] int NULL,
        [updated_at] datetime2 NULL,
        [updated_by] int NULL,
        [is_deleted] bit NOT NULL DEFAULT CAST(0 AS bit),
        [deleted_at] datetime2 NULL,
        [deleted_by] int NULL,
        CONSTRAINT [PK_StudentLoginAttempt] PRIMARY KEY ([id])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260803115733_AddStudentLoginAttemptTracking'
)
BEGIN
    CREATE UNIQUE INDEX [IX_StudentLoginAttempt_identifier_hash] ON [StudentLoginAttempt] ([identifier_hash]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260803115733_AddStudentLoginAttemptTracking'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260803115733_AddStudentLoginAttemptTracking', N'8.0.14');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260804160931_AddPhotoUrlRemovePhotoRaw'
)
BEGIN
    DECLARE @var0 sysname;
    SELECT @var0 = [d].[name]
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Student]') AND [c].[name] = N'photo_raw');
    IF @var0 IS NOT NULL EXEC(N'ALTER TABLE [Student] DROP CONSTRAINT [' + @var0 + '];');
    ALTER TABLE [Student] DROP COLUMN [photo_raw];
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260804160931_AddPhotoUrlRemovePhotoRaw'
)
BEGIN
    DECLARE @var1 sysname;
    SELECT @var1 = [d].[name]
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Student]') AND [c].[name] = N'face_id');
    IF @var1 IS NOT NULL EXEC(N'ALTER TABLE [Student] DROP CONSTRAINT [' + @var1 + '];');
    ALTER TABLE [Student] ALTER COLUMN [face_id] nvarchar(500) NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260804160931_AddPhotoUrlRemovePhotoRaw'
)
BEGIN
    ALTER TABLE [Student] ADD [photo_url] nvarchar(500) NOT NULL DEFAULT N'';
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260804160931_AddPhotoUrlRemovePhotoRaw'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260804160931_AddPhotoUrlRemovePhotoRaw', N'8.0.14');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260804173419_AddUserOtpFields'
)
BEGIN
    ALTER TABLE [User] ADD [reset_otp] nvarchar(max) NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260804173419_AddUserOtpFields'
)
BEGIN
    ALTER TABLE [User] ADD [reset_otp_expires_at] datetime2 NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260804173419_AddUserOtpFields'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260804173419_AddUserOtpFields', N'8.0.14');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260806184820_AddDeviceChecks'
)
BEGIN
    CREATE TABLE [DeviceCheck] (
        [id] int NOT NULL IDENTITY,
        [student_session_id] int NOT NULL,
        [device_id] nvarchar(36) NOT NULL,
        [checked_at_utc] datetime2 NOT NULL,
        [received_at_utc] datetime2 NOT NULL,
        [client_can_proceed] bit NOT NULL,
        [exam_session_status] nvarchar(20) NOT NULL,
        [created_at] datetime2 NOT NULL,
        [created_by] int NULL,
        [updated_at] datetime2 NULL,
        [updated_by] int NULL,
        [is_deleted] bit NOT NULL DEFAULT CAST(0 AS bit),
        [deleted_at] datetime2 NULL,
        [deleted_by] int NULL,
        CONSTRAINT [PK_DeviceCheck] PRIMARY KEY ([id]),
        CONSTRAINT [FK_DeviceCheck_StudentSession_student_session_id] FOREIGN KEY ([student_session_id]) REFERENCES [StudentSession] ([id]) ON DELETE CASCADE
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260806184820_AddDeviceChecks'
)
BEGIN
    CREATE TABLE [DeviceCheckRequirement] (
        [id] int NOT NULL IDENTITY,
        [device_check_id] int NOT NULL,
        [requirement_id] nvarchar(50) NOT NULL,
        [status] nvarchar(10) NOT NULL,
        [detail] nvarchar(200) NULL,
        [created_at] datetime2 NOT NULL,
        [created_by] int NULL,
        [updated_at] datetime2 NULL,
        [updated_by] int NULL,
        [is_deleted] bit NOT NULL DEFAULT CAST(0 AS bit),
        [deleted_at] datetime2 NULL,
        [deleted_by] int NULL,
        CONSTRAINT [PK_DeviceCheckRequirement] PRIMARY KEY ([id]),
        CONSTRAINT [FK_DeviceCheckRequirement_DeviceCheck_device_check_id] FOREIGN KEY ([device_check_id]) REFERENCES [DeviceCheck] ([id]) ON DELETE CASCADE
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260806184820_AddDeviceChecks'
)
BEGIN
    CREATE INDEX [IX_DeviceCheck_student_session_id_received_at_utc] ON [DeviceCheck] ([student_session_id], [received_at_utc]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260806184820_AddDeviceChecks'
)
BEGIN
    CREATE INDEX [IX_DeviceCheckRequirement_device_check_id] ON [DeviceCheckRequirement] ([device_check_id]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260806184820_AddDeviceChecks'
)
BEGIN
    CREATE INDEX [IX_DeviceCheckRequirement_requirement_id_status] ON [DeviceCheckRequirement] ([requirement_id], [status]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260806184820_AddDeviceChecks'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260806184820_AddDeviceChecks', N'8.0.14');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260806212850_AddMaxWarningsBeforeTermination'
)
BEGIN
    ALTER TABLE [SystemSettings] ADD [max_warnings_before_termination] int NOT NULL DEFAULT 3;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260806212850_AddMaxWarningsBeforeTermination'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260806212850_AddMaxWarningsBeforeTermination', N'8.0.14');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260807164856_BackfillPublishedQuestionBankStatus'
)
BEGIN
    UPDATE QuestionBank SET status = 'Locked' WHERE status = 'Published';
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260807164856_BackfillPublishedQuestionBankStatus'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260807164856_BackfillPublishedQuestionBankStatus', N'8.0.14');
END;
GO

COMMIT;
GO

