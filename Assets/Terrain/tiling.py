
""" Usage:
tiling.py filename.png

Will create filename.bytes that takes the first 512x512 of the image and makes
it an asset
"""

import sys
if len(sys.argv) != 2:
    print(__doc__)
    sys.exit(1)

from PIL import Image
import struct
import numpy as np
from matplotlib import pylab as plt
from scipy.interpolate import RectBivariateSpline
from imageio import imwrite

from matplotlib import cbook, cm
from matplotlib.colors import LightSource
from scipy.ndimage import zoom

fname = sys.argv[1]
im_frame = Image.open(fname)

max_h = np.array(im_frame.get_flattened_data()).max()

OUT_SIZE = 512

SCALE = 4

img = np.array(im_frame.get_flattened_data()).reshape(im_frame.size + (4,))[:OUT_SIZE,:OUT_SIZE,:1].reshape((OUT_SIZE, OUT_SIZE))
grid = np.arange(0, 512, dtype=np.int32)
rekd = RectBivariateSpline(grid, grid, img)
igrid = np.linspace(0, 512, int(512))
v = rekd(igrid, igrid)
with open(fname.replace('.png', '.bytes'), "wb") as f:
    for item in (v / max_h * 255).astype(np.uint8).flatten():
        f.write(struct.pack("B", item))

#img = Image.fromarray(v.flatten().astype(np.float32))
#img.save('foo.png')
