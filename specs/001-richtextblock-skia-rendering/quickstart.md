# Quickstart: RichTextBlock Skia Rendering (Phase 1)

## Prerequisites

1. Clone and set up the Uno Platform repository
2. Configure `src/crosstargeting_override.props` for Skia:
   ```xml
   <Project>
     <PropertyGroup>
       <UnoTargetFrameworkOverride>net10.0</UnoTargetFrameworkOverride>
     </PropertyGroup>
   </Project>
   ```
3. Ensure access to WinUI C++ sources at `D:\Work\microsoft-ui-xaml2`

## Build & Test

```bash
# Restore
cd src
dotnet restore Uno.UI-Skia-only.slnf

# Build
dotnet build Uno.UI-Skia-only.slnf --no-restore

# Unit tests
dotnet test Uno.UI/Uno.UI.Tests.csproj --no-build

# Runtime tests (headless Skia)
dotnet build SamplesApp/SamplesApp.Skia.Generic/SamplesApp.Skia.Generic.csproj -c Release -f net10.0
cd SamplesApp/SamplesApp.Skia.Generic/bin/Release/net10.0
dotnet SamplesApp.Skia.Generic.dll --runtime-tests=test-results.xml
```

## Key File Locations

| What | Where |
|------|-------|
| RichTextBlock control | `src/Uno.UI/UI/Xaml/Controls/RichTextBlock/` |
| Document model (Paragraph, Run, etc.) | `src/Uno.UI/UI/Xaml/Documents/` |
| Text rendering engine | `src/Uno.UI/UI/Xaml/Documents/UnicodeText.skia.cs` |
| Font infrastructure | `src/Uno.UI/UI/Xaml/Documents/TextFormatting/` |
| TextBlock reference implementation | `src/Uno.UI/UI/Xaml/Controls/TextBlock/TextBlock.skia.cs` |
| Generated stubs | `src/Uno.UI/Generated/3.0.0.0/Microsoft.UI.Xaml.Controls/RichTextBlock.cs` |
| Runtime tests | `src/Uno.UI.RuntimeTests/Tests/Microsoft_UI_Xaml_Controls/` |
| SamplesApp registration | `src/SamplesApp/UITests.Shared/UITests.Shared.projitems` |

## Implementation Order

1. **Invalidation chain fix** — Inline.cs, InlineCollection.cs, Paragraph.cs
2. **RichTextBlock.cs** — Remove NotImplemented, set up constructor
3. **RichTextBlock.Properties.cs** — All DependencyProperties
4. **RichTextBlockTextVisual.skia.cs** — Composition visual
5. **RichTextBlock.skia.cs** — MeasureOverride, ArrangeOverride, Draw
6. **Generated stub cleanup** — Remove __SKIA__ from #if guards
7. **Runtime tests** — Given_RichTextBlock.cs
8. **SamplesApp** — Visual validation sample

## Validation Checklist

- [ ] `<RichTextBlock><Paragraph><Run Text="Hello"/></Paragraph></RichTextBlock>` renders visible text on Skia
- [ ] Font properties (FontFamily, FontSize, FontWeight, FontStyle, FontStretch) affect rendering
- [ ] Inline formatting (Bold, Italic, Underline, Span) works within a single Paragraph
- [ ] TextWrapping defaults to Wrap (not NoWrap)
- [ ] TextAlignment, MaxLines, LineHeight, CharacterSpacing work
- [ ] Dynamic property changes cause re-layout
- [ ] Empty content (no paragraphs, empty paragraph) doesn't crash
- [ ] DesiredSize matches equivalent TextBlock content within 1 DIP tolerance
- [ ] All runtime tests pass
- [ ] Build completes without warnings related to RichTextBlock
