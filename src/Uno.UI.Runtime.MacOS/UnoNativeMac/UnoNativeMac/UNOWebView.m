//
//  UNOWebView.m
//

#import "UNONative.h"
#import "UNOWebView.h"

static uno_webview_javascript_fn_ptr execute;
static uno_webview_javascript_fn_ptr invoke;

inline uno_webview_javascript_fn_ptr uno_get_execute_callback(void)
{
    return execute;
}

void uno_set_execute_callback(uno_webview_javascript_fn_ptr p)
{
    execute = p;
}

inline uno_webview_javascript_fn_ptr uno_get_invoke_callback(void)
{
    return invoke;
}

void uno_set_invoke_callback(uno_webview_javascript_fn_ptr p)
{
    invoke = p;
}

static uno_webview_navigation_starting_fn_ptr navigation_starting;
static uno_webview_navigation_finishing_fn_ptr navigation_finishing;
static uno_webview_webview_notification_fn_ptr property_change_notification;
static uno_webview_receive_web_message_fn_ptr receive_web_message;
static uno_webview_navigation_failing_fn_ptr navigation_failing;

inline uno_webview_navigation_starting_fn_ptr uno_get_webview_navigation_starting_callback(void)
{
    return navigation_starting;
}

inline uno_webview_navigation_finishing_fn_ptr uno_get_webview_navigation_finishing_callback(void)
{
    return navigation_finishing;
}

inline uno_webview_webview_notification_fn_ptr uno_get_webview_notification_callback(void)
{
    return property_change_notification;
}

inline uno_webview_receive_web_message_fn_ptr uno_get_webview_receive_web_message_callback(void)
{
    return receive_web_message;
}

inline uno_webview_navigation_failing_fn_ptr uno_get_webview_navigation_failing_callback(void)
{
    return navigation_failing;
}

void uno_set_webview_navigation_callbacks(uno_webview_navigation_starting_fn_ptr starting, uno_webview_navigation_finishing_fn_ptr finishing, uno_webview_webview_notification_fn_ptr notification, uno_webview_receive_web_message_fn_ptr web_message, uno_webview_navigation_failing_fn_ptr failing)
{
    navigation_starting = starting;
    navigation_finishing = finishing;
    property_change_notification = notification;
    receive_web_message = web_message;
    navigation_failing = failing;
}

static uno_webview_unsupported_scheme_identified_fn_ptr unsupported_scheme_identified;

inline uno_webview_unsupported_scheme_identified_fn_ptr uno_get_webview_unsupported_scheme_identified_callback(void)
{
    return unsupported_scheme_identified;
}

void uno_set_webview_unsupported_scheme_identified_callback(uno_webview_unsupported_scheme_identified_fn_ptr fn_ptr)
{
    unsupported_scheme_identified = fn_ptr;
}

static uno_webview_resource_requested_fn_ptr resource_requested;

inline uno_webview_resource_requested_fn_ptr uno_get_webview_resource_requested_callback(void)
{
    return resource_requested;
}

void uno_set_webview_resource_requested_callback(uno_webview_resource_requested_fn_ptr fn_ptr)
{
    resource_requested = fn_ptr;
}


static uno_webview_scheme_task_start_fn_ptr scheme_task_start;
static uno_webview_scheme_task_stop_fn_ptr scheme_task_stop;

void uno_set_webview_scheme_task_callbacks(uno_webview_scheme_task_start_fn_ptr start, uno_webview_scheme_task_stop_fn_ptr stop)
{
    scheme_task_start = start;
    scheme_task_stop = stop;
}

// pending tasks by id - only accessed on the main thread, where WebKit calls the scheme handler
static NSMutableDictionary<NSNumber*, id<WKURLSchemeTask>> *scheme_tasks;
static NSInteger last_scheme_task_id;

static NSMutableDictionary<NSNumber*, id<WKURLSchemeTask>>* uno_webview_get_scheme_tasks(void)
{
    if (!scheme_tasks) {
        scheme_tasks = [[NSMutableDictionary alloc] init];
    }
    return scheme_tasks;
}

static NSData* uno_webview_read_request_body(NSURLRequest *request)
{
    if (request.HTTPBody) {
        return request.HTTPBody;
    }
    NSInputStream *stream = request.HTTPBodyStream;
    if (!stream) {
        return nil;
    }
    NSMutableData *data = [[NSMutableData alloc] init];
    uint8_t buffer[16384];
    [stream open];
    NSInteger read;
    while ((read = [stream read:buffer maxLength:sizeof(buffer)]) > 0) {
        [data appendBytes:buffer length:read];
    }
    [stream close];
    return data;
}

// a stopped task raises if it is messaged, so the id is removed from `scheme_tasks` before anything is sent
static id<WKURLSchemeTask> _Nullable uno_webview_take_scheme_task(NSInteger taskId)
{
    NSNumber *key = @(taskId);
    id<WKURLSchemeTask> task = uno_webview_get_scheme_tasks()[key];
    if (task) {
        [uno_webview_get_scheme_tasks() removeObjectForKey:key];
    }
    return task;
}

