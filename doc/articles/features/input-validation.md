---
uid: Uno.Features.InputValidation
---

# Input validation

Uno Platform can surface the validation errors of your view model on the control bound to it. When a binding's source implements [`INotifyDataErrorInfo`](https://learn.microsoft.com/dotnet/api/system.componentmodel.inotifydataerrorinfo), the errors it reports for the bound property are exposed on the control, where your XAML can display them.

The feature depends on `INotifyDataErrorInfo` and nothing else: no base class, attribute set or MVVM framework is required. It is opt-in, both application-wide and per control.

> [!NOTE]
> Input validation is an Uno Platform feature. WinUI has no public equivalent, so on Windows App SDK the `Validation` attached properties described below compile but have no effect. Your markup stays portable.

<!-- -->

> [!IMPORTANT]
> The built-in control styles don't display validation errors yet. Your app either displays them itself, as shown in [Displaying errors](#displaying-errors), or uses a custom control template, as shown in [Displaying errors in a control template](#displaying-errors-in-a-control-template).

## Supported controls

| Control | Validated property |
|---|---|
| `TextBox` | `Text` |
| `PasswordBox` | `Password` |
| `AutoSuggestBox` | `Text` |
| `ComboBox` | `SelectedItem` |

Errors are only read from the binding on the validated property. A control has a single validated property. To validate another property, or a control that isn't listed, see [Validating other properties and controls](#validating-other-properties-and-controls).

## Enabling input validation

1. Turn the feature on in the constructor of your `App` class, before any page is created:

    ```csharp
    public App()
    {
        Uno.UI.FeatureConfiguration.InputValidation.IsEnabled = true;

        this.InitializeComponent();
    }
    ```

    The flag is read when each binding is set, which for bindings declared in XAML happens during `InitializeComponent`. Bindings set before the flag is turned on are never validated. When the flag is off (the default), validation adds no cost to bindings.

1. Opt each control in by setting the `Validation.Mode` attached property from the `Uno.Extras.Input` namespace:

    ```xml
    <Page xmlns:input="using:Uno.Extras.Input">
        <TextBox Text="{Binding UserName, Mode=TwoWay, UpdateSourceTrigger=PropertyChanged}"
                 input:Validation.Mode="Auto" />
    </Page>
    ```

    `Validation.Mode` defaults to `Disabled`. Both `Auto` and `Default` enable validation. It can also be set from a `Style`:

    ```xml
    <Style TargetType="TextBox">
        <Setter Property="input:Validation.Mode" Value="Auto" />
    </Style>
    ```

`Validation` is part of the `Uno.UI.Extras` assembly, which is included in the `Uno.WinUI` package. No additional package is needed.

## Reporting errors from a view model

