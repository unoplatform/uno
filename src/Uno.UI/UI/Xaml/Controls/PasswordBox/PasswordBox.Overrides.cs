#nullable enable

using Microsoft.UI.Xaml.Input;

namespace Microsoft.UI.Xaml.Controls;

partial class PasswordBox
{
	// Mirrors TextBox.Overrides.skia.cs: the core cannot inherit these, so it exposes them as internal
	// methods and the control routes to them, calling base first.

	protected override void OnPointerMoved(PointerRoutedEventArgs e)
	{
		base.OnPointerMoved(e);

		_core.OnPointerMoved(e);
	}

	protected override void OnRightTapped(RightTappedRoutedEventArgs e)
	{
		base.OnRightTapped(e);

		_core.OnRightTapped(e);
	}

	protected override void OnDoubleTapped(DoubleTappedRoutedEventArgs args)
	{
		base.OnDoubleTapped(args);

		_core.OnDoubleTapped(args);
	}

	protected override void OnBringIntoViewRequested(BringIntoViewRequestedEventArgs e)
	{
		base.OnBringIntoViewRequested(e);

		_core.OnBringIntoViewRequested(e);
	}

	private protected override void OnContextRequestedImpl(ContextRequestedEventArgs args)
	{
		if (!_core.OnContextRequestedImpl(args))
		{
			base.OnContextRequestedImpl(args);
		}
	}

	internal override bool IsDelegatingFocusToTemplateChild() => _core.IsDelegatingFocusToTemplateChild();
}
