import { Component, ChangeDetectionStrategy } from '@angular/core';
import { DoseInputComponent } from '../dose-input/dose-input.component';

@Component({
  selector: 'app-page-1',
  imports: [DoseInputComponent],
  templateUrl: './page-1.component.html',
  changeDetection: ChangeDetectionStrategy.Eager,
  styleUrl: './page-1.component.scss',
})
export class Page1Component {}
