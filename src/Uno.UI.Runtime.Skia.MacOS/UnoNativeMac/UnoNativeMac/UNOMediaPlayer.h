//
//  UNOMediaPlayer.h
//

#pragma once

#import "UnoNativeMac.h"
#import "UNONative.h"
#import "UNOApplication.h"

NS_ASSUME_NONNULL_BEGIN

@interface UNOMediaPlayer : NSObject

@property (nonatomic, strong) AVQueuePlayer* player;
@property (nonatomic, strong) AVPlayerLayer* videoLayer;

@end

id uno_mediaplayer_create(void);

void uno_mediaplayer_pause(UNOMediaPlayer *media);
void uno_mediaplayer_play(UNOMediaPlayer *media);
void uno_mediaplayer_stop(UNOMediaPlayer *media);
    
NS_ASSUME_NONNULL_END
