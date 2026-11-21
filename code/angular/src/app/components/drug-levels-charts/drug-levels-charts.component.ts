import { Component, OnInit, ChangeDetectionStrategy, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { catchError, forkJoin, of } from 'rxjs';
import {
  DrugLevelChartComponent,
  PLOT_LEFT_PX,
} from '../drug-level-chart/drug-level-chart.component';
import {
  DrugLevelSeries,
  EFFECTIVELY_ZERO_MG,
  LevelsService,
} from '../../services/levels.service';
import { DoseService } from '../../services/dose.service';
import { MedicationService } from '../../services/medication.service';

/** Headroom above a drug's all-time peak, so the line doesn't touch the top of the chart. */
const Y_HEADROOM = 1.05;

/** A series plus what's needed to scale, order and hide its chart. */
interface ChartSeries extends DrugLevelSeries {
  /** Fixed y-axis maximum (all-time peak plus headroom), or null to auto-scale. */
  yMax: number | null;
  /**
   * Unix seconds of the last point up to now at or above EFFECTIVELY_ZERO_MG; every later point
   * up to now is effectively zero. Null while the drug is still active (above it now). The
   * projection past now is ignored, so a drug active today never counts as cleared.
   */
  zeroSinceSecs: number | null;
}

@Component({
  selector: 'app-drug-levels-charts',
  imports: [CommonModule, DrugLevelChartComponent],
  templateUrl: './drug-levels-charts.component.html',
  changeDetection: ChangeDetectionStrategy.Eager,
  styleUrls: ['./drug-levels-charts.component.scss'],
})
export class DrugLevelsChartsComponent implements OnInit {
  private readonly levelsService = inject(LevelsService);
  private readonly doseService = inject(DoseService);
  private readonly medicationService = inject(MedicationService);

  times: string[] = [];
  /** Active drugs first (API order), then cleared drugs, most recently cleared first. */
  series: ChartSeries[] = [];
  isLoading = false;
  error: string | null = null;
  rangeDays: number | null = 30;
  /** Left inset that lines headings and notes up with each chart's y-axis line. */
  readonly plotLeftPx = PLOT_LEFT_PX;
  /** Unix seconds of the API's "now": history up to here, projection after. */
  nowSecs = 0;

  // Window options in ascending order; 1M/3M approximate 30/90 days.
  readonly rangeOptions: { label: string; days: number | null }[] = [
    { label: '15D', days: 15 },
    { label: '1M', days: 30 },
    { label: '45D', days: 45 },
    { label: '3M', days: 90 },
    { label: 'All', days: null },
  ];

  /** Chart colour per medication name, from the API's medications. */
  private colors: Record<string, string> = {};

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
      // Colours are cosmetic: if the medications can't be fetched, still draw the charts (grey).
      medications: this.medicationService.getMedications().pipe(catchError(() => of([]))),
    }).subscribe({
      next: ({ response, medications }) => {
        this.colors = Object.fromEntries(medications.map((m) => [m.name, m.chartColor]));
        this.times = response.times;
        this.nowSecs = Math.floor(new Date(response.now).getTime() / 1000);
        this.series = this.prepareSeries(response.times, response.series);
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

  /**
   * Charts with a measurable level somewhere in the selected window. Because cleared drugs are
   * sorted most-recently-cleared first, narrowing the range only ever hides charts from the
   * bottom of the list.
   */
  get visibleSeries(): ChartSeries[] {
    return this.series.filter((s) => this.isVisible(s));
  }

  /** Names of the drugs hidden because they are effectively zero throughout the window. */
  get hiddenNames(): string[] {
    return this.series.filter((s) => !this.isVisible(s)).map((s) => s.medicationName);
  }

  private isVisible(s: ChartSeries): boolean {
    if (this.rangeDays == null || s.zeroSinceSecs == null) {
      return true;
    }
    // Same window start as the chart's own range filter.
    const cutoffSecs = this.nowSecs - this.rangeDays * 24 * 60 * 60;
    return s.zeroSinceSecs >= cutoffSecs;
  }

  private prepareSeries(times: string[], series: DrugLevelSeries[]): ChartSeries[] {
    const xs = times.map((t) => Math.floor(new Date(t).getTime() / 1000));
    // Index of the last point at or before now; later points are the projection.
    let nowIndex = xs.length - 1;
    while (nowIndex > 0 && xs[nowIndex] > this.nowSecs) {
      nowIndex--;
    }

    const prepared = series.map((s): ChartSeries => {
      const peak = s.amountsMg.reduce((max, a) => Math.max(max, a), 0);
      let lastAbove = -1;
      for (let i = nowIndex; i >= 0; i--) {
        if (s.amountsMg[i] >= EFFECTIVELY_ZERO_MG) {
          lastAbove = i;
          break;
        }
      }
      const active = lastAbove >= 0 && lastAbove === nowIndex;
      return {
        ...s,
        yMax: peak > 0 ? peak * Y_HEADROOM : null,
        zeroSinceSecs: active ? null : lastAbove >= 0 ? xs[lastAbove] : (xs[0] ?? 0),
      };
    });

    // Stable sort: active drugs keep API order; cleared drugs follow, most recently cleared first.
    return prepared.sort((a, b) => {
      if (a.zeroSinceSecs == null || b.zeroSinceSecs == null) {
        return (a.zeroSinceSecs == null ? 0 : 1) - (b.zeroSinceSecs == null ? 0 : 1);
      }
      return b.zeroSinceSecs - a.zeroSinceSecs;
    });
  }

  setRange(days: number | null): void {
    this.rangeDays = days;
  }

  onRetry(): void {
    this.load();
  }

  colorFor(medicationName: string): string {
    return this.colors[medicationName] ?? '#6b7280'; // gray-500 fallback
  }
}
