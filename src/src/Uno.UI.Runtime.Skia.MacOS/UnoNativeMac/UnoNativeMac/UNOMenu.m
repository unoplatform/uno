#import "UNOMenu.h"

static NSMenu* _uno_main_menu;
static NSMutableArray<NSMenu*>* _uno_menu_stack;
static NSMutableDictionary<NSString*, NSMenuItem*>* _uno_items_by_id;
static menu_item_click_fn_ptr _uno_click_callback;

static void ensure_containers() {
    if (!_uno_menu_stack) { _uno_menu_stack = [NSMutableArray arrayWithCapacity:4]; }
    if (!_uno_items_by_id) { _uno_items_by_id = [NSMutableDictionary dictionaryWithCapacity:32]; }
}

static void clear_state() {
    _uno_main_menu = nil;
    [_uno_menu_stack removeAllObjects];
    [_uno_items_by_id removeAllObjects];
}

void uno_menu_set_click_callback(menu_item_click_fn_ptr p) {
    _uno_click_callback = p;
}

void uno_menu_begin(void) {
    ensure_containers();
    clear_state();
    _uno_main_menu = [[NSMenu alloc] initWithTitle:@""];
}

void uno_menu_begin_top(const char* id, const char* title) {
    ensure_containers();
    NSMenu* submenu = [[NSMenu alloc] initWithTitle:[NSString stringWithUTF8String:title]];
    NSMenuItem* topItem = [[NSMenuItem alloc] initWithTitle:[NSString stringWithUTF8String:title] action:nil keyEquivalent:@""];
    [topItem setSubmenu:submenu];
    [_uno_main_menu addItem:topItem];
    [_uno_menu_stack addObject:submenu];
}

static void on_item_invoke(NSMenuItem* sender) {
    if (_uno_click_callback) {
        NSString* sid = (NSString*)sender.representedObject;
        _uno_click_callback(sid.UTF8String);
    }
}

void uno_menu_add_item(const char* id, const char* title, const char* keyEquivalent) {
    ensure_containers();
    NSMenu* current = _uno_menu_stack.lastObject;
    if (!current) { return; }

    NSMenuItem* item = [[NSMenuItem alloc] initWithTitle:[NSString stringWithUTF8String:title]
                                                  action:@selector(onInvoke:)
                                           keyEquivalent:(keyEquivalent ? [NSString stringWithUTF8String:keyEquivalent] : @"")];
    [item setTarget:NSApp];
    // We cannot set selector on NSApp for custom method, fallback to block action
    [item setAction:nil];
    item.target = nil;
    // Use representedObject to store id
    item.representedObject = [NSString stringWithUTF8String:id];
    // Add a proxy to call our C callback
    item.action = @selector(performClick:);
    // We will intercept with a custom target using block
    id blockTarget = [^ (id _) { on_item_invoke(item); } copy];
    [item setTarget:blockTarget];

    [current addItem:item];
    _uno_items_by_id[[NSString stringWithUTF8String:id]] = item;
}

void uno_menu_add_separator(void) {
    NSMenu* current = _uno_menu_stack.lastObject;
    if (current) { [current addItem:[NSMenuItem separatorItem]]; }
}

void uno_menu_begin_submenu(const char* id, const char* title) {
    ensure_containers();
    NSMenu* current = _uno_menu_stack.lastObject;
    if (!current) { return; }

    NSMenuItem* parentItem = [[NSMenuItem alloc] initWithTitle:[NSString stringWithUTF8String:title] action:nil keyEquivalent:@""];
    NSMenu* submenu = [[NSMenu alloc] initWithTitle:[NSString stringWithUTF8String:title]];
    [parentItem setSubmenu:submenu];
    parentItem.representedObject = [NSString stringWithUTF8String:id];

    [current addItem:parentItem];
    [_uno_menu_stack addObject:submenu];
}

void uno_menu_end_submenu(void) {
    if (_uno_menu_stack.count > 0) {
        [_uno_menu_stack removeLastObject];
    }
}

void uno_menu_end_top(void) {
    // nothing extra beyond stack mgmt
}

void uno_menu_commit(void) {
    [NSApplication sharedApplication].mainMenu = _uno_main_menu;
}

void uno_menu_set_enabled(const char* id, bool enabled) {
    NSString* sid = [NSString stringWithUTF8String:id];
    NSMenuItem* item = _uno_items_by_id[sid];
    if (item) { item.enabled = enabled ? YES : NO; }
}
