using System.Linq;

namespace Windows.UI.Xaml.Documents
{
	partial class Hyperlink
	{
		public Hyperlink() : base("a")
		{
			SetAttribute("target", "_blank");
		}

		#region NavigationTarget DependencyProperty

		public NavigationTarget NavigationTarget
		{
			get => (NavigationTarget)GetValue(NavigationTargetProperty);
			set => SetValue(NavigationTargetProperty, value);
		}

		// Using a DependencyProperty as the backing store for NavigationTarget.  This enables animation, styling, binding, etc...
		public static readonly DependencyProperty NavigationTargetProperty =
			DependencyProperty.Register("NavigationTarget", typeof(NavigationTarget), typeof(Hyperlink), new PropertyMetadata(NavigationTarget.NewDocument, (s, e) => ((Hyperlink)s)?.OnNavigationTargetChanged(e)));


		private void OnNavigationTargetChanged(DependencyPropertyChangedEventArgs e)
		{
			var newTarget = (NavigationTarget) e.NewValue;
			if (newTarget == NavigationTarget.NewDocument)
			{
				SetAttribute("target", "_blank");
			}
			else
			{
				SetAttribute("target", "");
			}
		}

		#endregion

		partial void OnNavigateUriChangedPartial()
		{
			SetAttribute("href", NavigateUri?.OriginalString ?? "");
			UpdateHitTest();
		}

		internal override bool IsViewHit() => NavigateUri != null || base.IsViewHit();
	}

	public enum NavigationTarget
	{
		NewDocument,
		SameDocument
	}
}