static void uno_webview_run_on_main_thread(dispatch_block_t block)
{
    if (NSThread.isMainThread) {
        block();
    } else {
        dispatch_async(dispatch_get_main_queue(), block);
    }
}

void uno_webview_scheme_task_complete(NSInteger taskId, int statusCode, const char* headersJson, const void* body, NSInteger bodyLength)
{
    // copy everything now: the managed buffers are only valid during this call
    NSData *data = body && bodyLength > 0 ? [NSData dataWithBytes:body length:bodyLength] : [NSData data];
    NSString *headers = headersJson ? [NSString stringWithUTF8String:headersJson] : @"[]";
    uno_webview_run_on_main_thread(^{
        id<WKURLSchemeTask> task = uno_webview_take_scheme_task(taskId);
        if (!task) {
#if DEBUG
            NSLog(@"uno_webview_scheme_task_complete task %ld was stopped", (long)taskId);
#endif
            return;
        }
        // headers are a JSON array of [name, value] pairs; repeated names are folded into one field
        NSMutableDictionary<NSString*, NSString*> *fields = [[NSMutableDictionary alloc] init];
        NSArray *pairs = [NSJSONSerialization JSONObjectWithData:[headers dataUsingEncoding:NSUTF8StringEncoding] options:0 error:nil];
        if ([pairs isKindOfClass:NSArray.class]) {
            for (NSArray *pair in pairs) {
                if (![pair isKindOfClass:NSArray.class] || pair.count != 2) {
                    continue;
                }
                NSString *name = pair[0];
                NSString *value = pair[1];
                NSString *existing = fields[name];
                fields[name] = existing ? [NSString stringWithFormat:@"%@, %@", existing, value] : value;
            }
        }
        @try {
            NSHTTPURLResponse *response = [[NSHTTPURLResponse alloc] initWithURL:task.request.URL statusCode:statusCode HTTPVersion:@"HTTP/1.1" headerFields:fields];
            [task didReceiveResponse:response];
            [task didReceiveData:data];
            [task didFinish];
        } @catch (NSException *ex) {
#if DEBUG
            NSLog(@"uno_webview_scheme_task_complete task %ld exception %@", (long)taskId, ex);
#endif
        }
    });
}

void uno_webview_scheme_task_fail(NSInteger taskId, const char* message)
{
    NSString *description = message ? [NSString stringWithUTF8String:message] : @"The resource could not be loaded.";
    uno_webview_run_on_main_thread(^{
        id<WKURLSchemeTask> task = uno_webview_take_scheme_task(taskId);
        if (!task) {
            return;
        }
        @try {
            NSError *error = [NSError errorWithDomain:NSURLErrorDomain code:NSURLErrorResourceUnavailable userInfo:@{
                NSLocalizedDescriptionKey: description,
                NSURLErrorFailingURLErrorKey: task.request.URL,
                NSURLErrorFailingURLStringErrorKey: task.request.URL.absoluteString ?: @"",
            }];
            [task didFailWithError:error];
        } @catch (NSException *ex) {
#if DEBUG
            NSLog(@"uno_webview_scheme_task_fail task %ld exception %@", (long)taskId, ex);
#endif
        }
    });
}

@implementation UNOWebViewSchemeHandler

- (void)webView:(WKWebView *)webView startURLSchemeTask:(id<WKURLSchemeTask>)urlSchemeTask {
    NSInteger taskId = ++last_scheme_task_id;
    uno_webview_get_scheme_tasks()[@(taskId)] = urlSchemeTask;

    if (!scheme_task_start) {
        uno_webview_scheme_task_fail(taskId, "No handler is registered for the scheme.");
        return;
    }

    NSURLRequest *request = urlSchemeTask.request;
    NS_VALID_UNTIL_END_OF_SCOPE NSString *headers = nil;
    if (request.allHTTPHeaderFields) {
        NSData *json = [NSJSONSerialization dataWithJSONObject:request.allHTTPHeaderFields options:0 error:nil];
        if (json) {
            headers = [[NSString alloc] initWithData:json encoding:NSUTF8StringEncoding];
        }
    }
    NS_VALID_UNTIL_END_OF_SCOPE NSData *body = uno_webview_read_request_body(request);
    NS_VALID_UNTIL_END_OF_SCOPE NSString *url = request.URL.absoluteString ?: @"";
    NS_VALID_UNTIL_END_OF_SCOPE NSString *method = request.HTTPMethod ?: @"GET";
    NS_VALID_UNTIL_END_OF_SCOPE NSString *mainDocument = request.mainDocumentURL.absoluteString;
#if DEBUG
    NSLog(@"startURLSchemeTask webview %p task %ld %@ %@ main document %@", webView, (long)taskId, method, url, mainDocument);
#endif
    scheme_task_start(webView, taskId, url.UTF8String, method.UTF8String, headers.UTF8String, body.bytes, (NSInteger)body.length, mainDocument.UTF8String);
}

