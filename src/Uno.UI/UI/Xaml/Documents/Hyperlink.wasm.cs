using System;
using System.Linq;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Input;

namespace Windows.UI.Xaml.Documents
{
	partial class Hyperlink
	{
		public Hyperlink() : base("a")
		{
			UpdateNavigationProperties(null, _defaultNavigationTarget);

			PointerPressed += TextBlock.OnPointerPressed;
			PointerReleased += TextBlock.OnPointerReleased;
			PointerCaptureLost += TextBlock.OnPointerCaptureLost;
		}

		#region NavigationTarget DependencyProperty
		private const NavigationTarget _defaultNavigationTarget = NavigationTarget.NewDocument;

		public NavigationTarget NavigationTarget
		{
			get => (NavigationTarget)GetValue(NavigationTargetProperty);
			set => SetValue(NavigationTargetProperty, value);
		}

		public static DependencyProperty NavigationTargetProperty { get ; } = DependencyProperty.Register(
			"NavigationTarget",
			typeof(NavigationTarget),
			typeof(Hyperlink),
			new FrameworkPropertyMetadata(_defaultNavigationTarget, (s, e) => ((Hyperlink)s).OnNavigationTargetChanged(e)));

		private void OnNavigationTargetChanged(DependencyPropertyChangedEventArgs e)
			=> UpdateNavigationProperties(NavigateUri, (NavigationTarget)e.NewValue);
		#endregion

		partial void OnNavigateUriChangedPartial(Uri newNavigateUri)
			=> UpdateNavigationProperties(newNavigateUri, NavigationTarget);

		private void UpdateNavigationProperties(Uri navigateUri, NavigationTarget target)
		{
			var uri = navigateUri?.OriginalString;
			if (string.IsNullOrWhiteSpace(uri))
			{
				SetAttribute(
					("target", ""),
					("href", "#") // Required to get the native hover visual state
				);
			}
			else if (target == NavigationTarget.NewDocument)
			{
				SetAttribute(
					("target", "_blank"),
					("href", uri)
				);
			}
			else
			{
				SetAttribute(
					("target", ""),
					("href", uri)
				);
			}
			UpdateHitTest();
		}

		internal override bool IsViewHit()
			=> NavigateUri != null || base.IsViewHit();

		public new event global::Windows.UI.Xaml.RoutedEventHandler GotFocus
		{
			add => base.GotFocus += value;
			remove => base.GotFocus -= value;
		}

		public new event global::Windows.UI.Xaml.RoutedEventHandler LostFocus
		{
			add => base.LostFocus += value;
			remove => base.LostFocus -= value;
		}

#if HAS_UNO_WINUI
		// The properties below have moved to UIElement in WinUI, but Hyperlink does not inherit from UIElement and does in Wasm.
		// This makes the properties move down incorrectly.
		// This section places those properties at the same location as the reference implementation.

		public new bool IsTabStop
		{
			get => base.IsTabStop;
			set => base.IsTabStop = value;
		}

		public static new DependencyProperty IsTabStopProperty { get; } = UIElement.IsTabStopProperty;

		public new FocusState FocusState
		{
			get => base.FocusState;
			set => base.FocusState = value;
		}

		public static new DependencyProperty FocusStateProperty { get; } = UIElement.FocusStateProperty;
#endif
	}

	public enum NavigationTarget
	{
		NewDocument,
		SameDocument
	}
}
