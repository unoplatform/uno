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
static uno_webview_navigation_finishing_fn_ptr receive_web_message;
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

inline uno_webview_navigation_finishing_fn_ptr uno_get_webview_receive_web_message_callback(void)
{
    return receive_web_message;
}

inline uno_webview_navigation_failing_fn_ptr uno_get_webview_navigation_failing_callback(void)
{
    return navigation_failing;
}

void uno_set_webview_navigation_callbacks(uno_webview_navigation_starting_fn_ptr starting, uno_webview_navigation_finishing_fn_ptr finishing, uno_webview_webview_notification_fn_ptr notification, uno_webview_navigation_finishing_fn_ptr web_message, uno_webview_navigation_failing_fn_ptr failing)
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


NSView* uno_webview_create(NSWindow *window, const char *ok, const char *cancel)
{
    WKWebViewConfiguration* config = [[WKWebViewConfiguration alloc] init];
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

    webview.originalSuperView = ((UNOWindow*)window).renderingView;
    return webview;
}

void uno_webview_register_message_handler(WKWebView *webview)
{
#if DEBUG
    NSLog(@"uno_webview_register_message_handler %p", webview);
#endif
    __weak id weakSelf = webview;
    [webview.configuration.userContentController addScriptMessageHandler:weakSelf name:@"unoWebView"];
}

const char* uno_webview_get_title(WKWebView *webview)
{
    return strdup(webview.title.UTF8String);
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
        const char* r = nil;
        const char* e = nil;
        if (error) {
#if DEBUG
            NSLog(@"uno_webview_execute_script %p completionHandler error: %@", webview, error);
#endif
            e = error.description.UTF8String;
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
                    e = jsonError.description.UTF8String;
                } else {
#if DEBUG
                    NSLog(@"uno_webview_execute_script %p completionHandler data: %@", webview, data);
#endif
                    r = [[[NSString alloc] initWithData:data encoding:NSUTF8StringEncoding] UTF8String];
                }
            } else if ([result isKindOfClass:NSString.class]) {
                // double quote existing quotes so we can quote the whole thing
                NSString *s = [((NSString*) result) stringByReplacingOccurrencesOfString:@"\"" withString:@"\\\""];
                NSString *quoted = [NSString stringWithFormat:@"\"%@\"", s];
#if DEBUG
                NSLog(@"uno_webview_execute_script %p completionHandler string: %@", webview, quoted);
#endif
                r = quoted.UTF8String;
            } else {
                NSString *s = result.description;
#if DEBUG
                NSLog(@"uno_webview_execute_script %p completionHandler object: %@", webview, s);
#endif
                r = s.UTF8String;
            }
        }
        uno_get_execute_callback()(handle, r, e);
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
        const char* r = result ? result.UTF8String : nil;
        const char* e = error ? error.description.UTF8String : nil;
        uno_get_invoke_callback()(handle, r, e);
    }];
}

void uno_webview_set_scrolling_enabled(UNOWebView* webview, bool enabled)
{
    webview.scrollingEnabled = enabled;
}

#pragma mark - CoreWebView2Settings.UserAgent

const char* uno_webview_get_user_agent(WKWebView *webview)
{
    NSString *ua = webview.customUserAgent;
    if (ua == nil || ua.length == 0) {
        return NULL;
    }
    return strdup(ua.UTF8String);
}

void uno_webview_set_user_agent(WKWebView *webview, const char* user_agent)
{
    webview.customUserAgent = user_agent ? [NSString stringWithUTF8String:user_agent] : nil;
}

#pragma mark - CoreWebView2Settings.IsScriptEnabled

bool uno_webview_get_javascript_enabled(WKWebView *webview)
{
    if (@available(macOS 11, *)) {
        return webview.configuration.defaultWebpagePreferences.allowsContentJavaScript;
    }
    return webview.configuration.preferences.javaScriptEnabled;
}

