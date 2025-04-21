//
//  UNOMediaPlayer.m
//

#import "UNOMediaPlayer.h"

static uno_mediaplayer_periodic_position_update_fn_ptr uno_mediaplayer_periodic_position_update;
inline uno_mediaplayer_periodic_position_update_fn_ptr uno_mediaplayer_get_periodic_position_update_callback(void)
{
    return uno_mediaplayer_periodic_position_update;
}

static uno_mediaplayer_rate_changed_fn_ptr uno_mediaplayer_rate_changed;
inline uno_mediaplayer_rate_changed_fn_ptr uno_mediaplayer_get_rate_changed_callback(void)
{
    return uno_mediaplayer_rate_changed;
}

static uno_mediaplayer_video_dimension_changed_fn_ptr uno_mediaplayer_video_dimension_changed;
inline uno_mediaplayer_video_dimension_changed_fn_ptr uno_mediaplayer_get_video_dimension_changed_callback(void)
{
    return uno_mediaplayer_video_dimension_changed;
}

static uno_mediaplayer_duration_changed_fn_ptr uno_mediaplayer_duration_changed;
inline uno_mediaplayer_duration_changed_fn_ptr uno_mediaplayer_get_duration_changed_callback(void)
{
    return uno_mediaplayer_duration_changed;
}

static uno_mediaplayer_ready_to_play_fn_ptr uno_mediaplayer_ready_to_play;
inline uno_mediaplayer_ready_to_play_fn_ptr uno_mediaplayer_get_ready_to_play_callback(void)
{
    return uno_mediaplayer_ready_to_play;
}

static uno_mediaplayer_buffering_progress_changed_fn_ptr uno_mediaplayer_buffering_progress_changed;
inline uno_mediaplayer_buffering_progress_changed_fn_ptr uno_mediaplayer_get_buffering_progress_changed_callback(void)
{
    return uno_mediaplayer_buffering_progress_changed;
}

static uno_mediaplayer_event_fn_ptr uno_mediaplayer_on_media_opened;
inline uno_mediaplayer_event_fn_ptr uno_mediaplayer_get_on_media_opened(void)
{
    return uno_mediaplayer_on_media_opened;
}

static uno_mediaplayer_event_fn_ptr uno_mediaplayer_on_media_ended;
inline uno_mediaplayer_event_fn_ptr uno_mediaplayer_get_on_media_ended(void)
{
    return uno_mediaplayer_on_media_ended;
}

static uno_mediaplayer_event_fn_ptr uno_mediaplayer_on_media_failed;
inline uno_mediaplayer_event_fn_ptr uno_mediaplayer_get_on_media_failed(void)
{
    return uno_mediaplayer_on_media_failed;
}

static uno_mediaplayer_event_fn_ptr uno_mediaplayer_on_media_stalled;
inline uno_mediaplayer_event_fn_ptr uno_mediaplayer_get_on_media_stalled(void)
{
    return uno_mediaplayer_on_media_stalled;
}

void uno_mediaplayer_set_callbacks(uno_mediaplayer_periodic_position_update_fn_ptr periodic_position_update, uno_mediaplayer_rate_changed_fn_ptr rate_changed, uno_mediaplayer_video_dimension_changed_fn_ptr video_dimension_changed, uno_mediaplayer_duration_changed_fn_ptr duration_changed, uno_mediaplayer_ready_to_play_fn_ptr ready_to_play, uno_mediaplayer_buffering_progress_changed_fn_ptr buffering_progress_changed, uno_mediaplayer_event_fn_ptr media_opened, uno_mediaplayer_event_fn_ptr media_ended, uno_mediaplayer_event_fn_ptr media_failed, uno_mediaplayer_event_fn_ptr media_stalled)
{
    uno_mediaplayer_periodic_position_update = periodic_position_update;
    uno_mediaplayer_rate_changed = rate_changed;
    uno_mediaplayer_video_dimension_changed = video_dimension_changed;
    uno_mediaplayer_duration_changed = duration_changed;
    uno_mediaplayer_ready_to_play = ready_to_play;
    uno_mediaplayer_buffering_progress_changed = buffering_progress_changed;
    uno_mediaplayer_on_media_opened = media_opened;
    uno_mediaplayer_on_media_ended = media_ended;
    uno_mediaplayer_on_media_failed = media_failed;
    uno_mediaplayer_on_media_stalled = media_stalled;
}


