//
//  UNOSpeechRecognizer.m
//

#import "UNOSpeechRecognizer.h"

#import <Speech/Speech.h>
#import <AVFoundation/AVFoundation.h>
#import <os/lock.h>
#import <math.h>

NSString * const UNOSpeechErrorDomain = @"UnoSpeech";

// Audio levels are reported to managed code once per window instead of once per tap buffer.
static const double UNOSpeechLevelWindowSeconds = 0.05;
// Audio captured while no utterance is active (between utterances, or while paused) is kept up to this duration.
static const double UNOSpeechMaxPendingAudioSeconds = 30.0;

static uno_speech_hypothesis_fn_ptr s_hypothesis_cb;
static uno_speech_signal_fn_ptr s_signal_cb;
static uno_speech_audio_level_fn_ptr s_audio_level_cb;
static uno_speech_utterance_fn_ptr s_utterance_cb;
static uno_speech_authorization_fn_ptr s_authorization_cb;

void uno_speech_set_callbacks(uno_speech_hypothesis_fn_ptr hypothesis,
                              uno_speech_signal_fn_ptr signal,
                              uno_speech_audio_level_fn_ptr audioLevel,
                              uno_speech_utterance_fn_ptr utterance,
                              uno_speech_authorization_fn_ptr authorization)
{
    s_hypothesis_cb = hypothesis;
    s_signal_cb = signal;
    s_audio_level_cb = audioLevel;
    s_utterance_cb = utterance;
    s_authorization_cb = authorization;
}

// Keeps live recognizers alive while managed code holds their pointer.
static NSMutableSet<UNOSpeechRecognizer *> *s_liveRecognizers;
static dispatch_once_t s_liveRecognizersOnce;

static float UNOSpeechToDecibels(float amplitude)
{
    return amplitude > 0 ? 20.0f * log10f(amplitude) : -160.0f;
}

static AVAudioPCMBuffer * _Nullable UNOSpeechCopyBuffer(AVAudioPCMBuffer *buffer)
{
    if (buffer.floatChannelData == NULL || buffer.frameLength == 0) {
        return nil;
    }

    AVAudioPCMBuffer *copy = [[AVAudioPCMBuffer alloc] initWithPCMFormat:buffer.format frameCapacity:buffer.frameLength];
    if (copy == nil) {
        return nil;
    }

    copy.frameLength = buffer.frameLength;
    for (AVAudioChannelCount channel = 0; channel < buffer.format.channelCount; channel++) {
        memcpy(copy.floatChannelData[channel], buffer.floatChannelData[channel], buffer.frameLength * sizeof(float));
    }
    return copy;
}

@interface UNOSpeechRecognizer () <SFSpeechRecognitionTaskDelegate>
{
    os_unfair_lock _audioLock;
    // Guarded by _audioLock: shared between the main queue and the audio tap thread.
    SFSpeechAudioBufferRecognitionRequest *_activeRequest;
    double _activeRequestAudioStart;
    NSMutableArray<NSArray *> *_pendingBuffers;
    double _pendingSeconds;

    // Only touched from the audio tap thread.
    double _levelSumSquares;
    float _levelPeak;
    AVAudioFrameCount _levelFrames;
}

@property (nonatomic, strong) SFSpeechRecognizer *speechRecognizer;
@property (nonatomic, strong) AVAudioEngine *audioEngine;
@property (nonatomic, strong, nullable) SFSpeechRecognitionTask *recognitionTask;
@property (nonatomic, strong, nullable) SFSpeechRecognitionResult *finalResult;
@property (nonatomic) BOOL tapInstalled;
@property (atomic) BOOL disposed;

@end

@implementation UNOSpeechRecognizer

- (nullable instancetype)initWithLocale:(const char *)locale
{
    self = [super init];
    if (!self) {
        return nil;
    }

    NSString *localeId = (locale != NULL) ? [NSString stringWithUTF8String:locale] : nil;
    NSLocale *nsLocale = (localeId.length > 0) ? [NSLocale localeWithLocaleIdentifier:localeId] : [NSLocale currentLocale];
    self.speechRecognizer = [[SFSpeechRecognizer alloc] initWithLocale:nsLocale];
    if (self.speechRecognizer == nil) {
        return nil;
    }

    self.audioEngine = [[AVAudioEngine alloc] init];
    _audioLock = OS_UNFAIR_LOCK_INIT;
    _pendingBuffers = [NSMutableArray array];
    return self;
}