- (void)webView:(WKWebView *)webView stopURLSchemeTask:(id<WKURLSchemeTask>)urlSchemeTask {
    NSArray<NSNumber*> *keys = [uno_webview_get_scheme_tasks() allKeysForObject:urlSchemeTask];
#if DEBUG
    NSLog(@"stopURLSchemeTask webview %p tasks %@", webView, keys);
#endif
    for (NSNumber *key in keys) {
        [uno_webview_get_scheme_tasks() removeObjectForKey:key];
        if (scheme_task_stop) {
            scheme_task_stop(webView, key.integerValue);
        }
    }
}

@end

void uno_webview_set_user_scripts(WKWebView *webview, const char *scriptsJson)
{
    NSData *data = [[NSString stringWithUTF8String:scriptsJson] dataUsingEncoding:NSUTF8StringEncoding];
    NSArray *scripts = [NSJSONSerialization JSONObjectWithData:data options:0 error:nil];
    WKUserContentController *controller = webview.configuration.userContentController;
    [controller removeAllUserScripts];
    if (![scripts isKindOfClass:NSArray.class]) {
        return;
    }
    for (NSString *source in scripts) {
        if (![source isKindOfClass:NSString.class]) {
            continue;
        }
        // all frames, like WebView2's AddScriptToExecuteOnDocumentCreated
        WKUserScript *script = [[WKUserScript alloc] initWithSource:source injectionTime:WKUserScriptInjectionTimeAtDocumentStart forMainFrameOnly:NO];
        [controller addUserScript:script];
    }
}

NSView* uno_webview_create(NSWindow *window, const char *ok, const char *cancel, const char *scheme)
{
    WKWebViewConfiguration* config = [[WKWebViewConfiguration alloc] init];
    NSString *virtualHostScheme = scheme ? [NSString stringWithUTF8String:scheme] : nil;
    if (virtualHostScheme.length > 0) {
        [config setURLSchemeHandler:[[UNOWebViewSchemeHandler alloc] init] forURLScheme:virtualHostScheme];
    }
    if (@available(macOS 11, *)) {
        config.defaultWebpagePreferences.allowsContentJavaScript = YES;
    } else {
        // dotnet 9 still supports macOS 10.15
        config.preferences.javaScriptEnabled = YES;
    }
    config.preferences.javaScriptCanOpenWindowsAutomatically = YES;
    config.mediaTypesRequiringUserActionForPlayback = WKAudiovisualMediaTypeVideo | WKAudiovisualMediaTypeAudio;
    
    UNOWebView* webview = [[UNOWebView alloc] initWithFrame:NSMakeRect(0,0,0,0) configuration:config];
#if DEBUG
    NSLog(@"uno_webview_create %p", webview);
#endif
    webview.okString = [NSString stringWithUTF8String:ok];
    webview.cancelString = [NSString stringWithUTF8String:cancel];
    webview.virtualHostScheme = virtualHostScheme;

    webview.originalSuperView = ((UNOWindow*)window).renderingView;
    uno_native_track(webview);
    return webview;
}

void uno_webview_register_message_handler(WKWebView *webview)
{
#if DEBUG
    NSLog(@"uno_webview_register_message_handler %p", webview);
#endif
    __weak id weakSelf = webview;
    // `addScriptMessageHandler:name:` raises on a name that is already taken, and the webview outlives
    // every visual tree exit, so a re-entering element registers on the same WKWebView a second time.
    [webview.configuration.userContentController removeScriptMessageHandlerForName:@"unoWebView"];
    [webview.configuration.userContentController addScriptMessageHandler:weakSelf name:@"unoWebView"];
}

void uno_webview_unregister_message_handler(WKWebView *webview)
{
#if DEBUG
    NSLog(@"uno_webview_unregister_message_handler %p", webview);
#endif
    [webview.configuration.userContentController removeScriptMessageHandlerForName:@"unoWebView"];
}

const char* uno_webview_get_title(WKWebView *webview)
{
    // WKWebView.title is nullable; [nil UTF8String] returns NULL and strdup(NULL) would crash.
    NSString *title = webview.title ?: @"";
    return strdup(title.UTF8String);
}

bool uno_webview_can_go_back(WKWebView *webview)
{
    return webview.canGoBack;
}

bool uno_webview_can_go_forward(WKWebView *webview)
{
    return webview.canGoForward;
}

void uno_webview_go_back(WKWebView *webview)
{
#if DEBUG
    NSLog(@"uno_webview_go_back %p", webview);
#endif
    [webview goBack];
}

void uno_webview_go_forward(WKWebView *webview)
{
#if DEBUG
    NSLog(@"uno_webview_go_forward %p", webview);
#endif
    [webview goForward];
}

