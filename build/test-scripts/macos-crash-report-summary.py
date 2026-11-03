#!/usr/bin/env python3
# Prints the exception and the crashed thread's frames from a macOS .ips crash report.
import json
import sys

with open(sys.argv[1], encoding="utf-8") as f:
	header = f.readline()
	report = json.loads(f.read())

print(header.strip())
print("exception:", json.dumps(report.get("exception")), "termination:", json.dumps(report.get("termination")))
images = report.get("usedImages", [])
for thread in report.get("threads", []):
	if not thread.get("triggered"):
		continue
	print("crashed thread:", thread.get("name") or thread.get("queue") or thread.get("id"))
	for i, frame in enumerate(thread.get("frames", [])):
		image = images[frame["imageIndex"]] if frame.get("imageIndex", -1) < len(images) else {}
		print(f"  {i:3} {image.get('name', '?'):40} {frame.get('symbol', hex(frame.get('imageOffset', 0)))} +{frame.get('symbolLocation', 0)}")
