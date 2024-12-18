//
//  UNOMediaPlayer.m
//

#import "UNOMediaPlayer.h"

id uno_mediaplayer_create(void)
{
    UNOMediaPlayer* player = [[UNOMediaPlayer alloc] init];
#if DEBUG
    NSLog(@"uno_mediaplayer_create %p", player);
#endif
    return player;
}

bool uno_mediaplayer_is_video(UNOMediaPlayer *media)
{
#if DEBUG
    NSLog(@"uno_mediaplayer_is_video %p -> %s", media, media.isVideo ? "TRUE" : "FALSE");
#endif
    return media.isVideo;
}

void uno_mediaplayer_set_source(UNOMediaPlayer *media, const char *uri)
{
    NSString *s = [NSString stringWithUTF8String:uri];
    NSURL *url = [NSURL URLWithString:s];
    AVPlayerItem* item = [AVPlayerItem playerItemWithURL:url];
#if DEBUG
    NSLog(@"uno_mediapalyer_set_source %p item %p url %@", media, item, url);
#endif
    [media.player replaceCurrentItemWithPlayerItem: item];
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
#if DEBUG
            NSLog(@"uno_mediaplayer_apply_stretch %p unknown value %d", media, stretch);
#endif
            return;
    }

#if DEBUG
    NSLog(@"uno_mediaplayer_apply_stretch %p stretch %d -> gravity %@", media, stretch, gravity);
#endif
    media.videoLayer.videoGravity = gravity;
}

void uno_mediaplayer_set_volume(UNOMediaPlayer *media, float volume)
{
#if DEBUG
    NSLog(@"uno_mediaplayer_set_volume %p %f", media, volume);
#endif
    media.player.volume = volume;
}

void uno_mediaplayer_pause(UNOMediaPlayer *media)
{
#if DEBUG
    NSLog(@"uno_mediaplayer_pause %p", media);
#endif
    [media.player pause];
}

void uno_mediaplayer_play(UNOMediaPlayer *media)
{
#if DEBUG
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

    [player play];
}

void uno_mediaplayer_stop(UNOMediaPlayer *media)
{
#if DEBUG
    NSLog(@"uno_mediaplayer_stop %p", media);
#endif
    AVPlayer *player = media.player;
    [player pause];
    [player.currentItem seekToTime:kCMTimeZero completionHandler:nil];
}

void uno_mediaplayer_toggle_mute(UNOMediaPlayer *media)
{
#if DEBUG
    NSLog(@"uno_mediaplayer_toggle_muted %p", media);
#endif
    media.player.muted = !media.player.muted;
}

void uno_mediaplayer_step_by(UNOMediaPlayer *media, int32_t frames)
{
    AVPlayerItem *item = media.player.currentItem;
#if DEBUG
    NSLog(@"uno_mediaplayer_step_by %p frames %d canStepForward %s canStepBackward %s", media, frames, item.canStepForward ? "true" : "false", item.canStepBackward ? "true" : "false");
#endif
    if ((frames > 0 && item.canStepForward) || (frames < 0 && item.canStepBackward)) {
        [item stepByCount:frames];
    }
}

static NSMutableSet<UNOMediaPlayer*> *players;

@implementation UNOMediaPlayer : NSObject

+ (void)initialize {
    players = [[NSMutableSet alloc] initWithCapacity:10];
}

