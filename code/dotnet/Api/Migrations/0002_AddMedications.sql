-- The medications the app knows about: shared reference data (not per user), not editable
-- from the app and not part of backup/restore. Change a value or add a medication with a new
-- migration. Sources and caveats for the pharmacokinetic values: docs/medications.md.

CREATE TABLE Medications (
    MedicationId INTEGER NOT NULL PRIMARY KEY,
    Name TEXT NOT NULL UNIQUE COLLATE NOCASE,
    DefaultDosingIntervalDays REAL NOT NULL,
    HalfLifeHours REAL NOT NULL,
    Bioavailability REAL NOT NULL,
    TmaxHours REAL NOT NULL,
    ChartColor TEXT NOT NULL,
    DisplayOrder INTEGER NOT NULL
);

INSERT INTO Medications
    (MedicationId, Name, DefaultDosingIntervalDays, HalfLifeHours, Bioavailability, TmaxHours, ChartColor, DisplayOrder)
VALUES
    (1, 'Semaglutide', 7, 165, 0.89, 48, '#3b82f6', 1),
    (2, 'Tirzepatide', 5, 117, 0.80, 24, '#10b981', 2),
    (3, 'Retatrutide', 6, 144, 1.0,  36, '#8b5cf6', 3);
