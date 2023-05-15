import argparse
import json
import sys

parser = argparse.ArgumentParser()
parser.add_argument("-o", "--output")
parser.add_argument("input", nargs="+")

args = parser.parse_args()

meta = []
for path in args.input:
    with open(path) as f:
        meta.extend(json.load(f))

if args.output is None:
    output = sys.stdout
else:
    output = open(args.output, "w")

with output:
    json.dump(meta, output, indent=2)
