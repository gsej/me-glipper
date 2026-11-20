import { Component, ChangeDetectionStrategy, inject } from '@angular/core';
import { AppUpdateService } from '../../services/app-update.service';

/** Slim bar over the top of the header announcing a new app version, with a Reload button. */
@Component({
  selector: 'app-update-banner',
  templateUrl: './update-banner.component.html',
  changeDetection: ChangeDetectionStrategy.Eager,
  styleUrls: ['./update-banner.component.scss'],
})
export class UpdateBannerComponent {
  readonly appUpdate = inject(AppUpdateService);
}