id uno_mediaplayer_create(void)
{
    UNOMediaPlayer* player = [[UNOMediaPlayer alloc] init];
#if DEBUG_MEDIAPLAYER
    NSLog(@"uno_mediaplayer_create %p", player);
#endif
    return player;
}

void uno_mediaplayer_set_notifications(UNOMediaPlayer *media)
{
    [media setNotifications];
}

bool uno_mediaplayer_is_video(UNOMediaPlayer *media)
{
#if DEBUG_MEDIAPLAYER
    NSLog(@"uno_mediaplayer_is_video %p -> %s", media, media.isVideo ? "TRUE" : "FALSE");
#endif
    return media.isVideo;
}

double uno_mediaplayer_get_current_time(UNOMediaPlayer *media)
{
    CMTime currentTime = media.player.currentItem.currentTime;
    double time = currentTime.value / currentTime.timescale;
#if DEBUG_MEDIAPLAYER
    NSLog(@"uno_mediaplayer_get_current_time %p -> %g", media, time);
#endif
    return time;
}

void uno_mediaplayer_set_current_time(UNOMediaPlayer *media, double seconds)
{
#if DEBUG_MEDIAPLAYER
    NSLog(@"uno_mediaplayer_set_current_time %p -> %g", media, seconds);
#endif
    CMTime time = CMTimeMakeWithSeconds(seconds, 100);
    [media.player.currentItem seekToTime:time completionHandler:nil];
}

float uno_mediaplayer_get_rate(UNOMediaPlayer *media)
{
#if DEBUG_MEDIAPLAYER
    NSLog(@"uno_mediaplayer_set_rate %p -> %g", media, media.player.rate);
#endif
    return media.player.rate;
}

void uno_mediaplayer_set_rate(UNOMediaPlayer *media, float rate)
{
#if DEBUG_MEDIAPLAYER
    NSLog(@"uno_mediaplayer_set_rate %p -> %g", media, rate);
#endif
    media.player.rate = rate;
    if (@available(macOS 13, *)) {
        media.player.defaultRate = rate;
    }
}

void uno_mediaplayer_set_source(UNOMediaPlayer *media, NSURL *url)
{
    AVPlayerItem* item = [AVPlayerItem playerItemWithURL:url];
#if DEBUG_MEDIAPLAYER
    NSLog(@"uno_mediaplayer_set_source %p item %p url %@", media, item, url);
#endif
    [media.player replaceCurrentItemWithPlayerItem: item];
}

void uno_mediaplayer_set_source_path(UNOMediaPlayer *media, const char *path)
{
    NSString *s = [NSString stringWithUTF8String:path];
    NSURL *url = [NSURL fileURLWithPath:s];
    if (uno_application_is_bundled()) {
        // convert [BundlePath] and [ResourcePath] to bundle specific paths
        s = [s stringByReplacingOccurrencesOfString:@"[BundlePath]" withString:NSBundle.mainBundle.bundlePath];
        s = [s stringByReplacingOccurrencesOfString:@"[ResourcePath]" withString:NSBundle.mainBundle.resourcePath];
    }
    uno_mediaplayer_set_source(media, url);
}

void uno_mediaplayer_set_source_uri(UNOMediaPlayer *media, const char *uri)
{
    NSString *s = [NSString stringWithUTF8String:uri];
    NSURL *url = [NSURL URLWithString:s];
    uno_mediaplayer_set_source(media, url);
}

