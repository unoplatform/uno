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
    
    // 2) Fall back to AppKit snapshot
    NSInteger appKitMask = [NSEvent pressedMouseButtons];
    
    if (m != 0) {
        // If we think buttons are down, but AppKit says NONE are down, we likely missed a MouseUp.
        // We trust AppKit and reset our counters.
        if (appKitMask == 0) {
            for (int i = 0; i < 8; i++) {
                sDepth[i] = 0;
            }
            return 0;
        }
        return m;
    }

    if (appKitMask != 0) return appKitMask;

    // 3) Quartz fallback (polls physical state; taps may already be up)
    for (int i = 0; i < 8; i++) {
        if (CGEventSourceButtonState(kCGEventSourceStateCombinedSessionState, (CGMouseButton)i)) {
            m |= (1 << i);
        }
    }
    return m;
}

@end
