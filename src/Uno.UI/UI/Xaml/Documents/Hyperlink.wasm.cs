using System;
using System.Linq;
using Uno.UI;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;

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
			OnUnderlineStyleChanged();
		}

		#region NavigationTarget DependencyProperty
		private const NavigationTarget _defaultNavigationTarget = NavigationTarget.NewDocument;

		// TODO: This was changed from public to internal. We need a way to expose it.
		// See https://github.com/unoplatform/uno/issues/14074 for info.
		internal NavigationTarget NavigationTarget
		{
			get => (NavigationTarget)GetValue(NavigationTargetProperty);
			set => SetValue(NavigationTargetProperty, value);
		}

		internal static DependencyProperty NavigationTargetProperty { get; } = DependencyProperty.Register(
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
	}

	public enum NavigationTarget
	{
		NewDocument,
		SameDocument
	}
}
