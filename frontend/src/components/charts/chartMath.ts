import type { TimeGranularity } from '../../api/types';

/**
 * "Nice" axis steps: 0, 50, 100, 150 rather than 0, 47.3, 94.6. Returns the tick values from
 * zero up to the first tick at or above the maximum, with about `count` intervals.
 */
export function niceTicks(max: number, count = 4): number[] {
  if (!(max > 0)) return [0];
  const rough = max / count;
  const magnitude = 10 ** Math.floor(Math.log10(rough));
  const step = [1, 2, 2.5, 5, 10].map((factor) => factor * magnitude).find((candidate) => candidate >= rough) ?? 10 * magnitude;
  const ticks: number[] = [];
  for (let value = 0; value < max + step * 0.0001; value += step) {
    ticks.push(Math.round(value * 1e6) / 1e6);
  }
  if (ticks[ticks.length - 1] < max) ticks.push(Math.round((ticks[ticks.length - 1] + step) * 1e6) / 1e6);
  return ticks;
}

/**
 * SVG path of a column with rounded top corners and a square base (the base sits on the axis).
 */
export function columnPath(x: number, y: number, width: number, height: number, radius = 4): string {
  if (height <= 0 || width <= 0) return '';
  const r = Math.min(radius, width / 2, height);
  const bottom = y + height;
  return [
    `M${x},${bottom}`,
    `V${y + r}`,
    `Q${x},${y} ${x + r},${y}`,
    `H${x + width - r}`,
    `Q${x + width},${y} ${x + width},${y + r}`,
    `V${bottom}`,
    'Z',
  ].join(' ');
}

const GRANULARITIES: TimeGranularity[] = ['Day', 'Week', 'Month'];

/** The backend sends the enum as text; a backend without JsonStringEnumConverter sends 0/1/2. */
export function granularityName(value: TimeGranularity | number): TimeGranularity {
  return typeof value === 'number' ? (GRANULARITIES[value] ?? 'Day') : value;
}
