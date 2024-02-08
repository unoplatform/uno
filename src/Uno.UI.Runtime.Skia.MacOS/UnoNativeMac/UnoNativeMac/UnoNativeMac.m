//
//  UnoNativeMac.m
//

#import "UnoNativeMac.h"
#import "UNOApplication.h"

static draw_fn_ptr draw;
static resize_fn_ptr resize;

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
