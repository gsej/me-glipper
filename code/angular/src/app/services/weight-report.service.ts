import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { BehaviorSubject } from 'rxjs';
import { SettingsService } from '../settings/settings.service';

export interface WeightReportEntry {
  date: string;
  recordedWeight: number;
  averageWeight: number;
  bmi: number;
  oneWeekChange: number;
  twoWeekChange: number;
  fourWeekChange: number;
  twelveWeekChange: number;
}

export interface WeightReport {
  entries: WeightReportEntry[];
}

@Injectable({
  providedIn: 'root'
})
export class WeightReportService {

  private weightReportSubject = new BehaviorSubject<WeightReport | null>(null);
  public weightReport$ = this.weightReportSubject.asObservable();

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

  loadWeightReport(): void {
    this.isLoadingSubject.next(true);
    this.errorSubject.next(null);

    this.http.get<WeightReport>(`${this.apiUrl}/api/report`)
      .subscribe({
        next: (data) => {

          const sortedEntries = [...data.entries].sort((a, b) => {
            return new Date(b.date).getTime() - new Date(a.date).getTime();
          });

          const formattedRecords = sortedEntries.map(entry => ({
            date: new Date(entry.date).toLocaleDateString(),
            recordedWeight: entry.recordedWeight,
            averageWeight: entry.averageWeight,
            bmi: entry.bmi,
            oneWeekChange: entry.oneWeekChange,
            twoWeekChange: entry.twoWeekChange,
            fourWeekChange: entry.fourWeekChange,
            twelveWeekChange: entry.twelveWeekChange

          }));
          this.weightReportSubject.next({ entries: formattedRecords });
          this.isLoadingSubject.next(false);
        },
        error: (error) => {
          console.error('Error fetching weight report:', error);
          this.errorSubject.next(error.status === 401 ? 'Error: 401 - API key must be supplied' : `Error: ${error.status || 'Unknown'}`);
          this.isLoadingSubject.next(false);
        }
      });
  }
}
