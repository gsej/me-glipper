# Medications

The medications me-glipper knows about live in the `Medications` table. It is shared
reference data: the same for every user, not editable from the app, and not part of
backup/restore. The rows are created by migrations (`code/dotnet/Api/Migrations/`,
starting with `0002_AddMedications.sql`).

Doses refer to their medication by `MedicationId`. The API and backups use the
medication's name, which the server looks up, ignoring case. An unknown name is rejected.

## Columns

| Column | Meaning |
|---|---|
| `Name` | Generic drug name, unique (ignoring case). This is what doses are recorded and shown against. |
| `DefaultDosingIntervalDays` | Days between doses, used for "next dose due". |
| `HalfLifeHours` | Elimination half-life (t½). |
| `Bioavailability` | Fraction of a subcutaneous dose that reaches the circulation (F). |
| `TmaxHours` | Time to peak after a single dose. Only used by the first-order absorption model, which derives the absorption rate from it. |
| `ChartColor` | Hex colour for the medication's chart. |
| `DisplayOrder` | Order in lists and on the charts. |

The pharmacokinetic values are population averages and some are uncertain. They drive
the estimated levels, which are an approximation, not a measurement.

## Values and sources

All three are given subcutaneously.

### Semaglutide

- t½ ≈ 165 h (~7 days), F ≈ 0.89.
- Tmax ≈ 48 h. This is an approximate placeholder; the label gives a range of 24–72 h.
- Sources:
  - FDA prescribing information for Ozempic/Wegovy, §12.3 Pharmacokinetics.
  - Carlsson Petri et al., "Semaglutide s.c. once-weekly in type 2 diabetes: a population
    pharmacokinetic analysis", *Diabetes Therapy* 2018;9(4):1533–1547.

### Tirzepatide

- t½ ≈ 117 h (~5 days), F ≈ 0.80.
- Tmax ≈ 24 h. This is an approximate placeholder; the label gives a range of 8–72 h.
- Sources:
  - FDA prescribing information for Mounjaro/Zepbound, §12.3 Pharmacokinetics.
  - Schneck & Urva, "Population pharmacokinetics of the GIP/GLP receptor agonist
    tirzepatide", *CPT: Pharmacometrics & Systems Pharmacology* 2024;13(3):494–503.

### Retatrutide

All the parameters are approximate, because only early trial data is available.

- t½ ≈ 144 h (~6 days).
- F is not well published, so it is set to 1.0.
- Tmax ≈ 36 h. This is an approximate placeholder; it is not well published.
- Sources:
  - Urva et al., *Lancet* 2022;400(10366):1869–1881 (phase 1b MAD trial).
  - Jastreboff et al., *NEJM* 2023;389(6):514–526 (PK in the supplementary appendix).

## Changing a value or adding a medication

Always add a new migration, never edit one that has been deployed:

- **Change a value:** `UPDATE Medications SET HalfLifeHours = … WHERE Name = '…';`
- **Add a medication:** `INSERT INTO Medications (…) VALUES (…);` with a new
  `MedicationId` and `DisplayOrder`.

Update this file to match, including the source for any new value.
