//
//  UNOMetalViewDelegate.h
//

#pragma once

#import <Foundation/Foundation.h>

NS_ASSUME_NONNULL_BEGIN

@import MetalKit;

@interface UNOMetalViewDelegate : NSObject<MTKViewDelegate>

- (nonnull instancetype)initWithMetalKitView:(nonnull MTKView *)mtkView;

@property (nullable) id<MTLCommandQueue> queue;

@end

NS_ASSUME_NONNULL_END
