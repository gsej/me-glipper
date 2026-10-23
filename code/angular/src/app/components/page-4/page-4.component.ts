import { Component, ChangeDetectionStrategy } from '@angular/core';

import { DrugLevelsChartsComponent } from '../drug-levels-charts/drug-levels-charts.component';

@Component({
  selector: 'app-page-4',
  imports: [DrugLevelsChartsComponent],
  templateUrl: './page-4.component.html',
  changeDetection: ChangeDetectionStrategy.Eager,
  styleUrls: ['./page-4.component.scss'],
})
export class Page4Component {
  // Container for the estimated-level charts (one per drug).
}
