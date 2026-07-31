"""Static file server for a published Uno WebAssembly app, reachable from other devices.

`python -m http.server` almost works, but two of its defaults break this use case:

* it binds loopback only, so a phone on the same network cannot reach it;
* it does not know the `.wasm` MIME type, so the browser refuses
  `WebAssembly.instantiateStreaming` and falls back to a slower path (or fails outright,
  depending on the browser).

Usage:
    python serve-wasm.py <wwwroot> [port]
"""

import functools
import mimetypes
import socket
import sys
from http.server import SimpleHTTPRequestHandler, ThreadingHTTPServer

DEFAULT_PORT = 8123

# Python's mimetypes DB is incomplete for the .NET WebAssembly asset set, and on Windows it is
# partly driven by the registry, so it varies per machine. Pin the ones that matter.
_MIME_TYPES = {
    ".wasm": "application/wasm",
    ".js": "text/javascript",
    ".mjs": "text/javascript",
    ".json": "application/json",
    ".dat": "application/octet-stream",
    ".blat": "application/octet-stream",
    ".pdb": "application/octet-stream",
    ".dll": "application/octet-stream",
    ".webcil": "application/octet-stream",
}


class Handler(SimpleHTTPRequestHandler):
    """Adds no-store so a republished app is picked up without clearing the service worker."""

    _base_end_headers = SimpleHTTPRequestHandler.end_headers

    def end_headers(self):
        self.send_header("Cache-Control", "no-store, must-revalidate")
        Handler._base_end_headers(self)

    def log_message(self, fmt, *args):
        # One line per asset is thousands of lines for a single page load.
        pass


def _lan_addresses():
    """Best-effort list of addresses another device on the network could use.

    Excludes loopback and link-local. Virtual adapters (VPN, WSL, Hyper-V) cannot be told
    apart reliably here, so all candidates are printed and the user picks the reachable one.
    """
    addresses = []
    try:
        for info in socket.getaddrinfo(socket.gethostname(), None, socket.AF_INET):
            address = info[4][0]
            if not address.startswith(("127.", "169.254.")) and address not in addresses:
                addresses.append(address)
    except socket.gaierror:
        pass
    return addresses


def main():
    if len(sys.argv) < 2:
        print(__doc__)
        return 1

    root = sys.argv[1]
    port = int(sys.argv[2]) if len(sys.argv) > 2 else DEFAULT_PORT

    for extension, mime in _MIME_TYPES.items():
        mimetypes.add_type(mime, extension)

    handler = functools.partial(Handler, directory=root)
    server = ThreadingHTTPServer(("0.0.0.0", port), handler)

    print(f"Serving {root}")
    print(f"  this machine : http://localhost:{port}/")
    for address in _lan_addresses():
        print(f"  other devices: http://{address}:{port}/")
    print("Ctrl+C to stop.", flush=True)

    try:
        server.serve_forever()
    except KeyboardInterrupt:
        pass
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
