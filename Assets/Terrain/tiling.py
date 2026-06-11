from PIL import Image
import struct
import numpy as np
from matplotlib import pylab as plt
from scipy.interpolate import RectBivariateSpline
from imageio import imwrite

from matplotlib import cbook, cm
from matplotlib.colors import LightSource
from scipy.ndimage import zoom

im_frame = Image.open('terrain.png')

max_h = np.array(im_frame.get_flattened_data()).max()

OUT_SIZE = 1024

def get_tile(v, ofs_x, ofs_y, size):
    # img = np.array(im_frame.get_flattened_data()).reshape((512, 512))[ofs_x:ofs_x + size, ofs_y:size + ofs_y]
    
    # grid = np.arange(0, size, dtype=np.int32)
    # rekd = RectBivariateSpline(grid, grid, img)
    # igrid = np.linspace(0, size, out_size)
    # v = rekd(igrid, igrid)

    rofs_x = ofs_x * TILE_SIZE * SCALE
    rofs_y = ofs_y * TILE_SIZE * SCALE
    if ofs_x < TILES - 1 and ofs_y < TILES - 1:
        v = v[rofs_x:rofs_x + size + 1, rofs_y:rofs_y + size + 1]
    if ofs_x == TILES - 1 and ofs_y < TILES - 1:
        v = v[rofs_x: rofs_x + size, rofs_y:rofs_y + size + 1]
        v = np.concat([v, v[-1, :].reshape(1, size + 1)], axis=0)
    if ofs_x < TILES - 1 and ofs_y == TILES - 1:
        v = v[rofs_x: rofs_x + size + 1, rofs_y:rofs_y + size]
        v = np.concat([v, v[:, -1].reshape(size + 1, 1)], axis=1)
    if ofs_x == TILES - 1 and ofs_y == TILES - 1:
        v = v[rofs_x:rofs_x + size, rofs_y:rofs_y + size]
        v = np.concat([v, v[-1, :].reshape(1, size)], axis=0)
        v = np.concat([v, v[:, -1].reshape(size + 1, 1)], axis=1)
    v = (v / max_h * 255).astype(np.uint8)
    return v

TILE_SIZE = 32
TILES = int(512 / TILE_SIZE)
SCALE = 4

img = np.array(im_frame.get_flattened_data()).reshape((512, 512))
grid = np.arange(0, 512, dtype=np.int32)
rekd = RectBivariateSpline(grid, grid, img)
igrid = np.linspace(0, 512, int(512 * (128 / TILE_SIZE)))
v = rekd(igrid, igrid)
with open("terrain.bytes", "wb") as f:
    for item in (v / max_h * 255).astype(np.uint8)[1024+512:,1024+512:].flatten():
        f.write(struct.pack("B", item))

for x in range(int(512 / TILE_SIZE)):
    for y in range(int(512 / TILE_SIZE)):
        o = get_tile(v, x, y, TILE_SIZE * SCALE)
        Image.fromarray(o, mode='L').save('tile%d_%d.png' % (x, y))
        with open("tile%d_%d.bytes" % (x, y), "wb") as f:
            for iy in range(o.shape[1]):
                for ix in range(o.shape[0]):
                    f.write(struct.pack("B", o[ix, iy]))

#img = Image.fromarray(v.flatten().astype(np.float32))
#img.save('foo.png')
