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
    [media.player play];
}

void uno_mediaplayer_stop(UNOMediaPlayer *media)
{
#if DEBUG
    NSLog(@"uno_mediaplayer_stop %p", media);
#endif
    [media.player removeAllItems];
}

static NSMutableSet<UNOMediaPlayer*> *players;

@implementation UNOMediaPlayer : NSObject

+ (void)initialize {
    players = [[NSMutableSet alloc] initWithCapacity:10];
}

- (id)init {
    self.player = [[AVQueuePlayer alloc] init];
    self.videoLayer = [AVPlayerLayer playerLayerWithPlayer:self.player];
#if DEBUG
    NSLog(@"UNOMediaPlayer %p %@ %@", self, self.player, self.videoLayer);
#endif
    [players addObject:self];
    return self;
}

- (void)dealloc {
#if DEBUG
    NSLog(@"UNOMediaPlayer %p dealloc", self);
#endif
    [players removeObject:self];
}

@end
