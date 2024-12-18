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

@property (nonatomic) BOOL isVideo;

@end

// https://learn.microsoft.com/en-us/uwp/api/windows.ui.xaml.media.stretch?view=winrt-26100
typedef NS_ENUM(sint32, Stretch) {
    StretchNone = 0,
    StretchFill = 1,
    StretchUniform = 2,
    StretchUniformToFill = 3,
};

id uno_mediaplayer_create(void);

bool uno_mediaplayer_is_video(UNOMediaPlayer *media);

void uno_mediaplayer_set_source(UNOMediaPlayer *media, const char *uri);
void uno_mediaplayer_set_stretch(UNOMediaPlayer *media, Stretch stretch);
void uno_mediaplayer_set_volume(UNOMediaPlayer *media, float volume);

void uno_mediaplayer_pause(UNOMediaPlayer *media);
void uno_mediaplayer_play(UNOMediaPlayer *media);
void uno_mediaplayer_stop(UNOMediaPlayer *media);
void uno_mediaplayer_toggle_muted(UNOMediaPlayer *media);
void uno_mediaplayer_step_by(UNOMediaPlayer *media, int32_t frames);

NS_ASSUME_NONNULL_END
