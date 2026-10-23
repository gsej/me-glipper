import { Component, OnInit, ChangeDetectionStrategy, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { DrugLevelChartComponent } from '../drug-level-chart/drug-level-chart.component';
import { DrugLevelSeries, LevelsService } from '../../services/levels.service';

@Component({
  selector: 'app-drug-levels-charts',
  imports: [CommonModule, DrugLevelChartComponent],
  templateUrl: './drug-levels-charts.component.html',
  changeDetection: ChangeDetectionStrategy.Eager,
  styleUrls: ['./drug-levels-charts.component.scss'],
})
export class DrugLevelsChartsComponent implements OnInit {
  private readonly levelsService = inject(LevelsService);

  times: string[] = [];
  series: DrugLevelSeries[] = [];
  isLoading = false;
  error: string | null = null;
  rangeMonths: number | null = 1;

  private readonly colors: Record<string, string> = {
    Semaglutide: '#3b82f6', // blue-500
    Tirzepatide: '#10b981', // emerald-500
    Retatrutide: '#8b5cf6', // violet-500
  };

  ngOnInit(): void {
    this.load();
  }

  load(): void {
    this.isLoading = true;
    this.error = null;
    this.levelsService.getSeries().subscribe({
      next: (response) => {
        this.times = response.times;
        this.series = response.series;
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

  setRange(months: number | null): void {
    this.rangeMonths = months;
  }

  onRetry(): void {
    this.load();
  }

  colorFor(medicationName: string): string {
    return this.colors[medicationName] ?? '#6b7280'; // gray-500 fallback
  }
}
