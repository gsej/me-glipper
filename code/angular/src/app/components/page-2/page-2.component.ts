import { Component, ChangeDetectionStrategy } from '@angular/core';

import { DoseRecordsComponent } from '../dose-records/dose-records.component';

@Component({
  selector: 'app-page-2',
  imports: [DoseRecordsComponent],
  templateUrl: './page-2.component.html',
  changeDetection: ChangeDetectionStrategy.Eager,
  styleUrls: ['./page-2.component.scss'],
})
export class Page2Component {
  // Container for the dose history list.
}
