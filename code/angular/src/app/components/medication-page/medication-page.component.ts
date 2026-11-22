import { Component, OnInit, ChangeDetectionStrategy, inject } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { ActivatedRoute, Router } from '@angular/router';
import { catchError, forkJoin, map, of } from 'rxjs';
import { MedicationService } from '../../services/medication.service';
import { LevelsService } from '../../services/levels.service';
import { MedicationNextDoseComponent } from '../medication-next-dose/medication-next-dose.component';
import { MedicationLevelChartComponent } from '../medication-level-chart/medication-level-chart.component';
import { MedicationDoseInputComponent } from '../medication-dose-input/medication-dose-input.component';

/**
 * Information about one medication, chosen from a dropdown: its next-dose card, its level
 * chart and a form to add a dose of it.
 * The selection follows the URL's `medication` query parameter (set e.g. by tapping a card on
 * the overview), and choosing from the dropdown updates it.
 */
@Component({
  selector: 'app-medication-page',
  imports: [
    FormsModule,
    MedicationNextDoseComponent,
    MedicationLevelChartComponent,
    MedicationDoseInputComponent,
  ],
  templateUrl: './medication-page.component.html',
  changeDetection: ChangeDetectionStrategy.Eager,
  styleUrls: ['./medication-page.component.scss'],
})
export class MedicationPageComponent implements OnInit {
  private readonly medicationService = inject(MedicationService);
  private readonly levelsService = inject(LevelsService);
  private readonly router = inject(Router);
  private readonly route = inject(ActivatedRoute);

  medications: string[] = [];
  selected = '';
  medicationsError = false;

  // The medication named in the URL, kept until the list has loaded and it can be applied.
  private requested: string | null = null;

  constructor() {
    this.route.queryParams.pipe(takeUntilDestroyed()).subscribe((params) => {
      this.requested = params['medication'] ?? null;
      this.applyRequested();
    });
  }

  ngOnInit(): void {
    forkJoin({
      medications: this.medicationService.getMedications(),
      // Only used to pick the default selection, so a failure just falls back to the first.
      current: this.levelsService.getNextDoses().pipe(
        map((r) => r.nextDoses.map((d) => d.medicationName)),
        catchError(() => of([] as string[])),
      ),
    }).subscribe({
      next: ({ medications, current }) => {
        this.medications = medications.map((m) => m.name);
        // Default: the first medication currently being taken, else the first in the list.
        this.selected =
          this.medications.find((name) => current.includes(name)) ?? this.medications[0] ?? '';
        // A medication named in the URL wins over the default.
        this.applyRequested();
      },
      error: (error) => {
        console.error('Error fetching medications:', error);
        this.medicationsError = true;
      },
    });
  }

  /** The dropdown changed: record the choice in the URL. */
  onSelect(name: string): void {
    this.selected = name;
    this.router.navigate([], {
      relativeTo: this.route,
      queryParams: { medication: name },
      queryParamsHandling: 'merge',
    });
  }

  /** Select the medication named in the URL, if it matches one in the list (ignoring case). */
  private applyRequested(): void {
    if (!this.requested) {
      return;
    }
    const match = this.medications.find(
      (name) => name.toLowerCase() === this.requested!.toLowerCase(),
    );
    if (match) {
      this.selected = match;
    }
  }
}
