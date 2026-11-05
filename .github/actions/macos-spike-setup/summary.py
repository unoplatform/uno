#!/usr/bin/env python3
# Writes a pass/fail line to the job summary: GitHub has no equivalent of ADO's PublishTestResults.
import glob
import os
import sys
import xml.etree.ElementTree as ET

title, patterns = sys.argv[1], sys.argv[2:]
files = [f for p in patterns for f in glob.glob(p)]
lines = []

for path in files:
    root = ET.parse(path).getroot()
    a = root.attrib
    lines.append(
        f"**{title}**: total={a.get('total', a.get('testcasecount', '?'))} "
        f"passed={a.get('passed', '?')} failed={a.get('failed', '?')} "
        f"skipped={a.get('skipped', '?')} inconclusive={a.get('inconclusive', '?')}"
    )
    for case in root.iter("test-case"):
        if case.get("result") == "Failed":
            lines.append(f"- {case.get('fullname')}")

if not files:
    lines.append(f"**{title}**: NO RESULTS FILE")

text = "\n".join(lines[:200])
print(text)
summary = os.environ.get("GITHUB_STEP_SUMMARY")
if summary:
    with open(summary, "a", encoding="utf-8") as f:
        f.write(text + "\n")