void uno_webview_navigate(WKWebView *webview, const char* url, const char *jsonHeaders)
{
    NSString *s = [NSString stringWithUTF8String:url];
    // note that if the app is not bundled then we get `/usr/local/share/dotnet/` and won't find the resources
    // this is covered inside managed code
    if (uno_application_is_bundled()) {
        // convert [BundlePath] and [ResourcePath] to bundle specific paths
        s = [s stringByReplacingOccurrencesOfString:@"[BundlePath]" withString:NSBundle.mainBundle.bundlePath];
        s = [s stringByReplacingOccurrencesOfString:@"[ResourcePath]" withString:NSBundle.mainBundle.resourcePath];
    }
    
    NSArray *headers = nil;
    if (jsonHeaders) {
        NSString *h = [NSString stringWithUTF8String:jsonHeaders];
        NSData *d = [h dataUsingEncoding:NSUTF8StringEncoding];
        NSError *e = nil;
        headers = [NSJSONSerialization JSONObjectWithData:d options:NSJSONReadingMutableContainers error: &e];
#if DEBUG
        if (e) {
            NSLog(@"uno_webview_navigate %p headers %@ error: %@", webview, headers, e);
        }
#endif
    }
    
    NSURL *u = [NSURL URLWithString:s];
    NSMutableURLRequest *r = [NSMutableURLRequest requestWithURL:u];
    if (headers) {
        // decode the JSON object into key and value strings
        for (int i = 0; i < [headers count]; i++) {
            NSDictionary* keyvalue = [headers objectAtIndex:i];
            NSString *key = keyvalue[@"Key"];
            NSArray *value = keyvalue[@"Value"];
            [r setValue:value.firstObject forHTTPHeaderField:key];
        }
    }
#if DEBUG
    NSLog(@"uno_webview_navigate %p url: %@ headers %@", webview, s, headers);
#endif
    [webview loadRequest:r];
}

void uno_webview_load_html(WKWebView *webview, const char* html)
{
    NSString *data = [NSString stringWithUTF8String:html];
#if DEBUG
    NSLog(@"uno_webview_load_html %@", data);
#endif
    // a nil baseURL slows downs things causing timeouts in tests
    NSURL* blank = [NSURL URLWithString:@"about:blank"];
    [webview loadHTMLString:data baseURL:blank];
}

void uno_webview_reload(WKWebView *webview)
{
#if DEBUG
    NSLog(@"uno_webview_reload %p", webview);
#endif
    [webview reload];
}

void uno_webview_stop(WKWebView *webview)
{
#if DEBUG
    NSLog(@"uno_webview_stop %p", webview);
#endif
    [webview stopLoading];
}

void uno_webview_execute_script(WKWebView *webview, NSInteger handle, const char *javascript)
{
#if DEBUG
    NSLog(@"uno_webview_execute_script %p handle: 0x%lx javascript: %s", webview, handle, javascript);
#endif
    NSString *js = [NSString stringWithUTF8String:javascript];
    [webview evaluateJavaScript:js completionHandler:^(NSObject* result, NSError *error) {
        // UTF8String points into its NSString, so the strings must outlive the callback that copies them.
        NS_VALID_UNTIL_END_OF_SCOPE NSString *r = nil;
        NS_VALID_UNTIL_END_OF_SCOPE NSString *e = nil;
        if (error) {
#if DEBUG
            NSLog(@"uno_webview_execute_script %p completionHandler error: %@", webview, error);
#endif
            e = error.description;
        } else if (result != nil) {
            if ([NSJSONSerialization isValidJSONObject:result]) {
#if DEBUG
                NSLog(@"uno_webview_execute_script %p completionHandler json: %@ string %@", webview, result, ((NSObject*)result).description);
#endif
                NSError *jsonError;
                NSData *data = [NSJSONSerialization dataWithJSONObject:result options:0 error:&jsonError];
                if (jsonError) {
#if DEBUG
                    NSLog(@"uno_webview_execute_script %p completionHandler jsonError: %@", webview, jsonError);
#endif
                    e = jsonError.description;
                } else {
#if DEBUG
                    NSLog(@"uno_webview_execute_script %p completionHandler data: %@", webview, data);
#endif
                    r = [[NSString alloc] initWithData:data encoding:NSUTF8StringEncoding];
                }
            } else if ([result isKindOfClass:NSString.class]) {
                // double quote existing quotes so we can quote the whole thing
                NSString *s = [((NSString*) result) stringByReplacingOccurrencesOfString:@"\"" withString:@"\\\""];
                NSString *quoted = [NSString stringWithFormat:@"\"%@\"", s];
#if DEBUG
                NSLog(@"uno_webview_execute_script %p completionHandler string: %@", webview, quoted);
#endif
                r = quoted;
            } else {
                NSString *s = result.description;
#if DEBUG
                NSLog(@"uno_webview_execute_script %p completionHandler object: %@", webview, s);
#endif
                r = s;
            }
        }
        uno_get_execute_callback()(handle, r.UTF8String, e.UTF8String);
    }];
}

void uno_webview_invoke_script(WKWebView *webview, NSInteger handle, const char *javascript)
{
#if DEBUG
    NSLog(@"uno_webview_invoke_script %p handle: 0x%lx javascript: %s", webview, handle, javascript);
#endif
    NSString *js = [NSString stringWithUTF8String:javascript];
    [webview evaluateJavaScript:js completionHandler:^(NSString *result, NSError *error) {
#if DEBUG
        NSLog(@"uno_webview_invoke_script %p completionHandler result: %@ error: %@", webview, result, error);
#endif
        NS_VALID_UNTIL_END_OF_SCOPE NSString *e = error.description;
        uno_get_invoke_callback()(handle, result.UTF8String, e.UTF8String);
    }];
}

