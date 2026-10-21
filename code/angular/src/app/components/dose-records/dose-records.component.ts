import { Component, OnInit, ChangeDetectionStrategy, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { DoseService } from '../../services/dose.service';

@Component({
  selector: 'app-dose-records',
  imports: [CommonModule, FormsModule],
  templateUrl: './dose-records.component.html',
  changeDetection: ChangeDetectionStrategy.Eager,
  styleUrls: ['./dose-records.component.scss'],
})
export class DoseRecordsComponent implements OnInit {
  private readonly doseService = inject(DoseService);

  readonly doseRecords$ = this.doseService.doseRecords$;
  readonly isLoading$ = this.doseService.isLoading$;
  readonly error$ = this.doseService.error$;

  editingId: string | null = null;
  editValue = '';

  ngOnInit(): void {
    this.loadDoses();
  }

  startEdit(doseId: string, currentNotes?: string): void {
    this.editingId = doseId;
    this.editValue = currentNotes ?? '';
  }

  cancelEdit(): void {
    this.editingId = null;
    this.editValue = '';
  }

  saveEdit(doseId: string): void {
    const trimmed = this.editValue.trim();
    this.doseService.updateNotes(doseId, trimmed === '' ? null : trimmed).subscribe({
      next: () => this.cancelEdit(),
      error: (error) => {
        console.error('Error updating notes:', error);
        this.cancelEdit();
      },
    });
  }

  loadDoses(): void {
    this.doseService.loadDoses();
  }

  onRetry(): void {
    this.loadDoses();
  }

  deleteRecord(doseId: string): void {
    if (confirm('Are you sure you want to delete this dose?')) {
      this.doseService.deleteDose(doseId).subscribe({
        error: (error) => {
          console.error('Error deleting dose:', error);
          // Error surfaces via the service's error$ subscription.
        },
      });
    }
  }
}
