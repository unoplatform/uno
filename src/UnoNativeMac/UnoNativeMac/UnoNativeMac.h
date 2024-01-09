//
//  UnoNativeMac.h
//

#import <Foundation/Foundation.h>
#import <AppKit/AppKit.h>
#import <Metal/Metal.h>
#import <MetalKit/MetalKit.h>

NS_ASSUME_NONNULL_BEGIN

void* uno_app_initialize(char *title);
NSWindow* uno_app_get_main_window(void);

typedef void (*draw_fn_ptr)(CGSize, void*);
draw_fn_ptr uno_get_draw_callback(void);
void uno_set_draw_callback(draw_fn_ptr p);

typedef void (*resize_fn_ptr)(CGSize);
resize_fn_ptr uno_get_resize_callback(void);
void uno_set_resize_callback(resize_fn_ptr p);

struct SharedScreenData {
    uint32 ScreenHeightInRawPixels;
    uint32 ScreenWidthInRawPixels;
    uint32 RawPixelsPerViewPixel;
};

typedef void (*window_did_change_screen_fn_ptr)(void);
window_did_change_screen_fn_ptr uno_get_window_did_change_screen_callback(void);
void uno_set_window_did_change_screen_callback(struct SharedScreenData *screenData, window_did_change_screen_fn_ptr p);

typedef void (*window_did_change_screen_parameters_fn_ptr)(void);
window_did_change_screen_parameters_fn_ptr uno_get_window_did_change_screen_parameters_callback(void);
void uno_set_window_did_change_screen_parameters_callback(window_did_change_screen_parameters_fn_ptr p);

NS_ASSUME_NONNULL_END
