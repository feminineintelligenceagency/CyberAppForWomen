/* 
   HOW TO USE:
     1. View > SQL Server Object Explorer
     2. Expand SQL Server > (localdb)\ProjectModels
     3. Right-click the server > New Query...
     4. Paste the following queries or juist what you need then run

     Then make sure Web.config's DefaultConnection points at this database:
       Data Source=(localdb)\ProjectModels;Initial Catalog=CyberAppDB;Integrated Security=True


   Contents (create in order):
     001  Users, UserCredentials
     002  AuditLog, Microcourses (+ tags, rules, prerequisites), Events, EventCourses
     003  Quiz rulesets/questions/options, QuizProgress, QuizResults (+ scores, items)
     004  MFA columns on UserCredentials, UserMfaBackupCodes
     005  PasswordResetTokens
     006  Story #18 workplace courses: microcourses, quiz domains, questions q41-q46
     007  Certificates (certificates of completion + verification numbers)
     008  ParticipantBadges

   Creates EMPTY tables (plus the Story #18 course/quiz rows). To copy the existing
   App_Data XML data in, run Import-AllData.ps1 afterwards.
*/

IF DB_ID(N'CyberAppDB') IS NULL
BEGIN
    CREATE DATABASE CyberAppDB;
END
GO

USE CyberAppDB;
GO


/*
   001_CreateUsersTables.sql
   Creates the Users and UserCredentials tables.
*/

