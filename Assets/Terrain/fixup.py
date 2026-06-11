
import os, re

for name in os.listdir('.'):
    if name.endswith('.meta'):
        f = open(name, "r")
        lines = f.readlines()
        f.close()
        for i, line in enumerate(lines):
            if 'isReadable' in line:
                lines[i] = line.replace('0', '1')
        f = open(name, "w")
        f.write("".join(lines))
        f.close()
