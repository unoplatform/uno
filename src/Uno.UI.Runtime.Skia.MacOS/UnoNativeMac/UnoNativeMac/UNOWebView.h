//
//  UNOWebView.h
//

#pragma once

#import "UnoNativeMac.h"
#import "UNOApplication.h"

NS_ASSUME_NONNULL_BEGIN

typedef void (*uno_webview_javascript_fn_ptr)(NSInteger /* handle */, const char* _Nullable /* result */, const char* _Nullable /* error */);

uno_webview_javascript_fn_ptr uno_get_execute_callback(void);
void uno_set_execute_callback(uno_webview_javascript_fn_ptr p);

uno_webview_javascript_fn_ptr uno_get_invoke_callback(void);
void uno_set_invoke_callback(uno_webview_javascript_fn_ptr p);

typedef bool (*uno_webview_navigation_starting_fn_ptr)(WKWebView* /* handle */, const char* _Nullable /* url */);
uno_webview_navigation_starting_fn_ptr uno_get_webview_navigation_starting_callback(void);

typedef void (*uno_webview_navigation_finishing_fn_ptr)(WKWebView* /* handle */, const char* _Nullable /* url */);
uno_webview_navigation_finishing_fn_ptr uno_get_webview_navigation_finishing_callback(void);
uno_webview_navigation_finishing_fn_ptr uno_get_webview_receive_web_message_callback(void);

typedef void (*uno_webview_navigation_failing_fn_ptr)(WKWebView* /* handle */, const char* _Nullable /* url */, int status);
uno_webview_navigation_failing_fn_ptr uno_get_webview_navigation_failing_callback(void);

typedef void (*uno_webview_webview_notification_fn_ptr)(WKWebView* /* handle */, NSInteger /* keyId */);
uno_webview_webview_notification_fn_ptr uno_get_webview_notification_callback(void);

void uno_set_webview_navigation_callbacks(uno_webview_navigation_starting_fn_ptr starting, uno_webview_navigation_finishing_fn_ptr finishing, uno_webview_webview_notification_fn_ptr notification, uno_webview_navigation_finishing_fn_ptr web_message, uno_webview_navigation_failing_fn_ptr failing);

typedef bool (*uno_webview_unsupported_scheme_identified_fn_ptr)(WKWebView* /* handle */, const char* /* url */);
uno_webview_unsupported_scheme_identified_fn_ptr uno_get_webview_unsupported_scheme_identified_callback(void);
void uno_set_webview_unsupported_scheme_identified_callback(uno_webview_unsupported_scheme_identified_fn_ptr fn_ptr);

// WebResourceRequested callback - returns JSON string of headers to add, or NULL if no modification needed
typedef const char* _Nullable (*uno_webview_resource_requested_fn_ptr)(WKWebView* /* handle */, const char* /* url */, const char* /* method */);
uno_webview_resource_requested_fn_ptr uno_get_webview_resource_requested_callback(void);
void uno_set_webview_resource_requested_callback(uno_webview_resource_requested_fn_ptr fn_ptr);

NSView* uno_webview_create(NSWindow *window, const char *ok, const char *cancel);

typedef int (*uno_webview_new_window_requested_fn_ptr)(WKWebView* /* handle */, const char* /* targetUrl */, const char* /* refererUrl */);
uno_webview_new_window_requested_fn_ptr uno_get_webview_new_window_requested_callback(void);
void uno_set_webview_new_window_requested_callback(uno_webview_new_window_requested_fn_ptr fn_ptr);

const char* uno_webview_get_title(WKWebView *webview);

void uno_webview_register_message_handler(WKWebView *webview);

bool uno_webview_can_go_back(WKWebView *webview);
bool uno_webview_can_go_forward(WKWebView *webview);

void uno_webview_go_back(WKWebView *webview);
void uno_webview_go_forward(WKWebView *webview);

void uno_webview_navigate(WKWebView *webview, const char* url, const char *jsonHeaders);
void uno_webview_load_html(WKWebView *webview, const char* html);

void uno_webview_reload(WKWebView *webview);
void uno_webview_stop(WKWebView *webview);

void uno_webview_execute_script(WKWebView *webview, NSInteger handle, const char *javascript);
void uno_webview_invoke_script(WKWebView *webview, NSInteger handle, const char *javascript);

// Settings.UserAgent
const char* _Nullable uno_webview_get_user_agent(WKWebView *webview);
void uno_webview_set_user_agent(WKWebView *webview, const char* _Nullable user_agent);

// Settings.IsScriptEnabled
bool uno_webview_get_javascript_enabled(WKWebView *webview);
void uno_webview_set_javascript_enabled(WKWebView *webview, bool enabled);

// PostWebMessageAsString / PostWebMessageAsJson
void uno_webview_post_web_message(WKWebView *webview, const char* payload, bool is_json);

// AddScriptToExecuteOnDocumentCreatedAsync / RemoveScriptToExecuteOnDocumentCreated
// Returns a heap-allocated id string (caller frees via free()).
const char* uno_webview_add_user_script(WKWebView *webview, const char* script);
void uno_webview_remove_user_script(WKWebView *webview, const char* id);

// ShowPrintUIAsync → returns 0 (succeeded), 1 (user-cancelled), 2 (other error)
int uno_webview_show_print_ui(WKWebView *webview);

// PrintToPdfStreamAsync (returns NSData bytes via out params; caller frees buffer with free())
typedef void (*uno_webview_pdf_fn_ptr)(NSInteger /* handle */, const uint8_t* _Nullable /* bytes */, NSInteger /* length */, const char* _Nullable /* error */);
void uno_set_webview_pdf_callback(uno_webview_pdf_fn_ptr fn_ptr);
void uno_webview_print_to_pdf(WKWebView *webview, NSInteger handle);

