//
//  UNOSpeechRecognizer.h
//

#pragma once

#import "UnoNativeMac.h"

NS_ASSUME_NONNULL_BEGIN

// Must match Windows.Media.SpeechRecognition.SpeechRecognizerState.
typedef NS_ENUM(int32_t, UNOSpeechRecognizerState) {
    UNOSpeechRecognizerStateIdle = 0,
    UNOSpeechRecognizerStateCapturing = 1,
    UNOSpeechRecognizerStateProcessing = 2,
    UNOSpeechRecognizerStateSoundStarted = 3,
    UNOSpeechRecognizerStateSoundEnded = 4,
    UNOSpeechRecognizerStateSpeechDetected = 5,
    UNOSpeechRecognizerStatePaused = 6,
};

@class UNOSpeechRecognizer;

typedef void (*uno_speech_hypothesis_fn_ptr)(UNOSpeechRecognizer * /* handle */, const char * /* text */);
typedef void (*uno_speech_state_fn_ptr)(UNOSpeechRecognizer * /* handle */, int32_t /* state */);
// alternates: optional null-terminated UTF-8 string; entries separated by '\x1F' (Unit Separator). NULL when none.
typedef void (*uno_speech_result_fn_ptr)(UNOSpeechRecognizer * /* handle */, const char * /* text */, const char * _Nullable /* alternates */);
typedef void (*uno_speech_error_fn_ptr)(UNOSpeechRecognizer * /* handle */, const char * /* error */);

uno_speech_hypothesis_fn_ptr uno_speech_get_hypothesis_callback(void);
uno_speech_state_fn_ptr uno_speech_get_state_callback(void);
uno_speech_result_fn_ptr uno_speech_get_result_callback(void);
uno_speech_error_fn_ptr uno_speech_get_error_callback(void);

void uno_speech_set_callbacks(uno_speech_hypothesis_fn_ptr hypothesis,
                              uno_speech_state_fn_ptr state,
                              uno_speech_result_fn_ptr result,
                              uno_speech_error_fn_ptr error);

@interface UNOSpeechRecognizer : NSObject

- (nullable instancetype)initWithLocale:(const char *)locale;

@end

UNOSpeechRecognizer * _Nullable uno_speech_recognizer_create(const char *locale);
void uno_speech_recognizer_destroy(UNOSpeechRecognizer *recognizer);
bool uno_speech_recognizer_start(UNOSpeechRecognizer *recognizer);
void uno_speech_recognizer_stop(UNOSpeechRecognizer *recognizer);
bool uno_speech_recognizer_is_available(UNOSpeechRecognizer *recognizer);

NS_ASSUME_NONNULL_END