void uno_mediaplayer_set_stretch(UNOMediaPlayer *media, Stretch stretch)
{
    AVLayerVideoGravity gravity;
    switch(stretch)
    {
        case StretchUniform:
            gravity = AVLayerVideoGravityResizeAspect;
            break;
        case StretchFill:
            gravity = AVLayerVideoGravityResize;
            break;
        case StretchNone:
        case StretchUniformToFill:
            gravity = AVLayerVideoGravityResizeAspectFill;
            break;
        default:
#if DEBUG_MEDIAPLAYER
            NSLog(@"uno_mediaplayer_apply_stretch %p unknown value %d", media, stretch);
#endif
            return;
    }

#if DEBUG_MEDIAPLAYER
    NSLog(@"uno_mediaplayer_apply_stretch %p stretch %d -> gravity %@", media, stretch, gravity);
#endif
    media.videoLayer.videoGravity = gravity;
}

void uno_mediaplayer_set_volume(UNOMediaPlayer *media, float volume)
{
#if DEBUG_MEDIAPLAYER
    NSLog(@"uno_mediaplayer_set_volume %p %f", media, volume);
#endif
    media.player.volume = volume;
}

void uno_mediaplayer_pause(UNOMediaPlayer *media)
{
#if DEBUG_MEDIAPLAYER
    NSLog(@"uno_mediaplayer_pause %p", media);
#endif
    [media.player pause];
}

void uno_mediaplayer_play(UNOMediaPlayer *media)
{
#if DEBUG_MEDIAPLAYER
    NSLog(@"uno_mediaplayer_play %p", media);
#endif
    AVPlayer *player = media.player;
    AVPlayerItem *item = player.currentItem;

    void* ctx = (__bridge void * _Nullable)(player);
    @try {
        [item removeObserver:media forKeyPath:@"duration" context:ctx];
        [item removeObserver:media forKeyPath:@"status" context:ctx];
        [item removeObserver:media forKeyPath:@"loadedTimeRanges" context:ctx];
    }
    @catch(...) {
        // NSRangeException is thrown if not yet observed
    }

    item.seekingWaitsForVideoCompositionRendering = true;
    // Adapt pitch to prevent "metallic echo" when changing playback rate
    item.audioTimePitchAlgorithm = AVAudioTimePitchAlgorithmTimeDomain;

    [item addObserver:media forKeyPath:@"duration" options:NSKeyValueObservingOptionInitial context:ctx];
    [item addObserver:media forKeyPath:@"status" options:NSKeyValueObservingOptionInitial | NSKeyValueObservingOptionNew context:ctx];
    [item addObserver:media forKeyPath:@"loadedTimeRanges" options:NSKeyValueObservingOptionInitial | NSKeyValueObservingOptionNew context:ctx];

    // Before macOS 13, iOS 16, tvOS 16, and watchOS 9, you can only call this method on the main thread or queue.
    // ref: https://developer.apple.com/documentation/avfoundation/avplayer/play()?language=objc
    if (@available(macOS 13, *)) {
        [player play];
    } else {
        dispatch_async(dispatch_get_main_queue(), ^{
            [player play];
        });
    }
}

void uno_mediaplayer_stop(UNOMediaPlayer *media)
{
#if DEBUG_MEDIAPLAYER
    NSLog(@"uno_mediaplayer_stop %p", media);
#endif
    AVPlayer *player = media.player;
    [player pause];
    [player.currentItem seekToTime:kCMTimeZero completionHandler:nil];
}

void uno_mediaplayer_toggle_mute(UNOMediaPlayer *media)
{
#if DEBUG_MEDIAPLAYER
    NSLog(@"uno_mediaplayer_toggle_muted %p", media);
#endif
    media.player.muted = !media.player.muted;
}

