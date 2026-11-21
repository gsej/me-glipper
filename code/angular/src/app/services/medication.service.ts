import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { shareReplay, tap } from 'rxjs/operators';
import { SettingsService } from '../settings/settings.service';

/** A medication a dose can be recorded against (from the API's Medications table). */
export interface Medication {
  medicationId: number;
  name: string;
  defaultDosingIntervalDays: number;
  halfLifeHours: number;
  bioavailability: number;
  tmaxHours: number;
  // Hex colour for the medication's chart, e.g. '#3b82f6'.
  chartColor: string;
}

@Injectable({
  providedIn: 'root',
})
export class MedicationService {
  private apiUrl: string;
  private medications$: Observable<Medication[]> | null = null;

  constructor(
    private http: HttpClient,
    settingsService: SettingsService,
  ) {
    this.apiUrl = settingsService.settings.apiUrl;
  }

  /**
   * The medications, in display order. They are reference data that only change with a
   * deploy, so the list is fetched once and shared by every caller; a failed fetch is not
   * cached, so the next call tries again.
   */
  getMedications(): Observable<Medication[]> {
    if (!this.medications$) {
      this.medications$ = this.http.get<Medication[]>(`${this.apiUrl}/api/medications`).pipe(
        tap({ error: () => (this.medications$ = null) }),
        shareReplay(1),
      );
    }
    return this.medications$;
  }
}
