//
//  UNOMediaPlayer.h
//

#pragma once

#import "UnoNativeMac.h"
#import "UNONative.h"
#import "UNOApplication.h"

#if DEBUG
#define DEBUG_MEDIAPLAYER   1
#endif

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

typedef void (*uno_mediaplayer_periodic_position_update_fn_ptr)(UNOMediaPlayer* /* handle */, double position);
uno_mediaplayer_periodic_position_update_fn_ptr uno_mediaplayer_get_periodic_position_update_callback(void);

typedef void (*uno_mediaplayer_rate_changed_fn_ptr)(UNOMediaPlayer* /* handle */, double rate);
uno_mediaplayer_rate_changed_fn_ptr uno_mediaplayer_get_rate_changed_callback(void);

typedef void (*uno_mediaplayer_video_dimension_changed_fn_ptr)(UNOMediaPlayer* /* handle */, double width, double height);
uno_mediaplayer_video_dimension_changed_fn_ptr uno_mediaplayer_get_video_dimension_changed_callback(void);

typedef void (*uno_mediaplayer_duration_changed_fn_ptr)(UNOMediaPlayer* /* handle */, double duration);
uno_mediaplayer_duration_changed_fn_ptr uno_mediaplayer_get_duration_changed_callback(void);

typedef void (*uno_mediaplayer_ready_to_play_fn_ptr)(UNOMediaPlayer* /* handle */, double rate);
uno_mediaplayer_ready_to_play_fn_ptr uno_mediaplayer_get_ready_to_play_callback(void);

typedef void (*uno_mediaplayer_buffering_progress_changed_fn_ptr)(UNOMediaPlayer* /* handle */, double progress);
uno_mediaplayer_buffering_progress_changed_fn_ptr uno_mediaplayer_get_buffering_progress_changed_callback(void);

typedef void (*uno_mediaplayer_event_fn_ptr)(UNOMediaPlayer* /* handle */);
uno_mediaplayer_event_fn_ptr uno_mediaplayer_get_on_media_opened(void);
uno_mediaplayer_event_fn_ptr uno_mediaplayer_get_on_media_ended(void);
uno_mediaplayer_event_fn_ptr uno_mediaplayer_get_on_media_failed(void);
uno_mediaplayer_event_fn_ptr uno_mediaplayer_get_on_media_stalled(void);

void uno_mediaplayer_set_callbacks(uno_mediaplayer_periodic_position_update_fn_ptr periodic_position_update, uno_mediaplayer_rate_changed_fn_ptr rate_changed, uno_mediaplayer_video_dimension_changed_fn_ptr video_dimension_changed, uno_mediaplayer_duration_changed_fn_ptr duration_changed, uno_mediaplayer_ready_to_play_fn_ptr ready_to_play, uno_mediaplayer_buffering_progress_changed_fn_ptr buffering_progress_changed, uno_mediaplayer_event_fn_ptr media_opened, uno_mediaplayer_event_fn_ptr media_ended, uno_mediaplayer_event_fn_ptr media_failed, uno_mediaplayer_event_fn_ptr media_stalled);

id uno_mediaplayer_create(void);

bool uno_mediaplayer_is_video(UNOMediaPlayer *media);

double uno_mediaplayer_get_current_time(UNOMediaPlayer *media);
void uno_mediaplayer_set_current_time(UNOMediaPlayer *media, double seconds);
float uno_mediaplayer_get_rate(UNOMediaPlayer *media);
void uno_mediaplayer_set_rate(UNOMediaPlayer *media, float rate);
void uno_mediaplayer_set_source_path(UNOMediaPlayer *media, const char *path);
void uno_mediaplayer_set_source_uri(UNOMediaPlayer *media, const char *uri);
void uno_mediaplayer_set_stretch(UNOMediaPlayer *media, Stretch stretch);
void uno_mediaplayer_set_volume(UNOMediaPlayer *media, float volume);

void uno_mediaplayer_pause(UNOMediaPlayer *media);
void uno_mediaplayer_play(UNOMediaPlayer *media);
void uno_mediaplayer_stop(UNOMediaPlayer *media);
void uno_mediaplayer_toggle_muted(UNOMediaPlayer *media);
void uno_mediaplayer_step_by(UNOMediaPlayer *media, int32_t frames);

@interface UNOMediaPlayerView : NSView<UNONativeElement>

- (nullable instancetype)initWithCoder:(NSCoder *)coder;
- (instancetype)initWithFrame:(CGRect)frame;
- (BOOL)wantsUpdateLayer;
- (void)layout;

@end

UNOMediaPlayerView* uno_mediaplayer_create_view(void);
void uno_mediaplayer_set_view(UNOMediaPlayer *media, UNOMediaPlayerView *view, NSWindow *window);

NS_ASSUME_NONNULL_END
