namespace Windows.UI.Xaml.Input;

/// <summary>
/// Derives from XamlUICommand, adding a set of standard platform commands with pre-defined properties.
/// </summary>
public partial class StandardUICommand : XamlUICommand
{
	/// <summary>
	/// Initializes a new instance of the StandardUICommand class.
	/// </summary>
	public StandardUICommand()
	{
		PrepareState();
	}

	/// <summary>
	/// Initializes a new instance of the StandardUICommand class of the specified kind.
	/// </summary>
	/// <param name="kind"></param>
	public StandardUICommand(StandardUICommandKind kind) : this()
	{
		Kind = kind;
	}

	/// <summary>
	/// Gets the platform command (with pre-defined properties such as
	/// icon, keyboard accelerator, and description) that can be used
	/// with a StandardUICommand.
	/// </summary>
	public StandardUICommandKind Kind
	{
		get => (StandardUICommandKind)GetValue(KindProperty);
		set => SetValue(KindProperty, value);
	}

	/// <summary>
	/// Identifies the Kind dependency property.
	/// </summary>
	public static DependencyProperty KindProperty { get; } =
		DependencyProperty.Register(
			nameof(Kind),
			typeof(StandardUICommandKind),
			typeof(StandardUICommand),
			new FrameworkPropertyMetadata(default(StandardUICommandKind)));
}
