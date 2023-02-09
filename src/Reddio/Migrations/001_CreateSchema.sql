BEGIN TRANSACTION;

CREATE TABLE Metadata (
    LastImport TEXT NOT NULL,
    CONSTRAINT CK_Metadata_LastImport CHECK (TRIM(LastImport) != '')
);

CREATE TABLE Station (
    Id INTEGER NOT NULL,
    Name TEXT NOT NULL,
    DisplayOrder INTEGER NOT NULL,
    CONSTRAINT PK_Station PRIMARY KEY (Id AUTOINCREMENT),
    CONSTRAINT UQ_Station_DisplayOrder UNIQUE (DisplayOrder),
    CONSTRAINT CK_Station_Name CHECK (TRIM(Name) != '')
);

CREATE UNIQUE INDEX IX_Station_Name ON Station (Name);

CREATE UNIQUE INDEX IX_Station_DisplayOrder ON Station (DisplayOrder);

CREATE TABLE Track (
    Id INTEGER NOT NULL,
    StationId INTEGER NOT NULL,
    ThreadId TEXT NOT NULL,
    Title TEXT NOT NULL,
    Url TEXT NOT NULL,
    CONSTRAINT PK_Track PRIMARY KEY (Id AUTOINCREMENT),
    CONSTRAINT FK_Track_Station FOREIGN KEY (StationId) REFERENCES Station (Id) ON DELETE CASCADE,
    CONSTRAINT UQ_Track_StationId_ThreadId UNIQUE (StationId, ThreadId) ON CONFLICT IGNORE,
    CONSTRAINT CK_Track_ThreadId CHECK (TRIM(ThreadId) != ''),
    CONSTRAINT CK_Track_Title CHECK (TRIM(Title) != ''),
    CONSTRAINT CK_Track_Url CHECK (TRIM(Url) != '')
);

CREATE INDEX IX_Track_StationId ON Track (StationId);

CREATE INDEX IX_Track_ThreadId ON Track (ThreadId);

CREATE TABLE KnownDomain (
    Id INTEGER NOT NULL,
    Domain TEXT NOT NULL,
    CONSTRAINT PK_KnownDomain PRIMARY KEY (Id AUTOINCREMENT),
    CONSTRAINT UQ_KnownDomain_Domain UNIQUE (Domain),
    CONSTRAINT CK_KnownDomain_Domain CHECK (TRIM(Domain) != '')
);

COMMIT;
