//
//  UnoNativeMac.m
//

#import "UnoNativeMac.h"
#import "UNOApplication.h"

static draw_fn_ptr draw;
static resize_fn_ptr resize;

static UNOApplicationDelegate *ad;

void uno_app_initialize(void)
{
    NSApplication *app = [NSApplication sharedApplication];
    app.delegate = ad = [[UNOApplicationDelegate alloc] init];
    [app setActivationPolicy:NSApplicationActivationPolicyRegular];
    
    // KVO observation for dark/light theme
    [app addObserver:ad forKeyPath:NSStringFromSelector(@selector(effectiveAppearance)) options:NSKeyValueObservingOptionNew context:nil];
}

inline draw_fn_ptr uno_get_draw_callback(void)
{
    return draw;
}

void uno_set_draw_callback(draw_fn_ptr p)
{
    draw = p;
}

inline resize_fn_ptr uno_get_resize_callback(void)
{
    return resize;
}

void uno_set_resize_callback(resize_fn_ptr p)
{
    resize = p;
}
