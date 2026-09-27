import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { map } from 'rxjs/operators';
import { SettingsService } from '../settings/settings.service';

interface HealthResponse {
  status: string;
  gitHash: string;
}

@Injectable({
  providedIn: 'root'
})
export class HealthService {

  private apiUrl: string;

  constructor(
    private http: HttpClient,
    settingsService: SettingsService
  ) {
    this.apiUrl = settingsService.settings.apiUrl;
  }

  getApiGitHash(): Observable<string> {
    return this.http.get<HealthResponse>(`${this.apiUrl}/api/healthz`)
      .pipe(map(response => response.gitHash));
  }
}
