#!/usr/bin/env python3
"""Makes the webgpu.h functions in a thin Apple static library global again.

Dawn's Apple library is compiled with hidden visibility, so its API functions are "private extern": linkable inside one
image but never exported from it. A dylib built from it exports nothing, and on iOS, where the library is linked into
the app, the binding cannot find the functions by name. Clearing the private-extern bit (N_PEXT) on exactly those
symbols makes them ordinary globals; run ranlib on the result so the archive's index lists them.

Usage: unhide-macho.py <thin libwebgpu_dawn.a> <webgpu_dawn.exp> <output .a>
"""
import struct
import sys

N_PEXT = 0x10
N_EXT = 0x01
MH_MAGIC_64 = 0xFEEDFACF
LC_SYMTAB = 0x2


def patch_object(data, start, names):
    if struct.unpack_from("<I", data, start)[0] != MH_MAGIC_64:
        return 0
    ncmds = struct.unpack_from("<I", data, start + 16)[0]
    offset = start + 32
    patched = 0
    for _ in range(ncmds):
        cmd, size = struct.unpack_from("<II", data, offset)
        if cmd == LC_SYMTAB:
            symoff, nsyms, stroff, _ = struct.unpack_from("<IIII", data, offset + 8)
            for i in range(nsyms):
                entry = start + symoff + i * 16
                strx = struct.unpack_from("<I", data, entry)[0]
                n_type = data[entry + 4]
                if not (n_type & N_EXT and n_type & N_PEXT):
                    continue
                name_start = start + stroff + strx
                name = bytes(data[name_start:data.index(b"\0", name_start)]).decode()
                if name in names:
                    data[entry + 4] = n_type & ~N_PEXT
                    patched += 1
        offset += size
    return patched


def main():
    if len(sys.argv) != 4:
        sys.exit(__doc__)
    data = bytearray(open(sys.argv[1], "rb").read())
    names = {line.strip() for line in open(sys.argv[2]) if line.strip()}
    if data[:8] != b"!<arch>\n":
        sys.exit("Not an ar archive (thin it with lipo first)")
    pos, patched = 8, 0
    while pos < len(data):
        header = data[pos:pos + 60]
        name = header[:16].decode().strip()
        size = int(header[48:58].decode().strip())
        body = pos + 60
        # BSD ar keeps a long member name at the start of the member's data.
        name_len = int(name[3:]) if name.startswith("#1/") else 0
        patched += patch_object(data, body + name_len, names)
        pos = body + size + (size & 1)
    open(sys.argv[3], "wb").write(data)
    found = len(names)
    print(f"Unhid {patched} symbols ({found} exported functions)")
    if patched < found:
        sys.exit(f"Only {patched} of the {found} exported functions were found hidden")


if __name__ == "__main__":
    main()
