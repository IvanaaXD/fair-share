"""
Turns the CSV results of the stepped runs into one table for the thesis.

  python summary.py results/mix

reads results/mix_10_stats.csv, results/mix_50_stats.csv ... (the "Aggregated" row of each)
and prints a Markdown table; it also saves it as results/mix_table.md.
"""
from __future__ import annotations

import csv
import glob
import re
import sys
from pathlib import Path


def main() -> int:
    if len(sys.argv) != 2:
        print(__doc__)
        return 2
    prefix = sys.argv[1]
    rows = []
    for path in glob.glob(f"{prefix}_*_stats.csv"):
        # Only "<prefix>_<number>_stats.csv": results/mix must not pick up results/mix_after_200.
        match = re.fullmatch(re.escape(Path(prefix).name) + r"_(\d+)_stats\.csv", Path(path).name)
        if not match:
            continue
        with open(path, newline="", encoding="utf-8") as file:
            for row in csv.DictReader(file):
                if row["Name"] == "Aggregated":
                    requests_total = int(row["Request Count"])
                    failures = int(row["Failure Count"])
                    rows.append((
                        int(match.group(1)),
                        float(row["Requests/s"]),
                        float(row["50%"]),
                        float(row["95%"]),
                        float(row["99%"]),
                        failures * 100 / requests_total if requests_total else 0.0,
                        requests_total,
                    ))
    if not rows:
        print(f"No files {prefix}_*_stats.csv")
        return 1

    lines = [
        "| Users | Requests/s | Median (ms) | p95 (ms) | p99 (ms) | Failures (%) | Total requests |",
        "|---:|---:|---:|---:|---:|---:|---:|",
    ]
    for users, rps, p50, p95, p99, error_rate, total in sorted(rows):
        lines.append(f"| {users} | {rps:.1f} | {p50:.0f} | {p95:.0f} | {p99:.0f} | {error_rate:.2f} | {total} |")
    table = "\n".join(lines)
    print(table)
    Path(f"{prefix}_table.md").write_text(table + "\n", encoding="utf-8")
    return 0


if __name__ == "__main__":
    sys.exit(main())
