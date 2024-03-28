using Uno.Extensions;
using Windows.UI.Xaml.Controls;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Core;
#if __ANDROID__
using Android.Views;
using View = Android.Views.View;
using Point = Android.Graphics.Point;
#elif __IOS__
using UIKit;
using View = UIKit.UIView;
using Point = System.Drawing.PointF;
#else
using View = Windows.UI.Xaml.UIElement;
#endif

namespace Windows.UI.Xaml.Media.Animation
{
	internal class PanelTransitionHelper
	{
		readonly Panel _source;
		Storyboard _onLoadedStoryboard;
		bool _onLoadedisAnimating;
		bool _onUpdatedIsAnimating;

		Storyboard _onUpdatedStoryboard;

		Dictionary<View, Point> _childsInitialPositions = new Dictionary<View, Point>();
		List<ChildWithOffset> _modifiedChildWithOffset = new List<ChildWithOffset>();
		List<IFrameworkElement> _previouslyAddedElements = new List<IFrameworkElement>();


		List<IFrameworkElement> _elements = new List<IFrameworkElement>();

		internal PanelTransitionHelper(Panel source)
		{
			_source = source;
		}

		internal void SetInitialChildrenPositions()
		{
			_childsInitialPositions = GetChildrenPositionsFromSource();
		}

		private Dictionary<View, Point> GetChildrenPositionsFromSource()
		{
			var elements = new Dictionary<View, Point>();
#if __ANDROID__
			foreach (View item in _source.Children)
			{
				elements.Add(item, new Point((int)item.GetX(), (int)item.GetY()));
			}
#elif __IOS__
			foreach (View item in _source.Children)
			{
				var x = (int)item.Frame.X;
				var y = (int)item.Frame.Y;
				elements.Add(item, new Point(x, y));
			}
#endif
			return elements;
		}

		internal void AddElement(IFrameworkElement element)
		{
			if (element.Transitions.Safe().Any())
			{
				return;
			}

			_elements.Add(element);
			//This collection is created to ensure we do not add LayoutUpdatedTransitions to the newly added elements
			_previouslyAddedElements.Add(element);

			//Hide the view before animation starts otherwise it will be seen as soon as its laid out
#if __IOS__
			((UIKit.UIView)element).Hidden = true;
#elif __ANDROID__
			((Android.Views.View)element).Visibility = Android.Views.ViewStates.Invisible;
#endif

			if (_onLoadedisAnimating)
			{
				return;
			}

			_onLoadedisAnimating = true;

			_ = CoreDispatcher.Main.RunAsync(
				CoreDispatcherPriority.Normal,
				() =>
				{
					RunOnLoadedAnimations();
					_onLoadedisAnimating = false;
				}
			);
		}

		private void RunOnLoadedAnimations()
		{

			if (_onLoadedStoryboard != null)
			{
				_onLoadedStoryboard.Stop();
			}

			_onLoadedStoryboard = new Storyboard();

			var beginTime = TimeSpan.Zero;

			foreach (var child in _source.Children.OfType<IFrameworkElement>())
			{


				//Restore the view before animating
#if __IOS__
				((UIKit.UIView)child).Hidden = false;
#elif __ANDROID__
				((Android.Views.View)child).Visibility = Android.Views.ViewStates.Visible;
#endif

				if (child.Transitions.Safe().Any() || !_elements.Contains(child))
				{
					continue;
				}

				foreach (var transition in _source.ChildrenTransitions)
				{

					//Skip Reposition theme Transition on Onloaded. We do not want to Attach RepositionThemeTransition when the elements are added.
					if (transition is RepositionThemeTransition)
					{
						continue;
					}

					transition.AttachToStoryboardAnimation(_onLoadedStoryboard, child, beginTime);


				}

				beginTime = beginTime.Add(TimeSpan.FromMilliseconds(100));//increment beginTime

			}

			_elements.Clear();

			_onLoadedStoryboard.Begin();
		}

		internal void LayoutUpdatedTransition()
		{
			var elementsCurrentPositions = GetChildrenPositionsFromSource();

			foreach (var childPosition in _childsInitialPositions)
			{
				//If the elements were just added do not set the the layoutUpdated transition
				if (_previouslyAddedElements.Contains(childPosition.Key as IFrameworkElement))
				{
					continue;
				}

				var newChildPosition = elementsCurrentPositions.First(pair => pair.Key == childPosition.Key);

				var childElement = newChildPosition.Key as IFrameworkElement;

				//if the element has a transitions inside or it is was removed. Do not add it to the collection of transitions.
				if (childElement == null || childElement.Transitions.Safe().Any())
				{
					continue;
				}

				var xOffset = (int)(childPosition.Value.X - newChildPosition.Value.X);
				var yOffset = (int)(childPosition.Value.Y - newChildPosition.Value.Y);

				//Add Items to the collection of transitions only if they have changed positions.
				if (xOffset != 0 || yOffset != 0)
				{
					var childWithOffset = new ChildWithOffset() { Element = childPosition.Key, OffsetX = xOffset, OffsetY = yOffset };
					_modifiedChildWithOffset.Add(childWithOffset);

					//Set transformation on rendertransform of child before the animation takes place.
					childElement.RenderTransform = new TranslateTransform()
					{
						X = xOffset,
						Y = yOffset
					};
				}

			}

			if (_onUpdatedIsAnimating)
			{
				return;
			}

			_onUpdatedIsAnimating = true;

			_ = CoreDispatcher.Main.RunAsync(
				CoreDispatcherPriority.Normal,
				() =>
				{
					RunLayoutUpdatedAnimations();
					_onUpdatedIsAnimating = false;
				}
			);

		}

		private void RunLayoutUpdatedAnimations()
		{
			_onUpdatedStoryboard = new Storyboard();

			var beginTime = TimeSpan.Zero;

			foreach (var child in _modifiedChildWithOffset)
			{

				var repositionTransition = _source.ChildrenTransitions.First(t => t is RepositionThemeTransition);

				repositionTransition.AttachToStoryboardAnimation(_onUpdatedStoryboard, (IFrameworkElement)child.Element, beginTime, child.OffsetX, child.OffsetY);

				beginTime = beginTime.Add(TimeSpan.FromMilliseconds(40));//increment beginTime for staggering

			}

			_modifiedChildWithOffset.Clear();
			_previouslyAddedElements.Clear();
			_onUpdatedStoryboard.Begin();
		}

		private class ChildWithOffset
		{
			public View Element
			{
				get; set;
			}
			public int OffsetX
			{
				get; set;
			}
			public int OffsetY
			{
				get; set;
			}
		}
	}
}
