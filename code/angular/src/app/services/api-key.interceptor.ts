import { HttpInterceptorFn } from '@angular/common/http';
import { inject } from '@angular/core';
import { ApiKeyService } from './api-key.service';
import { SettingsService } from '../settings/settings.service';

/**
 * Attaches the x-api-key header to requests aimed at the API. Only requests aimed at the
 * configured API base URL get the key.
 */
export const apiKeyInterceptor: HttpInterceptorFn = (req, next) => {
  const apiUrl = inject(SettingsService).settings.apiUrl;
  const apiKeyService = inject(ApiKeyService);

  if (apiUrl && req.url.startsWith(apiUrl)) {
    req = req.clone({ setHeaders: { 'x-api-key': apiKeyService.get() ?? '' } });
  }

  return next(req);
};
