import sys
from functools import partial
from http.server import SimpleHTTPRequestHandler, HTTPServer

# Static server adding the headers that make the page cross-origin isolated, which SharedArrayBuffer
# and therefore WebAssembly threading require.
class CrossOriginIsolatedRequestHandler(SimpleHTTPRequestHandler):
    def end_headers(self):
        self.send_header('Cross-Origin-Opener-Policy', 'same-origin')
        self.send_header('Cross-Origin-Embedder-Policy', 'require-corp')
        SimpleHTTPRequestHandler.end_headers(self)

if __name__ == '__main__':
    port = int(sys.argv[1])
    directory = sys.argv[2]
    handler = partial(CrossOriginIsolatedRequestHandler, directory=directory)
    HTTPServer(('', port), handler).serve_forever()
