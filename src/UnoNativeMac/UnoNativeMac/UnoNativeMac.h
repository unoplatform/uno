//
//  UnoNativeMac.h
//

#import <Foundation/Foundation.h>
#import <AppKit/AppKit.h>

NS_ASSUME_NONNULL_BEGIN

void uno_app_initialize(void);
NSWindow* uno_app_get_main_window(void);

typedef void (*draw_fn_ptr)(void* /* window */, double /* width */, double /* height */, void* /* texture */);
draw_fn_ptr uno_get_draw_callback(void);
void uno_set_draw_callback(draw_fn_ptr p);

typedef void (*resize_fn_ptr)(void* /* window */, double /* width */, double /* height */);
resize_fn_ptr uno_get_resize_callback(void);
void uno_set_resize_callback(resize_fn_ptr p);

NS_ASSUME_NONNULL_END