#pragma mark - Audio capture

- (BOOL)ensureAudioRunningWithErrorCode:(int64_t *)errorCode message:(NSString **)message
{
    if (self.audioEngine.isRunning) {
        return YES;
    }

    AVAudioInputNode *inputNode = nil;
    AVAudioFormat *format = nil;
    @try {
        inputNode = self.audioEngine.inputNode;
        format = [inputNode outputFormatForBus:0];
    } @catch (NSException *ex) {
        inputNode = nil;
    }

    if (inputNode == nil || format == nil || format.channelCount == 0 || format.sampleRate <= 0) {
        *errorCode = UNOSpeechErrorCodeNoInputDevice;
        *message = @"No audio input device is available.";
        return NO;
    }

    if (!self.tapInstalled) {
        __weak UNOSpeechRecognizer *weakSelf = self;
        [inputNode installTapOnBus:0 bufferSize:1024 format:format block:^(AVAudioPCMBuffer * _Nonnull buffer, AVAudioTime * _Nonnull when) {
            [weakSelf processAudioBuffer:buffer];
        }];
        self.tapInstalled = YES;
    }

    [self.audioEngine prepare];
    NSError *error = nil;
    if (![self.audioEngine startAndReturnError:&error]) {
        [self removeTap];
        *errorCode = UNOSpeechErrorCodeAudioEngineFailed;
        *message = error.localizedDescription ?: @"Failed to start the audio engine.";
        return NO;
    }

    return YES;
}

- (void)removeTap
{
    if (!self.tapInstalled) {
        return;
    }

    @try {
        [self.audioEngine.inputNode removeTapOnBus:0];
    } @catch (__unused NSException *ex) {
    }
    self.tapInstalled = NO;
}

// Runs on the audio tap thread.
- (void)processAudioBuffer:(AVAudioPCMBuffer *)buffer
{
    [self accumulateLevel:buffer];

    double sampleRate = buffer.format.sampleRate;
    double duration = sampleRate > 0 ? (double)buffer.frameLength / sampleRate : 0;
    double captureTime = [[NSDate date] timeIntervalSince1970] - duration;

    os_unfair_lock_lock(&_audioLock);
    if (_activeRequest != nil) {
        if (_activeRequestAudioStart == 0) {
            _activeRequestAudioStart = captureTime;
        }
        [_activeRequest appendAudioPCMBuffer:buffer];
    } else {
        AVAudioPCMBuffer *copy = UNOSpeechCopyBuffer(buffer);
        if (copy != nil) {
            [_pendingBuffers addObject:@[copy, @(captureTime), @(duration)]];
            _pendingSeconds += duration;
            while (_pendingSeconds > UNOSpeechMaxPendingAudioSeconds && _pendingBuffers.count > 0) {
                _pendingSeconds -= [_pendingBuffers.firstObject[2] doubleValue];
                [_pendingBuffers removeObjectAtIndex:0];
            }
        }
    }
    os_unfair_lock_unlock(&_audioLock);
}

// Runs on the audio tap thread.
- (void)accumulateLevel:(AVAudioPCMBuffer *)buffer
{
    if (buffer.floatChannelData == NULL || buffer.frameLength == 0) {
        return;
    }

    const float *samples = buffer.floatChannelData[0];
    for (AVAudioFrameCount i = 0; i < buffer.frameLength; i++) {
        float sample = samples[i];
        _levelSumSquares += (double)sample * sample;
        float magnitude = fabsf(sample);
        if (magnitude > _levelPeak) {
            _levelPeak = magnitude;
        }
    }
    _levelFrames += buffer.frameLength;

    if (_levelFrames < buffer.format.sampleRate * UNOSpeechLevelWindowSeconds) {
        return;
    }

    float rmsDb = UNOSpeechToDecibels((float)sqrt(_levelSumSquares / _levelFrames));
    float peakDb = UNOSpeechToDecibels(_levelPeak);
    _levelSumSquares = 0;
    _levelPeak = 0;
    _levelFrames = 0;

    __weak UNOSpeechRecognizer *weakSelf = self;
    dispatch_async(dispatch_get_main_queue(), ^{
        UNOSpeechRecognizer *strongSelf = weakSelf;
        if (strongSelf == nil || strongSelf.disposed || !strongSelf.audioEngine.isRunning) {
            return;
        }
        if (s_audio_level_cb != NULL) {
            s_audio_level_cb(strongSelf, rmsDb, peakDb);
        }
    });
}

