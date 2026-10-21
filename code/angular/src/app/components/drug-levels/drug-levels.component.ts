import { Component, OnInit, ChangeDetectionStrategy, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { DrugLevel, LevelsService } from '../../services/levels.service';

@Component({
  selector: 'app-drug-levels',
  imports: [CommonModule],
  templateUrl: './drug-levels.component.html',
  changeDetection: ChangeDetectionStrategy.Eager,
  styleUrls: ['./drug-levels.component.scss'],
})
export class DrugLevelsComponent implements OnInit {
  private readonly levelsService = inject(LevelsService);

  levels: DrugLevel[] = [];
  isLoading = false;
  error: string | null = null;

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

  onRetry(): void {
    this.loadLevels();
  }
}
