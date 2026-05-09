---
uid: Uno.Features.SpellChecking
---

# Spell Checking

Uno Platform provides optional spell-checking support for `TextBox` controls on all Skia-based targets. When enabled, misspelled words are underlined in red and opening the context menu (secondary click or long-press) on a misspelled word presents spelling suggestions.

## Enabling Spell Checking

Spell-checking is provided by the `Uno.WinUI.SpellChecking` package. To include it, add the `SpellChecking` feature to your project's `UnoFeatures`:

```xml
<UnoFeatures>
    SpellChecking;
    <!-- other features -->
</UnoFeatures>
```

Once enabled, any `TextBox` with `IsSpellCheckEnabled="True"` (the default) will display spell-checking visuals. An English (en_US) dictionary is included by default.

## Custom Dictionaries

The default dictionary is English (en_US). To add spell-checking for other languages, load Hunspell-compatible dictionary files (`.dic` and `.aff`) at application startup using `FeatureConfiguration.TextBox.CustomSpellCheckDictionaries`.

### Obtaining Dictionaries

Hunspell-compatible dictionaries are freely available from:

- [LibreOffice dictionaries](https://github.com/LibreOffice/dictionaries) — covers 100+ languages
- [Hunspell project on GitHub](https://github.com/hunspell/hunspell)

Download the `.dic` and `.aff` files for your target language and include them in your project as embedded resources.

### Loading a Custom Dictionary

1. Add the dictionary files to your project and set their **Build Action** to **Embedded Resource**.

2. Load the dictionaries at application startup (e.g., in `App.xaml.cs`):

```csharp
using Uno.UI;

// Load a French dictionary from embedded resources
var assembly = typeof(App).Assembly;
var frDic = assembly.GetManifestResourceStream("MyApp.Dictionaries.fr_FR.dic");
var frAff = assembly.GetManifestResourceStream("MyApp.Dictionaries.fr_FR.aff");

FeatureConfiguration.TextBox.CustomSpellCheckDictionaries = new()
{
    (frDic!, frAff!)
};
```

Multiple dictionaries can be loaded simultaneously. The spell-checker validates words against all loaded dictionaries — a word is considered correct if it is found in any dictionary:

```csharp
FeatureConfiguration.TextBox.CustomSpellCheckDictionaries = new()
{
    (frDic!, frAff!),
    (deDic!, deAff!)
};
```