Any implementation of `INotifyDataErrorInfo` works. It can be written by hand, generated, or inherited from a base class such as the [CommunityToolkit.Mvvm `ObservableValidator`](https://learn.microsoft.com/dotnet/communitytoolkit/mvvm/observablevalidator), which validates properties annotated with DataAnnotations attributes.

A minimal hand-written implementation:

```csharp
public class SignUpViewModel : INotifyPropertyChanged, INotifyDataErrorInfo
{
    private readonly Dictionary<string, List<string>> _errors = new();
    private string _userName = "";

    public string UserName
    {
        get => _userName;
        set
        {
            _userName = value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(UserName)));

            SetErrors(nameof(UserName), value.Length < 5 ? ["At least 5 characters, please."] : []);
        }
    }

    public bool HasErrors => _errors.Count != 0;

    public IEnumerable GetErrors(string? propertyName)
        => string.IsNullOrEmpty(propertyName)
            ? _errors.Values.SelectMany(errors => errors).ToList()
            : _errors.TryGetValue(propertyName, out var errors) ? errors : Array.Empty<string>();

    public event EventHandler<DataErrorsChangedEventArgs>? ErrorsChanged;

    public event PropertyChangedEventHandler? PropertyChanged;

    private void SetErrors(string propertyName, List<string> errors)
    {
        if (errors.Count == 0)
        {
            _errors.Remove(propertyName);
        }
        else
        {
            _errors[propertyName] = errors;
        }

        ErrorsChanged?.Invoke(this, new DataErrorsChangedEventArgs(propertyName));
    }
}
```

How the errors reach the control:

- **The errors come from the last object in the binding path.** For `{Binding Customer.Name}`, `Customer` must implement `INotifyDataErrorInfo`, and its errors for `Name` are used. If `Customer` is replaced, the control follows the new instance.
- **`{Binding}` and `{x:Bind}` are both supported.** An `{x:Bind}` to a function with several arguments has no single source property, and is not validated.
- **Each error is converted to text.** The control exposes every object returned by `GetErrors` as an `InputValidationError`, whose `ErrorMessage` is the object's `ToString()`. Return strings, or objects whose `ToString()` is the message to display.
- **`ErrorsChanged` may be raised from any thread.** The control updates on the UI thread.
- **The current errors are read when the binding resolves its source**, so errors that exist before the page loads are displayed right away.

## Displaying errors

A participating control exposes its errors through two attached properties:

| Property | Type | Description |
|---|---|---|
| `Validation.HasErrors` | `bool` | Whether the control currently has errors. |
| `Validation.Errors` | `IObservableVector<InputValidationError>` | The current errors. |

Both are set by the framework, and your app reads them. `Validation.Errors` stays the same collection for the lifetime of the control and is updated in place, so bind a list to it rather than converting it to a single value: a converter only runs when the property is set, not when the collection changes.

```xml
<StackPanel Spacing="4">
    <TextBox x:Name="UserNameBox"
             Header="User name"
             Text="{Binding UserName, Mode=TwoWay, UpdateSourceTrigger=PropertyChanged}"
             input:Validation.Mode="Auto" />

    <ItemsControl ItemsSource="{Binding (input:Validation.Errors), ElementName=UserNameBox}">
        <ItemsControl.ItemTemplate>
            <DataTemplate>
                <TextBlock Text="{Binding ErrorMessage}"
                           Foreground="{ThemeResource SystemControlErrorTextForegroundBrush}" />
            </DataTemplate>
        </ItemsControl.ItemTemplate>
    </ItemsControl>
</StackPanel>
```

`Validation.HasErrors` raises a property change whenever it toggles, so it suits bindings that need a single value, such as hiding an icon or disabling a submit button through a converter. The same values are available from code through `Validation.GetHasErrors(control)` and `Validation.GetErrors(control)`.

## Displaying errors in a control template

A control template can display the errors itself, which avoids repeating the markup above for every field. The framework drives two visual state groups and fills a presenter named `ErrorPresenter`, using the names WinUI defines for the same purpose.

### Visual states

| Group | State | Applied when |
|---|---|---|
| `InputValidationEnabledStates` | `ValidationDisabled` | `Validation.Mode` is `Disabled` |
| | `CompactValidationEnabled` | validation is enabled and `Validation.Kind` is `Compact` or `Auto` |
| | `InlineValidationEnabled` | validation is enabled and `Validation.Kind` is `Inline` |
| `InputValidationErrorStates` | `ErrorsCleared` | there are no errors |
| | `CompactErrors` | there are errors and `Validation.Kind` is `Compact` or `Auto` |
| | `InlineErrors` | there are errors and `Validation.Kind` is `Inline` |

Use `InputValidationEnabledStates` to reserve the space where errors will appear, so the layout does not shift as errors come and go, and `InputValidationErrorStates` to show and hide them. States are applied without transitions. A template that omits either group is ignored silently.

### The error presenter

When the control first gets errors, the framework looks for a `ContentPresenter` named `ErrorPresenter` in the template and fills it with the content of the control's `Validation.ErrorTemplate`. The data context of that content is the control itself, so it binds to `(input:Validation.Errors)` directly. Nothing is displayed when `Validation.ErrorTemplate` is not set.

`Validation.Kind` decides how the content is shown:

- `Compact` (and `Auto`, the default) places an error icon in the presenter, with the `ErrorTemplate` content in the icon's tooltip.
- `Inline` places the `ErrorTemplate` content in the presenter directly.

Set `Validation.Kind` before errors appear. Changing it later updates the visual states, but the presenter keeps its current content until the control next goes from no errors to having errors.

### Example

The following excerpt adds error display to a copy of the `TextBox` template. Only the parts related to validation are shown: a fourth row for inline errors, a third column for the compact icon, the presenter, and the two state groups. The existing elements keep their rows and columns.

```xml
<Page.Resources>
    <DataTemplate x:Key="ErrorListTemplate">
        <ItemsControl ItemsSource="{Binding (input:Validation.Errors)}">
            <ItemsControl.ItemTemplate>
                <DataTemplate>
                    <TextBlock Text="{Binding ErrorMessage}"
                               Foreground="{ThemeResource SystemControlErrorTextForegroundBrush}" />
                </DataTemplate>
            </ItemsControl.ItemTemplate>
        </ItemsControl>
    </DataTemplate>

    <Style x:Key="ValidatingTextBoxStyle" TargetType="TextBox">
        <Setter Property="input:Validation.Mode" Value="Auto" />
        <Setter Property="input:Validation.ErrorTemplate" Value="{StaticResource ErrorListTemplate}" />
        <Setter Property="Template">
            <Setter.Value>
                <ControlTemplate TargetType="TextBox">
                    <Grid>
                        <Grid.RowDefinitions>
                            <RowDefinition Height="Auto" /> <!-- header -->
                            <RowDefinition Height="*" />    <!-- input -->
                            <RowDefinition Height="Auto" /> <!-- description -->
                            <RowDefinition Height="Auto" /> <!-- inline errors -->
                        </Grid.RowDefinitions>
                        <Grid.ColumnDefinitions>
                            <ColumnDefinition Width="*" />
                            <ColumnDefinition Width="Auto" /> <!-- delete button -->
                            <ColumnDefinition Width="Auto" /> <!-- compact error icon -->
                        </Grid.ColumnDefinitions>

                        <VisualStateManager.VisualStateGroups>
                            <!-- ...the existing CommonStates and ButtonStates groups... -->

                            <VisualStateGroup x:Name="InputValidationEnabledStates">
                                <VisualState x:Name="ValidationDisabled" />
                                <VisualState x:Name="CompactValidationEnabled">
                                    <VisualState.Setters>
                                        <Setter Target="ErrorPresenter.(Grid.Row)" Value="1" />
                                        <Setter Target="ErrorPresenter.(Grid.Column)" Value="2" />
                                        <Setter Target="ErrorPresenter.(Grid.ColumnSpan)" Value="1" />
                                    </VisualState.Setters>
                                </VisualState>
                                <VisualState x:Name="InlineValidationEnabled">
                                    <VisualState.Setters>
                                        <Setter Target="ErrorPresenter.(Grid.Row)" Value="3" />
                                        <Setter Target="ErrorPresenter.(Grid.Column)" Value="0" />
                                        <Setter Target="ErrorPresenter.(Grid.ColumnSpan)" Value="3" />
                                    </VisualState.Setters>
                                </VisualState>
                            </VisualStateGroup>

                            <VisualStateGroup x:Name="InputValidationErrorStates">
                                <VisualState x:Name="ErrorsCleared" />
                                <VisualState x:Name="CompactErrors">
                                    <VisualState.Setters>
                                        <Setter Target="ErrorPresenter.Visibility" Value="Visible" />
                                    </VisualState.Setters>
                                </VisualState>
                                <VisualState x:Name="InlineErrors">
                                    <VisualState.Setters>
                                        <Setter Target="ErrorPresenter.Visibility" Value="Visible" />
                                    </VisualState.Setters>
                                </VisualState>
                            </VisualStateGroup>
                        </VisualStateManager.VisualStateGroups>

                        <!-- ...the existing header, border, content and description elements... -->

                        <ContentPresenter x:Name="ErrorPresenter"
                                          Grid.Row="3"
                                          Grid.ColumnSpan="3"
                                          Visibility="Collapsed"
                                          VerticalAlignment="Center" />
                    </Grid>
                </ControlTemplate>
            </Setter.Value>
        </Setter>
    </Style>
</Page.Resources>
```

> [!TIP]
> Keep the error states away from properties that `CommonStates` also sets, such as the border's `BorderBrush`. Two visual state groups that set the same property on the same element overwrite each other, and a `CommonStates` change such as pointer-over clears the error color. Placing error visuals on their own elements, like `ErrorPresenter` above, avoids the conflict.

A custom control that drives its own visual states can call the protected `Control.UpdateValidationStates()` method to re-apply the validation states, for example after resetting its other groups. The framework already applies them when the errors, `Validation.Mode` or `Validation.Kind` change, and when the template is applied.

## Validating other properties and controls

The validated property of a control type is resolved once and cached. It can be declared in two ways.

**With an attribute on your own control.** Apply `InputValidationPropertyAttribute`, from the `Uno.UI.Xaml.Controls` namespace, with the name of the dependency property to validate:

```csharp
[Uno.UI.Xaml.Controls.InputValidationProperty(nameof(Value))]
public partial class RatingControl : Control
{
    public static DependencyProperty ValueProperty { get; } = DependencyProperty.Register(
        nameof(Value), typeof(int), typeof(RatingControl), new PropertyMetadata(0));

    public int Value
    {
        get => (int)GetValue(ValueProperty);
        set => SetValue(ValueProperty, value);
    }
}
```

The attribute is inherited: a class deriving from `TextBox` validates `Text` without declaring anything, and a derived class that applies its own attribute replaces its base's.

**With a registration**, for a control you can't modify, or to change the property of a built-in control. Write to `FeatureConfiguration.InputValidation.ValidationProperties` at startup, before the controls concerned are bound:

```csharp
// Validate a third-party control
FeatureConfiguration.InputValidation.ValidationProperties[typeof(ThirdPartyEntry)] = ThirdPartyEntry.TextProperty;

// Validate SelectedIndex instead of SelectedItem on every ComboBox
FeatureConfiguration.InputValidation.ValidationProperties[typeof(ComboBox)] = Selector.SelectedIndexProperty;

// Opt a type out of the property it would otherwise inherit
FeatureConfiguration.InputValidation.ValidationProperties[typeof(SearchTextBox)] = null;
```

A registration applies to every instance of the type, and replaces the validated property rather than adding one. A registration made after a control is bound only takes effect the next time the property is bound. `ValidationProperties.Remove(type)` returns a type to its attribute. Access the map from the UI thread.

## API summary

Attached properties of `Uno.Extras.Input.Validation`, available on any `Control`:

| Property | Type | Default | Set by |
|---|---|---|---|
| `Mode` | `InputValidationMode` | `Disabled` | your app |
| `Kind` | `InputValidationKind` | `Auto` | your app |
| `ErrorTemplate` | `DataTemplate` | `null` | your app |
| `HasErrors` | `bool` | `false` | the framework |
| `Errors` | `IObservableVector<InputValidationError>` | created on demand | the framework |

Related types:

- `Uno.Extras.Input.InputValidationMode`: `Auto`, `Default`, `Disabled`.
- `Uno.Extras.Input.InputValidationKind`: `Auto`, `Compact`, `Inline`.
- `Uno.Extras.Input.InputValidationError`: a single error, with its `ErrorMessage`.
- `Uno.UI.FeatureConfiguration.InputValidation`: the `IsEnabled` switch and the `ValidationProperties` map.
- `Uno.UI.Xaml.Controls.InputValidationPropertyAttribute`: declares the validated property of a control type.
