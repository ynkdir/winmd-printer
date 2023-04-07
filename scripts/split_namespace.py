import json
import sys

def main():
    ns = {}
    meta_all = json.load(open(sys.argv[1]))
    for td in meta_all:
        if td["Namespace"] == "":
            continue
        if td["Namespace"] not in ns:
            ns[td["Namespace"]] = []
        ns[td["Namespace"]].append(td)
    for namespace, meta in ns.items():
        print(namespace)
        json.dump(meta, open(f"json/{namespace}.json", "w"))

if __name__ == "__main__":
    main()
