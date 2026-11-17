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

/** uPlot sync group shared by every drug chart, so their cursors move together. */
const CURSOR_SYNC_KEY = 'drug-levels';

/** uPlot's setCursor(opts, fireHooks, publishToSyncGroup); the typings omit the third arg. */
type SetCursorWithPublish = (opts: { left: number; top: number }, fire: boolean, pub: boolean) => void;

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
      this.attachTouchHandlers(this.chart);
      // The legend only exists once the chart is built; shrink the plot so it fits in the box.
      this.chart.setSize(this.currentSize());
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
      // Link cursors across all drug charts: moving on one shows the same moment on the others
      // (they share the time grid and range), each with its own legend values.
      cursor: { sync: { key: CURSOR_SYNC_KEY } },
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

  /**
   * uPlot only listens for mouse events. On a touchscreen a tap is turned into a synthetic click
   * (so it moves the cursor), but a drag is treated as a scroll and sends no mouse moves, so the
   * cursor wouldn't follow the finger. Move it ourselves on touchstart/touchmove. Vertical swipes
   * still scroll the page (`touch-action: pan-y` in the SCSS); horizontal drags reach us here.
   * The listeners live on uPlot's own element, so they go when the chart is destroyed.
   */
  private attachTouchHandlers(u: uPlot): void {
    const over = u.over;
    // Tell the app's swipe pager not to change page for drags that start on the plot.
    over.dataset['noPageSwipe'] = '';
    const moveCursor = (e: TouchEvent) => {
      const touch = e.touches[0];
      if (!touch) {
        return;
      }
      const rect = over.getBoundingClientRect();
      // fire hooks (legend visibility) and publish to the sync group, which uPlot doesn't do
      // for a programmatic setCursor unless asked. The runtime takes a third `_pub` argument
      // that uPlot's type definitions omit, hence the cast.
      (u.setCursor as SetCursorWithPublish)(
        {
          left: Math.min(Math.max(touch.clientX - rect.left, 0), rect.width),
          top: Math.min(Math.max(touch.clientY - rect.top, 0), rect.height),
        },
        true,
        true,
      );
    };
    over.addEventListener('touchstart', moveCursor, { passive: true });
    over.addEventListener('touchmove', moveCursor, { passive: true });
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

  /**
   * Size for uPlot. uPlot's height covers the plot and axes but not the legend, which it adds
   * below; subtract the legend's height so the whole chart, legend included, fits in the
   * container and the legend doesn't overflow into the space below it. (The legend is hidden
   * with `visibility`, so it keeps its height even when not shown.)
   */
  private currentSize(): { width: number; height: number } {
    const el = this.chartContainer!.nativeElement;
    const legend = this.chart?.root.querySelector<HTMLElement>('.u-legend');
    const legendHeight = legend?.offsetHeight ?? 0;
    return {
      width: el.clientWidth || 300,
      height: Math.max((el.clientHeight || 200) - legendHeight, 100),
    };
  }
}
