# `CommandBar`

`PrimaryCommands` are displayed as menu items directly on the right side of `CommandBar`. You can use the  `Icon` property or the `Content` property to dictate how they should look. Do note that the `Content` will be ignored when an `Icon` is set. The `Label` property has no usage when placed in `PrimaryCommands`<sup>1</sup>.

`SecondaryCommands` are grouped under the overflow (`...`) button as sub-menu items. You can use the `Label` property to set its title. The `Icon` and `Content` property are ignored.

See also: [working-with-commandbar.md]

---

<sup>1</sup>: On UWP, the `Label` is shown when the overflow menu is opened. On iOS, the `Label` is used for accessibility [label][accessibilitylabel] and [hint][accessibilityhint].

# Limitations

- `[iOS]CommandBar.SecondaryCommands`: are not supported.
- `AppBarButton.Icon`: Using `FontIcon`, `PathIcon` and `SymbolIcon` are currently not supported. Use `BitmapIcon` with an `UriSource` instead.

<!-- Links -->
[working-with-commandbar.md]: /doc/articles/controls/working-with-commandbar.md
[accessibilitylabel]: https://developer.apple.com/documentation/uikit/uiaccessibilityelement/1619577-accessibilitylabel
[accessibilityhint]: https://developer.apple.com/documentation/uikit/uiaccessibilityelement/1619585-accessibilityhint