import { useId, useState, type KeyboardEvent, type PointerEvent } from 'react';
import type { SpendingTimePoint, TimeGranularity } from '../../api/types';
import { useElementWidth } from '../../hooks/useElementWidth';
import { toDateOnly, useFormatters } from '../../i18n/format';
import { columnPath, granularityName, niceTicks } from './chartMath';
import styles from './Charts.module.css';

interface SpendingChartProps {
  points: SpendingTimePoint[];
  granularity: TimeGranularity | number;
  /** Amounts are shown as money in this currency... */
  currency?: string;
  /** ...or, for counts (new users, payments), as whole numbers. */
  unit?: 'money' | 'count';
  /** Accessible name of the chart, e.g. "Spending over time". */
  label: string;
  height?: number;
  /** Fewer x-axis labels and no y-axis title, for small cards. */
  compact?: boolean;
}

const MAX_BAR = 24;
const TOP = 16;
const BOTTOM = 28;
const RIGHT = 8;

/**
 * Spending per day, week or month as columns. One series, so no legend - the card title says
 * what is shown. Hover, touch or the arrow keys show the exact amount of a column.
 */
export function SpendingChart({
  points,
  granularity,
  currency = 'BAM',
  unit: valueUnit = 'money',
  label,
  height = 260,
  compact = false,
}: SpendingChartProps) {
  const format = useFormatters();
  const [ref, width] = useElementWidth<HTMLDivElement>();
  const [active, setActive] = useState<number | null>(null);
  const descriptionId = useId();
  const unit = granularityName(granularity);

  const totals = points.map((point) => point.total);
  // Counts never get ticks like 0,5 - a step is at least 1.
  const ticks = niceTicks(Math.max(0, ...totals), compact ? 3 : 4, valueUnit === 'count' ? 1 : 0);
  const value = (amount: number) => (valueUnit === 'count' ? format.number(amount) : format.money(amount, currency));
  const top = ticks[ticks.length - 1] || 1;
  const step = ticks[1] ?? 1;
  const tickDigits = step < 1 ? 2 : 0;
  const tickLabels = ticks.map((tick) => format.number(tick, tickDigits));

  // Room for the longest y-axis label (about 7px per character at 12px).
  const left = Math.max(...tickLabels.map((text) => text.length)) * 7 + 14;
  const plotWidth = Math.max(0, width - left - RIGHT);
  const plotHeight = height - TOP - BOTTOM;
  const band = points.length > 0 ? plotWidth / points.length : 0;
  // Columns never fill their slot: at most 24px, and always at least 2px of air between them.
  const barWidth = Math.max(1, Math.min(MAX_BAR, band * 0.7, band - 2));

  const y = (value: number) => TOP + plotHeight - (value / top) * plotHeight;

  const periodLabel = (point: SpendingTimePoint) => {
    if (unit === 'Month') return format.month(toDateOnly(point.periodStart).slice(0, 7));
    if (unit === 'Week') return format.dateRange(point.periodStart, point.periodEnd);
    return format.longDate(point.periodStart);
  };
  const axisLabel = (point: SpendingTimePoint) =>
    unit === 'Month' ? format.monthShort(point.periodStart) : format.dayMonth(point.periodStart);

  // Every n-th label, so they never run into each other.
  const labelSpacing = unit === 'Month' ? 44 : compact ? 72 : 64;
  const labelEvery = band > 0 ? Math.max(1, Math.ceil(labelSpacing / band)) : 1;

  function indexAt(event: PointerEvent<SVGRectElement>) {
    const box = event.currentTarget.getBoundingClientRect();
    const index = Math.floor((event.clientX - box.left) / band);
    return Math.min(points.length - 1, Math.max(0, index));
  }

  function onKeyDown(event: KeyboardEvent<HTMLDivElement>) {
    if (points.length === 0) return;
    const keys: Record<string, number> = { ArrowRight: 1, ArrowLeft: -1, Home: -Infinity, End: Infinity };
    if (!(event.key in keys)) return;
    event.preventDefault();
    const delta = keys[event.key];
    setActive((current) => {
      if (delta === -Infinity) return 0;
      if (delta === Infinity) return points.length - 1;
      if (current === null) return delta > 0 ? 0 : points.length - 1;
      return Math.min(points.length - 1, Math.max(0, current + delta));
    });
  }

  const activePoint = active !== null ? points[active] : null;
  const tooltipX = active !== null ? left + band * (active + 0.5) : 0;

  return (
    <div className={styles.chart} ref={ref} style={{ height }}>
      <div
        className={styles.chartFocus}
        tabIndex={points.length > 0 ? 0 : -1}
        role="group"
        aria-label={label}
        aria-describedby={descriptionId}
        onKeyDown={onKeyDown}
        onBlur={() => setActive(null)}
      >
        {width > 0 && (
          <svg width={width} height={height} aria-hidden="true" className={styles.svg}>
            {ticks.map((tick, index) => (
              <g key={tick}>
                <line x1={left} x2={width - RIGHT} y1={y(tick)} y2={y(tick)} className={index === 0 ? styles.baseline : styles.grid} />
                <text x={left - 8} y={y(tick)} dy="0.32em" textAnchor="end" className={styles.tick}>
                  {tickLabels[index]}
                </text>
              </g>
            ))}

            {/* The slot under the pointer is tinted, so even a day with 0 shows which one is meant. */}
            {active !== null && (
              <rect x={left + band * active} y={TOP} width={band} height={plotHeight} rx={4} className={styles.slot} />
            )}

            {points.map((point, index) => {
              const x = left + band * index + (band - barWidth) / 2;
              const barTop = y(point.total);
              return (
                <path
                  key={point.periodStart}
                  d={columnPath(x, barTop, barWidth, TOP + plotHeight - barTop)}
                  className={index === active ? `${styles.bar} ${styles.barActive}` : styles.bar}
                />
              );
            })}

            {points.map((point, index) => {
              if (index % labelEvery !== 0) return null;
              const center = left + band * (index + 0.5);
              // The first and last label may not stick out of the chart.
              const anchor = center - 28 < 0 ? 'start' : center + 28 > width ? 'end' : 'middle';
              return (
                <text key={point.periodStart} x={center} y={height - 8} textAnchor={anchor} className={styles.tick}>
                  {axisLabel(point)}
                </text>
              );
            })}

            {/* One hit area over the whole plot: the pointer only has to be over the right slot. */}
            <rect
              x={left}
              y={TOP}
              width={plotWidth}
              height={plotHeight}
              fill="transparent"
              onPointerMove={(event) => setActive(indexAt(event))}
              onPointerDown={(event) => setActive(indexAt(event))}
              onPointerLeave={(event) => {
                if (event.pointerType === 'mouse') setActive(null);
              }}
            />
          </svg>
        )}
      </div>

      {activePoint && (
        <div
          className={styles.tooltip}
          style={{
            left: Math.min(Math.max(tooltipX, 80), width - 80),
            top: Math.max(y(activePoint.total) - 10, 52),
          }}
          aria-hidden="true"
        >
          <strong className={valueUnit === 'money' ? 'money' : undefined}>{value(activePoint.total)}</strong>
          <span>{periodLabel(activePoint)}</span>
        </div>
      )}

      <p id={descriptionId} className="visually-hidden" aria-live="polite">
        {activePoint ? `${periodLabel(activePoint)}: ${value(activePoint.total)}` : ''}
      </p>
    </div>
  );
}

/** The same numbers as a table - for screen readers, and for anyone who wants exact values. */
export function SpendingTable({
  points,
  granularity,
  currency = 'BAM',
  unit: valueUnit = 'money',
  caption,
}: Omit<SpendingChartProps, 'label' | 'height' | 'compact'> & { caption: string }) {
  const format = useFormatters();
  const unit = granularityName(granularity);
  const periodLabel = (point: SpendingTimePoint) => {
    if (unit === 'Month') return format.month(toDateOnly(point.periodStart).slice(0, 7));
    if (unit === 'Week') return format.dateRange(point.periodStart, point.periodEnd);
    return format.longDate(point.periodStart);
  };

  return (
    <div className={styles.tableWrap}>
      <table className={styles.table}>
        <caption className="visually-hidden">{caption}</caption>
        <tbody>
          {points.map((point) => (
            <tr key={point.periodStart}>
              <th scope="row">{periodLabel(point)}</th>
              <td className={valueUnit === 'money' ? 'money' : undefined}>
                {valueUnit === 'count' ? format.number(point.total) : format.money(point.total, currency)}
              </td>
            </tr>
          ))}
        </tbody>
      </table>
    </div>
  );
}
