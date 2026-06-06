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

def get_tile(ofs_x, ofs_y, size, out_size):
    max_h = np.array(im_frame.get_flattened_data()).max()
    img = np.array(im_frame.get_flattened_data()).reshape((512, 512))[ofs_x:ofs_x + size, ofs_y:size + ofs_y]
    
    grid = np.arange(0, size, dtype=np.int32)
    rekd = RectBivariateSpline(grid, grid, img)
    igrid = np.linspace(0, size, out_size)
    v = rekd(igrid, igrid)
    
    x = np.arange(out_size)
    x, y = np.meshgrid(x, x)
    
    v = (v / max_h * 255).astype(np.uint8)
    return v

TILE_SIZE = 32
for x in range(int(512 / TILE_SIZE)):
    for y in range(int(512 / TILE_SIZE)):
        v = get_tile(x * TILE_SIZE, y * TILE_SIZE, TILE_SIZE, 128)
        Image.fromarray(v, mode='L').save('tile%d_%d.png' % (x, y))

#img = Image.fromarray(v.flatten().astype(np.float32))
#img.save('foo.png')
