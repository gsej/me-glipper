import { HttpInterceptorFn } from '@angular/common/http';
import { inject } from '@angular/core';
import { ApiKeyService } from './api-key.service';
import { SettingsService } from '../settings/settings.service';

/**
 * Attaches the x-api-key header to requests aimed at the API. Only requests under the
 * configured API base URL's /api/ path get the key.
 */
export const apiKeyInterceptor: HttpInterceptorFn = (req, next) => {
  const apiUrl = inject(SettingsService).settings.apiUrl;
  const apiKeyService = inject(ApiKeyService);

  // Requests to the API are `${apiUrl}/api/...`: absolute when the API is on another
  // origin, relative ('/api/...') when it shares the frontend's origin (apiUrl '').
  if (req.url.startsWith(`${apiUrl}/api/`)) {
    req = req.clone({ setHeaders: { 'x-api-key': apiKeyService.get() ?? '' } });
  }

  return next(req);
};
