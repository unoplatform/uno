---
uid: Uno.Controls.TextBox
---

# TextBox

## InputScopes Mapping Table

| Value                   | Description                                                                                                                               |                           Android                            |                 iOS                  |
|-------------------------|-------------------------------------------------------------------------------------------------------------------------------------------|:------------------------------------------------------------:|:------------------------------------:|
| AlphanumericFullWidth   | Input scope is intended for alphanumeric full-width characters.                                                                           |                                                              |                                      |
| AlphanumericHalfWidth   | Input scope is intended for alphanumeric half-width characters.                                                                           |                                                              |                                      |
| AlphanumericPin         | Expected input is an alphanumeric PIN.                                                                                                    |                                                              |                                      |
| Chat                    | Input scope is intended for chat strings.                                                                                                 |                                                              |                                      |
| ChatWithoutEmoji        | Expected input does not include emoji. Advises input processors to not show the emoji key.                                                |                                                              |                                      |
| ChineseFullWidth        | Input scope is intended for Chinese full-width characters.                                                                                |                                                              |                                      |
| ChineseHalfWidth        | Input scope is intended for Chinese half-width characters.                                                                                |                                                              |                                      |
| CurrencyAmount          | Input scope is intended for working with a currency amount (no currency symbol).                                                          |    InputTypes.ClassNumber OR InputTypes.NumberFlagDecimal    |      UIKeyboardType.DecimalPad       |
| CurrencyAmountAndSymbol | Input scope is intended for working with amount and symbol of currency.                                                                   |                                                              |                                      |
| DateDayNumber           | Input scope is intended for working with a numeric day of the month.                                                                      |                                                              |                                      |
| DateMonthNumber         | Input scope is intended for working with a numeric month of the year.                                                                     |                                                              |                                      |
| DateYear                | Input scope is intended for working with a numeric year.                                                                                  |                                                              |                                      |
| Default                 | No input scope is applied.                                                                                                                |                     InputTypes.ClassText                     |        UIKeyboardType.Default        |
| Digits                  | Input scope is intended for working with a collection of numbers.                                                                         |                                                              |                                      |
| EmailNameOrAddress      | Input scope is intended for working with an email name or full email address.                                                             | InputTypes.ClassText OR InputTypes.TextVariationEmailAddress |     UIKeyboardType.EmailAddress      |
| EmailSmtpAddress        | Input scope is intended for working with a Simple Mail Transport Protocol (SMTP) form e-mail address (accountname@host).                  | InputTypes.ClassText OR InputTypes.TextVariationEmailAddress |     UIKeyboardType.EmailAddress      |
| Formula                 | Input scope is intended for spreadsheet formula strings.                                                                                  |                                                              |                                      |
| FormulaNumber           | Expected input is a mathematical formula. Advises input processors to show the number page.                                               |                                                              |                                      |
| HangulFullWidth         | Input scope is intended for Hangul full-width characters.                                                                                 |                                                              |                                      |
| HangulHalfWidth         | Input scope is intended for Hangul half-width characters.                                                                                 |                                                              |                                      |
| Hanja                   | Input scope is intended for Hanja characters.                                                                                             |                                                              |                                      |
| Hiragana                | Input scope is intended for Hiragana characters.                                                                                          |                                                              |                                      |
| KatakanaFullWidth       | Input scope is intended for Katakana full-width characters.                                                                               |                                                              |                                      |
| KatakanaHalfWidth       | Input scope is intended for Katakana half-width characters.                                                                               |                                                              |                                      |
| Maps                    | Input scope is intended for working with a map location.                                                                                  |                                                              |                                      |
| NameOrPhoneNumber       | Input scope is intended for working with a name or telephone number.                                                                      |                                                              |                                      |
| NativeScript            | Input scope is intended for native script.                                                                                                |                                                              |                                      |
| Number                  | Input scope is intended for working with digits 0-9.                                                                                      |                    InputTypes.ClassNumber                    |       UIKeyboardType.NumberPad       |
| NumberFullWidth         | Input scope is intended for full-width number characters.                                                                                 |                   InputTypes.ClassPhone(*)                   | UIKeyboardType.NumbersAndPunctuation |
| NumericPassword         | Expected input is a numeric password, or PIN.                                                                                             |                                                              |                                      |
| NumericPin              | Expected input is a numeric PIN.                                                                                                          |                    InputTypes.ClassNumber                    | UIKeyboardType.NumbersAndPunctuation |
| Password                | Input scope is intended for working with an alphanumeric password, including other symbols, such as punctuation and mathematical symbols. |                                                              |                                      |
| PersonalFullName        | Input scope is intended for working with a complete personal name.                                                                        |                                                              |                                      |
| Search                  | Input scope is intended for search strings.                                                                                               |                     InputTypes.ClassText                     |        UIKeyboardType.Default        |
| SearchIncremental       | Input scope is intended for search boxes where incremental results are displayed as the user types.                                       |                                                              |                                      |
| TelephoneAreaCode       | Input scope is intended for working with a numeric telephone area code.                                                                   |                                                              |                                      |
| TelephoneCountryCode    | Input scope is intended for working with a numeric telephone country code.                                                                |                                                              |                                      |
| TelephoneLocalNumber    | Input scope is intended for working with a local telephone number.                                                                        |                                                              |                                      |
| TelephoneNumber         | Input scope is intended for working with telephone numbers.                                                                               |                    InputTypes.ClassPhone                     |       UIKeyboardType.PhonePad        |
| Text                    | Input scope is intended for working with text.                                                                                            |                                                              |                                      |
| TimeHour                | Input scope is intended for working with a numeric hour of the day.                                                                       |                                                              |                                      |
| TimeMinutesOrSeconds    | Input scope is intended for working with a numeric minute of the hour, or second of the minute.                                           |                                                              |                                      |
| Url                     | Indicates a Uniform Resource Identifier (URI). This can include URL, File, or File Transfer Protocol (FTP) formats.                       |     InputTypes.ClassText OR InputTypes.TextVariationUri      |          UIKeyboardType.Url          |

