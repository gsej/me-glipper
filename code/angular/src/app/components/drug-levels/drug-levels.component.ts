import { Component, OnInit, ChangeDetectionStrategy, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { DrugLevel, EFFECTIVELY_ZERO_MG, LevelsService } from '../../services/levels.service';
import { DoseService } from '../../services/dose.service';

@Component({
  selector: 'app-drug-levels',
  imports: [CommonModule],
  templateUrl: './drug-levels.component.html',
  changeDetection: ChangeDetectionStrategy.Eager,
  styleUrls: ['./drug-levels.component.scss'],
})
export class DrugLevelsComponent implements OnInit {
  private readonly levelsService = inject(LevelsService);
  private readonly doseService = inject(DoseService);

  levels: DrugLevel[] = [];
  isLoading = false;
  error: string | null = null;

  constructor() {
    // Reload whenever a dose is added or deleted elsewhere in the app.
    this.doseService.dosesChanged$.pipe(takeUntilDestroyed()).subscribe(() => this.loadLevels());
  }

  ngOnInit(): void {
    this.loadLevels();
  }

  loadLevels(): void {
    this.isLoading = true;
    this.error = null;
    this.levelsService.getLevels().subscribe({
      next: (response) => {
        this.levels = response.levels;
        this.isLoading = false;
      },
      error: (error) => {
        console.error('Error fetching levels:', error);
        this.error =
          error.status === 401
            ? 'Error: 401 - API key must be supplied'
            : `Error: ${error.status || 'Unknown'}`;
        this.isLoading = false;
      },
    });
  }

  /** Drugs with a measurable amount; ones that have effectively cleared are hidden. */
  get visibleLevels(): DrugLevel[] {
    return this.levels.filter((l) => l.amountRemainingMg >= EFFECTIVELY_ZERO_MG);
  }

  /** Names of the drugs hidden because they are effectively zero. */
  get hiddenNames(): string[] {
    return this.levels
      .filter((l) => l.amountRemainingMg < EFFECTIVELY_ZERO_MG)
      .map((l) => l.medicationName);
  }

  onRetry(): void {
    this.loadLevels();
  }
}