- (void)stopAudio
{
    [self abandonTask];

    if (self.audioEngine.isRunning) {
        [self.audioEngine stop];
    }
    [self removeTap];

    os_unfair_lock_lock(&_audioLock);
    _activeRequest = nil;
    [_pendingBuffers removeAllObjects];
    _pendingSeconds = 0;
    os_unfair_lock_unlock(&_audioLock);
}

#pragma mark - Utterances

- (void)beginUtteranceWithHint:(SFSpeechRecognitionTaskHint)hint contextualStrings:(NSArray<NSString *> *)contextualStrings
{
    [self abandonTask];

    if (!self.speechRecognizer.isAvailable) {
        [self reportErrorDomain:UNOSpeechErrorDomain
                           code:UNOSpeechErrorCodeRecognizerUnavailable
                        message:@"Speech recognition is currently unavailable for this language."];
        return;
    }

    int64_t errorCode = 0;
    NSString *message = nil;
    if (![self ensureAudioRunningWithErrorCode:&errorCode message:&message]) {
        [self reportErrorDomain:UNOSpeechErrorDomain code:errorCode message:message];
        return;
    }

    SFSpeechAudioBufferRecognitionRequest *request = [[SFSpeechAudioBufferRecognitionRequest alloc] init];
    request.shouldReportPartialResults = YES;
    request.taskHint = hint;
    request.contextualStrings = contextualStrings;

    self.finalResult = nil;
    self.recognitionTask = [self.speechRecognizer recognitionTaskWithRequest:request delegate:self];

    os_unfair_lock_lock(&_audioLock);
    _activeRequest = request;
    _activeRequestAudioStart = 0;
    for (NSArray *entry in _pendingBuffers) {
        if (_activeRequestAudioStart == 0) {
            _activeRequestAudioStart = [entry[1] doubleValue];
        }
        [request appendAudioPCMBuffer:entry[0]];
    }
    [_pendingBuffers removeAllObjects];
    _pendingSeconds = 0;
    os_unfair_lock_unlock(&_audioLock);
}

- (void)endUtterance
{
    os_unfair_lock_lock(&_audioLock);
    SFSpeechAudioBufferRecognitionRequest *request = _activeRequest;
    _activeRequest = nil;
    os_unfair_lock_unlock(&_audioLock);

    [request endAudio];
}

- (void)cancelUtterance
{
    if (self.recognitionTask == nil) {
        return;
    }

    [self abandonTask];
    [self reportOutcome:UNOSpeechUtteranceOutcomeCanceled json:nil errorDomain:nil errorCode:0 errorMessage:nil];
}

// Cancels the current task without reporting it; later delegate calls for it are ignored.
- (void)abandonTask
{
    SFSpeechRecognitionTask *task = self.recognitionTask;
    self.recognitionTask = nil;
    self.finalResult = nil;

    os_unfair_lock_lock(&_audioLock);
    _activeRequest = nil;
    os_unfair_lock_unlock(&_audioLock);

    [task cancel];
}

#pragma mark - SFSpeechRecognitionTaskDelegate

- (void)speechRecognitionDidDetectSpeech:(SFSpeechRecognitionTask *)task
{
    if (task == self.recognitionTask && s_signal_cb != NULL) {
        s_signal_cb(self, UNOSpeechSignalSpeechDetected);
    }
}

- (void)speechRecognitionTask:(SFSpeechRecognitionTask *)task didHypothesizeTranscription:(SFTranscription *)transcription
{
    if (task == self.recognitionTask && s_hypothesis_cb != NULL) {
        s_hypothesis_cb(self, [transcription.formattedString ?: @"" UTF8String]);
    }
}

- (void)speechRecognitionTask:(SFSpeechRecognitionTask *)task didFinishRecognition:(SFSpeechRecognitionResult *)recognitionResult
{
    if (task == self.recognitionTask) {
        self.finalResult = recognitionResult;
    }
}

- (void)speechRecognitionTaskFinishedReadingAudio:(SFSpeechRecognitionTask *)task
{
    if (task == self.recognitionTask && s_signal_cb != NULL) {
        s_signal_cb(self, UNOSpeechSignalFinishedReadingAudio);
    }
}

