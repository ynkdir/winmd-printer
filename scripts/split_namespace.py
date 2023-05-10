import argparse
import itertools
import json

parser = argparse.ArgumentParser()
parser.add_argument("-d", "--dir", default=".")
parser.add_argument("input")

args = parser.parse_args()

with open(args.input) as f:
    meta_all = sorted(json.load(f), key=lambda td: td["Namespace"])
    for namespace, meta_g in itertools.groupby(meta_all, key=lambda td: td["Namespace"]):
        if namespace == "":
            continue
        print(namespace)
        with open(f"{args.dir}/{namespace}.json", "w") as g:
            json.dump(list(meta_g), g, indent=2)
