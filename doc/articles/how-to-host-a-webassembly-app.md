---
uid: Uno.Development.HostWebAssemblyApp
---

# Hosting a WebAssembly App

- WASM Web Server Configuration
  - [Azure Static WebApps](guides/azure-static-webapps.md)
  - [Nginx](#nginx)
  - [Apache](#apache)

Regardless of the web server (or reverse proxy) software used, the support the following Content (MIME) types are always needed:

- `application/wasm`
- `application/octet-stream`
- `application/font-woff`

## Nginx

Below contains a performance tuned version of *nginx* fitting WebAssembly deployments. The MIME types have been extended on top of the base *nginx* MIME types with WASM support.

```nginx
user nginx;

pid /var/run/nginx.pid;

##################################################################################
# nginx.conf Performance Tuning: https://github.com/denji/nginx-tuning
##################################################################################

# you must set worker processes based on your CPU cores, nginx does not benefit from setting more than that
worker_processes auto; #some last versions calculate it automatically

# number of file descriptors used for nginx
# the limit for the maximum FDs on the server is usually set by the OS.
# if you don't set FD's then OS settings will be used which is by default 2000
worker_rlimit_nofile 100000;

# only log critical errors
error_log /var/log/nginx/error.log crit;

# provides the configuration file context in which the directives that affect connection processing are specified.
events {
  # determines how much clients will be served per worker
  # max clients = worker_connections * worker_processes
  # max clients is also limited by the number of socket connections available on the system (~64k)
  worker_connections 4000;

  # optmized to serve many clients with each thread, essential for linux -- for testing environment
  use epoll;

  # accept as many connections as possible, may flood worker connections if set too low -- for testing environment
  multi_accept on;
}

http {
  include /etc/nginx/mime.types;
  default_type application/octet-stream;

  types {
    application/wasm wasm;
    application/octet-stream clr;
    application/octet-stream pdb;
    application/font-woff woff;
    application/font-woff woff2;
  }

  log_format main '$remote_addr - $remote_user [$time_local] "$request" '
  '$status $body_bytes_sent "$http_referer" '
  '"$http_user_agent" "$http_x_forwarded_for"';

  # cache informations about FDs, frequently accessed files
  # can boost performance, but you need to test those values
  open_file_cache max=200000 inactive=20s;
  open_file_cache_valid 30s;
  open_file_cache_min_uses 2;
  open_file_cache_errors on;

  # to boost I/O on HDD we can disable access logs
  access_log off;

  # copies data between one FD and other from within the kernel
  # faster then read() + write()
  sendfile on;

  # Send headers in one piece, it's better then sending them one by one
  tcp_nopush on;

  # don't buffer data sent, good for small data bursts in real time
  tcp_nodelay on;

  # reduce the data that needs to be sent over network
  gzip on;
  gzip_min_length 10240;
  gzip_proxied expired no-cache no-store private auth;
  gzip_types text/plain text/css text/xml text/javascript application/x-javascript application/json application/xml application/wasm application/octet-stream;
  gzip_disable msie6;

  # allow the server to close connection on non responding client, this will free up memory
  reset_timedout_connection on;

  # request timed out -- default 60
  client_body_timeout 10;

  # if client stop responding, free up memory -- default 60
  send_timeout 2;

  # server will close connection after this time -- default 75
  keepalive_timeout 30;

  # number of requests client can make over keep-alive -- for testing environment
  keepalive_requests 100000;

  #########################################
  # Just For Security Reason
  #########################################

  # Security reasons, turn off nginx versions
  server_tokens off;

  # #########################################
  # # NGINX Simple DDoS Defense
  # #########################################

  # limit the number of connections per single IP
  limit_conn_zone $binary_remote_addr zone=conn_limit_per_ip:10m;

  # limit the number of requests for a given session
  limit_req_zone $binary_remote_addr zone=req_limit_per_ip:10m rate=5r/s;

  # zone which we want to limit by upper values, we want limit whole server
  server {
    limit_conn conn_limit_per_ip 10;
    limit_req zone=req_limit_per_ip burst=10 nodelay;
  }

  # if the request body size is more than the buffer size, then the entire (or partial)
  # request body is written into a temporary file
  client_body_buffer_size 128k;

  # headerbuffer size for the request header from client -- for testing environment
  client_header_buffer_size 3m;

  # maximum number and size of buffers for large headers to read from client request
  large_client_header_buffers 4 256k;

  # read timeout for the request body from client -- for testing environment
  # client_body_timeout   3m;

  # how long to wait for the client to send a request header -- for testing environment
  client_header_timeout 3m;

  include /etc/nginx/conf.d/*.conf;
}
```

In order to enable the fallback routes in Nginx, you can use the following location rule in the configuration file:

```
location ~ ^\/(?!(package_)) {
    try_files $uri $uri/ /index.html;
}
```

Additionally, in order to properly handle the caching of the WASM application by the browser, the Cache-control header should be set as follow:

```
location ~ ^\/(?!(package_)) {
    try_files $uri $uri/ /index.html;
    add_header Cache-Control "must-revalidate, max-age=3600";
}

location ~ ^\/package_ {
    try_files $uri $uri/ =404;
    add_header Cache-Control "public, immutable, max-age=31536000";
}
```

If you have *brotli* enabled via [`ngx-brotli` module](https://github.com/google/ngx_brotli), you can add the following block for extra compression performance.

```nginx
brotli_static on;
brotli on;
brotli_types text/plain text/css application/json application/javascript application/x-javascript text/javascript;
brotli_comp_level 4;
```

## Apache

For *Apache*, please use the [`AddType` directive](https://httpd.apache.org/docs/2.4/mod/mod_mime.html#addtype) to add WASM support to the root of your apache config, likely located at either `/etc/apache2/apache2.conf` or `/etc/httpd.d/conf/httpd.conf`. Then, restart your *Apache* web server or *Apache* container (if you running with Docker).

```apache
AddType application/wasm .wasm
AddType application/octet-stream .clr
AddType application/octet-stream .dat
AddType application/octet-stream .pdb
AddType application/font-woff .woff
AddType application/font-woff .woff2
```

## IIS

Windows Server IIS is supported, and needs some manual installation steps to be ready for Uno Platform WebAssembly apps.

Here are some steps:

- Install the [URL Rewriter module](https://learn.microsoft.com/iis/extensions/url-rewrite-module/url-rewrite-module-configuration-reference)
- Add an application to the local web site in IIS and set its physical path to: `...\MyApp\MyApp.Wasm\bin\Debug\netstandard2.0\dist` or `...\MyApp\MyApp.Wasm\bin\Debug\net5.0\dist`
- Add MIME type `application/octet-stream .clr` to IIS.
- Add MIME type `application/wasm .wasm` to IIS.
- Add MIME type `application/octet-stream .dat` to IIS.
- Add MIME type `application/woff2 .woff2` to IIS
- Add MIME type `application/pdb .pdb` to IIS

Run `http:localhost/Myapp` for testing
