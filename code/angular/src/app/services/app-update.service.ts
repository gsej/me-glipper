import { Injectable, inject, signal } from '@angular/core';
import { DOCUMENT } from '@angular/common';
import { SwUpdate, VersionReadyEvent } from '@angular/service-worker';
import { filter } from 'rxjs/operators';

/** What the update banner should say, if anything. */
export type AppUpdateState = 'none' | 'ready' | 'unrecoverable';

/**
 * Tells the user when a new version of the app has been downloaded by the service worker, so
 * they can reload into it instead of killing and relaunching the app. Also checks for updates
 * whenever the app returns to the foreground, since a phone PWA can sit in the background for
 * days. Does nothing when the service worker is disabled (local `ng serve`).
 */
@Injectable({ providedIn: 'root' })
export class AppUpdateService {
  private readonly swUpdate = inject(SwUpdate);
  private readonly document = inject(DOCUMENT);

  private readonly stateSignal = signal<AppUpdateState>('none');
  readonly state = this.stateSignal.asReadonly();

  constructor() {
    if (!this.swUpdate.isEnabled) {
      return;
    }

    this.swUpdate.versionUpdates
      .pipe(filter((e): e is VersionReadyEvent => e.type === 'VERSION_READY'))
      .subscribe(() => this.stateSignal.set('ready'));

    // The cached app is broken (e.g. files evicted) and can't be served: only a reload helps.
    this.swUpdate.unrecoverable.subscribe(() => this.stateSignal.set('unrecoverable'));

    this.document.addEventListener('visibilitychange', () => {
      if (this.document.visibilityState === 'visible') {
        // Offline or a failed check just means no banner this time.
        this.swUpdate.checkForUpdate().catch(() => undefined);
      }
    });
  }

  reload(): void {
    this.document.location.reload();
  }

  dismiss(): void {
    this.stateSignal.set('none');
  }
}
