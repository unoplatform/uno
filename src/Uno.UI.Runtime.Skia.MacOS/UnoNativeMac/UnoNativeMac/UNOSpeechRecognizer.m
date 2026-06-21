//
//  UNOSpeechRecognizer.m
//

#import "UNOSpeechRecognizer.h"

#import <Speech/Speech.h>
#import <AVFoundation/AVFoundation.h>

static uno_speech_hypothesis_fn_ptr s_hypothesis_cb;
static uno_speech_state_fn_ptr s_state_cb;
static uno_speech_result_fn_ptr s_result_cb;
static uno_speech_error_fn_ptr s_error_cb;

uno_speech_hypothesis_fn_ptr uno_speech_get_hypothesis_callback(void) { return s_hypothesis_cb; }
uno_speech_state_fn_ptr uno_speech_get_state_callback(void) { return s_state_cb; }
uno_speech_result_fn_ptr uno_speech_get_result_callback(void) { return s_result_cb; }
uno_speech_error_fn_ptr uno_speech_get_error_callback(void) { return s_error_cb; }

void uno_speech_set_callbacks(uno_speech_hypothesis_fn_ptr hypothesis,
                              uno_speech_state_fn_ptr state,
                              uno_speech_result_fn_ptr result,
                              uno_speech_error_fn_ptr error)
{
    s_hypothesis_cb = hypothesis;
    s_state_cb = state;
    s_result_cb = result;
    s_error_cb = error;
}

// Holds live UNOSpeechRecognizer instances so they survive ARC autorelease after returning to the managed caller.
// Keyed by the pointer value of the recognizer.
static NSMutableDictionary<NSValue *, UNOSpeechRecognizer *> *s_liveRecognizers;
static dispatch_once_t s_liveRecognizersOnce;

API_AVAILABLE(macos(10.15))
@interface UNOSpeechRecognizer ()
@property (nonatomic, strong) SFSpeechRecognizer *speechRecognizer;
@property (nonatomic, strong, nullable) SFSpeechAudioBufferRecognitionRequest *recognitionRequest;
@property (nonatomic, strong, nullable) SFSpeechRecognitionTask *recognitionTask;
@property (nonatomic, strong) AVAudioEngine *audioEngine;
@end

@implementation UNOSpeechRecognizer

- (nullable instancetype)initWithLocale:(const char *)locale
{
    self = [super init];
    if (!self) {
        return nil;
    }

    if (@available(macOS 10.15, *)) {
        NSString *localeId = (locale != NULL) ? [NSString stringWithUTF8String:locale] : nil;
        NSLocale *nsLocale = (localeId.length > 0) ? [NSLocale localeWithLocaleIdentifier:localeId] : [NSLocale currentLocale];
        self.speechRecognizer = [[SFSpeechRecognizer alloc] initWithLocale:nsLocale];
        if (self.speechRecognizer == nil) {
            return nil;
        }
        self.audioEngine = [[AVAudioEngine alloc] init];
    } else {
        return nil;
    }

    return self;
}

- (BOOL)isAvailable
{
    if (@available(macOS 10.15, *)) {
        return self.speechRecognizer.isAvailable;
    }
    return NO;
}

