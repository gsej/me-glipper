import { Component, ChangeDetectionStrategy } from '@angular/core';
import {
  FormBuilder,
  FormGroup,
  Validators,
  ReactiveFormsModule,
} from '@angular/forms';
import { CommonModule } from '@angular/common';
import { WeightService } from '../../services/weight.service';

@Component({
  selector: 'app-weight-input',
  templateUrl: './weight-input.component.html',
  styleUrls: ['./weight-input.component.scss'],
  changeDetection: ChangeDetectionStrategy.Eager,
  imports: [CommonModule, ReactiveFormsModule],
})
export class WeightInputComponent {
  weightForm: FormGroup;
  status: string | null = null;
  errorStatusCode: string | null = null;
  showNote = false;
  private successTimer: any;

  constructor(
    private fb: FormBuilder,
    private weightService: WeightService,
  ) {
    this.weightForm = this.fb.group({
      weight: ['', [Validators.required, Validators.min(0)]],
      comment: ['', [Validators.maxLength(200)]],
    });
  }

  toggleNote() {
    this.showNote = !this.showNote;
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
    if (this.weightForm.valid) {
      const weight = this.weightForm.value.weight;
      const comment = this.weightForm.value.comment?.trim() || undefined;

      this.weightService.addWeightRecord(weight, comment).subscribe({
        next: () => {
          this.status = 'Success';
          this.errorStatusCode = null;
          this.weightForm.reset();
          this.showNote = false;
          this.clearStatusAfterDelay();
        },
        error: (error) => {
          console.error('Error submitting weight:', error);
          this.status = 'Error';
          this.errorStatusCode = error.status || 'Unknown';
          this.clearStatusAfterDelay();
        },
      });
    }
  }
}