void uno_webview_set_scrolling_enabled(UNOWebView* webview, bool enabled)
{
    webview.scrollingEnabled = enabled;
}

void uno_webview_set_inspectable(WKWebView *webview, bool inspectable)
{
    if (@available(macOS 13.3, *)) {
        webview.inspectable = inspectable;
    }
}

@implementation UNOWebView : WKWebView

- (nullable instancetype)initWithCoder:(NSCoder *)coder {
    return [super initWithCoder:coder];
}

- (instancetype)initWithFrame:(CGRect)frame configuration:(WKWebViewConfiguration *)configuration {
    self = [super initWithFrame:frame configuration:configuration];
    if (self) {
        self.UIDelegate = self;
        self.navigationDelegate = self;
        self.scrollingEnabled = true;
    }
    return self;
}

- (void)didChangeValueForKey:(NSString *)key {
    NSInteger keyId = -1;
    if ([key isEqualToString:@"title"]) {
#if DEBUG
        NSLog(@"didChangeValueForKey webview %p key: %@ value: %@", self, key, self.title);
#endif
        keyId = 0;
    } else if ([key isEqualToString:@"URL"]) {
#if DEBUG
        NSLog(@"didChangeValueForKey webview %p key: %@ value: %@", self, key, self.URL);
#endif
        keyId = 1;
    } else if ([key isEqualToString:@"canGoBack"]) {
#if DEBUG
        NSLog(@"didChangeValueForKey webview %p key: %@ value: %s", self, key, self.canGoBack ? "TRUE" : "FALSE");
#endif
        keyId = 2;
    } else if ([key isEqualToString:@"canGoForward"]) {
#if DEBUG
        NSLog(@"didChangeValueForKey webview %p key: %@ value: %s", self, key, self.canGoForward ? "TRUE" : "FALSE");
#endif
        keyId = 3;
    }
    if (keyId != -1) {
        uno_get_webview_notification_callback()(self, keyId);
    }
    [super didChangeValueForKey:key];
}

- (void)scrollWheel:(NSEvent *)event {
    if (self.scrollingEnabled) {
        [super scrollWheel:event];
    } else {
        [[self nextResponder] scrollWheel:event];
    }
}

// UNONativeElement

- (void) dispose {
#if DEBUG
    NSLog(@"UNOWebView %p disposing with superview %p", self, self.superview);
#endif
    if (self.superview) {
        [self stopLoading];
        [self removeFromSuperview];
    }
    [self.configuration.userContentController removeScriptMessageHandlerForName:@"unoWebView"];
}

// WKUIDelegate

- (void)webView:(WKWebView *)webView runJavaScriptConfirmPanelWithMessage:(NSString *)message initiatedByFrame:(WKFrameInfo *)frame completionHandler:(void (^)(BOOL result))completionHandler {
#if DEBUG
    NSLog(@"runJavaScriptConfirmPanel webview %p message: %@", webView, message);
#endif
    NSAlert *alert = [[NSAlert alloc] init];
    alert.alertStyle = NSAlertStyleInformational;
    alert.informativeText = message;
    
    UNOWebView *uno = (UNOWebView*) webView;
    [alert addButtonWithTitle:uno.okString];
    [alert addButtonWithTitle:uno.cancelString];
    [alert beginSheetModalForWindow:webView.window completionHandler:^(NSModalResponse returnCode) {
        completionHandler(returnCode == NSAlertFirstButtonReturn);
    }];
}

- (void)webView:(WKWebView *)webView runJavaScriptAlertPanelWithMessage:(NSString *)message initiatedByFrame:(WKFrameInfo *)frame completionHandler:(void (^)(void))completionHandler {
#if DEBUG
    NSLog(@"runJavaScriptAlertPanel webview %p message: %@", webView, message);
#endif
    NSAlert *alert = [[NSAlert alloc] init];
    alert.alertStyle = NSAlertStyleInformational;
    alert.informativeText = message;
    [alert runModal];
    completionHandler();
}

- (void)webView:(WKWebView *)webView runJavaScriptTextInputPanelWithPrompt:(NSString *)prompt defaultText:(nullable NSString *)defaultText initiatedByFrame:(WKFrameInfo *)frame completionHandler:(void (^)(NSString * _Nullable result))completionHandler {
#if DEBUG
    NSLog(@"runJavaScriptTextInputPanelWithPrompt webview %p prompt: %@", webView, prompt);
#endif
    NSTextField *tf = [[NSTextField alloc] initWithFrame:NSMakeRect(0, 0, 300, 20)];
    tf.placeholderString = defaultText;
    
    NSAlert *alert = [[NSAlert alloc] init];
    alert.alertStyle = NSAlertStyleInformational;
    alert.informativeText = prompt;
    alert.accessoryView = tf;
    
    UNOWebView *uno = (UNOWebView*) webView;
    [alert addButtonWithTitle:uno.okString];
    [alert addButtonWithTitle:uno.cancelString];
    [alert beginSheetModalForWindow:webView.window completionHandler:^(NSModalResponse returnCode) {
        completionHandler(returnCode == NSAlertFirstButtonReturn ? tf.stringValue : nil);
    }];
}

