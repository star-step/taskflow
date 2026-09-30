#!/usr/bin/env python3
"""Print a Markdown coverage table from Cobertura reports (one per test project).

Usage: coverage_summary.py <results-dir>   (e.g. TestResults; searched recursively)
"""
import pathlib
import sys
import xml.etree.ElementTree as ET


def main() -> int:
    root = pathlib.Path(sys.argv[1] if len(sys.argv) > 1 else "TestResults")
    # --logger trx also copies each report into <machine>_<time>/In/...; skip those duplicates.
    reports = sorted(r for r in root.rglob("coverage.cobertura.xml") if "In" not in r.relative_to(root).parts)
    if not reports:
        print("No coverage reports found.")
        return 1

    print("### Code coverage (unit + pipeline tests)\n")
    print("| Assemblies covered | Line | Branch |")
    print("|---|---:|---:|")
    for report in reports:
        cov = ET.parse(report).getroot()
        packages = ", ".join(p.get("name", "?") for p in cov.iter("package"))
        line = float(cov.get("line-rate", 0)) * 100
        branch = float(cov.get("branch-rate", 0)) * 100
        print(f"| {packages} | {line:.1f}% | {branch:.1f}% |")
    print("\nGenerated EF migrations are excluded (see coverage.runsettings).")
    return 0


if __name__ == "__main__":
    sys.exit(main())
