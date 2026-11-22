import { Component, ChangeDetectionStrategy } from '@angular/core';

import { DoseRecordsComponent } from '../dose-records/dose-records.component';

@Component({
  selector: 'app-history-page',
  imports: [DoseRecordsComponent],
  templateUrl: './history-page.component.html',
  changeDetection: ChangeDetectionStrategy.Eager,
  styleUrls: ['./history-page.component.scss'],
})
export class HistoryPageComponent {
  // Container for the dose history list.
}