- (void)speechRecognitionTask:(SFSpeechRecognitionTask *)task didFinishSuccessfully:(BOOL)successfully
{
    if (task != self.recognitionTask) {
        return;
    }

    SFSpeechRecognitionResult *result = self.finalResult;
    self.recognitionTask = nil;
    self.finalResult = nil;

    os_unfair_lock_lock(&_audioLock);
    _activeRequest = nil;
    double audioStart = _activeRequestAudioStart;
    os_unfair_lock_unlock(&_audioLock);

    if (result != nil && (successfully || result.isFinal)) {
        [self reportOutcome:UNOSpeechUtteranceOutcomeSuccess json:[self serializeResult:result audioStart:audioStart] errorDomain:nil errorCode:0 errorMessage:nil];
    } else if (task.isCancelled) {
        [self reportOutcome:UNOSpeechUtteranceOutcomeCanceled json:nil errorDomain:nil errorCode:0 errorMessage:nil];
    } else {
        NSError *error = task.error;
        [self reportErrorDomain:error.domain ?: UNOSpeechErrorDomain
                           code:error.code
                        message:error.localizedDescription ?: @"Unknown speech recognition error"];
    }
}

#pragma mark - Reporting

- (NSString *)serializeResult:(SFSpeechRecognitionResult *)result audioStart:(double)audioStart
{
    NSArray<SFTranscription *> *transcriptions = result.transcriptions.count > 0 ? result.transcriptions : @[result.bestTranscription];
    NSMutableArray *entries = [NSMutableArray arrayWithCapacity:transcriptions.count];

    for (SFTranscription *transcription in transcriptions) {
        NSArray<SFTranscriptionSegment *> *segments = transcription.segments;
        double confidence = 0;
        double start = 0;
        double end = 0;
        if (segments.count > 0) {
            for (SFTranscriptionSegment *segment in segments) {
                confidence += segment.confidence;
            }
            confidence /= segments.count;
            start = segments.firstObject.timestamp;
            end = segments.lastObject.timestamp + segments.lastObject.duration;
        }

        [entries addObject:@{
            @"text": transcription.formattedString ?: @"",
            @"confidence": @(confidence),
            @"start": @(start),
            @"duration": @(MAX(0, end - start)),
        }];
    }

    NSData *data = [NSJSONSerialization dataWithJSONObject:@{ @"audioStart": @(audioStart), @"transcriptions": entries } options:0 error:nil];
    return data != nil ? [[NSString alloc] initWithData:data encoding:NSUTF8StringEncoding] : @"{}";
}

- (void)reportErrorDomain:(NSString *)domain code:(int64_t)code message:(NSString *)message
{
    [self reportOutcome:UNOSpeechUtteranceOutcomeError json:nil errorDomain:domain errorCode:code errorMessage:message];
}

- (void)reportOutcome:(UNOSpeechUtteranceOutcome)outcome
                 json:(nullable NSString *)json
          errorDomain:(nullable NSString *)errorDomain
            errorCode:(int64_t)errorCode
         errorMessage:(nullable NSString *)errorMessage
{
    if (self.disposed || s_utterance_cb == NULL) {
        return;
    }

    s_utterance_cb(self, outcome, json.UTF8String, errorDomain.UTF8String, errorCode, errorMessage.UTF8String);
}

@end

#pragma mark - Exports

