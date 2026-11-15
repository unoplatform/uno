"""Turn a binary STL into the 7-float point cloud the sample consumes.

Layout per point, little-endian float32: x, y, z, nx, ny, nz, ao
Matches the record the reference demo bakes, so the renderer is source-agnostic.
"""
import math
import os
import random
import struct
import sys


def load_stl(path):
    with open(path, 'rb') as f:
        f.read(80)
        count = struct.unpack('<I', f.read(4))[0]
        data = f.read(count * 50)
    return count, data


def build(path, out, n_points, seed=7, flip_y=False):
    count, data = load_stl(path)
    print(f'triangles: {count:,}')

    # Pull vertices + face normals, and the per-triangle area that weights the sampling.
    tri = []
    areas = []
    total = 0.0
    for i in range(count):
        o = i * 50
        nx, ny, nz, ax, ay, az, bx, by, bz, cx, cy, cz = struct.unpack_from('<12f', data, o)
        ux, uy, uz = bx - ax, by - ay, bz - az
        vx, vy, vz = cx - ax, cy - ay, cz - az
        crx, cry, crz = uy * vz - uz * vy, uz * vx - ux * vz, ux * vy - uy * vx
        area = 0.5 * math.sqrt(crx * crx + cry * cry + crz * crz)
        if area <= 0:
            continue
        ln = math.sqrt(nx * nx + ny * ny + nz * nz)
        if ln < 1e-9:
            nx, ny, nz = crx / (2 * area), cry / (2 * area), crz / (2 * area)
        else:
            nx, ny, nz = nx / ln, ny / ln, nz / ln
        tri.append((ax, ay, az, bx, by, bz, cx, cy, cz, nx, ny, nz))
        total += area
        areas.append(total)

    print(f'usable: {len(tri):,}  area: {total:.1f}')

    rng = random.Random(seed)
    import bisect
    pts = []
    for _ in range(n_points):
        t = tri[bisect.bisect_left(areas, rng.random() * total)]
        r1, r2 = rng.random(), rng.random()
        if r1 + r2 > 1.0:
            r1, r2 = 1.0 - r1, 1.0 - r2
        r0 = 1.0 - r1 - r2
        x = t[0] * r0 + t[3] * r1 + t[6] * r2
        y = t[1] * r0 + t[4] * r1 + t[7] * r2
        z = t[2] * r0 + t[5] * r1 + t[8] * r2
        pts.append([x, y, z, t[9], t[10], t[11], 1.0])

    # Normalise: centre x/z, scale so y spans [-1, 1].
    ys = [p[1] for p in pts]
    xs = [p[0] for p in pts]
    zs = [p[2] for p in pts]
    ymin, ymax = min(ys), max(ys)
    half = (ymax - ymin) * 0.5
    mid = (ymax + ymin) * 0.5
    cx = (max(xs) + min(xs)) * 0.5
    cz = (max(zs) + min(zs)) * 0.5
    for p in pts:
        p[0] = (p[0] - cx) / half
        p[1] = (p[1] - mid) / half
        p[2] = (p[2] - cz) / half

    # Several of these scans are authored Y-down, so the plinth lands where the head belongs.
    # Decided per figure by looking at the silhouette, not guessed: both ends of a bust are cut
    # flat, so "which end is flat" cannot tell them apart.
    if flip_y:
        for p in pts:
            p[1] = -p[1]
            p[4] = -p[4]

    # Scanner output sometimes carries inward normals; a majority vote against the centroid is
    # enough to catch a wholesale flip, which would light the figure from inside.
    outward = sum(1 for p in pts if p[0] * p[3] + p[1] * p[4] + p[2] * p[5] > 0)
    if outward < len(pts) * 0.5:
        print('normals point inward - flipping')
        for p in pts:
            p[3], p[4], p[5] = -p[3], -p[4], -p[5]

    bake_ao(pts, tri, areas, total, rng)

    with open(out, 'wb') as f:
        for p in pts:
            f.write(struct.pack('<7f', *p))
    print(f'wrote {out}  {len(pts):,} points  {os.path.getsize(out):,} bytes')


def bake_ao(pts, tri, areas, total, rng, voxel=1/64.0, rays=24, steps=26, reach=0.40):
    """Ray-cast occlusion against a voxelised copy of the surface.

    Density-based occlusion is useless here: area-weighted sampling makes point density uniform by
    construction, so it measures nothing. Casting a hemisphere of rays and asking what they hit is
    what darkens eye sockets, nostrils and the underside of a chin - the cues that make a scan read
    as the thing it was scanned from.
    """
    import bisect
    import math

    # Dense surface sample -> occupancy set. Far more points than we output, so thin features
    # (a nose edge, a lock of hair) still block rays.
    occ = set()
    for _ in range(260000):
        t = tri[bisect.bisect_left(areas, rng.random() * total)]
        r1, r2 = rng.random(), rng.random()
        if r1 + r2 > 1.0:
            r1, r2 = 1.0 - r1, 1.0 - r2
        r0 = 1.0 - r1 - r2
        occ.add((int((t[0] * r0 + t[3] * r1 + t[6] * r2) / voxel),
                 int((t[1] * r0 + t[4] * r1 + t[7] * r2) / voxel),
                 int((t[2] * r0 + t[5] * r1 + t[8] * r2) / voxel)))

    # Fixed hemisphere directions, reused for every point: a spiral is even enough at this count
    # and keeps the result free of per-point noise.
    dirs = []
    for i in range(rays):
        u = (i + 0.5) / rays
        z = 1.0 - u
        r = math.sqrt(max(0.0, 1.0 - z * z))
        phi = i * 2.399963229728653
        dirs.append((math.cos(phi) * r, math.sin(phi) * r, z))

    for p in pts:
        nx, ny, nz = p[3], p[4], p[5]
        # Build a frame around the normal so the spiral maps onto this point's hemisphere.
        ax, ay, az = (0.0, 0.0, 1.0) if abs(nz) < 0.9 else (1.0, 0.0, 0.0)
        tx, ty, tz = ny * az - nz * ay, nz * ax - nx * az, nx * ay - ny * ax
        tl = math.sqrt(tx * tx + ty * ty + tz * tz) or 1.0
        tx, ty, tz = tx / tl, ty / tl, tz / tl
        bx, by, bz = ny * tz - nz * ty, nz * tx - nx * tz, nx * ty - ny * tx

        hits = 0
        for dx, dy, dz in dirs:
            vx = tx * dx + bx * dy + nx * dz
            vy = ty * dx + by * dy + ny * dz
            vz = tz * dx + bz * dy + nz * dz
            # Step no further than a voxel or rays tunnel through the surface and find nothing.
            # Start clear of the point's own voxel so a surface never occludes itself.
            for s in range(2, steps + 1):
                d = reach * s / steps
                if (int((p[0] + vx * d) / voxel),
                        int((p[1] + vy * d) / voxel),
                        int((p[2] + vz * d) / voxel)) in occ:
                    hits += 1
                    break
        p[6] = max(0.05, min(1.0, 1.0 - hits / rays))


if __name__ == '__main__':
    build(sys.argv[1], sys.argv[2],
          int(sys.argv[3]) if len(sys.argv) > 3 else 12000,
          flip_y=len(sys.argv) > 4 and sys.argv[4] == 'flip')
