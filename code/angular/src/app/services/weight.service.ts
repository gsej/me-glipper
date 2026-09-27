import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { BehaviorSubject, Observable } from 'rxjs';
import { tap } from 'rxjs/operators';
import { SettingsService } from '../settings/settings.service';

export interface WeightRecord {
  weightId: string;
  date: string;
  weight: number;
  comment?: string;
}

export interface WeightsCollection {
  weightRecords: WeightRecord[];
}

@Injectable({
  providedIn: 'root'
})
export class WeightService {

  private weightRecordsSubject = new BehaviorSubject<WeightRecord[]>([]);
  public weightRecords$ = this.weightRecordsSubject.asObservable();

  private isLoadingSubject = new BehaviorSubject<boolean>(false);
  public isLoading$ = this.isLoadingSubject.asObservable();

  private errorSubject = new BehaviorSubject<string | null>(null);
  public error$ = this.errorSubject.asObservable();

  private apiUrl: string;

  constructor(
    private http: HttpClient,
    private settingsService: SettingsService
  ) {
    this.apiUrl = this.settingsService.settings.apiUrl;
  }

  loadWeightRecords(): void {
    this.isLoadingSubject.next(true);
    this.errorSubject.next(null);

    this.http.get<WeightsCollection>(`${this.apiUrl}/api/weights`)
      .subscribe({
        next: (data) => {

          const sortedRecords = [...data.weightRecords].sort((a, b) => {
            return new Date(b.date).getTime() - new Date(a.date).getTime();
          });

          const formattedRecords = sortedRecords.map(record => ({
            weightId: record.weightId,
            date: new Date(record.date).toLocaleDateString(),
            weight: record.weight,
            comment: record.comment
          }));
          this.weightRecordsSubject.next(formattedRecords);
          this.isLoadingSubject.next(false);
        },
        error: (error) => {
          console.error('Error fetching weight records:', error);
          this.errorSubject.next(`Error: ${error.status || 'Unknown'}`);
          this.isLoadingSubject.next(false);
        }
      });
  }

  addWeightRecord(weight: number, comment?: string): Observable<void> {
    const payload = {
      weight: weight,
      date: new Date(),
      comment: comment
    };

    return this.http.post<void>(`${this.apiUrl}/api/weight`, payload)
      .pipe(
        tap(() => {
          // After successfully adding a new record, refresh the list
          this.loadWeightRecords();
        })
      );
  }

  updateComment(weightId: string, comment: string | null): Observable<void> {
    return this.http.put<void>(`${this.apiUrl}/api/weight/${weightId}`, { comment })
      .pipe(
        tap(() => {
          // After successfully updating, refresh the list
          this.loadWeightRecords();
        })
      );
  }

  deleteWeightRecord(weightId: string): Observable<void> {
    return this.http.delete<void>(`${this.apiUrl}/api/weight/${weightId}`)
      .pipe(
        tap(() => {
          // After successfully deleting a record, refresh the list
          this.loadWeightRecords();
        })
      );
  }
}