void uno_webview_set_javascript_enabled(WKWebView *webview, bool enabled)
{
    if (@available(macOS 11, *)) {
        webview.configuration.defaultWebpagePreferences.allowsContentJavaScript = enabled ? YES : NO;
    } else {
        webview.configuration.preferences.javaScriptEnabled = enabled ? YES : NO;
    }
}

#pragma mark - PostWebMessage*

// Dispatch a chrome.webview message-event into the page. We install a tiny polyfill on first use
// (matches the WinUI3 contract: pages subscribe via window.chrome.webview.addEventListener('message', h)).
void uno_webview_post_web_message(WKWebView *webview, const char* payload, bool is_json)
{
    if (payload == NULL) {
        return;
    }
    NSString *data;
    if (is_json) {
        data = [NSString stringWithUTF8String:payload];
    } else {
        NSData *bytes = [[NSString stringWithUTF8String:payload] dataUsingEncoding:NSUTF8StringEncoding];
        NSError *e = nil;
        NSData *encoded = [NSJSONSerialization dataWithJSONObject:[[NSString alloc] initWithData:bytes encoding:NSUTF8StringEncoding] options:NSJSONWritingFragmentsAllowed error:&e];
        if (e || encoded == nil) {
            // Fallback: rough escape that quotes the string. Real escaping happens in the polyfill on the page side.
            NSString *escaped = [[NSString stringWithUTF8String:payload] stringByReplacingOccurrencesOfString:@"\\" withString:@"\\\\"];
            escaped = [escaped stringByReplacingOccurrencesOfString:@"\"" withString:@"\\\""];
            data = [NSString stringWithFormat:@"\"%@\"", escaped];
        } else {
            data = [[NSString alloc] initWithData:encoded encoding:NSUTF8StringEncoding];
        }
    }

    NSString *script = [NSString stringWithFormat:
        @"(function(){"
        @"window.chrome=window.chrome||{};"
        @"window.chrome.webview=window.chrome.webview||{};"
        @"if(!window.chrome.webview.__unoListeners){"
            @"window.chrome.webview.__unoListeners=[];"
            @"var oa=window.chrome.webview.addEventListener;"
            @"window.chrome.webview.addEventListener=function(t,h){"
                @"if(t==='message'){window.chrome.webview.__unoListeners.push(h);}"
                @"else if(typeof oa==='function'){oa.call(window.chrome.webview,t,h);}"
            @"};"
        @"}"
        @"var d=%@;var ev={data:d};"
        @"window.chrome.webview.__unoListeners.forEach(function(h){try{h(ev);}catch(e){}});"
        @"})();",
        data];
    [webview evaluateJavaScript:script completionHandler:nil];
}

#pragma mark - AddScript/RemoveScript

static NSMutableDictionary<NSValue*, NSMutableDictionary<NSString*, WKUserScript*>*>* _scriptRegistry = nil;

static NSMutableDictionary<NSString*, WKUserScript*>* uno_webview_script_dict(WKWebView *webview)
{
    if (_scriptRegistry == nil) {
        _scriptRegistry = [NSMutableDictionary dictionary];
    }
    NSValue *key = [NSValue valueWithNonretainedObject:webview];
    NSMutableDictionary *dict = _scriptRegistry[key];
    if (dict == nil) {
        dict = [NSMutableDictionary dictionary];
        _scriptRegistry[key] = dict;
    }
    return dict;
}

const char* uno_webview_add_user_script(WKWebView *webview, const char* script)
{
    NSString *source = [NSString stringWithUTF8String:script];
    WKUserScript *us = [[WKUserScript alloc] initWithSource:source
                                              injectionTime:WKUserScriptInjectionTimeAtDocumentStart
                                           forMainFrameOnly:NO];
    [webview.configuration.userContentController addUserScript:us];

    NSString *uuid = [[NSUUID UUID] UUIDString];
    uno_webview_script_dict(webview)[uuid] = us;
    return strdup(uuid.UTF8String);
}

