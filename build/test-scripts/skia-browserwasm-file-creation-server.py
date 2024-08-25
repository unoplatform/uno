import os
import sys
from http.server import SimpleHTTPRequestHandler, HTTPServer

class FileCreationRequestHandler(SimpleHTTPRequestHandler):
    def end_headers (self):
        self.send_header('Access-Control-Allow-Origin', '*')                
        self.send_header('Access-Control-Allow-Methods', 'GET, POST, OPTIONS')
        self.send_header("Access-Control-Allow-Headers", "Content-Type, X-Requested-With")
        SimpleHTTPRequestHandler.end_headers(self)

    def do_POST(self):
        content_length = int(self.headers['Content-Length'])
        post_data = self.rfile.read(content_length)
        try:
            data = eval(post_data.decode('utf-8'))
            filename = data.get('FilePath')
            string_to_write = data.get('Content')

            # We use utf16 by default because the main use case for this server
            # is writing the XML test results file, and that XML document specifies
            # the encoding to be utf16. So far, we haven't had the need to add
            # a parameter for the encoding in the server API.
            with open(filename, mode='w', encoding='utf-16') as file:
                file.write(string_to_write)
            
            self.send_response(200)
            self.end_headers()
            self.wfile.write(b"File created successfully.")
        except Exception as e:
            self.send_error(400, str(e))
            self.end_headers()

    def do_OPTIONS(self):           
        self.send_response(200)
        self.end_headers()

if __name__ == "__main__":
    server_address = ('', sys.argv[1] if len(sys.argv) > 1 else 8001)
    httpd = HTTPServer(server_address, FileCreationRequestHandler)
    print(f"Creating files on port {server_address[1]}...")
    httpd.serve_forever()
