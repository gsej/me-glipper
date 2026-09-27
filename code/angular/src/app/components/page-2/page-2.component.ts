import { Component, ChangeDetectionStrategy } from '@angular/core';

import { WeightRecordsComponent } from '../weight-records/weight-records.component';

@Component({
  selector: 'app-page-2',
  imports: [WeightRecordsComponent],
  templateUrl: './page-2.component.html',
  changeDetection: ChangeDetectionStrategy.Eager,
  styleUrls: ['./page-2.component.scss'],
})
export class Page2Component {
  // This component now only acts as a container
}
