"""Decimate a binary STL into a compact indexed mesh for the side-by-side solid render.

Layout, little-endian:
    uint32 vertCount, uint32 triCount
    vertCount * 6 float32   x, y, z, nx, ny, nz
    triCount  * 3 uint32    indices

Decimation is vertex clustering: snap every vertex to a grid cell, average the cell, and keep the
triangles whose three corners landed in different cells. Crude next to quadric collapse, but it
preserves silhouette at this scale and is the difference between a 24 MB scan and a ~200 KB asset.
"""
import math
import os
import struct
import sys


def build(path, out, grid=110, flip_y=False):
    with open(path, 'rb') as f:
        f.read(80)
        count = struct.unpack('<I', f.read(4))[0]
        data = f.read(count * 50)
    print(f'triangles in: {count:,}')

    raw = []
    for i in range(count):
        v = struct.unpack_from('<12f', data, i * 50)
        raw.append((v[3], v[4], v[5], v[6], v[7], v[8], v[9], v[10], v[11]))

    xs = [t[j] for t in raw for j in (0, 3, 6)]
    ys = [t[j] for t in raw for j in (1, 4, 7)]
    zs = [t[j] for t in raw for j in (2, 5, 8)]
    half = (max(ys) - min(ys)) * 0.5
    midy = (max(ys) + min(ys)) * 0.5
    cx = (max(xs) + min(xs)) * 0.5
    cz = (max(zs) + min(zs)) * 0.5

    def norm(x, y, z):
        y = (y - midy) / half
        return (x - cx) / half, (-y if flip_y else y), (z - cz) / half

    cell = 2.0 / grid
    acc = {}
    tris = []
    for t in raw:
        keys = []
        for j in (0, 3, 6):
            p = norm(t[j], t[j + 1], t[j + 2])
            k = (int(math.floor(p[0] / cell)), int(math.floor(p[1] / cell)), int(math.floor(p[2] / cell)))
            a = acc.get(k)
            if a is None:
                acc[k] = [p[0], p[1], p[2], 1]
            else:
                a[0] += p[0]; a[1] += p[1]; a[2] += p[2]; a[3] += 1
            keys.append(k)
        if keys[0] != keys[1] and keys[1] != keys[2] and keys[0] != keys[2]:
            # Mirroring one axis reverses handedness, so a Y flip has to swap two corners or every
            # face ends up wound backwards - normals inverted and the renderer culling the front.
            tris.append((keys[0], keys[2], keys[1]) if flip_y else tuple(keys))

    index = {k: i for i, k in enumerate(acc)}
    verts = []
    for k, a in acc.items():
        verts.append([a[0] / a[3], a[1] / a[3], a[2] / a[3], 0.0, 0.0, 0.0])

    # Drop duplicate faces left behind by clustering, then accumulate area-weighted vertex normals
    # so the solid render shades smoothly instead of faceting along the grid.
    seen = set()
    faces = []
    for a, b, c in tris:
        ia, ib, ic = index[a], index[b], index[c]
        # Rotate to a canonical start but keep the order: sorting would make a face and its
        # opposite-winding twin equal, and on a thin feature - an ear, a lock of hair - both sides
        # cluster to the same three cells. Dropping one punches a hole the cull then shows through.
        tri3 = (ia, ib, ic)
        lo = tri3.index(min(tri3))
        key = (tri3[lo], tri3[(lo + 1) % 3], tri3[(lo + 2) % 3])
        if key in seen:
            continue
        seen.add(key)
        faces.append((ia, ib, ic))

        ax, ay, az = verts[ia][:3]
        bx, by, bz = verts[ib][:3]
        cx2, cy2, cz2 = verts[ic][:3]
        ux, uy, uz = bx - ax, by - ay, bz - az
        vx, vy, vz = cx2 - ax, cy2 - ay, cz2 - az
        nx, ny, nz = uy * vz - uz * vy, uz * vx - ux * vz, ux * vy - uy * vx
        for i in (ia, ib, ic):
            verts[i][3] += nx
            verts[i][4] += ny
            verts[i][5] += nz

    for v in verts:
        ln = math.sqrt(v[3] * v[3] + v[4] * v[4] + v[5] * v[5])
        if ln > 1e-9:
            v[3] /= ln; v[4] /= ln; v[5] /= ln
        else:
            v[3], v[4], v[5] = 0.0, 1.0, 0.0

    # Point normals outward; a wholesale inward flip would light the figure from inside.
    outward = sum(1 for v in verts if v[0] * v[3] + v[1] * v[4] + v[2] * v[5] > 0)
    if outward < len(verts) * 0.5:
        print('normals inward - flipping')
        for v in verts:
            v[3], v[4], v[5] = -v[3], -v[4], -v[5]

    with open(out, 'wb') as f:
        f.write(struct.pack('<II', len(verts), len(faces)))
        for v in verts:
            f.write(struct.pack('<6f', *v))
        for t in faces:
            f.write(struct.pack('<3I', *t))
    print(f'wrote {out}: {len(verts):,} verts  {len(faces):,} tris  {os.path.getsize(out):,} bytes')


if __name__ == '__main__':
    build(sys.argv[1], sys.argv[2],
          grid=int(sys.argv[3]) if len(sys.argv) > 3 else 110,
          flip_y=len(sys.argv) > 4 and sys.argv[4] == 'flip')
