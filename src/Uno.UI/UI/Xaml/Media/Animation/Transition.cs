using System;
using System.Collections.Generic;
using System.Text;
using Uno.Extensions;
using Uno.Disposables;
#if __ANDROID__
using View = Android.Views.View;
#elif __APPLE_UIKIT__
using View = UIKit.UIView;
#endif

namespace Microsoft.UI.Xaml.Media.Animation
{
	/// <summary>
	/// Transition : Based on WinRT Transition
	/// (https://msdn.microsoft.com/en-us/library/windows/apps/windows.ui.xaml.media.animation.transition.Aspx)
	/// </summary>
	public partial class Transition : DependencyObject
	{
		private Transform _elementTransform;

		public Transition()
		{
			InitializeBinder();
		}

		/// <summary>
		/// Called from the Transitions Property of IFrameworkElement
		/// Attaches to the Loaded event
		/// </summary>
		/// <param name="element">Framework element to apply the transition to</param>
		internal virtual void AttachToElement(IFrameworkElement element)
		{
			element.Loaded += OnElementLoaded;
		}

		/// <summary>
		/// Called from the Transitions Property of IFrameworkElement
		/// </summary>
		/// <param name="element">Framework element to stop applying the transition to</param>
		internal virtual void DetachFromElement(IFrameworkElement element)
		{
			element.Loaded -= OnElementLoaded;
		}

		/// <summary>
		/// When the Element is loaded - Start the Transition
		/// </summary>        
		private void OnElementLoaded(object sender, RoutedEventArgs e)
		{
			var sb = new Storyboard(); //Create a Storyboard, attach the Transitions and Start the transition
			AttachToStoryboardAnimation(sb, (IFrameworkElement)sender, TimeSpan.Zero);
			sb.Begin();
		}

		/// <summary>
		/// Called from the ChildTransistions Property of a Panel
		/// Attaches to the Completed event - Keeps the starting RenderTansform on the FrameworkElement and restores it;
		/// </summary>
		/// <param name="storyboard">The Panel may run several Transitions on the same Storyboard</param>
		/// <param name="element">FrameworkElement to Animate</param>
		/// <param name="beginTime">Used for Staggering (default Timespan.Zero)</param>
		/// <param name="xOffset">Used to determine the x axis renderTransform translation. The previous position of the IFrameworkElement in a List (Only used in reposition)</param>
		/// <param name="yOffset">Used to determine the y axis renderTransform translation. The previous position of the IFrameworkElement in a List (Only used in reposition)</param>
		internal virtual void AttachToStoryboardAnimation(Storyboard storyboard, IFrameworkElement element, TimeSpan beginTime, int xOffset = 0, int yOffset = 0)
		{
			PushRenderTransform(element);

			storyboard.Completed += (s, e) => PopRenderTransform(element);
		}

		/// <summary>
		/// Restores the Stored Transformation
		/// </summary>
		private void PopRenderTransform(IFrameworkElement element)
		{
			(element).RenderTransform = _elementTransform;
		}

		/// <summary>
		/// Stores the elements Transformation - to be restored in the future
		/// </summary>
		private void PushRenderTransform(IFrameworkElement element)
		{
			_elementTransform = element.RenderTransform;
		}
	}
}


