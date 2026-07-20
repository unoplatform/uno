#if __APPLE_UIKIT__ || __ANDROID__
#define HAS_NATIVE_COMMANDBAR
#endif

#nullable enable

using DirectUI;
#if HAS_NATIVE_COMMANDBAR
using Uno.UI.Controls;
#endif

namespace Microsoft.UI.Xaml.Controls;

partial class AppBar : IBackButtonListener
#if HAS_NATIVE_COMMANDBAR
	, ICustomClippingElement
#endif
{
	// Only ever true when the native CommandBar template is applied (iOS/Android), in which case the
	// native presenter drives measure and clipping instead of the managed AppBar logic.
#pragma warning disable CS0649 // Never assigned on targets without a native CommandBar.
	private bool _isNativeTemplate;
#pragma warning restore CS0649

	bool IBackButtonListener.OnBackButtonPressed()
	{
		OnBackButtonPressedImpl(out var handled);
		return handled;
	}

#if HAS_NATIVE_COMMANDBAR
	bool ICustomClippingElement.AllowClippingToLayoutSlot => !_isNativeTemplate;

	bool ICustomClippingElement.ForceClippingToLayoutSlot => false;
#endif

	private void UpdateIsNativeTemplate()
	{
#if HAS_NATIVE_COMMANDBAR
		_isNativeTemplate = Uno.UI.Extensions.DependencyObjectExtensions
			.FindFirstChild<NativeCommandBarPresenter?>(this) != null;
#endif
	}
}
