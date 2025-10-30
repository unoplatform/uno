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

NSView* uno_webview_create(NSWindow *window, const char *ok, const char *cancel);

const char* uno_webview_get_title(WKWebView *webview);

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