void uno_speech_request_authorization(int64_t token)
{
    void (^report)(int32_t, int32_t) = ^(int32_t speechStatus, int32_t microphoneStatus) {
        dispatch_async(dispatch_get_main_queue(), ^{
            if (s_authorization_cb != NULL) {
                s_authorization_cb(token, speechStatus, microphoneStatus);
            }
        });
    };

    NSDictionary *info = [[NSBundle mainBundle] infoDictionary];
    BOOL hasSpeechDescription = info[@"NSSpeechRecognitionUsageDescription"] != nil;
    BOOL hasMicrophoneDescription = info[@"NSMicrophoneUsageDescription"] != nil;
    if (!hasSpeechDescription || !hasMicrophoneDescription) {
        report(hasSpeechDescription ? (int32_t)[SFSpeechRecognizer authorizationStatus] : UNO_SPEECH_AUTHORIZATION_MISSING_USAGE_DESCRIPTION,
               hasMicrophoneDescription ? (int32_t)[AVCaptureDevice authorizationStatusForMediaType:AVMediaTypeAudio] : UNO_SPEECH_AUTHORIZATION_MISSING_USAGE_DESCRIPTION);
        return;
    }

    [SFSpeechRecognizer requestAuthorization:^(SFSpeechRecognizerAuthorizationStatus speechStatus) {
        if (speechStatus != SFSpeechRecognizerAuthorizationStatusAuthorized) {
            report((int32_t)speechStatus, (int32_t)[AVCaptureDevice authorizationStatusForMediaType:AVMediaTypeAudio]);
            return;
        }

        [AVCaptureDevice requestAccessForMediaType:AVMediaTypeAudio completionHandler:^(BOOL granted) {
            report((int32_t)speechStatus, (int32_t)[AVCaptureDevice authorizationStatusForMediaType:AVMediaTypeAudio]);
        }];
    }];
}

static NSString *UNOSpeechLanguageTag(NSLocale *locale)
{
    return [locale.localeIdentifier stringByReplacingOccurrencesOfString:@"_" withString:@"-"];
}

char * _Nullable uno_speech_copy_supported_locales(void)
{
    NSMutableArray<NSString *> *tags = [NSMutableArray array];
    for (NSLocale *locale in [SFSpeechRecognizer supportedLocales]) {
        [tags addObject:UNOSpeechLanguageTag(locale)];
    }
    [tags sortUsingSelector:@selector(compare:)];
    return strdup([[tags componentsJoinedByString:@"\x1F"] UTF8String]);
}

char * _Nullable uno_speech_copy_default_locale(void)
{
    SFSpeechRecognizer *recognizer = [[SFSpeechRecognizer alloc] init];
    return recognizer != nil ? strdup([UNOSpeechLanguageTag(recognizer.locale) UTF8String]) : NULL;
}

UNOSpeechRecognizer * _Nullable uno_speech_recognizer_create(const char *locale)
{
    dispatch_once(&s_liveRecognizersOnce, ^{
        s_liveRecognizers = [[NSMutableSet alloc] init];
    });

    UNOSpeechRecognizer *recognizer = [[UNOSpeechRecognizer alloc] initWithLocale:locale];
    if (recognizer == nil) {
        return NULL;
    }

    @synchronized (s_liveRecognizers) {
        [s_liveRecognizers addObject:recognizer];
    }
    return recognizer;
}

void uno_speech_recognizer_destroy(UNOSpeechRecognizer *recognizer)
{
    if (recognizer == nil) {
        return;
    }

    recognizer.disposed = YES;
    dispatch_async(dispatch_get_main_queue(), ^{
        [recognizer stopAudio];
        @synchronized (s_liveRecognizers) {
            [s_liveRecognizers removeObject:recognizer];
        }
    });
}

void uno_speech_recognizer_begin_utterance(UNOSpeechRecognizer *recognizer, int32_t taskHint, const char * _Nullable contextualStrings)
{
    NSArray<NSString *> *strings = @[];
    if (contextualStrings != NULL && contextualStrings[0] != '\0') {
        strings = [[NSString stringWithUTF8String:contextualStrings] componentsSeparatedByString:@"\x1F"];
    }

    dispatch_async(dispatch_get_main_queue(), ^{
        if (!recognizer.disposed) {
            [recognizer beginUtteranceWithHint:(SFSpeechRecognitionTaskHint)taskHint contextualStrings:strings];
        }
    });
}

void uno_speech_recognizer_end_utterance(UNOSpeechRecognizer *recognizer)
{
    dispatch_async(dispatch_get_main_queue(), ^{
        if (!recognizer.disposed) {
            [recognizer endUtterance];
        }
    });
}

void uno_speech_recognizer_cancel_utterance(UNOSpeechRecognizer *recognizer)
{
    dispatch_async(dispatch_get_main_queue(), ^{
        if (!recognizer.disposed) {
            [recognizer cancelUtterance];
        }
    });
}

void uno_speech_recognizer_stop_audio(UNOSpeechRecognizer *recognizer)
{
    dispatch_async(dispatch_get_main_queue(), ^{
        if (!recognizer.disposed) {
            [recognizer stopAudio];
        }
    });
}