// Cookies — JSON-encoded payload exchange to keep marshalling simple.
typedef void (*uno_webview_cookies_fn_ptr)(NSInteger /* handle */, const char* _Nullable /* json */, const char* _Nullable /* error */);
void uno_set_webview_cookies_callback(uno_webview_cookies_fn_ptr fn_ptr);
void uno_webview_get_cookies(WKWebView *webview, NSInteger handle, const char* _Nullable uri);
// cookie_json: { name, value, domain, path, isSecure, isHttpOnly, expires } where expires is unix seconds (-1 for session)
void uno_webview_set_cookie(WKWebView *webview, const char* cookie_json);
void uno_webview_delete_cookies(WKWebView *webview, const char* name, const char* _Nullable domain, const char* _Nullable path);
void uno_webview_delete_all_cookies(WKWebView *webview);

// https://learn.microsoft.com/en-us/dotnet/api/microsoft.web.webview2.core.corewebview2weberrorstatus
typedef NS_ENUM(uint32, CoreWebView2WebErrorStatus) {
    CoreWebView2WebErrorStatusUnknown = 0,
    CoreWebView2WebErrorStatusCertificateCommonNameIsIncorrect = 1,
    CoreWebView2WebErrorStatusCertificateExpired = 2,
    CoreWebView2WebErrorStatusClientCertificateContainsErrors = 3,
    CoreWebView2WebErrorStatusCertificateRevoked = 4,
    CoreWebView2WebErrorStatusCertificateIsInvalid = 5,
    CoreWebView2WebErrorStatusServerUnreachable = 6,
    CoreWebView2WebErrorStatusTimeout = 7,
    CoreWebView2WebErrorStatusErrorHttpInvalidServerResponse = 8,
    CoreWebView2WebErrorStatusConnectionAborted = 9,
    CoreWebView2WebErrorStatusConnectionReset = 10,
    CoreWebView2WebErrorStatusDisconnected = 11,
    CoreWebView2WebErrorStatusCannotConnect = 12,
    CoreWebView2WebErrorStatusHostNameNotResolved = 13,
    CoreWebView2WebErrorStatusOperationCanceled = 14,
    CoreWebView2WebErrorStatusRedirectFailed = 15,
    CoreWebView2WebErrorStatusUnexpectedError = 16,
    CoreWebView2WebErrorStatusValidAuthenticationCredentialsRequired = 17,
    CoreWebView2WebErrorStatusValidProxyAuthenticationRequired = 18,
};

@interface UNOWebView : WKWebView <UNONativeElement, WKUIDelegate, WKNavigationDelegate, WKScriptMessageHandler>

@property (nonatomic, retain) NSString *okString;
@property (nonatomic, retain) NSString *cancelString;

@property (nonatomic, retain) NSURL *lastNavigationUrl;

@property (nonatomic) bool scrollingEnabled;

- (nullable instancetype)initWithCoder:(NSCoder *)coder NS_DESIGNATED_INITIALIZER;

- (instancetype)initWithFrame:(CGRect)frame configuration:(WKWebViewConfiguration *)configuration NS_DESIGNATED_INITIALIZER;

- (void)didChangeValueForKey:(NSString *)key;

// WKUIDelegate

- (void)webView:(WKWebView *)webView runJavaScriptConfirmPanelWithMessage:(NSString *)message initiatedByFrame:(WKFrameInfo *)frame completionHandler:(void (^)(BOOL result))completionHandler;

- (void)webView:(WKWebView *)webView runJavaScriptAlertPanelWithMessage:(NSString *)message initiatedByFrame:(WKFrameInfo *)frame completionHandler:(void (^)(void))completionHandler;

- (void)webView:(WKWebView *)webView runJavaScriptTextInputPanelWithPrompt:(NSString *)prompt defaultText:(nullable NSString *)defaultText initiatedByFrame:(WKFrameInfo *)frame completionHandler:(void (^)(NSString * _Nullable result))completionHandler;

- (void)webView:(WKWebView *)webView runOpenPanelWithParameters:(WKOpenPanelParameters *)parameters initiatedByFrame:(WKFrameInfo *)frame completionHandler:(void (^)(NSArray<NSURL *> * _Nullable URLs))completionHandler;

- (WKWebView *)webView:(WKWebView *)webView createWebViewWithConfiguration:(WKWebViewConfiguration *)configuration forNavigationAction:(WKNavigationAction *)navigationAction windowFeatures:(WKWindowFeatures *)windowFeatures;

// WKNavigationDelegate

- (void)webView:(WKWebView *)webView didStartProvisionalNavigation:(null_unspecified WKNavigation *)navigation;
- (void)webView:(WKWebView *)webView didFailProvisionalNavigation:(null_unspecified WKNavigation *)navigation withError:(NSError *)error;
- (void)webView:(WKWebView *)webView didFinishNavigation:(null_unspecified WKNavigation *)navigation;

- (void)webView:(WKWebView *)webView didReceiveServerRedirectForProvisionalNavigation:(null_unspecified WKNavigation *)navigation;

- (void)webView:(WKWebView *)webView decidePolicyForNavigationAction:(WKNavigationAction *)navigationAction decisionHandler:(void (^)(WKNavigationActionPolicy))decisionHandler;

// WKScriptMessageHandler

- (void)userContentController:(WKUserContentController *)userContentController didReceiveScriptMessage:(WKScriptMessage *)message;

@end

NS_ASSUME_NONNULL_END
