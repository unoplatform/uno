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

| Feature                                    | iOS | Android | macOS (Skia) | Remarks                       |
|--------------------------------------------|-----|---------|--------------|-------------------------------|
| SpeechRecognizer()                         | X   | X       | X            |                               |
| SpeechRecognizer(Language)                 | X   | X       | X            |                               |
| Constraints                                | -   | -       | -            |                               |
| ContinuousRecognitionSession               | -   | -       | -            |                               |
| CurrentLanguage                            | X   | X       | X            |                               |
| State                                      | X   | X       | X            |                               |
| SupportedGrammarLanguages                  | -   | -       | -            |                               |
| SupportedTopicLanguages                    | -   | -       | -            |                               |
| SystemSpeechLanguage                       | -   | -       | -            |                               |
| Timeouts                                   | X   | X       | X            |                               |
| UIOptions                                  | X   | X       | X            | Not used                      |
| CompileConstraintsAsync()                  | X   | X       | X            | Always return Success (implemented to meet WinUI constraint that requires `CompileConstraintsAsync()` to be called before `RecognizeAsync()`) |
| Dispose()                                  | X   | X       | X            |                               |
| RecognizeAsync()                           | X   | X       | X            |                               |
| RecognizeWithUIAsync()                     | -   | -       | -            |                               |
| StopRecognitionAsync()()                   | X   | X       | X            |                               |
| TrySetSystemSpeechLanguageAsync(Language)  | -   | -       | -            |                               |
| HypothesisGenerated                        | X   | X       | X            |                               |
| RecognitionQualityDegrading                | -   | -       | -            |                               |
| StateChanged                               | X   | X       | X            |                               |

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

* macOS 10.15 or later is required.
* The following lines need to be added to your app bundle's `Info.plist`:

  ```xml
  <key>NSSpeechRecognitionUsageDescription</key>
  <string>[SpeechRecognition usage description]</string>
  <key>NSMicrophoneUsageDescription</key>
  <string>[SpeechRecognition usage description]</string>
  ```

* The app must be packaged as an `.app` bundle when running speech recognition — macOS only displays the TCC authorization prompt for bundled apps. Launching the executable directly from a terminal (`dotnet run`) will result in `SFSpeechRecognizer` returning `Denied` without prompting.

> [!NOTE]
> For contributors: The Uno samples app (`SamplesApp.Skia.Generic`) ships a build target that automatically produces a minimal `.app` bundle next to the regular output on macOS. After `dotnet build`, launch it with:
>
> ```bash
> open src/SamplesApp/SamplesApp.Skia.Generic/bin/Debug/net10.0/SamplesApp.> Skia.Generic.app
> ```

## Limitation

In `Windows.Media.SpeechRecognition.SpeechRecognitionResult`, only `Text`, `Alternates`, and `GetAlternates(uint maxAlternates)` are implemented.
In particular, `RawConfidence` and `Confidence` fields are not currently supported.
