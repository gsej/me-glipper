import { Component, OnInit, ChangeDetectionStrategy, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import {
  AbstractControl,
  FormBuilder,
  FormsModule,
  ReactiveFormsModule,
  ValidationErrors,
  Validators,
} from '@angular/forms';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { ActivatedRoute, Router } from '@angular/router';
import { catchError, forkJoin, map, of } from 'rxjs';
import { MedicationService } from '../../services/medication.service';
import { LevelsService } from '../../services/levels.service';
import { VialService, Vial } from '../../services/vial.service';
import { DoseService, DoseRecord } from '../../services/dose.service';
import { VialDoseCalculatorComponent } from '../vial-dose-calculator/vial-dose-calculator.component';

/** The values the form holds: null when an input is blank. */
interface VialValues {
  initialAmountMg: number | null;
  initialVolumeMl: number | null;
  // yyyy-MM-dd.
  startedOn: string | null;
}

/**
 * The user's current vial of one medication, chosen from a dropdown: how much medication it
 * was made up with (mg), in how much liquid (ml), the date it was started and (read-only) its
 * age in days. The values can be edited and saved, or reset to the last saved values.
 * Like the Medication page, the selection follows the URL's `medication` query parameter, so
 * the two pages show the same medication.
 * Below the form, a dose calculator works from the saved vial.
 */
@Component({
  selector: 'app-vial-page',
  imports: [CommonModule, FormsModule, ReactiveFormsModule, VialDoseCalculatorComponent],
  templateUrl: './vial-page.component.html',
  changeDetection: ChangeDetectionStrategy.Eager,
  styleUrls: ['./vial-page.component.scss'],
})
export class VialPageComponent implements OnInit {
  private readonly medicationService = inject(MedicationService);
  private readonly levelsService = inject(LevelsService);
  private readonly vialService = inject(VialService);
  private readonly doseService = inject(DoseService);
  private readonly router = inject(Router);
  private readonly route = inject(ActivatedRoute);
  private readonly fb = inject(FormBuilder);

  medications: string[] = [];
  selected = '';
  loadError = false;

  status: 'Saved' | 'Error' | null = null;
  errorStatusCode: string | null = null;
  saving = false;
  private statusTimer: ReturnType<typeof setTimeout> | undefined;

  form = this.fb.group({
    initialAmountMg: this.fb.control<number | null>(null, [Validators.required, Validators.min(0.001)]),
    initialVolumeMl: this.fb.control<number | null>(null, [Validators.required, Validators.min(0.001)]),
    startedOn: this.fb.control<string | null>(null, [Validators.required, notAfterToday]),
  });

  // The saved vials, by medication name.
  private vials = new Map<string, Vial>();

  // The medication named in the URL, kept until the list has loaded and it can be applied.
  private requested: string | null = null;

  // All doses, most recent first. Loaded (and refreshed after a dose is added) by the
  // History page, which is always rendered, so this page makes no request of its own.
  private doses: DoseRecord[] = [];

  constructor() {
    this.doseService.doseRecords$
      .pipe(takeUntilDestroyed())
      .subscribe((doses) => (this.doses = doses));
    this.route.queryParams.pipe(takeUntilDestroyed()).subscribe((params) => {
      this.requested = params['medication'] ?? null;
      this.applyRequested();
    });
  }

  ngOnInit(): void {
    forkJoin({
      medications: this.medicationService.getMedications(),
      vials: this.vialService.getVials(),
      // Only used to pick the default selection, so a failure just falls back to the first.
      current: this.levelsService.getNextDoses().pipe(
        map((r) => r.nextDoses.map((d) => d.medicationName)),
        catchError(() => of([] as string[])),
      ),
    }).subscribe({
      next: ({ medications, vials, current }) => {
        this.medications = medications.map((m) => m.name);
        this.vials = new Map(vials.map((v) => [v.medicationName, v]));
        // Default: the first medication currently being taken, else the first in the list.
        this.select(
          this.medications.find((name) => current.includes(name)) ?? this.medications[0] ?? '',
        );
        // A medication named in the URL wins over the default.
        this.applyRequested();
      },
      error: (error) => {
        console.error('Error loading vials:', error);
        this.loadError = true;
      },
    });
  }

  /** The selected medication's saved vial (what the calculator works from), or null. */
  get savedVial(): Vial | null {
    return this.vials.get(this.selected) ?? null;
  }

  /** The mg of the most recent dose of the selected medication, or null if there is none. */
  get lastDoseMg(): number | null {
    return this.doses.find((d) => d.medicationName === this.selected)?.doseAmountMg ?? null;
  }

  /** Today's local date, yyyy-MM-dd: the latest start date allowed. */
  get today(): string {
    return toDateInput(new Date());
  }

  /**
   * The vial's age in whole days, from the start date in the form (so it follows edits before
   * they are saved): 0 on the day it was started. Null when the date is blank or in the future.
   */
  get ageDays(): number | null {
    const startedOn = this.form.getRawValue().startedOn;
    if (!startedOn) {
      return null;
    }
    const days = daysBetween(startedOn, this.today);
    return days >= 0 ? days : null;
  }

  /**
   * The values saved for the selected medication. If it has no vial saved, the amounts are
   * blank and the start date is today.
   */
  private get saved(): VialValues {
    const vial = this.vials.get(this.selected);
    return vial
      ? {
          initialAmountMg: vial.initialAmountMg,
          initialVolumeMl: vial.initialVolumeMl,
          startedOn: vial.startedOn,
        }
      : { initialAmountMg: null, initialVolumeMl: null, startedOn: this.today };
  }

  /** True when the inputs differ from the saved values. */
  get changed(): boolean {
    const saved = this.saved;
    const value = this.form.getRawValue();
    return (
      toNumber(value.initialAmountMg) !== saved.initialAmountMg ||
      toNumber(value.initialVolumeMl) !== saved.initialVolumeMl ||
      (value.startedOn || null) !== saved.startedOn
    );
  }

  /** The dropdown changed: record the choice in the URL. */
  onSelect(name: string): void {
    this.select(name);
    this.router.navigate([], {
      relativeTo: this.route,
      queryParams: { medication: name },
      queryParamsHandling: 'merge',
    });
  }

  /** Put the inputs back to the last saved values. */
  onReset(): void {
    this.form.reset(this.saved);
    this.status = null;
  }

  onSave(): void {
    if (!this.form.valid || !this.changed || this.saving) {
      return;
    }
    const value = this.form.getRawValue();
    const medicationName = this.selected;
    const vial: Vial = {
      medicationName,
      initialAmountMg: Number(value.initialAmountMg),
      initialVolumeMl: Number(value.initialVolumeMl),
      startedOn: value.startedOn!,
    };

    this.saving = true;
    this.vialService.saveVial(vial).subscribe({
      next: () => {
        this.saving = false;
        this.vials.set(medicationName, vial);
        if (medicationName === this.selected) {
          // The saved values are now what Reset goes back to.
          this.form.reset(this.saved);
        }
        this.showStatus('Saved', null);
      },
      error: (error) => {
        console.error('Error saving vial:', error);
        this.saving = false;
        this.showStatus('Error', error.status != null ? String(error.status) : 'Unknown');
      },
    });
  }

  /** Show a medication's saved values, discarding any unsaved edits. */
  private select(name: string): void {
    this.selected = name;
    this.form.reset(this.saved);
    this.status = null;
  }

  private showStatus(status: 'Saved' | 'Error', errorStatusCode: string | null): void {
    this.status = status;
    this.errorStatusCode = errorStatusCode;
    clearTimeout(this.statusTimer);
    this.statusTimer = setTimeout(() => (this.status = null), 3000);
  }

  /** Select the medication named in the URL, if it matches one in the list (ignoring case). */
  private applyRequested(): void {
    if (!this.requested) {
      return;
    }
    const match = this.medications.find(
      (name) => name.toLowerCase() === this.requested!.toLowerCase(),
    );
    if (match && match !== this.selected) {
      this.select(match);
    }
  }
}

/** A date input's value is valid only if it is not after today (local time). */
function notAfterToday(control: AbstractControl): ValidationErrors | null {
  const value = control.value as string | null;
  return value && value > toDateInput(new Date()) ? { afterToday: true } : null;
}

/** A local date as a date input's value, yyyy-MM-dd. */
function toDateInput(d: Date): string {
  const pad = (n: number) => String(n).padStart(2, '0');
  return `${d.getFullYear()}-${pad(d.getMonth() + 1)}-${pad(d.getDate())}`;
}

/** Whole calendar days from one yyyy-MM-dd date to another (unaffected by clock changes). */
function daysBetween(from: string, to: string): number {
  const toUtc = (s: string) => {
    const [y, m, d] = s.split('-').map(Number);
    return Date.UTC(y, m - 1, d);
  };
  return Math.round((toUtc(to) - toUtc(from)) / 86_400_000);
}

/** A number input's value as a number, or null when blank. */
function toNumber(value: number | string | null | undefined): number | null {
  return value === null || value === undefined || value === '' ? null : Number(value);
}
