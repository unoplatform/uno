using Uno.UI.DataBinding;

namespace Windows.UI.Xaml.Controls;

/// <summary>
/// Provides data for the TextChanged event.
/// </summary>
public partial class AutoSuggestBoxTextChangedEventArgs : DependencyObject
{
	private string _originalText;
	private ManagedWeakReference _owner;
	private int _counter = 0;

	/// <summary>
	/// Initializes a new instance of the AutoSuggestBoxTextChangedEventArgs class.
	/// </summary>
	public AutoSuggestBoxTextChangedEventArgs() : base()
	{
		Reason = AutoSuggestionBoxTextChangeReason.ProgrammaticChange;
	}

	/// <summary>
	/// Returns a Boolean value indicating if the current value of the
	/// TextBox is unchanged from the point in time when the TextChanged event was raised.
	/// </summary>
	/// <returns>
	/// Indicates if the current value of the TextBox is unchanged from the point in time
	/// when the TextChanged event was raised.
	/// </returns>
	public bool CheckCurrent()
	{
		if (!_owner.IsAlive || _owner.Target is not AutoSuggestBox owner)
		{
			return false;
		}

		return owner.GetTextChangedEventCounter() == _counter;
	}

	/// <summary>
	/// Gets or sets a value that indicates the reason for the text changing in the AutoSuggestBox.
	/// </summary>
	public AutoSuggestionBoxTextChangeReason Reason
	{
		get => (AutoSuggestionBoxTextChangeReason)this.GetValue(ReasonProperty);
		set => this.SetValue(ReasonProperty, value);
	}

	/// <summary>
	/// Identifies the Reason dependency property.
	/// </summary>
	public static DependencyProperty ReasonProperty { get; } =
		DependencyProperty.Register(
			nameof(Reason),
			typeof(AutoSuggestionBoxTextChangeReason),
			typeof(AutoSuggestBoxTextChangedEventArgs),
			new FrameworkPropertyMetadata(default(AutoSuggestionBoxTextChangeReason)));

	internal void SetCounter(int counter) => _counter = counter;

	internal void SetOwner(AutoSuggestBox owner) => _owner = WeakReferencePool.RentWeakReference(this, owner);
}
