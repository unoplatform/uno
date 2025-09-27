# ResourcesExtractor

This tool helps generating localized resources from a running WinUI application given the localizedResource.h file from WinUI source code.

The language codes were extracted based on [winrt.h](https://raw.githubusercontent.com/dwmcore/WinSDK-Tools/78c9c2464212f8ecf16d6f20f072423b3ab76901/Include/10.0.25967.0/um/winrt.h). The following method can be used to get a language id:

```csharp
private static int MAKELANGID(int primaryLang, int subLang)
{
   return ((((ushort)(subLang)) << 10) | (ushort)(primaryLang));
}
```

For example, let's look into winrt.h for en-US. The two relevant entries are:

```c
#define LANG_ENGLISH                     0x09
#define SUBLANG_ENGLISH_US                          0x01    // English (USA)
```

So, now if you do `Console.WriteLine(MAKELANGID(0x09, 0x01).ToString("X"));`, you'll get 409, so it's en-US is defined in `Languages` enum as `en_us = 0x409`.

The result of this tool is added in [`src\Uno.UI\UI\Xaml\Controls\WinUIResources`](https://github.com/unoplatform/uno/tree/aba02b0fdaa2b529e19e4751843aed1cfc969fbf/src/Uno.UI/UI/Xaml/Controls/WinUIResources)
