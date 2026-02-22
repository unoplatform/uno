//
//  UNOMenu.m
//

#import "UNOMenu.h"

static menu_item_callback_fn_ptr s_menuItemCallback = NULL;

@interface UNOMenuItemTarget : NSObject
@property (nonatomic, assign) int32 callbackId;
- (void)menuItemClicked:(id)sender;
@end

@implementation UNOMenuItemTarget

- (void)menuItemClicked:(id)sender {
    if (s_menuItemCallback != NULL) {
        s_menuItemCallback(self.callbackId);
    }
}

@end

// We need to retain the targets to prevent them from being deallocated
static NSMutableArray<UNOMenuItemTarget *> *s_menuTargets = nil;

void uno_menu_set_item_callback(menu_item_callback_fn_ptr callback) {
    s_menuItemCallback = callback;
}

void uno_menu_bar_clear(void) {
    NSApplication *app = [NSApplication sharedApplication];
    NSMenu *mainMenu = [app mainMenu];
    
    if (mainMenu == nil) {
        mainMenu = [[NSMenu alloc] init];
        [app setMainMenu:mainMenu];
    }
    
    // Keep the Apple menu (first item) if it exists
    while ([mainMenu numberOfItems] > 1) {
        [mainMenu removeItemAtIndex:1];
    }
    
    // Clear the callback targets
    s_menuTargets = [[NSMutableArray alloc] init];
}

void uno_menu_bar_add_menu(NSMenu *menu, const char *title) {
    if (menu == nil) {
        return;
    }
    
    NSApplication *app = [NSApplication sharedApplication];
    NSMenu *mainMenu = [app mainMenu];
    
    if (mainMenu == nil) {
        mainMenu = [[NSMenu alloc] init];
        [app setMainMenu:mainMenu];
    }
    
    NSString *titleStr = [NSString stringWithUTF8String:title];
    NSMenuItem *menuItem = [[NSMenuItem alloc] initWithTitle:titleStr action:nil keyEquivalent:@""];
    [menuItem setSubmenu:menu];
    [mainMenu addItem:menuItem];
}

NSMenu* uno_menu_create(const char *title) {
    NSString *titleStr = [NSString stringWithUTF8String:title];
    NSMenu *menu = [[NSMenu alloc] initWithTitle:titleStr];
    [menu setAutoenablesItems:NO];
    return menu;
}

void uno_menu_add_separator(NSMenu *menu) {
    if (menu == nil) {
        return;
    }
    [menu addItem:[NSMenuItem separatorItem]];
}

void uno_menu_add_item(NSMenu *menu, const char *title, bool enabled, int32 callbackId) {
    if (menu == nil) {
        return;
    }
    
    NSString *titleStr = [NSString stringWithUTF8String:title];
    
    UNOMenuItemTarget *target = [[UNOMenuItemTarget alloc] init];
    target.callbackId = callbackId;
    
    if (s_menuTargets == nil) {
        s_menuTargets = [[NSMutableArray alloc] init];
    }
    [s_menuTargets addObject:target];
    
    NSMenuItem *item = [[NSMenuItem alloc] initWithTitle:titleStr
                                                  action:@selector(menuItemClicked:)
                                           keyEquivalent:@""];
    [item setTarget:target];
    [item setEnabled:enabled];
    [menu addItem:item];
}

void uno_menu_add_toggle_item(NSMenu *menu, const char *title, bool isChecked, bool enabled, int32 callbackId) {
    if (menu == nil) {
        return;
    }
    
    NSString *titleStr = [NSString stringWithUTF8String:title];
    
    UNOMenuItemTarget *target = [[UNOMenuItemTarget alloc] init];
    target.callbackId = callbackId;
    
    if (s_menuTargets == nil) {
        s_menuTargets = [[NSMutableArray alloc] init];
    }
    [s_menuTargets addObject:target];
    
    NSMenuItem *item = [[NSMenuItem alloc] initWithTitle:titleStr
                                                  action:@selector(menuItemClicked:)
                                           keyEquivalent:@""];
    [item setTarget:target];
    [item setEnabled:enabled];
    [item setState:isChecked ? NSControlStateValueOn : NSControlStateValueOff];
    [menu addItem:item];
}

void uno_menu_add_submenu(NSMenu *parentMenu, NSMenu *submenu, const char *title) {
    if (parentMenu == nil || submenu == nil) {
        return;
    }
    
    NSString *titleStr = [NSString stringWithUTF8String:title];
    NSMenuItem *item = [[NSMenuItem alloc] initWithTitle:titleStr action:nil keyEquivalent:@""];
    [item setSubmenu:submenu];
    [parentMenu addItem:item];
}
