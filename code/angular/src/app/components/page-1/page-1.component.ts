import { Component, ChangeDetectionStrategy } from '@angular/core';
import { WeightInputComponent } from '../weight-input/weight-input.component';

@Component({
  selector: 'app-page-1',
  imports: [WeightInputComponent],
  templateUrl: './page-1.component.html',
  changeDetection: ChangeDetectionStrategy.Eager,
  styleUrl: './page-1.component.scss',
})
export class Page1Component {}
