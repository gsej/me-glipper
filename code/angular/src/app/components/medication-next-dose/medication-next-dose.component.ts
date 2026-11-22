import {
  Component,
  Input,
  OnInit,
  ChangeDetectionStrategy,
  inject,
} from '@angular/core';
import { CommonModule } from '@angular/common';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { LevelsService, NextDose } from '../../services/levels.service';
import { DoseService } from '../../services/dose.service';

/**
 * The next-dose card for one medication, on the Medication page. Started as a copy of the
 * overview's next-doses list (deliberately not shared, so the two can change separately).
 */
@Component({
  selector: 'app-medication-next-dose',
  imports: [CommonModule],
  templateUrl: './medication-next-dose.component.html',
  changeDetection: ChangeDetectionStrategy.Eager,
  styleUrls: ['./medication-next-dose.component.scss'],
})
export class MedicationNextDoseComponent implements OnInit {
  private readonly levelsService = inject(LevelsService);
  private readonly doseService = inject(DoseService);

  @Input() medicationName = '';

  private nextDoses: NextDose[] = [];
  isLoading = false;
  error: string | null = null;

  constructor() {
    // Reload whenever a dose is added or deleted elsewhere in the app.
    this.doseService.dosesChanged$.pipe(takeUntilDestroyed()).subscribe(() => this.load());
  }

  ngOnInit(): void {
    this.load();
  }

  /** The selected medication's next dose, or null if it isn't currently being taken. */
  get nextDose(): NextDose | null {
    return (
      this.nextDoses.find(
        (d) => d.medicationName.toLowerCase() === this.medicationName.toLowerCase(),
      ) ?? null
    );
  }

  load(): void {
    this.isLoading = true;
    this.error = null;
    this.levelsService.getNextDoses().subscribe({
      next: (response) => {
        this.nextDoses = response.nextDoses;
        this.isLoading = false;
      },
      error: (error) => {
        console.error('Error fetching next doses:', error);
        this.error =
          error.status === 401
            ? 'Error: 401 - API key must be supplied'
            : `Error: ${error.status || 'Unknown'}`;
        this.isLoading = false;
      },
    });
  }

  onRetry(): void {
    this.load();
  }

  /** Overdue when the due date (local calendar day) is before today. */
  isOverdue(nextDose: NextDose): boolean {
    return this.daysUntilDue(nextDose) < 0;
  }

  /** e.g. "due today", "due tomorrow", "due in 2 days", "overdue by 1 day". */
  relativeText(nextDose: NextDose): string {
    const days = this.daysUntilDue(nextDose);
    if (days === 0) {
      return 'due today';
    }
    if (days === 1) {
      return 'due tomorrow';
    }
    if (days > 1) {
      return `due in ${days} days`;
    }
    return days === -1 ? 'overdue by 1 day' : `overdue by ${-days} days`;
  }

  /**
   * Whole calendar days from today to the due date, in the browser's local time zone; the time
   * of day is ignored. Date.UTC on the local date parts avoids DST-length days skewing the count.
   */
  private daysUntilDue(nextDose: NextDose): number {
    const due = new Date(nextDose.nextDueAt);
    const today = new Date();
    const dueDay = Date.UTC(due.getFullYear(), due.getMonth(), due.getDate());
    const todayDay = Date.UTC(today.getFullYear(), today.getMonth(), today.getDate());
    return Math.round((dueDay - todayDay) / (24 * 60 * 60 * 1000));
  }
}