- (id)init {
    self.player = [[AVQueuePlayer alloc] init];
    self.videoLayer = [AVPlayerLayer playerLayerWithPlayer:self.player];
    self.isVideo = NO;
#if DEBUG
    NSLog(@"UNOMediaPlayer %p %@ %@", self, self.player, self.videoLayer);
#endif

    [self.videoLayer addObserver:self forKeyPath:@"videoRect" options:NSKeyValueObservingOptionNew | NSKeyValueObservingOptionInitial context:(__bridge void * _Nullable)(self.videoLayer)];
    [self.player addObserver:self forKeyPath:@"rate" options:NSKeyValueObservingOptionNew | NSKeyValueObservingOptionInitial context:(__bridge void * _Nullable)(self.player)];

    // TODO AVPlayerItemFailedToPlayToEndTimeNotification
    // TODO AVPlayerItemPlaybackStalledNotification
    // TODO AVPlayerItemDidPlayToEndTimeNotification

    __weak typeof(self) wself = self;
    [self.player addPeriodicTimeObserverForInterval:CMTimeMake(1, 4) queue:dispatch_get_main_queue() usingBlock:^(CMTime time) {
        __strong typeof(wself) self = wself;
        if (self && self.player.currentItem) {
            // TODO call back into managed to update position
            double position = self.player.currentTime.value / self.player.currentTime.timescale;
#if DEBUG
            NSLog(@"addPeriodicTimeObserverForInterval %p position %g seconds", self, position);
#endif
        } else {
#if DEBUG
            NSLog(@"addPeriodicTimeObserverForInterval %p position unknown", self);
#endif
        }
    }];

    [players addObject:self];
    return self;
}

- (void)dealloc {
#if DEBUG
    NSLog(@"UNOMediaPlayer %p dealloc", self);
#endif
    [players removeObject:self];
}

- (void)observeValueForKeyPath:(NSString *)keyPath ofObject:(id)object change:(NSDictionary<NSString *,id> *)change context:(void *)context
{
    if ([keyPath isEqualToString:@"duration"]) {
        CMTime duration = self.player.currentItem.duration;
        if (!CMTIME_IS_INDEFINITE(duration)) {
            // TODO callback managed with duration
#if DEBUG
            NSLog(@"UNOMediaPlayer.observeValueForKeyPath keyPath:%@ duration %lld", keyPath, duration.value);
#endif
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
#if DEBUG
            NSLog(@"UNOMediaPlayer.observeValueForKeyPath keyPath:%@ -> isVideo %s", keyPath, self.isVideo ? "TRUE" : "FALSE");
#endif

            AVPlayerStatus status = player.status;
            if (item.status == AVPlayerItemStatusFailed || status == AVPlayerStatusFailed) {
#if DEBUG
                NSLog(@"UNOMediaPlayer.observeValueForKeyPath keyPath:%@ -> OnMediaFailed", keyPath);
#endif
                // TODO callback managed OnMediaFailed
                return;
            }

            if (status == AVPlayerStatusReadyToPlay) {
#if DEBUG
                NSLog(@"UNOMediaPlayer.observeValueForKeyPath keyPath:%@ -> rate %g", keyPath, player.rate);
#endif
                // TODO callback managed with `rate` since other conditions are based on managed data
             }

            // the player's status might have changed in the previous callback (e.g. if play was called)
            if (player.status == AVPlayerStatusReadyToPlay) {
#if DEBUG
                NSLog(@"UNOMediaPlayer.observeValueForKeyPath keyPath:%@ -> MediaOpened", keyPath);
#endif
                // TODO callback managed MediaOpened
            }
        } else {
            self.isVideo = NO;
#if DEBUG
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
#if DEBUG
        NSLog(@"UNOMediaPlayer.observeValueForKeyPath keyPath:%@ progress %g", keyPath, progress);
#endif
        // TODO call managed with progress (0)
    } else if ([keyPath isEqualToString:@"rate"]) {
#if DEBUG
        NSLog(@"UNOMediaPlayer.observeValueForKeyPath keyPath:%@ rate %g", keyPath, self.player.rate);
#endif
        // TODO call managed callback with `self.player.rate` since logic needs managed state
    } else if ([keyPath isEqualToString:@"videoRect"]) {
        AVPlayerLayer *layer = self.videoLayer;
        uint32_t width = 0;
        uint32_t height = 0;
        if (layer) {
            CGRect r = layer.videoRect;
            width = r.size.width;
            height = r.size.height;
        }
#if DEBUG
        NSLog(@"UNOMediaPlayer.observeValueForKeyPath keyPath:%@ layer %@ width %d height %d", keyPath, layer, width, height);
#endif
        // TODO callback managed NaturalVideoDimensionChanged
    } else {
#if DEBUG
        NSLog(@"UNOMediaPlayer.observeValueForKeyPath keyPath:%@ (unprocessed)", keyPath);
#endif
    }
}

@end