- (void)webView:(WKWebView *)webView runOpenPanelWithParameters:(WKOpenPanelParameters *)parameters initiatedByFrame:(WKFrameInfo *)frame completionHandler:(void (^)(NSArray<NSURL *> * _Nullable URLs))completionHandler {

    NSOpenPanel *openPanel = [NSOpenPanel openPanel];
    openPanel.allowsMultipleSelection = parameters.allowsMultipleSelection;
    if (@available(macOS 10.13.4, *)) {
        openPanel.canChooseDirectories = parameters.allowsDirectories;
    }
    [openPanel beginSheetModalForWindow:webView.window completionHandler:^(NSInteger result) {
        if (result == NSModalResponseOK)
            completionHandler(openPanel.URLs);
        else
            completionHandler(nil);
    }];
}

static uno_webview_new_window_requested_fn_ptr new_window_requested;

inline uno_webview_new_window_requested_fn_ptr uno_get_webview_new_window_requested_callback(void)
{
    return new_window_requested;
}

void uno_set_webview_new_window_requested_callback(uno_webview_new_window_requested_fn_ptr fn_ptr)
{
    new_window_requested = fn_ptr;
}

- (WKWebView *)webView:(WKWebView *)webView
createWebViewWithConfiguration:(WKWebViewConfiguration *)configuration
           forNavigationAction:(WKNavigationAction *)navigationAction
                windowFeatures:(WKWindowFeatures *)windowFeatures {

#if DEBUG
    NSLog(@"createWebViewWithConfiguration: fired for URL: %@", navigationAction.request.URL);
#endif

    uno_webview_new_window_requested_fn_ptr callback = uno_get_webview_new_window_requested_callback();
    
    assert(callback);
#if DEBUG
    NSLog(@"createWebViewWithConfiguration: Found C# callback. Attempting to call...");
#endif
        
    const char* targetUrl = [navigationAction.request.URL.absoluteString UTF8String];
    if (targetUrl == nil) {
        targetUrl = "about:blank";
    }
    
    const char* refererUrl = [navigationAction.sourceFrame.request.URL.absoluteString UTF8String];
    if (refererUrl == nil) {
        refererUrl = "about:blank";
    }

    int handled = callback(webView, targetUrl, refererUrl);

#if DEBUG
    NSLog(@"createWebViewWithConfiguration: C# callback returned: %d", handled);
#endif

    // Check if C# handled it (returned 1)
    if (handled == 1) {
#if DEBUG
        NSLog(@"createWebViewWithConfiguration: C# handled the request. Cancelling native new window.");
#endif
        return nil;
    }

#if DEBUG
    NSLog(@"createWebViewWithConfiguration: C# did not handle. Opening in default browser.");
#endif
    if (navigationAction.request.URL) {
        [[NSWorkspace sharedWorkspace] openURL:navigationAction.request.URL];
    }
    
    return nil;
}

// WKNavigationDelegate

// note: this is NOT called when navigating to a different anchor of the same URL
- (void)webView:(WKWebView *)webView didStartProvisionalNavigation:(null_unspecified WKNavigation *)navigation {
#if DEBUG
    NSLog(@"didStartProvisionalNavigation webView %p nagivation %@ URL: %@", webView, navigation, webView.URL);
#endif
    uno_get_webview_navigation_starting_callback()(webView, webView.URL.absoluteString.UTF8String);
}

