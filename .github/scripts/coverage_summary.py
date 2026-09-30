#!/usr/bin/env python3
"""Print a Markdown coverage table from Cobertura reports and optionally enforce a minimum.

Usage: coverage_summary.py <results-dir> [--min-line PERCENT]
Exits 1 when no report is found, or when any report's line coverage is below --min-line.
"""
import argparse
import pathlib
import sys
import xml.etree.ElementTree as ET


def main() -> int:
    parser = argparse.ArgumentParser()
    parser.add_argument("results", nargs="?", default="TestResults")
    parser.add_argument("--min-line", type=float, default=None, help="minimum line coverage %% per test run")
    args = parser.parse_args()

    root = pathlib.Path(args.results)
    # --logger trx also copies each report into <machine>_<time>/In/...; skip those duplicates.
    reports = sorted(r for r in root.rglob("coverage.cobertura.xml") if "In" not in r.relative_to(root).parts)
    if not reports:
        print("No coverage reports found.")
        return 1

    failures = []
    print("### Code coverage (unit + pipeline tests)\n")
    print("| Assemblies covered | Line | Branch |")
    print("|---|---:|---:|")
    for report in reports:
        cov = ET.parse(report).getroot()
        packages = ", ".join(p.get("name", "?") for p in cov.iter("package"))
        line = float(cov.get("line-rate", 0)) * 100
        branch = float(cov.get("branch-rate", 0)) * 100
        print(f"| {packages} | {line:.1f}% | {branch:.1f}% |")
        if args.min_line is not None and line < args.min_line:
            failures.append(f"{packages}: {line:.1f}% < {args.min_line:.0f}%")

    print("\nGenerated EF migrations are excluded (see coverage.runsettings).")
    if args.min_line is not None:
        print(f"\nMinimum line coverage: {args.min_line:.0f}%. " + ("**Below threshold:** " + "; ".join(failures) if failures else "Met."))
    for failure in failures:
        print(f"::error title=Coverage below threshold::{failure}", file=sys.stderr)
    return 1 if failures else 0


if __name__ == "__main__":
    sys.exit(main())