- (BOOL)startRecognition
{
    if (@available(macOS 10.15, *)) {
        // Cancel any previous task.
        if (self.recognitionTask) {
            [self.recognitionTask cancel];
            self.recognitionTask = nil;
        }

        NSError *audioError = nil;
        AVAudioInputNode *inputNode = self.audioEngine.inputNode;
        if (inputNode == nil) {
            [self raiseError:@"Audio engine has no input node"];
            return NO;
        }

        self.recognitionRequest = [[SFSpeechAudioBufferRecognitionRequest alloc] init];
        self.recognitionRequest.shouldReportPartialResults = YES;
        self.recognitionRequest.taskHint = SFSpeechRecognitionTaskHintDictation;

        __weak UNOSpeechRecognizer *weakSelf = self;
        self.recognitionTask = [self.speechRecognizer recognitionTaskWithRequest:self.recognitionRequest
                                                                   resultHandler:^(SFSpeechRecognitionResult * _Nullable result, NSError * _Nullable error) {
            UNOSpeechRecognizer *strongSelf = weakSelf;
            if (strongSelf == nil) {
                return;
            }

            BOOL isFinal = NO;
            NSString *bestText = nil;
            NSMutableArray<NSString *> *alternates = nil;

            if (result != nil) {
                bestText = result.bestTranscription.formattedString ?: @"";
                isFinal = result.isFinal;

                if (result.transcriptions.count > 0) {
                    alternates = [NSMutableArray arrayWithCapacity:result.transcriptions.count];
                    for (SFTranscription *t in result.transcriptions) {
                        [alternates addObject:(t.formattedString ?: @"")];
                    }
                }

                // Fire HypothesisGenerated for partial / non-final results.
                if (s_hypothesis_cb != NULL && bestText != nil) {
                    s_hypothesis_cb(strongSelf, [bestText UTF8String]);
                }
            }

            if (error != nil || isFinal) {
                [strongSelf teardownAudio];

                if (s_state_cb != NULL) {
                    s_state_cb(strongSelf, (int32_t)UNOSpeechRecognizerStateIdle);
                }

                if (bestText != nil && error == nil) {
                    NSString *altsJoined = nil;
                    if (alternates.count > 0) {
                        altsJoined = [alternates componentsJoinedByString:@"\x1F"];
                    }
                    if (s_result_cb != NULL) {
                        s_result_cb(strongSelf, [bestText UTF8String], altsJoined != nil ? [altsJoined UTF8String] : NULL);
                    }
                } else {
                    NSString *desc = error.localizedDescription ?: @"Unknown speech recognition error";
                    if (s_error_cb != NULL) {
                        s_error_cb(strongSelf, [desc UTF8String]);
                    }
                }
            }
        }];

        AVAudioFormat *recordingFormat = [inputNode outputFormatForBus:0];
        [inputNode installTapOnBus:0
                        bufferSize:1024
                            format:recordingFormat
                             block:^(AVAudioPCMBuffer * _Nonnull buffer, AVAudioTime * _Nonnull when) {
            UNOSpeechRecognizer *strongSelf = weakSelf;
            [strongSelf.recognitionRequest appendAudioPCMBuffer:buffer];
        }];

        [self.audioEngine prepare];
        BOOL started = [self.audioEngine startAndReturnError:&audioError];
        if (!started) {
            NSString *desc = audioError.localizedDescription ?: @"Failed to start audio engine";
            [self raiseError:desc];
            return NO;
        }

        if (s_state_cb != NULL) {
            s_state_cb(self, (int32_t)UNOSpeechRecognizerStateCapturing);
        }
        return YES;
    } else {
        [self raiseError:@"Speech recognition requires macOS 10.15 or later"];
        return NO;
    }
}

- (void)stopRecognition
{
    if (@available(macOS 10.15, *)) {
        [self.recognitionRequest endAudio];
    }
}

- (void)teardownAudio
{
    if (self.audioEngine.isRunning) {
        [self.audioEngine stop];
    }
    AVAudioInputNode *inputNode = self.audioEngine.inputNode;
    @try {
        [inputNode removeTapOnBus:0];
    } @catch (__unused NSException *ex) {
        // ignore — happens if tap was never installed
    }
    self.recognitionRequest = nil;
    self.recognitionTask = nil;
}

- (void)raiseError:(NSString *)message
{
    if (s_error_cb != NULL) {
        s_error_cb(self, [message UTF8String]);
    }
}

@end

UNOSpeechRecognizer * _Nullable uno_speech_recognizer_create(const char *locale)
{
    if (@available(macOS 10.15, *)) {
        dispatch_once(&s_liveRecognizersOnce, ^{
            s_liveRecognizers = [[NSMutableDictionary alloc] init];
        });

        UNOSpeechRecognizer *recognizer = [[UNOSpeechRecognizer alloc] initWithLocale:locale];
        if (recognizer == nil) {
            return NULL;
        }

        // Retain the recognizer so it survives the ARC autorelease pool after we return to managed code.
        @synchronized (s_liveRecognizers) {
            s_liveRecognizers[[NSValue valueWithPointer:(__bridge const void *)recognizer]] = recognizer;
        }

        // Request authorization eagerly. Recognition will simply not produce results until granted.
        [SFSpeechRecognizer requestAuthorization:^(SFSpeechRecognizerAuthorizationStatus status) {
            // Status changes are surfaced via the result/error callbacks once recognition is attempted.
        }];

        return recognizer;
    }
    return NULL;
}

void uno_speech_recognizer_destroy(UNOSpeechRecognizer *recognizer)
{
    if (recognizer == nil) {
        return;
    }
    if (@available(macOS 10.15, *)) {
        [recognizer stopRecognition];
        [recognizer teardownAudio];
    }
    @synchronized (s_liveRecognizers) {
        [s_liveRecognizers removeObjectForKey:[NSValue valueWithPointer:(__bridge const void *)recognizer]];
    }
}

bool uno_speech_recognizer_start(UNOSpeechRecognizer *recognizer)
{
    if (recognizer == nil) {
        return false;
    }
    if (@available(macOS 10.15, *)) {
        return [recognizer startRecognition] ? true : false;
    }
    return false;
}

void uno_speech_recognizer_stop(UNOSpeechRecognizer *recognizer)
{
    if (recognizer == nil) {
        return;
    }
    if (@available(macOS 10.15, *)) {
        [recognizer stopRecognition];
    }
}

bool uno_speech_recognizer_is_available(UNOSpeechRecognizer *recognizer)
{
    if (recognizer == nil) {
        return false;
    }
    if (@available(macOS 10.15, *)) {
        return [recognizer isAvailable] ? true : false;
    }
    return false;
}
