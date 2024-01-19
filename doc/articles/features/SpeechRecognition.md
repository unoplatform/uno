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

| Feature                                    | iOS | Android | Remarks                       |
|--------------------------------------------|-----|---------|-------------------------------|
| SpeechRecognizer()                         | X   | X       |                               |
| SpeechRecognizer(Language)                 | X   | X       |                               |
| Constraints                                | -   | -       |                               |
| ContinuousRecognitionSession               | -   | -       |                               |
| CurrentLanguage                            | X   | X       |                               |
| State                                      | X   | X       |                               |
| SupportedGrammarLanguages                  | -   | -       |                               |
| SupportedTopicLanguages                    | -   | -       |                               |
| SystemSpeechLanguage                       | -   | -       |                               |
| Timeouts                                   | X   | X       |                               |
| UIOptions                                  | X   | X       | Not used                      |
| CompileConstraintsAsync()                  | X   | X       | Always return Success (implemented to meet UWP constraint that requires `CompileConstraintsAsync()` to be called before `RecognizeAsync()`) |
| Dispose()                                  | X   | X       |                               |
| RecognizeAsync()                           | X   | X       |                               |
| RecognizeWithUIAsync()                     | -   | -       |                               |
| StopRecognitionAsync()()                   | X   | X       |                               |
| TrySetSystemSpeechLanguageAsync(Language)  | -   | -       |                               |
| HypothesisGenerated                        | X   | X       |                               |
| RecognitionQualityDegrading                | -   | -       |                               |
| StateChanged                               | X   | X       |                               |

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

## Limitation

In `Windows.Media.SpeechRecognition.SpeechRecognitionResult`, only `Text`, `Alternates`, and `GetAlternates(uint maxAlternates)` are implemented.
In particular, `RawConfidence` and `Confidence` fields are not currently supported.
