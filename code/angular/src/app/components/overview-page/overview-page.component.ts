import { Component, ChangeDetectionStrategy } from '@angular/core';

import { NextDosesComponent } from '../next-doses/next-doses.component';

@Component({
  selector: 'app-overview-page',
  imports: [NextDosesComponent],
  templateUrl: './overview-page.component.html',
  changeDetection: ChangeDetectionStrategy.Eager,
  styleUrls: ['./overview-page.component.scss'],
})
export class OverviewPageComponent {
  // First page: an at-a-glance overview (currently the next dose due per medication).
}