IF OBJECT_ID(N'dbo.Users', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.Users
    (
        -- Same 32-character id the app already uses (Guid.ToString("N")), so
        -- Session["UserId"] and ids in existing audit logs keep working.
        UserId                CHAR(32)       NOT NULL,

        -- Case-insensitive collation: Test@Gmail.com and test@gmail.com count as
        -- the same email for the unique constraint. Stored lowercased by the app.
        Email                 NVARCHAR(256)  COLLATE SQL_Latin1_General_CP1_CI_AS NOT NULL,

        FirstName             NVARCHAR(100)  NOT NULL,
        LastName              NVARCHAR(100)  NOT NULL,
        Role                  VARCHAR(20)    NOT NULL,

        -- Plain text for now, matching users.xml. Can become a foreign key to a
        -- Universities table in a later migration.
        University            NVARCHAR(200)  NULL,

        CreatedAtUtc          DATETIME2(3)   NOT NULL CONSTRAINT DF_Users_CreatedAtUtc DEFAULT SYSUTCDATETIME(),
        ConsentAcceptedAtUtc  DATETIME2(3)   NULL,
        ConsentIp             VARCHAR(45)    NULL,   -- 45 chars fits an IPv6 address

        CONSTRAINT PK_Users PRIMARY KEY (UserId),
        CONSTRAINT UQ_Users_Email UNIQUE (Email),
        CONSTRAINT CK_Users_UserId CHECK (LEN(UserId) = 32 AND UserId NOT LIKE '%[^0-9a-f]%'),
        CONSTRAINT CK_Users_Role CHECK (Role IN ('Participant', 'Helper', 'UniversityAdmin', 'SuperAdmin'))
    );
END
GO

IF OBJECT_ID(N'dbo.UserCredentials', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.UserCredentials
    (
        -- One row per user. Deleting a user deletes their credentials too.
        UserId                CHAR(32)       NOT NULL,

        -- Raw bytes (not Base64). Argon2 and PBKDF2 hashes are 32 bytes; salts are 16.
        PasswordHash          VARBINARY(64)  NOT NULL,
        PasswordSalt          VARBINARY(64)  NOT NULL,

        -- 'pbkdf2' for legacy accounts, 'Argon2' (passHasher.Argon2ID) for upgraded ones.
        Algorithm             VARCHAR(20)    NOT NULL,

        -- Argon2 settings the hash was created with. NULL for PBKDF2 hashes.
        Argon2MemoryKiB       INT            NULL,
        Argon2Iterations      INT            NULL,
        Argon2Parallelism     INT            NULL,

        -- Login rate limiting / lockout.
        FailedLoginCount      INT            NOT NULL CONSTRAINT DF_UserCredentials_FailedLoginCount DEFAULT 0,
        LastFailedLoginUtc    DATETIME2(3)   NULL,
        LockoutUntilUtc       DATETIME2(3)   NULL,

        PasswordUpdatedAtUtc  DATETIME2(3)   NOT NULL CONSTRAINT DF_UserCredentials_PasswordUpdatedAtUtc DEFAULT SYSUTCDATETIME(),

        CONSTRAINT PK_UserCredentials PRIMARY KEY (UserId),
        CONSTRAINT FK_UserCredentials_Users FOREIGN KEY (UserId)
            REFERENCES dbo.Users (UserId) ON DELETE CASCADE,
        CONSTRAINT CK_UserCredentials_Algorithm CHECK (Algorithm IN ('pbkdf2', 'Argon2', 'argon2id')),
        CONSTRAINT CK_UserCredentials_Argon2Params CHECK (
            Algorithm = 'pbkdf2'
            OR (Argon2MemoryKiB > 0 AND Argon2Iterations > 0 AND Argon2Parallelism > 0)
        ),
        CONSTRAINT CK_UserCredentials_FailedLoginCount CHECK (FailedLoginCount >= 0)
    );
END
GO


/*
   002_CreateBatch1Tables.sql
   Batch 1: AuditLog, Microcourses,
            Events, EventCourses.

*/

/* 
   AuditLog  (from Audit_Log/UnvAdminAudit.xml)
*/
IF OBJECT_ID(N'dbo.AuditLog', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.AuditLog
    (
        -- Same "log-<32 hex>" format the app already generates.
        LogId         VARCHAR(40)    NOT NULL
                      CONSTRAINT DF_AuditLog_LogId
                      DEFAULT ('log-' + LOWER(REPLACE(CONVERT(VARCHAR(36), NEWID()), '-', ''))),
        TimestampUtc  DATETIME2(3)   NOT NULL CONSTRAINT DF_AuditLog_TimestampUtc DEFAULT SYSUTCDATETIME(),
        Type          NVARCHAR(100)  NOT NULL,   -- e.g. "Sign In", "Account Created", "Sign In Failed (Bad Password)"
        UserId        CHAR(32)       NULL,       -- filled in by new code; NULL for imported entries and failed logins
        Email         NVARCHAR(256)  NULL,
        FirstName     NVARCHAR(100)  NULL,
        Role          VARCHAR(30)    NULL,       -- includes "Unknown" for failed sign-ins
        University    NVARCHAR(200)  NULL,
        Details       NVARCHAR(MAX)  NULL,

        CONSTRAINT PK_AuditLog PRIMARY KEY NONCLUSTERED (LogId)
    );

    -- Logs are almost always read newest-first or by date range.
    CREATE CLUSTERED INDEX IX_AuditLog_TimestampUtc ON dbo.AuditLog (TimestampUtc);
    -- UniversityAdmin view: "entries for my university since X".
    CREATE INDEX IX_AuditLog_University_TimestampUtc ON dbo.AuditLog (University, TimestampUtc);
END
GO

/* 
   Microcourses  (from microcourses.xml)
*/
IF OBJECT_ID(N'dbo.Microcourses', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.Microcourses
    (
        CourseId         CHAR(32)       NOT NULL,
        Title            NVARCHAR(200)  NOT NULL,
        Summary          NVARCHAR(MAX)  NULL,
        Status           VARCHAR(20)    NOT NULL,   -- e.g. "Published", "Draft"
        DurationMinutes  INT            NULL,
        ExternalLink     NVARCHAR(500)  NULL,
        StartTimeUtc     DATETIME2(3)   NULL,
        EndTimeUtc       DATETIME2(3)   NULL,
        MaxParticipants  INT            NULL,
        CreatedAtUtc     DATETIME2(3)   NOT NULL CONSTRAINT DF_Microcourses_CreatedAtUtc DEFAULT SYSUTCDATETIME(),
        CreatedByEmail   NVARCHAR(256)  NULL,

        CONSTRAINT PK_Microcourses PRIMARY KEY (CourseId),
        CONSTRAINT CK_Microcourses_CourseId CHECK (LEN(CourseId) = 32 AND CourseId NOT LIKE '%[^0-9a-f]%'),
        CONSTRAINT CK_Microcourses_Duration CHECK (DurationMinutes IS NULL OR DurationMinutes > 0),
        CONSTRAINT CK_Microcourses_MaxParticipants CHECK (MaxParticipants IS NULL OR MaxParticipants > 0)
    );
END
GO

-- <tags><tag>privacy</tag>...</tags>
IF OBJECT_ID(N'dbo.MicrocourseTags', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.MicrocourseTags
    (
        CourseId  CHAR(32)      NOT NULL,
        Tag       NVARCHAR(50)  NOT NULL,

        CONSTRAINT PK_MicrocourseTags PRIMARY KEY (CourseId, Tag),
        CONSTRAINT FK_MicrocourseTags_Microcourses FOREIGN KEY (CourseId)
            REFERENCES dbo.Microcourses (CourseId) ON DELETE CASCADE
    );
    CREATE INDEX IX_MicrocourseTags_Tag ON dbo.MicrocourseTags (Tag);   -- "find courses tagged phishing"
END
GO

-- <requiredRules><rule id="4" /></requiredRules>
-- RuleId gets a foreign key once the rules file (certificationRules.xml or
-- quiz_rules.xml) has its own table.
IF OBJECT_ID(N'dbo.MicrocourseRequiredRules', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.MicrocourseRequiredRules
    (
        CourseId  CHAR(32)  NOT NULL,
        RuleId    INT       NOT NULL,

        CONSTRAINT PK_MicrocourseRequiredRules PRIMARY KEY (CourseId, RuleId),
        CONSTRAINT FK_MicrocourseRequiredRules_Microcourses FOREIGN KEY (CourseId)
            REFERENCES dbo.Microcourses (CourseId) ON DELETE CASCADE
    );
END
GO

-- Deleting a course that is another course's prerequisite is blocked on purpose.
IF OBJECT_ID(N'dbo.MicrocoursePrerequisites', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.MicrocoursePrerequisites
    (
        CourseId              CHAR(32)  NOT NULL,
        PrerequisiteCourseId  CHAR(32)  NOT NULL,

        CONSTRAINT PK_MicrocoursePrerequisites PRIMARY KEY (CourseId, PrerequisiteCourseId),
        CONSTRAINT FK_MicrocoursePrerequisites_Course FOREIGN KEY (CourseId)
            REFERENCES dbo.Microcourses (CourseId) ON DELETE CASCADE,
        CONSTRAINT FK_MicrocoursePrerequisites_Prerequisite FOREIGN KEY (PrerequisiteCourseId)
            REFERENCES dbo.Microcourses (CourseId),
        CONSTRAINT CK_MicrocoursePrerequisites_NotSelf CHECK (CourseId <> PrerequisiteCourseId)
    );
END
GO

/*
   Events  (from events.xml)
*/
IF OBJECT_ID(N'dbo.Events', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.Events
    (
        EventId         CHAR(32)       NOT NULL,
        Name            NVARCHAR(200)  NOT NULL,
        Description     NVARCHAR(MAX)  NULL,
        University      NVARCHAR(200)  NULL,
        Status          VARCHAR(20)    NOT NULL,   -- e.g. "Draft", "Published"
        StartDateUtc    DATETIME2(3)   NULL,
        EndDateUtc      DATETIME2(3)   NULL,
        CreatedAtUtc    DATETIME2(3)   NOT NULL CONSTRAINT DF_Events_CreatedAtUtc DEFAULT SYSUTCDATETIME(),
        CreatedByEmail  NVARCHAR(256)  NULL,

        CONSTRAINT PK_Events PRIMARY KEY (EventId),
        CONSTRAINT CK_Events_EventId CHECK (LEN(EventId) = 32 AND EventId NOT LIKE '%[^0-9a-f]%'),
        CONSTRAINT CK_Events_Dates CHECK (StartDateUtc IS NULL OR EndDateUtc IS NULL OR EndDateUtc >= StartDateUtc)
    );
    CREATE INDEX IX_Events_University_StartDateUtc ON dbo.Events (University, StartDateUtc);
END
GO

/*
   EventCourses  (from eventCourses.xml)
   Which microcourses an event offers, and whether each is turned on.
*/
IF OBJECT_ID(N'dbo.EventCourses', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.EventCourses
    (
        EventId    CHAR(32)  NOT NULL,
        CourseId   CHAR(32)  NOT NULL,
        IsEnabled  BIT       NOT NULL CONSTRAINT DF_EventCourses_IsEnabled DEFAULT 1,

        CONSTRAINT PK_EventCourses PRIMARY KEY (EventId, CourseId),
        CONSTRAINT FK_EventCourses_Events FOREIGN KEY (EventId)
            REFERENCES dbo.Events (EventId) ON DELETE CASCADE,
        CONSTRAINT FK_EventCourses_Microcourses FOREIGN KEY (CourseId)
            REFERENCES dbo.Microcourses (CourseId) ON DELETE CASCADE
    );
    CREATE INDEX IX_EventCourses_CourseId ON dbo.EventCourses (CourseId);
END
GO

/*
   003_CreateQuizTables.sql
   Batch 2: the risk-assessment quiz.

     QuizRulesets, QuizDomains            <- quiz_rules.xml
     QuizQuestions, QuizOptions,
     QuizOptionWeights                    <- quiz_questions.xml
     QuizProgress, QuizProgressAnswers    <- quiz_progress.xml
     QuizResults, QuizResultDomainScores,
     QuizResultItems                      <- quiz_results.xml

*/

/*
   Rulesets and domains (quiz_rules.xml)
*/
IF OBJECT_ID(N'dbo.QuizRulesets', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.QuizRulesets
    (
        Version       VARCHAR(10)   NOT NULL,   -- e.g. "2.0"
        CreatedAtUtc  DATETIME2(3)  NULL,       -- NULL for versions only seen on old results

        CONSTRAINT PK_QuizRulesets PRIMARY KEY (Version)
    );
END
GO

-- One row per security topic, e.g. social_privacy, phishing, spyware.
IF OBJECT_ID(N'dbo.QuizDomains', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.QuizDomains
    (
        DomainKey      VARCHAR(50)    NOT NULL,   -- the id="..." used by <Map module="..."/>
        Title          NVARCHAR(200)  NOT NULL,
        QuickWin       NVARCHAR(500)  NULL,
        Factor         NVARCHAR(500)  NULL,

        -- The microcourse that teaches this topic (matched by title on import),
        -- so results can recommend courses directly.
        CourseId       CHAR(32)       NULL,

        RulesetVersion VARCHAR(10)    NOT NULL,   -- ruleset this definition came from

        CONSTRAINT PK_QuizDomains PRIMARY KEY (DomainKey),
        CONSTRAINT UQ_QuizDomains_Title UNIQUE (Title),
        CONSTRAINT FK_QuizDomains_Microcourses FOREIGN KEY (CourseId)
            REFERENCES dbo.Microcourses (CourseId) ON DELETE SET NULL,
        CONSTRAINT FK_QuizDomains_QuizRulesets FOREIGN KEY (RulesetVersion)
            REFERENCES dbo.QuizRulesets (Version)
    );
END
GO

/*
   Questions, options and weights (quiz_questions.xml)
*/
IF OBJECT_ID(N'dbo.QuizQuestions', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.QuizQuestions
    (
        QuestionId    VARCHAR(20)     NOT NULL,   -- "q1", "q2", ...
        QuestionText  NVARCHAR(1000)  NOT NULL,
        SortOrder     INT             NOT NULL,

        -- Retire questions by setting this to 0 instead of deleting them:
        -- deleting is blocked once participants have answered.
        IsActive      BIT             NOT NULL CONSTRAINT DF_QuizQuestions_IsActive DEFAULT 1,

        CONSTRAINT PK_QuizQuestions PRIMARY KEY (QuestionId)
    );
END
GO

IF OBJECT_ID(N'dbo.QuizOptions', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.QuizOptions
    (
        QuestionId  VARCHAR(20)    NOT NULL,
        OptionKey   CHAR(1)        NOT NULL,   -- "A", "B", "C", "D"
        OptionText  NVARCHAR(500)  NOT NULL,

        CONSTRAINT PK_QuizOptions PRIMARY KEY (QuestionId, OptionKey),
        CONSTRAINT FK_QuizOptions_QuizQuestions FOREIGN KEY (QuestionId)
            REFERENCES dbo.QuizQuestions (QuestionId) ON DELETE CASCADE,
        CONSTRAINT CK_QuizOptions_OptionKey CHECK (OptionKey LIKE '[A-Z]')
    );
END
GO

-- <Map module="social_privacy" weight="3" /> : choosing this option adds
-- Weight points (can be negative) to that domain's risk score.
IF OBJECT_ID(N'dbo.QuizOptionWeights', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.QuizOptionWeights
    (
        QuestionId  VARCHAR(20)  NOT NULL,
        OptionKey   CHAR(1)      NOT NULL,
        DomainKey   VARCHAR(50)  NOT NULL,
        Weight      INT          NOT NULL,

        CONSTRAINT PK_QuizOptionWeights PRIMARY KEY (QuestionId, OptionKey, DomainKey),
        CONSTRAINT FK_QuizOptionWeights_QuizOptions FOREIGN KEY (QuestionId, OptionKey)
            REFERENCES dbo.QuizOptions (QuestionId, OptionKey) ON DELETE CASCADE,
        CONSTRAINT FK_QuizOptionWeights_QuizDomains FOREIGN KEY (DomainKey)
            REFERENCES dbo.QuizDomains (DomainKey)
    );
    CREATE INDEX IX_QuizOptionWeights_DomainKey ON dbo.QuizOptionWeights (DomainKey);
END
GO

/*
   In-progress / latest answers (quiz_progress.xml) - one per user
*/
IF OBJECT_ID(N'dbo.QuizProgress', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.QuizProgress
    (
        UserId          CHAR(32)      NOT NULL,
        RulesetVersion  VARCHAR(10)   NOT NULL,
        LastIndex       INT           NOT NULL CONSTRAINT DF_QuizProgress_LastIndex DEFAULT 0,  -- 0-based
        TotalQuestions  INT           NOT NULL,
        IsCompleted     BIT           NOT NULL CONSTRAINT DF_QuizProgress_IsCompleted DEFAULT 0,
        UpdatedAtUtc    DATETIME2(3)  NOT NULL CONSTRAINT DF_QuizProgress_UpdatedAtUtc DEFAULT SYSUTCDATETIME(),

        CONSTRAINT PK_QuizProgress PRIMARY KEY (UserId),
        CONSTRAINT FK_QuizProgress_Users FOREIGN KEY (UserId)
            REFERENCES dbo.Users (UserId) ON DELETE CASCADE,
        CONSTRAINT FK_QuizProgress_QuizRulesets FOREIGN KEY (RulesetVersion)
            REFERENCES dbo.QuizRulesets (Version),
        CONSTRAINT CK_QuizProgress_Counts CHECK (TotalQuestions >= 0 AND LastIndex >= 0)
    );
END
GO

IF OBJECT_ID(N'dbo.QuizProgressAnswers', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.QuizProgressAnswers
    (
        UserId      CHAR(32)     NOT NULL,
        QuestionId  VARCHAR(20)  NOT NULL,
        Choice      CHAR(1)      NOT NULL,

        CONSTRAINT PK_QuizProgressAnswers PRIMARY KEY (UserId, QuestionId),
        CONSTRAINT FK_QuizProgressAnswers_QuizProgress FOREIGN KEY (UserId)
            REFERENCES dbo.QuizProgress (UserId) ON DELETE CASCADE,
        -- Guarantees every stored answer is a real option of a real question.
        CONSTRAINT FK_QuizProgressAnswers_QuizOptions FOREIGN KEY (QuestionId, Choice)
            REFERENCES dbo.QuizOptions (QuestionId, OptionKey)
    );
END
GO

/*
   Completed results (quiz_results.xml) - a user can have several (retakes)
*/
IF OBJECT_ID(N'dbo.QuizResults', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.QuizResults
    (
        ResultId         INT IDENTITY(1,1) NOT NULL,
        UserId           CHAR(32)      NOT NULL,
        RulesetVersion   VARCHAR(10)   NOT NULL,
        CompletedAtUtc   DATETIME2(3)  NOT NULL,
        OverallScore     DECIMAL(5,2)  NOT NULL,
        ShareWithHelper  BIT           NOT NULL CONSTRAINT DF_QuizResults_ShareWithHelper DEFAULT 0,

        CONSTRAINT PK_QuizResults PRIMARY KEY (ResultId),
        CONSTRAINT UQ_QuizResults_User_Completed UNIQUE (UserId, CompletedAtUtc),
        CONSTRAINT FK_QuizResults_Users FOREIGN KEY (UserId)
            REFERENCES dbo.Users (UserId) ON DELETE CASCADE,
        CONSTRAINT FK_QuizResults_QuizRulesets FOREIGN KEY (RulesetVersion)
            REFERENCES dbo.QuizRulesets (Version)
    );
END
GO

IF OBJECT_ID(N'dbo.QuizResultDomainScores', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.QuizResultDomainScores
    (
        ResultId   INT           NOT NULL,
        DomainKey  VARCHAR(50)   NOT NULL,
        Score      DECIMAL(5,2)  NOT NULL,

        CONSTRAINT PK_QuizResultDomainScores PRIMARY KEY (ResultId, DomainKey),
        CONSTRAINT FK_QuizResultDomainScores_QuizResults FOREIGN KEY (ResultId)
            REFERENCES dbo.QuizResults (ResultId) ON DELETE CASCADE,
        CONSTRAINT FK_QuizResultDomainScores_QuizDomains FOREIGN KEY (DomainKey)
            REFERENCES dbo.QuizDomains (DomainKey)
    );
END
GO

-- The Top Factors and Quick Wins exactly as the participant saw them, so old
-- results don't change if the ruleset wording is edited later.
IF OBJECT_ID(N'dbo.QuizResultItems', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.QuizResultItems
    (
        ResultId   INT            NOT NULL,
        ItemType   VARCHAR(10)    NOT NULL,   -- 'Factor' or 'QuickWin'
        SortOrder  INT            NOT NULL,
        ItemText   NVARCHAR(500)  NOT NULL,

        CONSTRAINT PK_QuizResultItems PRIMARY KEY (ResultId, ItemType, SortOrder),
        CONSTRAINT FK_QuizResultItems_QuizResults FOREIGN KEY (ResultId)
            REFERENCES dbo.QuizResults (ResultId) ON DELETE CASCADE,
        CONSTRAINT CK_QuizResultItems_ItemType CHECK (ItemType IN ('Factor', 'QuickWin'))
    );
END
GO

/*
   004_AddMfaColumns.sql
   Adds two-factor (TOTP) authentication support to dbo.UserCredentials.

   - MfaEnabled : 1 when the user must enter a 6-digit code after their password
   - MfaSecret  : the user's TOTP secret, stored ENCRYPTED by the app
                  (never the raw Base32 secret)
   - MfaLastTimeStep : last authenticator time step accepted (replay protection)
   - UserMfaBackupCodes : one row per backup code, hashed, single use
*/

IF COL_LENGTH('dbo.UserCredentials', 'MfaEnabled') IS NULL
    ALTER TABLE dbo.UserCredentials
        ADD MfaEnabled BIT NOT NULL CONSTRAINT DF_UserCredentials_MfaEnabled DEFAULT 0;
GO

IF COL_LENGTH('dbo.UserCredentials', 'MfaSecret') IS NULL
    ALTER TABLE dbo.UserCredentials
        ADD MfaSecret NVARCHAR(512) NULL;
GO

-- Last authenticator time step used, so a code can't be replayed within its 30-90 second window.
IF COL_LENGTH('dbo.UserCredentials', 'MfaLastTimeStep') IS NULL
    ALTER TABLE dbo.UserCredentials
        ADD MfaLastTimeStep BIGINT NULL;
GO


IF NOT EXISTS (SELECT 1 FROM sys.check_constraints
               WHERE name = 'CK_UserCredentials_MfaSecret'
                 AND parent_object_id = OBJECT_ID('dbo.UserCredentials'))
    ALTER TABLE dbo.UserCredentials
        ADD CONSTRAINT CK_UserCredentials_MfaSecret CHECK (MfaEnabled = 0 OR MfaSecret IS NOT NULL);
GO

/*
   Backup codes
*/
IF OBJECT_ID(N'dbo.UserMfaBackupCodes', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.UserMfaBackupCodes
    (
        BackupCodeId  INT IDENTITY(1,1) NOT NULL,
        UserId        CHAR(32)       NOT NULL,
        CodeHash      VARBINARY(32)  NOT NULL,   -- SHA-256 of the normalized code
        CreatedAtUtc  DATETIME2(3)   NOT NULL CONSTRAINT DF_UserMfaBackupCodes_CreatedAtUtc DEFAULT SYSUTCDATETIME(),
        UsedAtUtc     DATETIME2(3)   NULL,

        CONSTRAINT PK_UserMfaBackupCodes PRIMARY KEY (BackupCodeId),
        CONSTRAINT FK_UserMfaBackupCodes_UserCredentials FOREIGN KEY (UserId)
            REFERENCES dbo.UserCredentials (UserId) ON DELETE CASCADE,
        CONSTRAINT UQ_UserMfaBackupCodes_User_Code UNIQUE (UserId, CodeHash)
    );
END
GO

/*
   005_CreatePasswordResetTokens.sql
   Password reset links (replaces App_Data/passwordResets.xml).

   - Only the SHA-256 hash of each token is stored, so someone reading the
     table can't use the links.
   - Each token expires (ExpiresAtUtc) and works once (UsedAtUtc).
   - Deleting a user deletes their tokens.

   Safe to run more than once. Works on LocalDB and Azure SQL.
*/

IF OBJECT_ID(N'dbo.PasswordResetTokens', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.PasswordResetTokens
    (
        TokenHash     BINARY(32)    NOT NULL,   -- SHA-256 of the token in the email link
        UserId        CHAR(32)      NOT NULL,
        CreatedAtUtc  DATETIME2(3)  NOT NULL CONSTRAINT DF_PasswordResetTokens_CreatedAtUtc DEFAULT SYSUTCDATETIME(),
        ExpiresAtUtc  DATETIME2(3)  NOT NULL,
        UsedAtUtc     DATETIME2(3)  NULL,       -- set when used OR cancelled by a newer link

        CONSTRAINT PK_PasswordResetTokens PRIMARY KEY (TokenHash),
        CONSTRAINT FK_PasswordResetTokens_Users FOREIGN KEY (UserId)
            REFERENCES dbo.Users (UserId) ON DELETE CASCADE,
        CONSTRAINT CK_PasswordResetTokens_Expiry CHECK (ExpiresAtUtc > CreatedAtUtc)
    );

    CREATE INDEX IX_PasswordResetTokens_UserId ON dbo.PasswordResetTokens (UserId);
END
GO

/*
   006_Story18_WorkplaceCourses.sql
   Story #18: courses on recognizing retaliation, setting boundaries, and documenting incidents.

   - Adds the three microcourses (same ids as App_Data/microcourses.xml)
   - Adds quiz domains retaliation / boundaries / documentation (same as App_Data/quiz_rules.xml)
   - Adds risk-quiz questions q41-q46 with their option weights (same as App_Data/quiz_questions.xml)

   Ruleset stays 2.0, so nobody is forced to retake the quiz. Participants get the new
   recommendations the next time they take it.

   Safe to run more than once, and safe on a brand-new database: the quiz ruleset row
   is created if missing, and weights for domains that don't exist yet (e.g. social_privacy
   before the XML import) are skipped. Import-Batch2.ps1 fills those in afterwards.
*/

-- Ruleset 2.0 must exist before domains can point to it (empty on a brand-new database).
IF NOT EXISTS (SELECT 1 FROM dbo.QuizRulesets WHERE Version = '2.0')
    INSERT INTO dbo.QuizRulesets (Version, CreatedAtUtc) VALUES ('2.0', NULL);
GO

-- Microcourses
INSERT INTO dbo.Microcourses (CourseId, Title, Summary, Status, DurationMinutes, CreatedAtUtc, CreatedByEmail)
SELECT v.CourseId, v.Title, v.Summary, 'Published', 50, '2026-11-17T00:00:00', NULL
  FROM (VALUES
        ('dac2f400336d484a9486ed20261d359c', N'Recognizing Retaliation at Work',
         N'Learn to tell a normal work change from retaliation: lost shifts, worse tasks, sudden write-ups, or being left out right after you said no or spoke up. Includes a real-world workplace example and a hands-on timeline you build on your own phone.'),
        ('d89b58c2a48a41e4973850510ea3ef0d', N'Setting Boundaries in Work Group Chats',
         N'Say a clear, calm "no" to unwanted comments in work group chats, control what coworkers can see on your personal social media, and know when to escalate. Includes a real-world workplace example and hands-on privacy steps.'),
        ('99a6567f5ab846a78535e5894fed7bcd', N'Documenting Incidents',
         N'Write dated incident notes, save screenshots that show who, what, and when, and keep your records somewhere work can''t take away. Includes a real-world workplace example and a ready-to-use incident log template.')
       ) AS v (CourseId, Title, Summary)
 WHERE NOT EXISTS (SELECT 1 FROM dbo.Microcourses m WHERE m.CourseId = v.CourseId);

INSERT INTO dbo.MicrocourseTags (CourseId, Tag)
SELECT v.CourseId, v.Tag
  FROM (VALUES
        ('dac2f400336d484a9486ed20261d359c', N'workplace'), ('dac2f400336d484a9486ed20261d359c', N'retaliation'), ('dac2f400336d484a9486ed20261d359c', N'safety'),
        ('d89b58c2a48a41e4973850510ea3ef0d', N'workplace'), ('d89b58c2a48a41e4973850510ea3ef0d', N'boundaries'),  ('d89b58c2a48a41e4973850510ea3ef0d', N'social-media'),
        ('99a6567f5ab846a78535e5894fed7bcd', N'workplace'), ('99a6567f5ab846a78535e5894fed7bcd', N'documentation'), ('99a6567f5ab846a78535e5894fed7bcd', N'safety')
       ) AS v (CourseId, Tag)
 WHERE NOT EXISTS (SELECT 1 FROM dbo.MicrocourseTags t WHERE t.CourseId = v.CourseId AND t.Tag = v.Tag);
GO

-- Quiz domains
INSERT INTO dbo.QuizDomains (DomainKey, Title, QuickWin, Factor, CourseId, RulesetVersion)
SELECT v.DomainKey, v.Title, v.QuickWin, v.Factor, v.CourseId, '2.0'
  FROM (VALUES
        ('retaliation', N'Recognizing Retaliation at Work',
         N'Write down the date you said no or spoke up, and every work change since.',
         N'Lost shifts or worse treatment after saying no or reporting can be retaliation.',
         'dac2f400336d484a9486ed20261d359c'),
        ('boundaries', N'Setting Boundaries in Work Group Chats',
         N'Save a one-sentence boundary in your notes; make personal accounts private.',
         N'Unwanted comments and personal contact from coworkers go unchecked.',
         'd89b58c2a48a41e4973850510ea3ef0d'),
        ('documentation', N'Documenting Incidents',
         N'Start a private, dated work log on your own phone and save screenshots.',
         N'Without dated records, incidents are easy to dismiss.',
         '99a6567f5ab846a78535e5894fed7bcd')
       ) AS v (DomainKey, Title, QuickWin, Factor, CourseId)
 WHERE NOT EXISTS (SELECT 1 FROM dbo.QuizDomains d WHERE d.DomainKey = v.DomainKey);
GO

-- Quiz questions
INSERT INTO dbo.QuizQuestions (QuestionId, QuestionText, SortOrder, IsActive)
SELECT v.QuestionId, v.QuestionText, v.SortOrder, 1
  FROM (VALUES
        ('q41', N'In the past year, after you said no to someone at work (a date, a favor, extra hours), did anything change for you, like fewer shifts, worse schedules, or being left out?', 41),
        ('q42', N'Has a manager or coworker treated you worse after you reported a problem or set a limit with them?', 42),
        ('q43', N'Has a coworker or manager followed or messaged you on your personal social media when you did not want them to?', 43),
        ('q44', N'In work group chats, how often are comments about your looks, body, or personal life brushed off as "just jokes"?', 44),
        ('q45', N'If a message in a work chat crossed a line for you, how confident are you that you would know what to say?', 45),
        ('q46', N'If something at work made you uncomfortable, would you keep a record of it (dates, screenshots, who was there)?', 46)
       ) AS v (QuestionId, QuestionText, SortOrder)
 WHERE NOT EXISTS (SELECT 1 FROM dbo.QuizQuestions q WHERE q.QuestionId = v.QuestionId);

INSERT INTO dbo.QuizOptions (QuestionId, OptionKey, OptionText)
SELECT v.QuestionId, v.OptionKey, v.OptionText
  FROM (VALUES
        ('q41', 'A', N'Yes, and it started soon after I said no'),
        ('q41', 'B', N'Maybe, but I am not sure if it is connected'),
        ('q41', 'C', N'No'),
        ('q41', 'D', N'I prefer not to say'),
        ('q42', 'A', N'Yes'),
        ('q42', 'B', N'It might have happened once or twice'),
        ('q42', 'C', N'No'),
        ('q42', 'D', N'I prefer not to say'),
        ('q43', 'A', N'Yes, and I did not feel I could say no'),
        ('q43', 'B', N'Yes, but I handled it the way I wanted'),
        ('q43', 'C', N'No'),
        ('q43', 'D', N'I prefer not to say'),
        ('q44', 'A', N'Often'),
        ('q44', 'B', N'Sometimes'),
        ('q44', 'C', N'Rarely or never'),
        ('q44', 'D', N'I am not in work group chats or prefer not to say'),
        ('q45', 'A', N'Not confident, I would probably stay quiet'),
        ('q45', 'B', N'Somewhat confident'),
        ('q45', 'C', N'Very confident'),
        ('q45', 'D', N'I prefer not to say'),
        ('q46', 'A', N'No, I would not know what to save'),
        ('q46', 'B', N'Sometimes, but it would be scattered'),
        ('q46', 'C', N'Yes, dated notes and screenshots saved somewhere private'),
        ('q46', 'D', N'I prefer not to say')
       ) AS v (QuestionId, OptionKey, OptionText)
 WHERE NOT EXISTS (SELECT 1 FROM dbo.QuizOptions o WHERE o.QuestionId = v.QuestionId AND o.OptionKey = v.OptionKey);

INSERT INTO dbo.QuizOptionWeights (QuestionId, OptionKey, DomainKey, Weight)
SELECT v.QuestionId, v.OptionKey, v.DomainKey, v.Weight
  FROM (VALUES
        ('q41', 'A', 'retaliation', 4),   ('q41', 'A', 'documentation', 2),
        ('q41', 'B', 'retaliation', 3),   ('q41', 'B', 'documentation', 2),
        ('q41', 'C', 'retaliation', -1),
        ('q41', 'D', 'retaliation', 0),
        ('q42', 'A', 'retaliation', 3),   ('q42', 'A', 'documentation', 2),
        ('q42', 'B', 'retaliation', 2),   ('q42', 'B', 'documentation', 1),
        ('q42', 'C', 'retaliation', -1),
        ('q42', 'D', 'retaliation', 0),
        ('q43', 'A', 'boundaries', 4),    ('q43', 'A', 'documentation', 1), ('q43', 'A', 'social_privacy', 1),
        ('q43', 'B', 'boundaries', 1),
        ('q43', 'C', 'boundaries', -1),
        ('q43', 'D', 'boundaries', 0),
        ('q44', 'A', 'boundaries', 4),    ('q44', 'A', 'documentation', 2),
        ('q44', 'B', 'boundaries', 2),    ('q44', 'B', 'documentation', 1),
        ('q44', 'C', 'boundaries', -1),
        ('q44', 'D', 'boundaries', 0),
        ('q45', 'A', 'boundaries', 3),
        ('q45', 'B', 'boundaries', 1),
        ('q45', 'C', 'boundaries', -2),
        ('q45', 'D', 'boundaries', 0),
        ('q46', 'A', 'documentation', 4),
        ('q46', 'B', 'documentation', 2),
        ('q46', 'C', 'documentation', -2),
        ('q46', 'D', 'documentation', 0)
       ) AS v (QuestionId, OptionKey, DomainKey, Weight)
 WHERE NOT EXISTS (SELECT 1 FROM dbo.QuizOptionWeights w
                    WHERE w.QuestionId = v.QuestionId AND w.OptionKey = v.OptionKey AND w.DomainKey = v.DomainKey)
   AND EXISTS (SELECT 1 FROM dbo.QuizDomains d WHERE d.DomainKey = v.DomainKey);   -- skip domains not created yet
GO

/* =====================================================================
   007_CreateCertificates.sql
   ===================================================================== */
IF OBJECT_ID(N'dbo.Certificates', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.Certificates
    (
        CertificateId     INT IDENTITY(1,1) NOT NULL,
        VerificationCode  CHAR(18)       NOT NULL,   -- FIA-XXXX-XXXX-XXXX
        UserId            CHAR(32)       NOT NULL,
        CourseId          NVARCHAR(100)  NOT NULL,
        CourseTitle       NVARCHAR(200)  NOT NULL,
        RecipientName     NVARCHAR(201)  NOT NULL,
        IssuedAtUtc       DATETIME2(3)   NOT NULL CONSTRAINT DF_Certificates_IssuedAtUtc DEFAULT SYSUTCDATETIME(),

        CONSTRAINT PK_Certificates PRIMARY KEY (CertificateId),
        CONSTRAINT UQ_Certificates_VerificationCode UNIQUE (VerificationCode),
        CONSTRAINT UQ_Certificates_User_Course UNIQUE (UserId, CourseId),   -- one certificate per course
        CONSTRAINT FK_Certificates_Users FOREIGN KEY (UserId)
            REFERENCES dbo.Users (UserId) ON DELETE CASCADE
    );

    CREATE INDEX IX_Certificates_UserId ON dbo.Certificates (UserId);
END
GO

/* =====================================================================
   008_CreateParticipantBadges.sql
   ===================================================================== */
IF OBJECT_ID(N'dbo.ParticipantBadges', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.ParticipantBadges
    (
        BadgeId       INT IDENTITY(1,1) NOT NULL,
        UserId        CHAR(32)       NOT NULL,
        CourseId      NVARCHAR(100)  NOT NULL,
        CourseTitle   NVARCHAR(200)  NOT NULL,
        ImagePath     NVARCHAR(400)  NULL,
        AwardedAtUtc  DATETIME2(3)   NOT NULL CONSTRAINT DF_ParticipantBadges_AwardedAtUtc DEFAULT SYSUTCDATETIME(),

        CONSTRAINT PK_ParticipantBadges PRIMARY KEY (BadgeId),
        CONSTRAINT UQ_ParticipantBadges_User_Course UNIQUE (UserId, CourseId),   -- one badge per course
        CONSTRAINT FK_ParticipantBadges_Users FOREIGN KEY (UserId)
            REFERENCES dbo.Users (UserId) ON DELETE CASCADE
    );
END
GO

SELECT TABLE_NAME FROM INFORMATION_SCHEMA.TABLES
 WHERE TABLE_TYPE = 'BASE TABLE'
 ORDER BY TABLE_NAME;
GO
