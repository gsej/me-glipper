import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { SettingsService } from '../settings/settings.service';

/**
 * The user's current vial of a medication: how much medication (mg) in how much liquid (ml),
 * and the date it was started.
 */
export interface Vial {
  medicationName: string;
  initialAmountMg: number;
  initialVolumeMl: number;
  // A calendar date, yyyy-MM-dd (no time or timezone).
  startedOn: string;
}

@Injectable({
  providedIn: 'root',
})
export class VialService {
  private apiUrl: string;

  constructor(
    private http: HttpClient,
    settingsService: SettingsService,
  ) {
    this.apiUrl = settingsService.settings.apiUrl;
  }

  /** The user's vials. Medications with no vial saved are left out. */
  getVials(): Observable<Vial[]> {
    return this.http.get<Vial[]>(`${this.apiUrl}/api/vials`);
  }

  /** Sets the user's vial of a medication, replacing any they already have. */
  saveVial(vial: Vial): Observable<void> {
    return this.http.put<void>(
      `${this.apiUrl}/api/vials/${encodeURIComponent(vial.medicationName)}`,
      {
        initialAmountMg: vial.initialAmountMg,
        initialVolumeMl: vial.initialVolumeMl,
        startedOn: vial.startedOn,
      },
    );
  }
}
