import { Component, OnInit, ChangeDetectionStrategy, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { WeightReportService } from '../../services/weight-report.service';

@Component({
  selector: 'app-weight-report',
  imports: [CommonModule],
  templateUrl: './weight-report.component.html',
  changeDetection: ChangeDetectionStrategy.Eager,
  styleUrls: ['./weight-report.component.scss'],
})
export class WeightReportComponent implements OnInit {
  private readonly weightReportService = inject(WeightReportService);

  readonly weightReport$ = this.weightReportService.weightReport$;
  readonly isLoading$ = this.weightReportService.isLoading$;
  readonly error$ = this.weightReportService.error$;

  ngOnInit(): void {
    this.loadWeightReport();
  }

  loadWeightReport(): void {
    this.weightReportService.loadWeightReport();
  }

  onRetry(): void {
    this.loadWeightReport();
  }
}
