---
uid: Uno.Features.WinUISuccinctSyntax
---

# Uno Support for WinUI succinct syntax

Uno supports the [succinct syntax](https://github.com/microsoft/microsoft-ui-xaml-specs/blob/master/active/gridsyntax/GridSyntaxSpec.md) WinUI XAML language feature that allows the initialization of collection-type properties (including read-only properties) using element attribute syntax.

## Examples

### New succinct syntax

The code below has the same functionality as the code shown above with the original syntax. It creates a grid and defines five different rows and columns, each with their own specific height/width, and adds them to the Grid.

```xml
<Grid ColumnDefinitions="1*, 2*, Auto, *, 300"
      RowDefinitions="1*, Auto, 25, 14, 20">
</Grid>
```

### Grid-specific syntax using assigned ContentProperty

The code below has the same functionality as the code shown above with the original syntax, but uses the ColumnDefinition and RowDefinition content property assignments to write it in the following way.

```xml
<Grid>
    <Grid.ColumnDefinitions>
          <ColumnDefinition>1*</ColumnDefinition>
          <ColumnDefinition>2*</ColumnDefinition>
          <ColumnDefinition>Auto</ColumnDefinition>
          <ColumnDefinition>*</ColumnDefinition>
          <ColumnDefinition>300</ColumnDefinition>
    </Grid.ColumnDefinitions>
    <Grid.RowDefinitions>
          <RowDefinition>1*</RowDefinition>
          <RowDefinition>Auto</RowDefinition>
          <RowDefinition>25</RowDefinition>
          <RowDefinition>14</RowDefinition>
          <RowDefinition>20</RowDefinition>
    </Grid.RowDefinitions>
</Grid>
```

## Notes

1. Uno.UI projects in a UWP-based solution also support this feature, but using it in a UWP project will cause a build error because UWP doesn't support it.
2. Uno currently only supports this syntax for Grid.
  
See the [WinUI documentation](https://github.com/microsoft/microsoft-ui-xaml-specs/blob/master/active/gridsyntax/GridSyntaxSpec.md) for more details.