- (void)webView:(WKWebView *)webView didFailProvisionalNavigation:(null_unspecified WKNavigation *)navigation withError:(NSError *)error {
#if DEBUG
    NSLog(@"didFailProvisionalNavigation webView %p nagivation %@ URL: %@ error: %@", webView, navigation, webView.URL, error);
#endif
    int status = -1;
    // based on uno/src/Uno.UI/UI/Xaml/Controls/WebView/Native/iOSmacOS/UnoWKWebView.ErrorMap.iOSmacOS.cs
    switch(error.code) {
        case NSURLErrorCannotLoadFromNetwork:
            status = CoreWebView2WebErrorStatusServerUnreachable;
            break;
        case NSURLErrorClientCertificateRequired:
            status = CoreWebView2WebErrorStatusValidAuthenticationCredentialsRequired;
            break;
        case NSURLErrorClientCertificateRejected:
            status = CoreWebView2WebErrorStatusClientCertificateContainsErrors;
            break;
        case NSURLErrorServerCertificateNotYetValid:
        case NSURLErrorServerCertificateHasUnknownRoot:
            status = CoreWebView2WebErrorStatusCertificateIsInvalid;
            break;
        case NSURLErrorServerCertificateHasBadDate:
            status = CoreWebView2WebErrorStatusCertificateExpired;
            break;
        case NSURLErrorSecureConnectionFailed:
            status = CoreWebView2WebErrorStatusServerUnreachable;
            break;
        case NSURLErrorAppTransportSecurityRequiresSecureConnection:
            status = CoreWebView2WebErrorStatusCannotConnect;
            break;
        case NSURLErrorUserAuthenticationRequired:
            status = CoreWebView2WebErrorStatusValidAuthenticationCredentialsRequired;
            break;
        case NSURLErrorBadServerResponse:
            status = CoreWebView2WebErrorStatusErrorHttpInvalidServerResponse;
            break;
        case NSURLErrorServerCertificateUntrusted:
            status = CoreWebView2WebErrorStatusCertificateIsInvalid;
            break;
        case NSURLErrorRedirectToNonExistentLocation:
        case NSURLErrorHTTPTooManyRedirects:
            status = CoreWebView2WebErrorStatusRedirectFailed;
            break;
        case NSURLErrorResourceUnavailable:
            status = CoreWebView2WebErrorStatusServerUnreachable;
            break;
        case NSURLErrorDNSLookupFailed:
        case NSURLErrorCannotFindHost:
            status = CoreWebView2WebErrorStatusHostNameNotResolved;
            break;
        case NSURLErrorNetworkConnectionLost:
        case NSURLErrorCannotConnectToHost:
            status = CoreWebView2WebErrorStatusServerUnreachable;
            break;
        case NSURLErrorTimedOut:
            status = CoreWebView2WebErrorStatusTimeout;
            break;
        case NSURLErrorCancelled:
            status = CoreWebView2WebErrorStatusOperationCanceled;
            break;
        default:
            status = CoreWebView2WebErrorStatusUnexpectedError;
            break;
    }
    uno_get_webview_navigation_failing_callback()(webView, webView.URL.absoluteString.UTF8String, status);
}

// note: this is NOT called when navigating to a different anchor of the same URL
- (void)webView:(WKWebView *)webView didFinishNavigation:(null_unspecified WKNavigation *)navigation {
#if DEBUG
    NSLog(@"didFinishNavigation webView %p URL: %@", webView, webView.URL);
#endif
    ((UNOWebView*)webView).lastNavigationUrl = webView.URL;
    uno_get_webview_navigation_finishing_callback()(webView, webView.URL.absoluteString.UTF8String);
}

- (void)webView:(WKWebView *)webView didReceiveServerRedirectForProvisionalNavigation:(null_unspecified WKNavigation *)navigation {
#if DEBUG
    NSLog(@"didReceiveServerRedirectForProvisionalNavigation webView %p", webView);
#endif
    uno_get_webview_navigation_starting_callback()(webView, webView.URL.absoluteString.UTF8String);
}

- (bool)isAnchorNavigation:(UNOWebView*) webView url:(NSURL*) newUrl {
    NSURL *previousUrl = webView.lastNavigationUrl;
    if (previousUrl) {
        NSArray* previousUrlParts = [previousUrl.absoluteString componentsSeparatedByString:@"#"];
        NSArray* newUrlParts = [newUrl.absoluteString componentsSeparatedByString:@"#"];
        return previousUrlParts.count > 0 && newUrlParts.count > 1 && [previousUrlParts[0] isEqual:newUrlParts[0]];
    }
    return false;
}

