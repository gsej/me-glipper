import { Component, OnInit, ChangeDetectionStrategy, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { WeightService } from '../../services/weight.service';

@Component({
  selector: 'app-weight-records',
  imports: [CommonModule, FormsModule],
  templateUrl: './weight-records.component.html',
  changeDetection: ChangeDetectionStrategy.Eager,
  styleUrls: ['./weight-records.component.scss'],
})
export class WeightRecordsComponent implements OnInit {
  private readonly weightService = inject(WeightService);

  readonly weightRecords$ = this.weightService.weightRecords$;
  readonly isLoading$ = this.weightService.isLoading$;
  readonly error$ = this.weightService.error$;

  editingId: string | null = null;
  editValue = '';

  ngOnInit(): void {
    this.loadWeightRecords();
  }

  startEdit(weightId: string, currentComment?: string): void {
    this.editingId = weightId;
    this.editValue = currentComment ?? '';
  }

  cancelEdit(): void {
    this.editingId = null;
    this.editValue = '';
  }

  saveEdit(weightId: string): void {
    const trimmed = this.editValue.trim();
    this.weightService.updateComment(weightId, trimmed === '' ? null : trimmed).subscribe({
      next: () => this.cancelEdit(),
      error: (error) => {
        console.error('Error updating comment:', error);
        this.cancelEdit();
      },
    });
  }

  loadWeightRecords(): void {
    this.weightService.loadWeightRecords();
  }

  onRetry(): void {
    this.loadWeightRecords();
  }

  deleteRecord(id: string): void {
    if (confirm('Are you sure you want to delete this weight record?')) {
      this.weightService.deleteWeightRecord(id).subscribe({
        error: (error) => {
          console.error('Error deleting weight record:', error);
          // Error will be handled by the service subscription
        },
      });
    }
  }
}
