import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { SettingsService } from '../settings/settings.service';

export interface DrugLevel {
  medicationName: string;
  amountRemainingMg: number;
}

export interface LevelsResponse {
  levels: DrugLevel[];
}

@Injectable({
  providedIn: 'root',
})
export class LevelsService {
  private apiUrl: string;

  constructor(
    private http: HttpClient,
    settingsService: SettingsService,
  ) {
    this.apiUrl = settingsService.settings.apiUrl;
  }

  getLevels(): Observable<LevelsResponse> {
    return this.http.get<LevelsResponse>(`${this.apiUrl}/api/levels`);
  }
}