- (void)webView:(WKWebView *)webView decidePolicyForNavigationAction:(WKNavigationAction *)navigationAction decisionHandler:(void (^)(WKNavigationActionPolicy))decisionHandler {
    
    NSURL* requestUrl = navigationAction.request.URL;
    NSString *scheme = requestUrl.scheme;
    
    if ([requestUrl.scheme isEqualToString:@"mailto"] || [requestUrl.scheme isEqualToString:@"tel"]) {
        // launch the associated app
        [[NSWorkspace sharedWorkspace] openURL:requestUrl];
        // nothing more to do inside the webview
        decisionHandler(WKNavigationActionPolicyCancel);
    } else if ([self isAnchorNavigation: (UNOWebView*)webView url: requestUrl]) {
        const char *url = requestUrl.absoluteString.UTF8String;
        // fake starting event
        bool cancel = uno_get_webview_navigation_starting_callback()(webView, url);
#if DEBUG
        NSLog(@"decidePolicyForNavigationAction webView %p URL: %@ previousURL %@ -> %s", webView, requestUrl, ((UNOWebView*)webView).lastNavigationUrl, cancel ? "Cancel" : "Allow");
#endif

        decisionHandler(cancel ? WKNavigationActionPolicyCancel : WKNavigationActionPolicyAllow);
        ((UNOWebView*)webView).lastNavigationUrl = requestUrl;
        
        // fake finishing event
        uno_get_webview_navigation_finishing_callback()(webView, url);
    } else if (((UNOWebView*)webView).virtualHostScheme && [scheme caseInsensitiveCompare:((UNOWebView*)webView).virtualHostScheme] == NSOrderedSame) {
        // virtual hosts are served (and WebResourceRequested raised) by the scheme handler
#if DEBUG
        NSLog(@"decidePolicyForNavigationAction webView %p URL: %@ virtual host -> Allow", webView, requestUrl);
#endif
        decisionHandler(WKNavigationActionPolicyAllow);
        ((UNOWebView*)webView).lastNavigationUrl = requestUrl;
    } else {
        bool isUnsupportedScheme = [scheme caseInsensitiveCompare:@"http"] != NSOrderedSame &&
        [scheme caseInsensitiveCompare:@"https"] != NSOrderedSame &&
        [scheme caseInsensitiveCompare:@"file"] != NSOrderedSame;
        if (isUnsupportedScheme) {
            const char *url = requestUrl.absoluteString.UTF8String;
            bool cancelled = uno_get_webview_unsupported_scheme_identified_callback()(webView, url);
#if DEBUG
            NSLog(@"decidePolicyForNavigationAction webView %p URL: %@ scheme %@ action: %s", webView, requestUrl, scheme, cancelled ? "Cancel" : "Allow");
#endif
            decisionHandler(cancelled ? WKNavigationActionPolicyCancel : WKNavigationActionPolicyAllow);
        } else {
            uno_webview_resource_requested_fn_ptr resourceCallback = uno_get_webview_resource_requested_callback();
            const char *headersJson = NULL;
#if DEBUG
            NSLog(@"decidePolicyForNavigationAction webView %p URL: %@ scheme %@ - checking WebResourceRequested", webView, requestUrl, requestUrl.scheme);
#endif
            if (resourceCallback != NULL) {
                headersJson = resourceCallback(webView, requestUrl.absoluteString.UTF8String, navigationAction.request.HTTPMethod.UTF8String);
            }

            if (headersJson != NULL) {
                // headersJson contains potentially sensitive header names and values (e.g. Authorization tokens).
                // Avoid logging header values to prevent leaking secrets in debug output. Log only the header names.
                
#if DEBUG
                @try {
                    NSString *h = [NSString stringWithUTF8String:headersJson];
                    NSData *d = [h dataUsingEncoding:NSUTF8StringEncoding];
                    NSError *parseError = nil;
                    NSDictionary *parsed = [NSJSONSerialization JSONObjectWithData:d options:0 error:&parseError];
                    if (parsed && !parseError) {
                        NSArray *keys = [parsed allKeys];
                        NSLog(@"decidePolicyForNavigationAction webView %p - injecting headers (keys only): %@", webView, keys);
                    } else {
                        // Fallback: do not log the raw JSON
                        NSLog(@"decidePolicyForNavigationAction webView %p - injecting headers (keys unavailable)", webView);
                    }
                } @catch (NSException *ex) {
                    NSLog(@"decidePolicyForNavigationAction webView %p - injecting headers (parse error)", webView);
                }
#endif
                decisionHandler(WKNavigationActionPolicyCancel);

                NSMutableURLRequest *newRequest = [navigationAction.request mutableCopy];
                NSString *h = [NSString stringWithUTF8String:headersJson];
                NSData *d = [h dataUsingEncoding:NSUTF8StringEncoding];
                NSError *e = nil;
                NSDictionary *headers = [NSJSONSerialization JSONObjectWithData:d options:0 error:&e];
                if (headers && !e) {
                    for (NSString *key in headers) {
                        NSString *value = headers[key];
                        [newRequest setValue:value forHTTPHeaderField:key];
#if DEBUG
                        NSLog(@"decidePolicyForNavigationAction - adding header %@: %@", key, value);
#endif
                    }
                }
#if DEBUG
                else if (e) {
                    NSLog(@"decidePolicyForNavigationAction webView %p - JSON parse error: %@", webView, e);
                }
#endif

                free((void*)headersJson);
                [webView loadRequest:newRequest];
            } else {
#if DEBUG
                NSLog(@"decidePolicyForNavigationAction webView %p URL: %@ scheme %@ -> Allow", webView, requestUrl, requestUrl.scheme);
#endif
                decisionHandler(WKNavigationActionPolicyAllow);
                ((UNOWebView*)webView).lastNavigationUrl = requestUrl;
            }
        }
    }
}

// WKScriptMessageHandler

- (void)userContentController:(WKUserContentController *)userContentController didReceiveScriptMessage:(WKScriptMessage *)message {
#if DEBUG
    NSLog(@"userContentController %p didReceiveScriptMessage: %p name %@ -> Allow", userContentController, message, message.name);
#endif
    if ([message.name isEqualToString:@"unoWebView"]) {
        if ([message.body isKindOfClass:NSString.class]) {
            const char* body = ((NSString*)message.body).UTF8String;
#if DEBUG
            NSLog(@"didReceiveScriptMessage body %s", body);
#endif
            NS_VALID_UNTIL_END_OF_SCOPE NSString *frameUrl = message.frameInfo.request.URL.absoluteString;
            uno_get_webview_receive_web_message_callback()(self, body, frameUrl.UTF8String, message.frameInfo.isMainFrame);
        } else {
#if DEBUG
            NSLog(@"didReceiveScriptMessage (not a string) body: %@", message.body);
#endif
        }
    }
}

@synthesize originalSuperView;

@end
