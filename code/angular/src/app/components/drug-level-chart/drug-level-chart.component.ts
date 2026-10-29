import {
  Component,
  AfterViewInit,
  OnChanges,
  OnDestroy,
  ChangeDetectionStrategy,
  ViewChild,
  ElementRef,
  Input,
} from '@angular/core';
import { CommonModule } from '@angular/common';
import uPlot from 'uplot';

/**
 * Renders one drug's estimated-level series as a uPlot line chart. Presentational: the
 * data (shared ISO times + aligned amounts) and the visible range come in via @Input;
 * fetching lives in the parent. Mirrors the me-tracker weight-chart lifecycle.
 */
@Component({
  selector: 'app-drug-level-chart',
  imports: [CommonModule],
  templateUrl: './drug-level-chart.component.html',
  changeDetection: ChangeDetectionStrategy.Eager,
  styleUrls: ['./drug-level-chart.component.scss'],
})
export class DrugLevelChartComponent implements AfterViewInit, OnChanges, OnDestroy {
  @Input() times: string[] = [];
  @Input() amountsMg: number[] = [];
  @Input() color = '#3b82f6';
  /** Days of history to show (from the latest point), or `null` for all data. */
  @Input() rangeDays: number | null = 30;

  @ViewChild('chartContainer') private chartContainer?: ElementRef<HTMLDivElement>;

  private chart?: uPlot;
  private resizeObserver?: ResizeObserver;
  private viewReady = false;
  private data?: uPlot.AlignedData;

  ngAfterViewInit(): void {
    this.viewReady = true;
    this.setupResizeObserver();
    this.rebuild();
  }

  ngOnChanges(): void {
    this.rebuild();
  }

  ngOnDestroy(): void {
    this.resizeObserver?.disconnect();
    this.chart?.destroy();
  }

  private rebuild(): void {
    this.data = this.toChartData();
    this.renderChart();
  }

  /** Build uPlot [x, y]: x = unix seconds (ascending), filtered to the selected range. */
  private toChartData(): uPlot.AlignedData | undefined {
    if (this.times.length === 0) {
      return undefined;
    }
    let xs = this.times.map((t) => Math.floor(new Date(t).getTime() / 1000));
    let ys = this.amountsMg;

    if (this.rangeDays != null && xs.length > 0) {
      const cutoffSecs = xs[xs.length - 1] - this.rangeDays * 24 * 60 * 60;
      const from = xs.findIndex((x) => x >= cutoffSecs);
      if (from > 0) {
        xs = xs.slice(from);
        ys = ys.slice(from);
      }
    }

    return xs.length > 0 ? [xs, ys] : undefined;
  }

  private renderChart(): void {
    if (!this.viewReady || !this.chartContainer) {
      return;
    }
    if (!this.data) {
      this.chart?.destroy();
      this.chart = undefined;
      return;
    }
    if (this.chart) {
      this.chart.setData(this.data);
    } else {
      this.chart = new uPlot(this.buildOptions(), this.data, this.chartContainer.nativeElement);
    }
  }

  private buildOptions(): uPlot.Options {
    const { width, height } = this.currentSize();
    return {
      width,
      height,
      scales: { x: { time: true } },
      series: [
        {
          label: 'Time',
          value: (_self, rawValue) =>
            rawValue == null ? '--' : new Date(rawValue * 1000).toLocaleDateString(),
        },
        {
          label: 'mg',
          stroke: this.color,
          fill: this.color + '22', // light translucent area under the line
          width: 2,
          points: { show: false },
        },
      ],
      axes: [{}, { label: 'mg' }],
    };
  }

  private setupResizeObserver(): void {
    if (!this.chartContainer) {
      return;
    }
    this.resizeObserver = new ResizeObserver(() => {
      if (this.chart) {
        this.chart.setSize(this.currentSize());
      }
    });
    this.resizeObserver.observe(this.chartContainer.nativeElement);
  }

  private currentSize(): { width: number; height: number } {
    const el = this.chartContainer!.nativeElement;
    return {
      width: el.clientWidth || 300,
      height: el.clientHeight || 200,
    };
  }
}
