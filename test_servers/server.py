from BaseHTTPServer import BaseHTTPRequestHandler, HTTPServer
import SocketServer

class S(BaseHTTPRequestHandler):
    def _set_headers(self):
        self.send_response(200)
        self.send_header('Content-type', 'text/html')
        self.end_headers()

    def do_GET(self):
        message = '\n'.join([
            'CLIENT VALUES:',
            'client_address=%s (%s)' % (self.client_address, self.address_string()),
            'command=%s' % self.command,
            'path=%s' % self.path,
            'request_version=%s' % self.request_version,
            'SERVER VALUES:',
            'server_version=%s' % self.server_version,
            'sys_version=%s' % self.sys_version,
            'protocol_version=%s' % self.protocol_version,
            '',
            ])
        print 'Responding to GET (', message, ')...'
        self._set_headers()
        self.wfile.write("OK")

    def do_HEAD(self):
        self._set_headers()
        
    def do_POST(self):
        self._set_headers()
        self.wfile.write("OK")
        
def run(server_class=HTTPServer, handler_class=S, port=80):
    #server_address = ('127.0.0.1', port)
    server_address = ('', port)
    httpd = server_class(server_address, handler_class)
    print 'Starting httpd...'
    httpd.serve_forever()

if __name__ == "__main__":
    from sys import argv

    if len(argv) == 2:
        run(port=int(argv[1]))
    else:
        run()