void uno_mediaplayer_step_by(UNOMediaPlayer *media, int32_t frames)
{
    AVPlayerItem *item = media.player.currentItem;
#if DEBUG_MEDIAPLAYER
    NSLog(@"uno_mediaplayer_step_by %p frames %d canStepForward %s canStepBackward %s", media, frames, item.canStepForward ? "true" : "false", item.canStepBackward ? "true" : "false");
#endif
    if ((frames > 0 && item.canStepForward) || (frames < 0 && item.canStepBackward)) {
        [item stepByCount:frames];
    }
}

UNOMediaPlayerView* uno_mediaplayer_create_view(void)
{
    UNOMediaPlayerView* view = [[UNOMediaPlayerView alloc] initWithFrame:CGRectMake(0,0,0,0)];
#if DEBUG_MEDIAPLAYER
    NSLog(@"uno_mediaplayer_create_view #%p %@", view, view.layer);
#endif
    return view;
}

void uno_mediaplayer_set_view(UNOMediaPlayer *media, UNOMediaPlayerView *view, NSWindow *window)
{
#if DEBUG_MEDIAPLAYER
    NSLog(@"uno_mediaplayer_set_view #%p %@ videoPlayer %@", media, view, media.videoLayer);
#endif
    media.videoLayer.frame = view.frame;
    media.videoLayer.videoGravity = kCAGravityResizeAspect;
    [view.layer addSublayer:media.videoLayer];
    [window.contentViewController.view addSubview:view positioned:NSWindowAbove relativeTo:nil];
}

static NSMutableSet<UNOMediaPlayer*> *players;

@implementation UNOMediaPlayer : NSObject

id timeObserver;

+ (void)initialize {
    players = [[NSMutableSet alloc] initWithCapacity:10];
}

- (id)init {
    self.player = [[AVQueuePlayer alloc] init];
    self.player.actionAtItemEnd = AVPlayerActionAtItemEndNone;
    self.videoLayer = [AVPlayerLayer playerLayerWithPlayer:self.player];
    self.isVideo = NO;
#if DEBUG_MEDIAPLAYER
    NSLog(@"UNOMediaPlayer %p %@ %@", self, self.player, self.videoLayer);
#endif

    [players addObject:self];
    return self;
}

// doing this inside `init` means some notifications can get to (managed code) before
// we're ready to dispatch them (missing events and confusing warnings in the logs)
- (void)setNotifications {
    [self.videoLayer addObserver:self forKeyPath:@"videoRect" options:NSKeyValueObservingOptionNew | NSKeyValueObservingOptionInitial context:(__bridge void * _Nullable)(self.videoLayer)];
    [self.player addObserver:self forKeyPath:@"rate" options:NSKeyValueObservingOptionNew | NSKeyValueObservingOptionInitial context:(__bridge void * _Nullable)(self.player)];

    [[NSNotificationCenter defaultCenter] addObserver:self selector:@selector(playBackDidFailed:) name:AVPlayerItemFailedToPlayToEndTimeNotification object:self.player.currentItem];
    [[NSNotificationCenter defaultCenter] addObserver:self selector:@selector(playBackDidStall:) name:AVPlayerItemPlaybackStalledNotification object:self.player.currentItem];
    [[NSNotificationCenter defaultCenter] addObserver:self selector:@selector(playBackDidFinish:) name:AVPlayerItemDidPlayToEndTimeNotification object:self.player.currentItem];

    __weak typeof(self) wself = self;
    timeObserver = [self.player addPeriodicTimeObserverForInterval:CMTimeMake(1, 4) queue:dispatch_get_main_queue() usingBlock:^(CMTime time) {
        __strong typeof(wself) self = wself;
        if (self && self.player.currentItem) {
            // call back into managed to update position
            double position = self.player.currentTime.value / self.player.currentTime.timescale;
            uno_mediaplayer_get_periodic_position_update_callback()(self, position);
#if DEBUG_MEDIAPLAYER
            NSLog(@"addPeriodicTimeObserverForInterval %p position %g seconds", self, position);
#endif
        } else {
#if DEBUG_MEDIAPLAYER
            NSLog(@"addPeriodicTimeObserverForInterval %p position unknown", self);
#endif
        }
    }];
}

