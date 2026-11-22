import {
  Component,
  Input,
  OnInit,
  ChangeDetectionStrategy,
  inject,
} from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { catchError, forkJoin, of } from 'rxjs';
import { DrugLevelChartComponent } from '../drug-level-chart/drug-level-chart.component';
import { DrugLevelSeries, LevelsService } from '../../services/levels.service';
import { DoseService } from '../../services/dose.service';
import { MedicationService } from '../../services/medication.service';

/** Headroom above the drug's all-time peak, so the line doesn't touch the top of the chart. */
const Y_HEADROOM = 1.05;

/** Y-axis maximum (mg) for a medication with no level at all, so its flat line sits at 0. */
const EMPTY_Y_MAX = 1;

/**
 * The estimated-level chart for one medication, with its own range buttons, on the Medication
 * page. Separate from the overview's drug-levels-charts so the two can change independently;
 * both draw with the shared drug-level-chart.
 */
@Component({
  selector: 'app-medication-level-chart',
  imports: [DrugLevelChartComponent],
  templateUrl: './medication-level-chart.component.html',
  changeDetection: ChangeDetectionStrategy.Eager,
  styleUrls: ['./medication-level-chart.component.scss'],
})
export class MedicationLevelChartComponent implements OnInit {
  private readonly levelsService = inject(LevelsService);
  private readonly doseService = inject(DoseService);
  private readonly medicationService = inject(MedicationService);

  @Input() medicationName = '';

  times: string[] = [];
  nowSecs = 0;
  private allSeries: DrugLevelSeries[] = [];
  private colors: Record<string, string> = {};
  isLoading = false;
  error: string | null = null;
  rangeDays: number | null = 30;

  // Window options in ascending order; 1M/3M approximate 30/90 days.
  readonly rangeOptions: { label: string; days: number | null }[] = [
    { label: '15D', days: 15 },
    { label: '1M', days: 30 },
    { label: '45D', days: 45 },
    { label: '3M', days: 90 },
    { label: 'All', days: null },
  ];

  constructor() {
    // Reload whenever a dose is added or deleted elsewhere in the app.
    this.doseService.dosesChanged$.pipe(takeUntilDestroyed()).subscribe(() => this.load());
  }

  ngOnInit(): void {
    this.load();
  }

  load(): void {
    this.isLoading = true;
    this.error = null;
    forkJoin({
      response: this.levelsService.getSeries(),
      // Colours are cosmetic: if the medications can't be fetched, still draw the chart (grey).
      medications: this.medicationService.getMedications().pipe(catchError(() => of([]))),
    }).subscribe({
      next: ({ response, medications }) => {
        this.colors = Object.fromEntries(medications.map((m) => [m.name, m.chartColor]));
        this.times = response.times;
        this.nowSecs = Math.floor(new Date(response.now).getTime() / 1000);
        this.allSeries = response.series;
        this.isLoading = false;
      },
      error: (error) => {
        console.error('Error fetching level series:', error);
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

  setRange(days: number | null): void {
    this.rangeDays = days;
  }

  /** Whether there is a time grid to draw on (false only when no doses are recorded at all). */
  get hasData(): boolean {
    return this.times.length > 0;
  }

  /**
   * The selected medication's amounts on the shared time grid. A medication that has never been
   * dosed has no series from the API, so it gets a flat zero line: the chart is always shown,
   * even for medications not currently being taken.
   */
  get amountsMg(): number[] {
    const name = this.medicationName.toLowerCase();
    const series = this.allSeries.find((s) => s.medicationName.toLowerCase() === name);
    return series?.amountsMg ?? this.zeros();
  }

  get color(): string {
    return this.colors[this.medicationName] ?? '#6b7280'; // gray-500 fallback
  }

  /**
   * Fixed y-axis maximum: the all-time peak plus headroom (so a cleared drug's flat line sits at
   * the bottom of a meaningful scale), or 0–1 mg when there has never been any level.
   */
  get yMax(): number {
    const peak = this.amountsMg.reduce((max, a) => Math.max(max, a), 0);
    return peak > 0 ? peak * Y_HEADROOM : EMPTY_Y_MAX;
  }

  // Cached so the zero line keeps the same array (and the chart isn't rebuilt) between checks.
  private zeroLine: number[] = [];
  private zeros(): number[] {
    if (this.zeroLine.length !== this.times.length) {
      this.zeroLine = this.times.map(() => 0);
    }
    return this.zeroLine;
  }
}
