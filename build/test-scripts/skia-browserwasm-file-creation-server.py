import os
from http.server import SimpleHTTPRequestHandler, HTTPServer

# Class to handle dynamic file creation requests
class FileCreationRequestHandler(SimpleHTTPRequestHandler):
    def do_POST(self):
        content_length = int(self.headers['Content-Length'])
        post_data = self.rfile.read(content_length)
        try:
            data = eval(post_data.decode('utf-8'))
            filename = data.get('FilePath')
            string_to_write = data.get('Content')

            # Create the file and write the string to it
            with open(filename, 'w') as file:
                file.write(string_to_write)
            
            self.send_response(200)
            self.send_header('Access-Control-Allow-Origin', '*')                
            self.send_header('Access-Control-Allow-Methods', 'POST, OPTIONS')
            self.send_header("Access-Control-Allow-Headers", "Content-Type, X-Requested-With")
            self.end_headers()
            self.wfile.write(b"File created successfully.")
        except Exception as e:
            self.send_error(400, str(e))
            self.end_headers()

    def do_OPTIONS(self):           
        self.send_response(200)       
        self.send_header('Access-Control-Allow-Origin', '*')                
        self.send_header('Access-Control-Allow-Methods', 'POST, OPTIONS')
        self.send_header("Access-Control-Allow-Headers", "Content-Type, X-Requested-With")        
        self.end_headers()

if __name__ == "__main__":
    server_address = ('', 8001)
    httpd = HTTPServer(server_address, FileCreationRequestHandler)
    print(f"Creating files on port {server_address[1]}...")
    httpd.serve_forever()
