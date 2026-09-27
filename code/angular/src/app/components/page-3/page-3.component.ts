import { Component, ChangeDetectionStrategy } from '@angular/core';

import { WeightReportComponent } from '../weight-report/weight-report.component';

@Component({
  selector: 'app-page-3',
  imports: [WeightReportComponent],
  templateUrl: './page-3.component.html',
  changeDetection: ChangeDetectionStrategy.Eager,
  styleUrls: ['./page-3.component.scss'],
})
export class Page3Component {
  // This component now only acts as a container
}
