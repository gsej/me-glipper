-- Baseline: the schema as it was before DbUp, when each repository created its own
-- table on startup. IF NOT EXISTS so it is a no-op on databases created that way, and
-- only gets recorded in SchemaVersions there.

CREATE TABLE IF NOT EXISTS Doses (
    DoseId TEXT NOT NULL PRIMARY KEY,
    UserId TEXT NOT NULL,
    TakenAt TEXT NOT NULL,
    MedicationName TEXT NOT NULL,
    DoseAmountMg TEXT NOT NULL,
    Deleted INTEGER NOT NULL DEFAULT 0,
    Notes TEXT
);

CREATE TABLE IF NOT EXISTS Users (
    UserId TEXT NOT NULL PRIMARY KEY
);
