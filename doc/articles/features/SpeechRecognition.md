---
uid: Uno.Features.SpeechRecognition
---

# Speech Recognition in Uno

> [!TIP]
> This article covers Uno-specific information for `Windows.Media.SpeechRecognition`. For a full description of the feature and instructions on using it, see [SpeechRecognition Namespace](https://learn.microsoft.com/uwp/api/windows.media.speechrecognition).

* The `Windows.Media.SpeechRecognition` class allows an application to recognize voice input.

Uno's implementation currently supports basic native speech recognition.

## Supported Features

The following features of `Windows.Media.SpeechRecognition.SpeechRecognizer` are currently supported:

| Feature                                    | iOS | Android | macOS (Skia) | WebAssembly | Remarks                       |
|--------------------------------------------|-----|---------|--------------|-------------|-------------------------------|
| SpeechRecognizer()                         | X   | X       | X            | X           |                               |
| SpeechRecognizer(Language)                 | X   | X       | X            | X           |                               |
| Constraints                                | -   | -       | X            | -           | macOS: list and topic constraints, see [macOS behavior](#macos-behavior) |
| ContinuousRecognitionSession               | -   | -       | X            | -           |                               |
| CurrentLanguage                            | X   | X       | X            | X           |                               |
| State                                      | X   | X       | X            | X           |                               |
| SupportedGrammarLanguages                  | -   | -       | X            | -           |                               |
| SupportedTopicLanguages                    | -   | -       | X            | -           |                               |
| SystemSpeechLanguage                       | -   | -       | X            | -           |                               |
| Timeouts                                   | X   | X       | X            | X           |                               |
| UIOptions                                  | X   | X       | X            | X           | Not used                      |
| CompileConstraintsAsync()                  | X   | X       | X            | X           | iOS, Android, WebAssembly: always returns Success (implemented to meet WinUI constraint that requires `CompileConstraintsAsync()` to be called before `RecognizeAsync()`). macOS: returns `TopicLanguageNotSupported` for unsupported languages, and `GrammarCompilationFailure` for unsupported constraints |
| Dispose()                                  | X   | X       | X            | X           |                               |
| RecognizeAsync()                           | X   | X       | X            | X           |                               |
| RecognizeWithUIAsync()                     | -   | -       | -            | -           |                               |
| StopRecognitionAsync()                     | X   | X       | X            | X           |                               |
| TrySetSystemSpeechLanguageAsync(Language)  | -   | -       | X            | -           | macOS: always returns `false`, apps can't change the system speech language |
| HypothesisGenerated                        | X   | X       | X            | X           |                               |
| RecognitionQualityDegrading                | -   | -       | X            | -           | macOS: estimated from the microphone level |
| StateChanged                               | X   | X       | X            | X           |                               |

## Requirement

### iOS

* iOS 10 or later is required.
* The following lines need to be added to your info.plist:

  ```xml
  <key>NSSpeechRecognitionUsageDescription</key>  
  <string>[SpeechRecognition usage description]</string>  
  <key>NSMicrophoneUsageDescription</key>  
  <string>[SpeechRecognition usage description]</string> 
  ```

### Android

The following lines need to be added to your AndroidManifest.xml:

```xml
<uses-permission android:name="android.permission.INTERNET" />
<uses-permission android:name="android.permission.RECORD_AUDIO" />
```

### macOS (Skia)

* The following lines need to be added to your app bundle's `Info.plist`:

  ```xml
  <key>NSSpeechRecognitionUsageDescription</key>
  <string>[SpeechRecognition usage description]</string>
  <key>NSMicrophoneUsageDescription</key>
  <string>[SpeechRecognition usage description]</string>
  ```

* The app must be packaged as an `.app` bundle when running speech recognition — macOS only displays the TCC authorization prompt for bundled apps. When the app runs without these `Info.plist` keys, for example when launched with `dotnet run`, `RecognizeAsync()` throws instead of requesting permission.

> [!NOTE]
> For contributors: The Uno Platform samples app (`SamplesApp`) ships a build target that automatically produces a minimal `.app` bundle next to the regular output on macOS. After `dotnet build`, launch it with:
>
> ```bash
> open src/SamplesApp/SamplesApp/bin/Debug/net11.0-desktop/SamplesApp.app
> ```

### WebAssembly

Speech recognition on WebAssembly relies on the browser's Web Speech API implementation:

* `window.SpeechRecognition`, or
* `window.webkitSpeechRecognition` (vendor-prefixed fallback).

Requirements and behavior notes:

* The app must run in a browser that exposes one of the APIs above.
* The user must grant microphone permission in the browser.
* Browsers typically require a secure context (`https://`), with `http://localhost` commonly allowed for local development.
* Starting recognition may require user interaction (for example, a button click) depending on browser policies.

## macOS behavior

On macOS, speech recognition follows the WinUI behavior described in the Microsoft documentation, using Apple's Speech framework:

* `State` goes through `Capturing`, `SoundStarted`, `SpeechDetected`, `SoundEnded`, `Processing` and back to `Idle`. A continuous session stays in `Capturing` between phrases and is `Paused` after `PauseAsync()`.
* `Timeouts` default to 5 seconds of `InitialSilenceTimeout`, 150 milliseconds of `EndSilenceTimeout` and no `BabbleTimeout`. `AutoStopSilenceTimeout` defaults to 20 seconds. Set a timeout to `TimeSpan.Zero` to disable it.
* Failures are reported through `SpeechRecognitionResult.Status` (for example `TimeoutExceeded`, `UserCanceled`, `MicrophoneUnavailable` or `NetworkFailure`) and `SpeechContinuousRecognitionCompletedEventArgs.Status`, rather than as exceptions.
* When the user denies speech recognition or microphone access, or the `Info.plist` keys are missing, `RecognizeAsync()` and `StartAsync()` throw a `COMException` with `HResult` `0x80045509`, like WinUI when the speech privacy policy isn't accepted.
* Calling `RecognizeAsync()`, `StartAsync()` or `CompileConstraintsAsync()` while the recognizer is busy throws an `InvalidOperationException`.
* `StopRecognitionAsync()` returns the words recognized so far, or a `UserCanceled` result if nothing was said yet.
* `SpeechRecognitionResult` provides `Text`, `Status`, `Confidence`, `RawConfidence`, `PhraseStartTime`, `PhraseDuration`, `Constraint` and `GetAlternates()`. `RawConfidence` is the average of Apple's per-word confidence; `Confidence` is `High` from 0.8, `Medium` from 0.5, and `Low` below.
* A `SpeechRecognitionListConstraint` biases recognition toward its commands, and the result must be one of the commands (ignoring case and punctuation): otherwise the result has a `Rejected` confidence and an empty `Text`. Apple's engine uses up to 100 commands.
* A `SpeechRecognitionTopicConstraint` selects the dictation or search mode of the engine. Grammar file and voice command definition constraints aren't supported.
* While a continuous session is paused, audio is still captured (up to 30 seconds) and recognized after `Resume()`.
* Events are raised on the UI thread when recognition is started from it.

## Limitation

On iOS, Android and WebAssembly, only `Text`, `Alternates`, and `GetAlternates(uint maxAlternates)` are implemented in `Windows.Media.SpeechRecognition.SpeechRecognitionResult`, and `RawConfidence` is only provided on WebAssembly.

On macOS, `RecognizeWithUIAsync()`, `RulePath` and `SemanticInterpretation` aren't supported. Apple limits each recognition to about one minute of audio; continuous recognition starts a new recognition for each phrase.

On WebAssembly specifically, availability is browser-dependent. If speech recognition is not available in the current browser, `RecognizeAsync()` fails with an `InvalidOperationException`.
