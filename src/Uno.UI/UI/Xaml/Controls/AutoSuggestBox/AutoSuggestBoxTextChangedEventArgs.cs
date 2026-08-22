// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference src\dxaml\xcp\dxaml\lib\AutoSuggestBoxTextChangedEventArgs_Partial.h, tag winui3/release/1.7.1, commit 5f27a786

using System;

namespace Microsoft.UI.Xaml.Controls;

/// <summary>
/// Provides data for the TextChanged event.
/// </summary>
public partial class AutoSuggestBoxTextChangedEventArgs : DependencyObject
{
	private WeakReference<AutoSuggestBox> _owner;
	private uint _counter;

	/// <summary>
	/// Gets or sets a value that indicates why the text content of the AutoSuggestBox changed.
	/// </summary>
	public AutoSuggestionBoxTextChangeReason Reason
	{
		get => (AutoSuggestionBoxTextChangeReason)GetValue(ReasonProperty);
		set => SetValue(ReasonProperty, value);
	}

	/// <summary>
	/// Identifies the Reason dependency property.
	/// </summary>
	public static DependencyProperty ReasonProperty { get; } =
		DependencyProperty.Register(
			nameof(Reason),
			typeof(AutoSuggestionBoxTextChangeReason),
			typeof(AutoSuggestBoxTextChangedEventArgs),
			new FrameworkPropertyMetadata(AutoSuggestionBoxTextChangeReason.ProgrammaticChange));

	/// <summary>
	/// Sets the owner AutoSuggestBox.
	/// </summary>
	internal void SetOwner(AutoSuggestBox owner) => _owner = new WeakReference<AutoSuggestBox>(owner);

	/// <summary>
	/// Initializes a new instance of the AutoSuggestBoxTextChangedEventArgs class.
	/// </summary>
	public AutoSuggestBoxTextChangedEventArgs() : base()
	{
	}

	/// <summary>
	/// Sets the counter value used for CheckCurrent comparison.
	/// </summary>
	internal void SetCounter(uint counter)
	{
		_counter = counter;
	}

	/// <summary>
	/// Returns a Boolean value indicating if the current value of the TextBox is unchanged from the point in time when the TextChanged event was raised.
	/// </summary>
	/// <returns>True if the text is unchanged; otherwise, false.</returns>
	/// <remarks>
	/// This method uses a counter-based approach matching the WinUI implementation.
	/// The counter is incremented each time the text changes, and CheckCurrent compares
	/// the counter at the time of event creation with the current counter.
	/// </remarks>
	public bool CheckCurrent()
	{
#if HAS_UNO
		if (_owner?.TryGetTarget(out var owner) == true)
		{
			// The counter comparison approach matches WinUI behavior.
			// If the counter hasn't changed since this event was created,
			// then the current text value is the same as when this event was raised.
			return _counter == owner.GetTextChangedCounter();
		}
#endif
		return false;
	}
}
