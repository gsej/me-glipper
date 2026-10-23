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

export interface DrugLevelSeries {
  medicationName: string;
  amountsMg: number[];
}

export interface LevelSeriesResponse {
  // Shared x-axis (ISO timestamps); each series' amountsMg aligns to it element-for-element.
  times: string[];
  series: DrugLevelSeries[];
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

  getSeries(): Observable<LevelSeriesResponse> {
    return this.http.get<LevelSeriesResponse>(`${this.apiUrl}/api/levels/series`);
  }
}