void uno_webview_remove_user_script(WKWebView *webview, const char* id)
{
    NSString *uuid = [NSString stringWithUTF8String:id];
    NSMutableDictionary *dict = uno_webview_script_dict(webview);
    if (dict[uuid] == nil) {
        return;
    }
    [dict removeObjectForKey:uuid];

    // WKUserContentController has no "remove single script" API; rebuild from the remaining set.
    [webview.configuration.userContentController removeAllUserScripts];
    for (WKUserScript *remaining in dict.allValues) {
        [webview.configuration.userContentController addUserScript:remaining];
    }
}

#pragma mark - Print

int uno_webview_show_print_ui(WKWebView *webview)
{
    if (@available(macOS 11, *)) {
        NSPrintInfo *info = [NSPrintInfo sharedPrintInfo];
        NSPrintOperation *op = [webview printOperationWithPrintInfo:info];
        if (op == nil) {
            return 2;
        }
        BOOL ok = [op runOperation];
        return ok ? 0 : 1;
    }
    return 2;
}

static uno_webview_pdf_fn_ptr pdf_callback;

void uno_set_webview_pdf_callback(uno_webview_pdf_fn_ptr fn_ptr)
{
    pdf_callback = fn_ptr;
}

void uno_webview_print_to_pdf(WKWebView *webview, NSInteger handle)
{
    if (@available(macOS 11, *)) {
        WKPDFConfiguration *config = [[WKPDFConfiguration alloc] init];
        [webview createPDFWithConfiguration:config completionHandler:^(NSData * _Nullable data, NSError * _Nullable error) {
            if (pdf_callback == NULL) {
                return;
            }
            if (error != nil || data == nil) {
                const char *e = error ? error.description.UTF8String : "PDF generation returned no data";
                pdf_callback(handle, NULL, 0, e);
            } else {
                pdf_callback(handle, (const uint8_t*)data.bytes, (NSInteger)data.length, NULL);
            }
        }];
    } else if (pdf_callback != NULL) {
        pdf_callback(handle, NULL, 0, "createPDF requires macOS 11+");
    }
}

#pragma mark - Cookies

static uno_webview_cookies_fn_ptr cookies_callback;

void uno_set_webview_cookies_callback(uno_webview_cookies_fn_ptr fn_ptr)
{
    cookies_callback = fn_ptr;
}

static NSString* uno_webview_cookies_to_json(NSArray<NSHTTPCookie*> *cookies, NSString* _Nullable hostFilter)
{
    NSMutableArray *list = [NSMutableArray array];
    for (NSHTTPCookie *c in cookies) {
        if (hostFilter != nil) {
            NSString *domain = [c.domain hasPrefix:@"."] ? [c.domain substringFromIndex:1] : c.domain;
            if (![[domain lowercaseString] isEqualToString:[hostFilter lowercaseString]] &&
                ![[hostFilter lowercaseString] hasSuffix:[@"." stringByAppendingString:[domain lowercaseString]]]) {
                continue;
            }
        }
        NSMutableDictionary *d = [NSMutableDictionary dictionary];
        d[@"name"] = c.name ?: @"";
        d[@"value"] = c.value ?: @"";
        d[@"domain"] = c.domain ?: @"";
        d[@"path"] = c.path ?: @"/";
        d[@"isSecure"] = @(c.isSecure);
        d[@"isHttpOnly"] = @(c.isHTTPOnly);
        if (c.expiresDate != nil) {
            d[@"expires"] = @([c.expiresDate timeIntervalSince1970]);
        } else {
            d[@"expires"] = @(-1);
        }
        [list addObject:d];
    }
    NSError *err = nil;
    NSData *data = [NSJSONSerialization dataWithJSONObject:list options:0 error:&err];
    if (err) {
        return @"[]";
    }
    return [[NSString alloc] initWithData:data encoding:NSUTF8StringEncoding];
}

