using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Input;
using Uno.Disposables;

namespace Microsoft/* UWP don't rename */.UI.Xaml.Controls;

partial class NumberBox
{
	private readonly SerialDisposable _eventSubscriptions = new();

	public InputScope InputScope
	{
		get => (InputScope)GetValue(InputScopeProperty);
		set => SetValue(InputScopeProperty, value);
	}

	public static DependencyProperty InputScopeProperty { get; } =
		DependencyProperty.Register(nameof(InputScope), typeof(InputScope), typeof(NumberBox), new FrameworkPropertyMetadata(null));

	public TextAlignment TextAlignment
	{
		get => (TextAlignment)GetValue(TextAlignmentProperty);
		set => SetValue(TextAlignmentProperty, value);
	}

	public static DependencyProperty TextAlignmentProperty { get; } =
		DependencyProperty.Register(nameof(TextAlignment), typeof(TextAlignment), typeof(NumberBox), new FrameworkPropertyMetadata(TextAlignment.Left));

	private void ReApplyTemplate()
	{
		// No need to reapply template on initial load.
		if (_eventSubscriptions.Disposable is null)
		{
			InitializeTemplate();
		}
	}

	private void DisposeRegistrations() => UnhookEvents();
}
