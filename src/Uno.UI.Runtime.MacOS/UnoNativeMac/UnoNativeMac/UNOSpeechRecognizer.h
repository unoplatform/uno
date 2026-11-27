//
//  UNOSpeechRecognizer.h
//

#pragma once

#import "UnoNativeMac.h"

NS_ASSUME_NONNULL_BEGIN

// Must match Uno.UI.Runtime.MacOS.MacOSSpeechRecognizerExtension.
typedef NS_ENUM(int32_t, UNOSpeechSignal) {
    UNOSpeechSignalSpeechDetected = 0,
    UNOSpeechSignalFinishedReadingAudio = 1,
};

typedef NS_ENUM(int32_t, UNOSpeechUtteranceOutcome) {
    UNOSpeechUtteranceOutcomeSuccess = 0,
    UNOSpeechUtteranceOutcomeError = 1,
    UNOSpeechUtteranceOutcomeCanceled = 2,
};

// Error codes reported with the UNOSpeechErrorDomain domain.
typedef NS_ENUM(int64_t, UNOSpeechErrorCode) {
    UNOSpeechErrorCodeNoInputDevice = 1,
    UNOSpeechErrorCodeAudioEngineFailed = 2,
    UNOSpeechErrorCodeRecognizerUnavailable = 3,
};

extern NSString * const UNOSpeechErrorDomain;

// Reported instead of an authorization status when the Info.plist usage description is missing,
// because the system terminates the process when the permission is requested without it.
#define UNO_SPEECH_AUTHORIZATION_MISSING_USAGE_DESCRIPTION (-1)

@class UNOSpeechRecognizer;

typedef void (*uno_speech_hypothesis_fn_ptr)(UNOSpeechRecognizer * /* handle */, const char * /* text */);
typedef void (*uno_speech_signal_fn_ptr)(UNOSpeechRecognizer * /* handle */, int32_t /* UNOSpeechSignal */);
typedef void (*uno_speech_audio_level_fn_ptr)(UNOSpeechRecognizer * /* handle */, float /* rmsDb */, float /* peakDb */);
// resultJson is set for UNOSpeechUtteranceOutcomeSuccess, the error fields for UNOSpeechUtteranceOutcomeError.
typedef void (*uno_speech_utterance_fn_ptr)(UNOSpeechRecognizer * /* handle */, int32_t /* UNOSpeechUtteranceOutcome */,
                                            const char * _Nullable /* resultJson */,
                                            const char * _Nullable /* errorDomain */, int64_t /* errorCode */,
                                            const char * _Nullable /* errorMessage */);
// Statuses are SFSpeechRecognizerAuthorizationStatus / AVAuthorizationStatus, or UNO_SPEECH_AUTHORIZATION_MISSING_USAGE_DESCRIPTION.
typedef void (*uno_speech_authorization_fn_ptr)(int64_t /* token */, int32_t /* speechStatus */, int32_t /* microphoneStatus */);

void uno_speech_set_callbacks(uno_speech_hypothesis_fn_ptr hypothesis,
                              uno_speech_signal_fn_ptr signal,
                              uno_speech_audio_level_fn_ptr audioLevel,
                              uno_speech_utterance_fn_ptr utterance,
                              uno_speech_authorization_fn_ptr authorization);

void uno_speech_request_authorization(int64_t token);

// Both return a malloc'ed UTF-8 string the caller must free(), or NULL.
// Locales are BCP-47 tags separated by '\x1F' (Unit Separator).
char * _Nullable uno_speech_copy_supported_locales(void);
char * _Nullable uno_speech_copy_default_locale(void);

@interface UNOSpeechRecognizer : NSObject

- (nullable instancetype)initWithLocale:(const char *)locale;

@end

// All instance functions return immediately; the work runs on the main queue, where every callback is raised.
UNOSpeechRecognizer * _Nullable uno_speech_recognizer_create(const char *locale);
void uno_speech_recognizer_destroy(UNOSpeechRecognizer *recognizer);
// contextualStrings: optional '\x1F'-separated phrases. Starts audio capture if needed.
void uno_speech_recognizer_begin_utterance(UNOSpeechRecognizer *recognizer, int32_t taskHint, const char * _Nullable contextualStrings);
void uno_speech_recognizer_end_utterance(UNOSpeechRecognizer *recognizer);
void uno_speech_recognizer_cancel_utterance(UNOSpeechRecognizer *recognizer);
void uno_speech_recognizer_stop_audio(UNOSpeechRecognizer *recognizer);

NS_ASSUME_NONNULL_END
