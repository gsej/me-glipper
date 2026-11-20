import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { SettingsService } from '../settings/settings.service';

/**
 * Below this an amount is treated as effectively zero. Amounts are shown to 2 decimal places,
 * so anything under this already displays as 0.00 mg. Used by the levels page and the charts.
 */
export const EFFECTIVELY_ZERO_MG = 0.005;

export interface DrugLevel {
  medicationName: string;
  amountRemainingMg: number;
}

export interface LevelsResponse {
  levels: DrugLevel[];
}

export interface DrugLevelSeries {
  medicationName: string;
  // First-order absorption (Bateman) model.
  amountsMg: number[];
}

export interface LevelSeriesResponse {
  // One of the times: points up to it are history, later ones a projection assuming no further
  // doses.
  now: string;
  // Shared x-axis (ISO timestamps); each series' amountsMg aligns to it element-for-element.
  times: string[];
  series: DrugLevelSeries[];
}

export interface NextDose {
  medicationName: string;
  intervalDays: number;
  // ISO timestamps (UTC).
  lastTakenAt: string;
  lastDoseAmountMg: number;
  nextDueAt: string;
}

export interface NextDosesResponse {
  // Only medications currently being taken (see the API's DosingSchedules).
  nextDoses: NextDose[];
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

  getNextDoses(): Observable<NextDosesResponse> {
    return this.http.get<NextDosesResponse>(`${this.apiUrl}/api/levels/next-doses`);
  }
}
