from PIL import Image
import numpy as np
from matplotlib import pylab as plt
from scipy.interpolate import RectBivariateSpline
from imageio import imwrite

from matplotlib import cbook, cm
from matplotlib.colors import LightSource
from scipy.ndimage import zoom

im_frame = Image.open('terrain.png')
OUT_SIZE = 1024

def get_tile(v, ofs_x, ofs_y, size, out_size):
    max_h = np.array(im_frame.get_flattened_data()).max()
    # img = np.array(im_frame.get_flattened_data()).reshape((512, 512))[ofs_x:ofs_x + size, ofs_y:size + ofs_y]
    
    # grid = np.arange(0, size, dtype=np.int32)
    # rekd = RectBivariateSpline(grid, grid, img)
    # igrid = np.linspace(0, size, out_size)
    # v = rekd(igrid, igrid)

    v = v[ofs_x:ofs_x + size + (ofs_x < TILES - 1), ofs_y:ofs_y + size + (ofs_y < TILES - 1)]
    
    v = (v / max_h * 255).astype(np.uint8)
    return v

TILE_SIZE = 32
TILES = int(512 / TILE_SIZE)

img = np.array(im_frame.get_flattened_data()).reshape((512, 512))
grid = np.arange(0, 512, dtype=np.int32)
rekd = RectBivariateSpline(grid, grid, img)
igrid = np.linspace(0, 512, int(512 * (128 / TILE_SIZE)))
v = rekd(igrid, igrid)

for x in range(int(512 / TILE_SIZE)):
    for y in range(int(512 / TILE_SIZE)):
        o = get_tile(v, x * TILE_SIZE, y * TILE_SIZE, TILE_SIZE, 128)
        Image.fromarray(o, mode='L').save('tile%d_%d.png' % (x, y))

#img = Image.fromarray(v.flatten().astype(np.float32))
#img.save('foo.png')
