import { Component, ChangeDetectionStrategy } from '@angular/core';

import { DrugLevelsComponent } from '../drug-levels/drug-levels.component';

@Component({
  selector: 'app-page-3',
  imports: [DrugLevelsComponent],
  templateUrl: './page-3.component.html',
  changeDetection: ChangeDetectionStrategy.Eager,
  styleUrls: ['./page-3.component.scss'],
})
export class Page3Component {
  // Container for the estimated drug-levels view.
}
