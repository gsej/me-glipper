import { Component, ChangeDetectionStrategy } from '@angular/core';

import { DrugLevelsChartsComponent } from '../drug-levels-charts/drug-levels-charts.component';
import { NextDosesComponent } from '../next-doses/next-doses.component';

@Component({
  selector: 'app-overview-page',
  imports: [DrugLevelsChartsComponent, NextDosesComponent],
  templateUrl: './overview-page.component.html',
  changeDetection: ChangeDetectionStrategy.Eager,
  styleUrls: ['./overview-page.component.scss'],
})
export class OverviewPageComponent {
  // First page: an at-a-glance overview: the estimated-level charts, then the next dose due per
  // medication. (The charts are also still on the Charts page for now.)
}
