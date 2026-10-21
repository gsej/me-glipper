import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { BehaviorSubject, Observable } from 'rxjs';
import { tap } from 'rxjs/operators';
import { SettingsService } from '../settings/settings.service';

export interface DoseRecord {
  doseId: string;
  takenAt: string;
  medicationName: string;
  doseAmountMg: number;
  notes?: string;
}

export interface DosesCollection {
  doseRecords: DoseRecord[];
}

export interface NewDose {
  takenAt: Date;
  medicationName: string;
  doseAmountMg: number;
  notes?: string;
}

@Injectable({
  providedIn: 'root',
})
export class DoseService {
  private doseRecordsSubject = new BehaviorSubject<DoseRecord[]>([]);
  public doseRecords$ = this.doseRecordsSubject.asObservable();

  private isLoadingSubject = new BehaviorSubject<boolean>(false);
  public isLoading$ = this.isLoadingSubject.asObservable();

  private errorSubject = new BehaviorSubject<string | null>(null);
  public error$ = this.errorSubject.asObservable();

  private apiUrl: string;

  constructor(
    private http: HttpClient,
    private settingsService: SettingsService,
  ) {
    this.apiUrl = this.settingsService.settings.apiUrl;
  }

  loadDoses(): void {
    this.isLoadingSubject.next(true);
    this.errorSubject.next(null);

    this.http.get<DosesCollection>(`${this.apiUrl}/api/doses`).subscribe({
      next: (data) => {
        // Most recent dose first.
        const sortedRecords = [...data.doseRecords].sort(
          (a, b) => new Date(b.takenAt).getTime() - new Date(a.takenAt).getTime(),
        );
        this.doseRecordsSubject.next(sortedRecords);
        this.isLoadingSubject.next(false);
      },
      error: (error) => {
        console.error('Error fetching doses:', error);
        this.errorSubject.next(
          error.status === 401
            ? 'Error: 401 - API key must be supplied'
            : `Error: ${error.status || 'Unknown'}`,
        );
        this.isLoadingSubject.next(false);
      },
    });
  }

  addDose(dose: NewDose): Observable<void> {
    const payload = {
      takenAt: dose.takenAt,
      medicationName: dose.medicationName,
      doseAmountMg: dose.doseAmountMg,
      notes: dose.notes,
    };

    return this.http.post<void>(`${this.apiUrl}/api/dose`, payload).pipe(
      // After successfully adding a new dose, refresh the list.
      tap(() => this.loadDoses()),
    );
  }

  updateNotes(doseId: string, notes: string | null): Observable<void> {
    return this.http
      .put<void>(`${this.apiUrl}/api/dose/${doseId}`, { notes })
      .pipe(tap(() => this.loadDoses()));
  }

  deleteDose(doseId: string): Observable<void> {
    return this.http
      .delete<void>(`${this.apiUrl}/api/dose/${doseId}`)
      .pipe(tap(() => this.loadDoses()));
  }
}
