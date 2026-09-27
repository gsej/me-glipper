import {
  ApplicationConfig,
  provideZoneChangeDetection,
  isDevMode,
  inject,
  provideAppInitializer,
} from '@angular/core';
import { provideRouter } from '@angular/router';

import { routes } from './app.routes';
import { provideHttpClient, withXhr, withInterceptors } from '@angular/common/http';
import { SettingsHttpService } from './settings/settings.http.service';
import { apiKeyInterceptor } from './services/api-key.interceptor';
import { provideServiceWorker } from '@angular/service-worker';

export function initializeApp(settingsHttpService: SettingsHttpService) {
  return () => settingsHttpService.initializeApp();
}

export const appConfig: ApplicationConfig = {
  providers: [
    provideZoneChangeDetection({ eventCoalescing: true }),
    provideRouter(routes),
    provideHttpClient(withXhr(), withInterceptors([apiKeyInterceptor])),
    provideAppInitializer(() => {
      const initializerFn = initializeApp(inject(SettingsHttpService));
      return initializerFn();
    }),
    provideServiceWorker('ngsw-worker.js', {
      enabled: !isDevMode(),
      registrationStrategy: 'registerWhenStable:30000',
    }),
  ],
};
