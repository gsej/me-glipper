-- Each user's current vial of a medication: how much medication it was made up with (mg)
-- and in how much liquid (ml), used to work out how many ml a dose of so many mg is.
-- VialId is the key so a vial history can be added later by dropping the UNIQUE constraint
-- (and adding e.g. a StartedAt column); for now each user has at most one vial per medication.
-- Amounts are stored as invariant-culture text, like Doses.DoseAmountMg. StartedOn is the
-- calendar date the vial was started (yyyy-MM-dd, no time or timezone).

CREATE TABLE Vials (
    VialId TEXT NOT NULL PRIMARY KEY,
    UserId TEXT NOT NULL,
    MedicationId INTEGER NOT NULL REFERENCES Medications (MedicationId),
    InitialAmountMg TEXT NOT NULL,
    InitialVolumeMl TEXT NOT NULL,
    StartedOn TEXT NOT NULL,
    UNIQUE (UserId, MedicationId)
);
