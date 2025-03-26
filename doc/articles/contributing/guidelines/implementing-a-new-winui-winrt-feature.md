---
uid: Uno.Contributing.ImplementWinUIWinRTAPI
---

# Guidelines for implementing a new WinUI/WinRT API

Implementing a new WinUI/WinRT API generally requires:

- Finding the generated API source file (e.g. `src\Uno.UWP\Generated\3.0.0.0\Windows.Data.Pdf\PdfDocument.cs`).
- Copying the file to the non-generated location (e.g. `src\Uno.UWP\Data.Pdf\PdfDocument.cs`).
- Keeping only the members that need to be implemented in the non-generated location.
- Removing (completely or partially depending on the platforms) the implemented members in the generated file.

If your API implementation is for a specific platform:

- You can use a platform suffix in the source file name (`PdfDocument.Android.cs`) so the file is built only for this platform.
- Remove the parts that relate to your platform in the `NotImplemented` attribute:

    ```csharp
    #if __ANDROID__ || __IOS__ || __TVOS__ || IS_UNIT_TESTS || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__
    [global::Uno.NotImplemented("__ANDROID__", "__IOS__", "__TVOS__", "IS_UNIT_TESTS", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__")]
    public  global::Windows.Data.Pdf.PdfPageDimensions Dimensions
    {
    #endif
    ```

    becomes

    ```csharp
    #if false || __IOS__ || __TVOS__ || IS_UNIT_TESTS || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__
    [global::Uno.NotImplemented("__IOS__", "__TVOS__", "IS_UNIT_TESTS", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__")]
    public  global::Windows.Data.Pdf.PdfPageDimensions Dimensions
    {
    #endif
    ```

    when implemented for Android only.

When implementing a feature, try to place as much code as possible in a common source file (a non-suffixed file), so that it is reused across platforms. Make sure to follow [partial classes coding guidelines](xref:Uno.Contributing.CodeStyle).

Note that the generated files may be overridden at any point, and placing custom code (aside from changing the `NotImplemented` values) will be overwritten.
