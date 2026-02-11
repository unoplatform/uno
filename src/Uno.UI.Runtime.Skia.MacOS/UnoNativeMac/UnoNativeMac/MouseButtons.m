//
//  MouseButtons.m
//  UnoNativeMac
//
//  Created by Andres Pineda on 2025-08-29.
//
#import "MouseButtons.h"
#import <CoreGraphics/CoreGraphics.h>

@implementation MouseButtons

// Simple depth counters per button index (0..7). Main-thread access is fine for AppKit.
static int sDepth[8] = {0};

+ (void)track:(NSEvent *)e
{
    switch (e.type) {
        case NSEventTypeLeftMouseDown:
            sDepth[0]++;
            break;
        case NSEventTypeLeftMouseUp:
            if (sDepth[0] > 0) sDepth[0]--;
            break;
        case NSEventTypeRightMouseDown:
            sDepth[1]++;
            break;
        case NSEventTypeRightMouseUp:
            if (sDepth[1] > 0) sDepth[1]--;
            break;
        case NSEventTypeOtherMouseDown: {
            NSInteger idx = e.buttonNumber;
            if (idx >= 0 && idx < 8) sDepth[idx]++;
            break;
        }
        case NSEventTypeOtherMouseUp: {
            NSInteger idx = e.buttonNumber;
            if (idx >= 0 && idx < 8 && sDepth[idx] > 0) sDepth[idx]--;
            break;
        }

        default:
            break;
    }
}

+ (NSInteger)mask
{
    // 1) Build mask from our tracked state
    NSInteger m = 0;
    for (int i = 0; i < 8; i++) {
        if (sDepth[i] > 0) {
            m |= (1 << i);
        }
    }

    // 2) Get AppKit snapshot
    NSInteger appKitMask = [NSEvent pressedMouseButtons];

    // 3) Decision logic when we have tracked buttons
    if (m != 0) {
        // If AppKit agrees we have buttons pressed, trust our tracking immediately
        if (appKitMask != 0) {
            return m;
        }

        // AppKit says no buttons, but we think there are - check hardware state (lazy evaluation)
        NSInteger quartzMask = 0;
        for (int i = 0; i < 8; i++) {
            if (CGEventSourceButtonState(kCGEventSourceStateCombinedSessionState, (CGMouseButton)i)) {
                quartzMask |= (1 << i);
            }
        }

        // If BOTH AppKit AND Quartz say no buttons are pressed, we likely missed MouseUp events
        // (e.g., native element intercepted them). Reset and trust the hardware state.
        if (quartzMask == 0) {
#if DEBUG
            NSLog(@"MouseButtons: Both AppKit and Quartz report no buttons pressed. Resetting counters.");
#endif
            for (int i = 0; i < 8; i++) {
                sDepth[i] = 0;
            }
            return 0;
        }

        // Quartz shows buttons pressed - trust our tracking (handles macOS 15+ external trackpad where AppKit is wrong)
        return m;
    }

    // 4) No tracked state - prefer AppKit
    if (appKitMask != 0) return appKitMask;

    // 5) Final fallback: poll Quartz for hardware state (lazy evaluation)
    NSInteger quartzMask = 0;
    for (int i = 0; i < 8; i++) {
        if (CGEventSourceButtonState(kCGEventSourceStateCombinedSessionState, (CGMouseButton)i)) {
            quartzMask |= (1 << i);
        }
    }
    return quartzMask;
}

@end