\[\*: Workaround]

## Controlling the Keyboard on iOS

If a view needs to keep the keyboard opened when tapping on it, use the `Uno.UI.Controls.Window.SetNeedsKeyboard` attached property.

## Paste event

Support for capturing and handling the `Paste` event is implemented on all targets except for macOS.

In the case of Android, it is currently limited to the pastes that are triggered by the native context menu (e.g. after long-pressing the `TextBox`). It cannot detect paste triggered by the virtual keyboard or via a hardware keyboard shortcut.

## Pointer capture on WebAssembly

In addition to focus, `TextBox` control also captures pointers, so that manipulations like scrolling through contents are possible. However, on WASM setting the programmatic capture will prevent the native scrolling behavior of the underlying `<input>`. For that reason, we avoid performing the pointer capture in this case. If you still want to capture the pointer, you can do so by setting the `IsPointerCaptureRequired` attached property to `true`:

In C#:

```csharp
#if __WASM__
myTextBox.IsPointerCaptureRequired = true;
#endif
```

Or in XAML:

```xml
<TextBox wasm:IsPointerCaptureRequired="True" />
```

> Follow [Platform-specific XAML guide](xref:Uno.Development.PlatformSpecificXaml) to add the `wasm` namespace.

The value only affects WebAssembly, all other targets capture by default.

## Customizing the Enter key appearance

On mobile targets you can customize the visual appearance of the Enter key on the virtual keyboard. This can be done using the `TextBoxExtensions.InputReturnType` attached property:

```csharp
Uno.UI.Xaml.Controls.TextBoxExtensions.SetInputReturnType(myTextBox, Uno.UI.Xaml.Controls.InputReturnType.Search);
```

These are the supported input return type values:

- **Enter**: Typically indicating inserting a new line.
- **Done**: There is nothing more to input, and the input method editor (IME) will be closed.
- **Go**: Typically meaning to take the user to the target of the text they typed.
- **Next**: Typically taking the user to the next field that will accept text.
- **Previous**: Typically taking the user to the previous field that will accept text.
- **Search**: Typically taking the user to the results of searching for the text they have typed.
- **Send**: Typically delivering the text to its target.
- **Default**: System default.