void uno_webview_get_cookies(WKWebView *webview, NSInteger handle, const char* uri)
{
    NSString *hostFilter = nil;
    if (uri != NULL) {
        NSURL *url = [NSURL URLWithString:[NSString stringWithUTF8String:uri]];
        hostFilter = url.host;
    }
    WKHTTPCookieStore *store = webview.configuration.websiteDataStore.httpCookieStore;
    [store getAllCookies:^(NSArray<NSHTTPCookie *> *cookies) {
        if (cookies_callback == NULL) {
            return;
        }
        @try {
            NSString *json = uno_webview_cookies_to_json(cookies, hostFilter);
            cookies_callback(handle, json.UTF8String, NULL);
        } @catch (NSException *ex) {
            cookies_callback(handle, NULL, ex.reason.UTF8String);
        }
    }];
}

static NSHTTPCookie* uno_webview_cookie_from_json(NSString *json)
{
    NSData *data = [json dataUsingEncoding:NSUTF8StringEncoding];
    NSError *err = nil;
    NSDictionary *dict = [NSJSONSerialization JSONObjectWithData:data options:0 error:&err];
    if (err || dict == nil) {
        return nil;
    }
    NSMutableDictionary *props = [NSMutableDictionary dictionary];
    props[NSHTTPCookieName] = dict[@"name"] ?: @"";
    props[NSHTTPCookieValue] = dict[@"value"] ?: @"";
    NSString *domain = dict[@"domain"];
    props[NSHTTPCookieDomain] = (domain && domain.length > 0) ? domain : @"localhost";
    NSString *path = dict[@"path"];
    props[NSHTTPCookiePath] = (path && path.length > 0) ? path : @"/";
    if ([dict[@"isSecure"] boolValue]) {
        props[NSHTTPCookieSecure] = @"TRUE";
    }
    NSNumber *expires = dict[@"expires"];
    if (expires != nil && expires.doubleValue > 0) {
        props[NSHTTPCookieExpires] = [NSDate dateWithTimeIntervalSince1970:expires.doubleValue];
    }
    return [NSHTTPCookie cookieWithProperties:props];
}

void uno_webview_set_cookie(WKWebView *webview, const char* cookie_json)
{
    if (cookie_json == NULL) {
        return;
    }
    NSHTTPCookie *cookie = uno_webview_cookie_from_json([NSString stringWithUTF8String:cookie_json]);
    if (cookie == nil) {
        return;
    }
    [webview.configuration.websiteDataStore.httpCookieStore setCookie:cookie completionHandler:nil];
}

void uno_webview_delete_cookies(WKWebView *webview, const char* name, const char* domain, const char* path)
{
    NSString *nameStr = [NSString stringWithUTF8String:name];
    NSString *domainStr = domain ? [NSString stringWithUTF8String:domain] : nil;
    NSString *pathStr = path ? [NSString stringWithUTF8String:path] : nil;

    WKHTTPCookieStore *store = webview.configuration.websiteDataStore.httpCookieStore;
    [store getAllCookies:^(NSArray<NSHTTPCookie *> *cookies) {
        for (NSHTTPCookie *c in cookies) {
            if (![c.name isEqualToString:nameStr]) {
                continue;
            }
            if (domainStr != nil) {
                NSString *cd = [c.domain hasPrefix:@"."] ? [c.domain substringFromIndex:1] : c.domain;
                NSString *want = [domainStr hasPrefix:@"."] ? [domainStr substringFromIndex:1] : domainStr;
                if (![[cd lowercaseString] isEqualToString:[want lowercaseString]]) {
                    continue;
                }
            }
            if (pathStr != nil && ![c.path isEqualToString:pathStr]) {
                continue;
            }
            [store deleteCookie:c completionHandler:nil];
        }
    }];
}

void uno_webview_delete_all_cookies(WKWebView *webview)
{
    WKHTTPCookieStore *store = webview.configuration.websiteDataStore.httpCookieStore;
    [store getAllCookies:^(NSArray<NSHTTPCookie *> *cookies) {
        for (NSHTTPCookie *c in cookies) {
            [store deleteCookie:c completionHandler:nil];
        }
    }];
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
            uno_get_webview_receive_web_message_callback()(self, body);
        } else {
#if DEBUG
            NSLog(@"didReceiveScriptMessage (not a string) body: %@", message.body);
#endif
        }
    }
}

@synthesize originalSuperView;

@end
