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
 * Share of the chart given to the projection: the future shown is a quarter of the past shown,
 * so past and future split the chart 80/20 whatever the range.
 */
const FUTURE_TO_PAST_RATIO = 0.25;

const TODAY_LINE_COLOR = '#9ca3af'; // gray-400

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
  /** Days of history to show (back from now), or `null` for all of it. */
  @Input() rangeDays: number | null = 30;
  /**
   * Unix seconds of "now": points up to here are history (solid), later ones the projection
   * (dashed). A "today" line is drawn here.
   */
  @Input() nowSecs = 0;

  @ViewChild('chartContainer') private chartContainer?: ElementRef<HTMLDivElement>;

  private chart?: uPlot;
  private resizeObserver?: ResizeObserver;
  private viewReady = false;
  private data?: uPlot.AlignedData;
  /** The yMax the current chart was built with; the scale range is fixed at construction. */
  private builtYMax: number | null = null;
  /** The colour the current chart was built with; series strokes are fixed at construction. */
  private builtColor: string | null = null;
  /** The current chart's legend, watched so the plot re-fits once uPlot has laid it out. */
  private observedLegend?: HTMLElement;

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

  private destroyChart(): void {
    if (this.observedLegend) {
      this.resizeObserver?.unobserve(this.observedLegend);
      this.observedLegend = undefined;
    }
    this.chart?.destroy();
    this.chart = undefined;
  }

  private rebuild(): void {
    this.data = this.toChartData();
    this.renderChart();
  }

  /**
   * Build uPlot [x, history, projection]: x = unix seconds (ascending), windowed to the selected
   * range back from now plus a quarter of that forward. The two y series share the point at now
   * so the line is continuous; each is null on the other's side.
   */
  private toChartData(): uPlot.AlignedData | undefined {
    if (this.times.length === 0) {
      return undefined;
    }
    const allXs = this.times.map((t) => Math.floor(new Date(t).getTime() / 1000));

    // Past shown: the selected range, capped at the history there is.
    const firstSecs = allXs[0];
    const startSecs =
      this.rangeDays == null
        ? firstSecs
        : Math.max(firstSecs, this.nowSecs - this.rangeDays * 24 * 60 * 60);
    const endSecs = this.nowSecs + (this.nowSecs - startSecs) * FUTURE_TO_PAST_RATIO;

    const xs: number[] = [];
    const history: (number | null)[] = [];
    const projection: (number | null)[] = [];
    allXs.forEach((x, i) => {
      if (x < startSecs || x > endSecs) {
        return;
      }
      const y = this.amountsMg[i];
      xs.push(x);
      history.push(x <= this.nowSecs ? y : null);
      projection.push(x >= this.nowSecs ? y : null);
    });

    if (xs.length === 0) {
      return undefined;
    }
    return [xs, history, projection];
  }

  private renderChart(): void {
    if (!this.viewReady || !this.chartContainer) {
      return;
    }
    if (!this.data) {
      this.destroyChart();
      return;
    }
    if (this.chart && (this.builtYMax !== this.yMax || this.builtColor !== this.color)) {
      this.destroyChart();
    }
    if (this.chart) {
      this.chart.setData(this.data);
    } else {
      this.chart = new uPlot(this.buildOptions(), this.data, this.chartContainer.nativeElement);
      this.builtYMax = this.yMax;
      this.builtColor = this.color;
      this.attachTouchHandlers(this.chart);
      // The legend only exists once the chart is built; shrink the plot so it fits in the box.
      this.chart.setSize(this.currentSize());
      // Right after construction uPlot hasn't finished laying the legend out (it can measure
      // narrow and tall, which would squash the plot to its minimum height), so also re-fit
      // whenever the legend's size changes. Its height depends only on the width, so the
      // re-fit can't trigger itself.
      this.observedLegend = this.chart.root.querySelector<HTMLElement>('.u-legend') ?? undefined;
      if (this.observedLegend) {
        this.resizeObserver?.observe(this.observedLegend);
      }
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
          // One legend value for both series: past now the history is null, so show the
          // projection's value instead (its own legend entry is hidden in the SCSS).
          // Same formatting as uPlot's default legend values.
          value: (u, rawValue, _seriesIdx, idx) => {
            if (idx == null) {
              return '--';
            }
            const v = rawValue ?? u.data[2][idx];
            return v == null ? '' : uPlot.fmtNum(v);
          },
        },
        {
          label: 'projected mg',
          stroke: this.color,
          fill: this.color + '0d', // fainter than the history's fill
          width: 2,
          dash: [6, 4],
          points: { show: false },
        },
      ],
      axes: [{}, { label: 'mg', size: Y_AXIS_SIZE_PX, labelSize: Y_AXIS_LABEL_SIZE_PX }],
      // Link cursors across all drug charts: moving on one shows the same moment on the others
      // (they share the time grid and range), each with its own legend values.
      cursor: { sync: { key: CURSOR_SYNC_KEY } },
      hooks: {
        draw: [(u) => this.drawTodayLine(u)],
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

  /** A thin vertical line at now, labelled "today", between history and projection. */
  private drawTodayLine(u: uPlot): void {
    const x = Math.round(u.valToPos(this.nowSecs, 'x', true));
    const { top, height, left, width } = u.bbox;
    if (x < left || x > left + width) {
      return;
    }
    const ctx = u.ctx;
    const pxRatio = window.devicePixelRatio || 1;
    ctx.save();
    ctx.strokeStyle = TODAY_LINE_COLOR;
    ctx.lineWidth = pxRatio;
    ctx.beginPath();
    ctx.moveTo(x, top);
    ctx.lineTo(x, top + height);
    ctx.stroke();
    ctx.fillStyle = TODAY_LINE_COLOR;
    ctx.font = `${10 * pxRatio}px sans-serif`;
    ctx.textAlign = 'right';
    ctx.textBaseline = 'top';
    ctx.fillText('today', x - 3 * pxRatio, top + 2 * pxRatio);
    ctx.restore();
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
    // Watches the container (and, once a chart exists, its legend; see renderChart).
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
