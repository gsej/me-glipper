-- Doses refer to their medication by MedicationId (a foreign key) instead of by name.
-- SQLite cannot add a foreign key to an existing table, so the table is rebuilt.
--
-- Each dose's id is looked up from its name, ignoring case (Medications.Name is
-- COLLATE NOCASE). A name with no matching medication gives NULL, which fails the
-- NOT NULL constraint: the script's transaction rolls back, leaving the data as it was,
-- and startup fails.

CREATE TABLE Doses_new (
    DoseId TEXT NOT NULL PRIMARY KEY,
    UserId TEXT NOT NULL,
    TakenAt TEXT NOT NULL,
    MedicationId INTEGER NOT NULL REFERENCES Medications (MedicationId),
    DoseAmountMg TEXT NOT NULL,
    Deleted INTEGER NOT NULL DEFAULT 0,
    Notes TEXT
);

INSERT INTO Doses_new (DoseId, UserId, TakenAt, MedicationId, DoseAmountMg, Deleted, Notes)
SELECT d.DoseId,
       d.UserId,
       d.TakenAt,
       (SELECT m.MedicationId FROM Medications m WHERE m.Name = d.MedicationName),
       d.DoseAmountMg,
       d.Deleted,
       d.Notes
FROM Doses d;

DROP TABLE Doses;

ALTER TABLE Doses_new RENAME TO Doses;
