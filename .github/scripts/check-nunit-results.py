#!/usr/bin/env python3
"""Fail the job on failed or missing runtime test results, and summarize them on the run page.

The device scripts only fail on an empty results file; on ADO, PublishTestResults@2 owns the
failed-tests verdict. GitHub Actions has no equivalent, so this does it.

Usage: check-nunit-results.py <title> <file-or-glob>...
"""
import glob
import os
import sys
import xml.etree.ElementTree as ET

title = sys.argv[1]
files = sorted({f for pattern in sys.argv[2:] for f in glob.glob(pattern)})

total = passed = failed = skipped = 0
failures = []
for path in files:
    for case in ET.parse(path).getroot().iter("test-case"):
        total += 1
        result = case.get("result")
        if result == "Passed":
            passed += 1
        elif result == "Failed":
            failed += 1
            failures.append(case.get("fullname") or case.get("name"))
        else:
            skipped += 1

lines = [f"### {title}", ""]
if not files:
    lines.append("No results file was produced (did the app crash or time out?).")
else:
    lines.append(f"{total} tests: {passed} passed, {failed} failed, {skipped} skipped/inconclusive")
    for name in failures[:100]:
        lines.append(f"- `{name}`")
    if len(failures) > 100:
        lines.append(f"- ... and {len(failures) - 100} more")

summary = "\n".join(lines) + "\n"
print(summary)
if os.environ.get("GITHUB_STEP_SUMMARY"):
    with open(os.environ["GITHUB_STEP_SUMMARY"], "a", encoding="utf-8") as f:
        f.write(summary)

if not files or total == 0:
    print("::error::No runtime test results were found.")
    sys.exit(1)
if failed:
    print(f"::error::{failed} runtime test(s) failed.")
    sys.exit(1)