- (void)dealloc {
#if DEBUG_MEDIAPLAYER
    NSLog(@"UNOMediaPlayer %p dealloc", self);
#endif
    [self.player removeTimeObserver:timeObserver];
    [[NSNotificationCenter defaultCenter] removeObserver:self name:AVPlayerItemFailedToPlayToEndTimeNotification object:self.player];
    [[NSNotificationCenter defaultCenter] removeObserver:self name:AVPlayerItemPlaybackStalledNotification object:self.player];
    [[NSNotificationCenter defaultCenter] removeObserver:self name:AVPlayerItemDidPlayToEndTimeNotification object:self.player];
    [players removeObject:self];
}

- (void)observeValueForKeyPath:(NSString *)keyPath ofObject:(id)object change:(NSDictionary<NSString *,id> *)change context:(void *)context
{
    if ([keyPath isEqualToString:@"duration"]) {
        CMTime duration = self.player.currentItem.duration;
        if (!CMTIME_IS_INDEFINITE(duration)) {
            double seconds = duration.value/ duration.timescale;
#if DEBUG_MEDIAPLAYER
            NSLog(@"UNOMediaPlayer.observeValueForKeyPath keyPath:%@ duration %g", keyPath, seconds);
#endif
            // callback managed with duration (in seconds)
            uno_mediaplayer_get_duration_changed_callback()(self, seconds);
        }
    } else if ([keyPath isEqualToString:@"status"]) {
        AVPlayer* player = self.player;
        AVPlayerItem* item = player.currentItem;
        if (item) {
            // the managed `IsVideo` property is not called often so we want to avoid more (both in number and cost) N2M calls
            // as such we compute the value here and the property will do M2N calls if/when needed
            self.isVideo = NO;
            for (AVPlayerItemTrack* track in item.tracks) {
                for (id fd in track.assetTrack.formatDescriptions) {
                    CMFormatDescriptionRef desc = (__bridge CMFormatDescriptionRef)fd;
                    if (CMFormatDescriptionGetMediaType(desc) == kCMMediaType_Video) {
                        self.isVideo = YES;
                        break;
                    }
                }
            }
#if DEBUG_MEDIAPLAYER
            NSLog(@"UNOMediaPlayer.observeValueForKeyPath keyPath:%@ -> isVideo %s", keyPath, self.isVideo ? "TRUE" : "FALSE");
#endif

            AVPlayerStatus status = player.status;
            if (item.status == AVPlayerItemStatusFailed || status == AVPlayerStatusFailed) {
#if DEBUG_MEDIAPLAYER
                NSLog(@"UNOMediaPlayer.observeValueForKeyPath keyPath:%@ -> OnMediaFailed", keyPath);
#endif
                uno_mediaplayer_get_on_media_failed()(self);
                return;
            }

            if (status == AVPlayerStatusReadyToPlay) {
#if DEBUG_MEDIAPLAYER
                NSLog(@"UNOMediaPlayer.observeValueForKeyPath keyPath:%@ -> rate %g", keyPath, player.rate);
#endif
                // callback managed with `rate` since other conditions are based on managed data
                uno_mediaplayer_get_ready_to_play_callback()(self, self.player.rate);
             }

            // the player's status might have changed in the previous callback (e.g. if play was called)
            if (player.status == AVPlayerStatusReadyToPlay) {
#if DEBUG_MEDIAPLAYER
                NSLog(@"UNOMediaPlayer.observeValueForKeyPath keyPath:%@ -> MediaOpened", keyPath);
#endif
                uno_mediaplayer_get_on_media_opened()(self);
            }
        } else {
            self.isVideo = NO;
#if DEBUG_MEDIAPLAYER
            NSLog(@"UNOMediaPlayer.observeValueForKeyPath keyPath:%@ -> No AVPlayerItem available", keyPath);
#endif
        }
    } else if ([keyPath isEqualToString:@"loadedTimeRanges"]) {
        double progress = 0;
        NSArray<NSValue*>* list = self.player.currentItem.loadedTimeRanges;
        if (list && list.count > 0) {
            for (NSValue* value in list) {
                double d = value.CMTimeRangeValue.start.value + value.CMTimeRangeValue.duration.value;
                if (d > progress) {
                    progress = d;
                }
            }
        }
#if DEBUG_MEDIAPLAYER
        NSLog(@"UNOMediaPlayer.observeValueForKeyPath keyPath:%@ progress %g", keyPath, progress);
#endif
        // call managed with buffering progress
        uno_mediaplayer_get_buffering_progress_changed_callback()(self, progress);
    } else if ([keyPath isEqualToString:@"rate"]) {
#if DEBUG_MEDIAPLAYER
        NSLog(@"UNOMediaPlayer.observeValueForKeyPath keyPath:%@ rate %g", keyPath, self.player.rate);
#endif
        // call managed callback with `self.player.rate` since logic needs managed state
        uno_mediaplayer_get_rate_changed_callback()(self, self.player.rate);
    } else if ([keyPath isEqualToString:@"videoRect"]) {
        AVPlayerLayer *layer = self.videoLayer;
        uint32_t width = 0;
        uint32_t height = 0;
        if (layer) {
            CGRect r = layer.videoRect;
            width = r.size.width;
            height = r.size.height;
        }
#if DEBUG_MEDIAPLAYER
        NSLog(@"UNOMediaPlayer.observeValueForKeyPath keyPath:%@ layer %@ width %d height %d", keyPath, layer, width, height);
#endif
        // callback managed NaturalVideoDimensionChanged
        uno_mediaplayer_get_video_dimension_changed_callback()(self, width, height);
    } else {
#if DEBUG_MEDIAPLAYER
        NSLog(@"UNOMediaPlayer.observeValueForKeyPath keyPath:%@ (unprocessed)", keyPath);
#endif
    }
}

