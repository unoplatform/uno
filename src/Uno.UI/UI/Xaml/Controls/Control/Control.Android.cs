using Android.Views;
using System.Linq;
using Uno.UI;

namespace Windows.UI.Xaml.Controls
{
	public partial class Control
	{
		public Control()
		{
			InitializeControl();

			// Android elevation shadows require Views to have a background in order to be cast.
			// Because Controls don't have backgrounds (the background element is part of their template),
			// we cast elevation shadows using their rectangular bounds.
			// Note that because it uses the rectangular bounds of the Control, it won't consider rounded corners.
			if (Android.OS.Build.VERSION.SdkInt >= Android.OS.BuildVersionCodes.Lollipop)
			{
				OutlineProvider = ViewOutlineProvider.Bounds;
			}
		}

		partial void UnregisterSubView()
		{
			if (ChildCount > 0)
			{
				RemoveViewAt(0);
			}
		}

		partial void RegisterSubView(View child)
		{
			AddView(child);
		}

		/// <summary>
		/// Gets the first sub-view of this control or null if there is none
		/// </summary>
		internal IFrameworkElement GetTemplateRoot()
		{
			return this.GetChildren()?.FirstOrDefault() as IFrameworkElement;
		}
	}
}
