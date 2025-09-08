//
//  MouseButtons.h
//  UnoNativeMac
//
//  Created by Andres Pineda on 2025-08-29.
//

#import <AppKit/AppKit.h>

NS_ASSUME_NONNULL_BEGIN

@interface MouseButtons : NSObject

/// Tracks NSEvents received (from -[NSWindow sendEvent:] or -[NSApplication sendEvent:]).
+ (void)track:(NSEvent *)event;

/// Current pressed-buttons bitmask (0=none). Built from tracked events; falls back to AppKit/Quartz if needed.
+ (NSInteger)mask;

@end

NS_ASSUME_NONNULL_END
