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

// Space reserved for the y-axis: tick labels plus the "mg" axis label. These are uPlot's
// defaults, pinned so a library update can't move the plot edge. uPlot adds no extra left
// padding when there is a left axis, so the drawn y-axis line sits exactly this far in from
// the chart's left edge; the parent uses it to align headings and notes with that line.
const Y_AXIS_SIZE_PX = 50;
const Y_AXIS_LABEL_SIZE_PX = 30;
export const PLOT_LEFT_PX = Y_AXIS_SIZE_PX + Y_AXIS_LABEL_SIZE_PX;

/**
 * Renders one drug's estimated-level series as a uPlot line chart. Presentational: the
 * data (shared ISO times + aligned amounts) and the visible range come in via @Input;
 * fetching lives in the parent.
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
  /**
   * Fixed y-axis maximum (y always starts at 0), so the scale reflects real amounts rather than
   * stretching a near-zero tail to fill the chart. `null` auto-scales.
   */
  @Input() yMax: number | null = null;
  /** Days of history to show (from the latest point), or `null` for all data. */
  @Input() rangeDays: number | null = 30;

  @ViewChild('chartContainer') private chartContainer?: ElementRef<HTMLDivElement>;

  private chart?: uPlot;
  private resizeObserver?: ResizeObserver;
  private viewReady = false;
  private data?: uPlot.AlignedData;
  /** The yMax the current chart was built with; the scale range is fixed at construction. */
  private builtYMax: number | null = null;

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

    if (xs.length === 0) {
      return undefined;
    }
    return [xs, ys];
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
    if (this.chart && this.builtYMax !== this.yMax) {
      this.chart.destroy();
      this.chart = undefined;
    }
    if (this.chart) {
      this.chart.setData(this.data);
    } else {
      this.chart = new uPlot(this.buildOptions(), this.data, this.chartContainer.nativeElement);
      this.builtYMax = this.yMax;
    }
  }

  private buildOptions(): uPlot.Options {
    const { width, height } = this.currentSize();
    return {
      width,
      height,
      scales: {
        x: { time: true },
        ...(this.yMax != null ? { y: { range: [0, this.yMax] as uPlot.Range.MinMax } } : {}),
      },
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
      axes: [{}, { label: 'mg', size: Y_AXIS_SIZE_PX, labelSize: Y_AXIS_LABEL_SIZE_PX }],
      hooks: {
        // Show the legend (values at the cursor) only while the cursor is over the plot; uPlot
        // sets cursor.left to a negative value when it leaves. Styled in the component SCSS.
        setCursor: [
          (u) => {
            const active = u.cursor.left != null && u.cursor.left >= 0;
            u.root.classList.toggle('cursor-active', active);
          },
        ],
      },
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