- (void) playBackDidFinish:(NSNotification*)notification {
#if DEBUG_MEDIAPLAYER
    NSLog(@"playBackDidFinish");
#endif
    uno_mediaplayer_get_on_media_ended()(self);
}

- (void) playBackDidFailed:(NSNotification*)notification {
#if DEBUG_MEDIAPLAYER
    NSLog(@"playBackDidFailed");
#endif
    uno_mediaplayer_get_on_media_failed()(self);
}

- (void) playBackDidStall:(NSNotification*)notification {
#if DEBUG_MEDIAPLAYER
    NSLog(@"playBackDidStall");
#endif
    uno_mediaplayer_get_on_media_stalled()(self);
}

@end

@implementation UNOMediaPlayerView : NSView

- (nullable instancetype)initWithCoder:(NSCoder *)coder {
    return [super initWithCoder:coder];
}

- (instancetype)initWithFrame:(CGRect)frame {
    self = [super initWithFrame:frame];
    if (self) {
        CALayer* layer = [[CALayer alloc] init];
        self.layer = layer;
    }
    return self;
}

-(BOOL) isFlipped {
    return YES;
}

- (BOOL)wantsUpdateLayer
{
    return true;
}

@synthesize visible;

// UNONativeElement

- (void)detach {
#if DEBUG
    NSLog(@"detach mediaplayer %p", self);
#endif
//    [self removeFromSuperview];
}

- (void)layout {
    [super layout];

    CALayer *layer = self.layer;
    if (layer.sublayers && layer.sublayers.count > 0) {
        Class k = AVPlayerLayer.class;
        for (CALayer* sub in layer.sublayers) {
            if ([sub isKindOfClass:k]) {
                sub.frame = self.bounds;
            }
        }
    }
}

@end
