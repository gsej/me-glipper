import { Component, ChangeDetectionStrategy } from '@angular/core';
import {
  FormBuilder,
  FormGroup,
  Validators,
  ReactiveFormsModule,
} from '@angular/forms';
import { CommonModule } from '@angular/common';
import { DoseService } from '../../services/dose.service';

@Component({
  selector: 'app-dose-input',
  templateUrl: './dose-input.component.html',
  styleUrls: ['./dose-input.component.scss'],
  changeDetection: ChangeDetectionStrategy.Eager,
  imports: [CommonModule, ReactiveFormsModule],
})
export class DoseInputComponent {
  // Hard-coded for now; will likely become user-configurable in a later slice.
  readonly medications = ['Semaglutide', 'Tirzepatide', 'Retatrutide'];

  doseForm: FormGroup;
  status: string | null = null;
  errorStatusCode: string | null = null;
  showNote = false;
  private successTimer: any;

  constructor(
    private fb: FormBuilder,
    private doseService: DoseService,
  ) {
    const now = new Date();
    this.doseForm = this.fb.group({
      date: [this.toDateInput(now), [Validators.required]],
      time: [this.toTimeInput(now), [Validators.required]],
      medicationName: ['', [Validators.required]],
      doseAmountMg: ['', [Validators.required, Validators.min(0)]],
      notes: ['', [Validators.maxLength(200)]],
    });
  }

  toggleNote() {
    this.showNote = !this.showNote;
  }

  private toDateInput(d: Date): string {
    const pad = (n: number) => String(n).padStart(2, '0');
    return `${d.getFullYear()}-${pad(d.getMonth() + 1)}-${pad(d.getDate())}`;
  }

  private toTimeInput(d: Date): string {
    const pad = (n: number) => String(n).padStart(2, '0');
    return `${pad(d.getHours())}:${pad(d.getMinutes())}`;
  }

  private clearStatusAfterDelay() {
    if (this.successTimer) {
      clearTimeout(this.successTimer);
    }
    this.successTimer = setTimeout(() => {
      this.status = null;
    }, 3000);
  }

  onSubmit() {
    if (!this.doseForm.valid) {
      return;
    }

    const { date, time, medicationName, doseAmountMg, notes } = this.doseForm.value;
    // Combine the date and time pickers into a single local instant; it is serialised to
    // UTC on the wire and stored as UTC by the API.
    const takenAt = new Date(`${date}T${time}`);

    this.doseService
      .addDose({
        takenAt,
        medicationName,
        doseAmountMg: Number(doseAmountMg),
        notes: notes?.trim() || undefined,
      })
      .subscribe({
        next: () => {
          this.status = 'Success';
          this.errorStatusCode = null;
          this.resetForm();
          this.clearStatusAfterDelay();
        },
        error: (error) => {
          console.error('Error submitting dose:', error);
          this.status = 'Error';
          this.errorStatusCode = error.status != null ? String(error.status) : 'Unknown';
          this.clearStatusAfterDelay();
        },
      });
  }

  // Reset to a fresh form defaulted to "now", clearing the dose-specific fields.
  private resetForm() {
    const now = new Date();
    this.doseForm.reset({
      date: this.toDateInput(now),
      time: this.toTimeInput(now),
      medicationName: '',
      doseAmountMg: '',
      notes: '',
    });
    this.showNote = false;
  }
}
