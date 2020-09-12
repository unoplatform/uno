using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using CoreGraphics;
using UIKit;
using Uno.Disposables;
using Uno.Extensions;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.ViewManagement;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media;

namespace Uno.UI.Controls
{
	internal partial class CommandBarNavigationItemRenderer : Renderer<CommandBar, UINavigationItem>
	{
		private static DependencyProperty NavigationCommandProperty = ToolkitHelper.GetProperty("Uno.UI.Toolkit.CommandBarExtensions", "NavigationCommand");
		private static DependencyProperty BackButtonTitleProperty = ToolkitHelper.GetProperty("Uno.UI.Toolkit.CommandBarExtensions", "BackButtonTitle");

		private TitleView _titleView;

		private SerialDisposable _visibilitySubscriptions;

		public CommandBarNavigationItemRenderer(CommandBar element) : base(element) { }

		protected override UINavigationItem CreateNativeInstance() => new UINavigationItem();

		protected override IEnumerable<IDisposable> Initialize()
		{
			_visibilitySubscriptions = new SerialDisposable();
			yield return _visibilitySubscriptions;

			// Content
			_titleView = new TitleView();
			_titleView.SetParent(Element);
			_titleView.RegisterParentChangedCallback(this, OnTitleViewParentChanged);
			yield return Disposable.Create(() => _titleView = null);

			// Commands
			VectorChangedEventHandler<ICommandBarElement> OnVectorChanged = (s, e) => RegisterCommandVisibilityAndInvalidate();
			Element.PrimaryCommands.VectorChanged += OnVectorChanged;
			Element.SecondaryCommands.VectorChanged += OnVectorChanged;
			yield return Disposable.Create(() => Element.PrimaryCommands.VectorChanged -= OnVectorChanged);
			yield return Disposable.Create(() => Element.SecondaryCommands.VectorChanged -= OnVectorChanged);

			// Properties
			yield return Element.RegisterDisposableNestedPropertyChangedCallback(
				(s, e) => RegisterCommandVisibilityAndInvalidate(),
				new[] { CommandBar.PrimaryCommandsProperty },
				new[] { CommandBar.ContentProperty },
				new[] { NavigationCommandProperty },
				new[] { NavigationCommandProperty, AppBarButton.VisibilityProperty },
				new[] { BackButtonTitleProperty }
			);

			RegisterCommandVisibilityAndInvalidate();
		}

		protected override void Render()
		{
			// Content
			Native.Title = Element.Content as string;
			Native.TitleView = Element.Content is UIElement
				? _titleView
				: null;
			_titleView.Child = Element.Content as UIElement;

			// PrimaryCommands
			Native.RightBarButtonItems = Element
				.PrimaryCommands
				.OfType<AppBarButton>()
				.Where(btn => btn.Visibility == Visibility.Visible && (((btn.Content as FrameworkElement)?.Visibility ?? Visibility.Visible) == Visibility.Visible))
				.Do(appBarButton => appBarButton.SetParent(Element)) // This ensures that Behaviors expecting this button to be in the logical tree work. 
				.Select(appBarButton => appBarButton.GetRenderer(() => new AppBarButtonRenderer(appBarButton)).Native)
				.Reverse()
				.ToArray();

			// CommandBarExtensions.NavigationCommand
			var navigationCommand = Element.GetValue(NavigationCommandProperty) as AppBarButton;
			if (navigationCommand?.Visibility == Visibility.Visible)
			{
				navigationCommand.SetParent(Element); // This ensures that Behaviors expecting this button to be in the logical tree work. 
				Native.LeftBarButtonItem = navigationCommand.GetRenderer(() => new AppBarButtonRenderer(navigationCommand)).Native;
			}
			else
			{
				Native.LeftBarButtonItem = null;
			}

			// CommandBarExtensions.BackButtonText	
			var backButtonText = Element.GetValue(BackButtonTitleProperty) as string;
			if (backButtonText != null)
			{
				Native.BackBarButtonItem = new UIBarButtonItem(backButtonText, UIBarButtonItemStyle.Plain, null);
			}
			else
			{
				Native.BackBarButtonItem = null;
			}
		}

		private void OnTitleViewParentChanged(object instance, object key, DependencyObjectParentChangedEventArgs args)
		{
			// Even though we set the CommandBar as the parent of the TitleView,
			// it will change to the native control when the view is added.
			// This control is the visual parent but is not a DependencyObject and will not propagate the DataContext.
			// In order to ensure the DataContext is propagated properly, we restore the CommandBar
			// parent that can propagate the DataContext.
			if (args.NewParent != Element && args.NewParent != null)
			{
				_titleView.SetParent(Element);
			}
		}

		private void RegisterCommandVisibilityAndInvalidate()
		{
			var disposables = Element
				.PrimaryCommands
				.OfType<AppBarButton>()
				.Select(command => command.RegisterDisposableNestedPropertyChangedCallback(
					(s, e) => Invalidate(),
					new[] { AppBarButton.VisibilityProperty },
					new[] { AppBarButton.ContentProperty, FrameworkElement.VisibilityProperty }
				));

			_visibilitySubscriptions.Disposable = new CompositeDisposable(disposables);

			Invalidate();
		}
	}

	internal partial class TitleView : Border
	{
		private bool _blockReentrantMeasure;
		private Size _childSize;
		private Size? _lastAvailableSize;

		public TitleView()
		{
			if (UIDevice.CurrentDevice.CheckSystemVersion(11, 0))
			{
				// Set the Frame to the full screen size so that the child can measure itself properly.
				// It will be constrained later on.
				var bounds = ApplicationView.GetForCurrentView().VisibleBounds;
				Frame = new CGRect(new CGPoint(0, 0), new CGSize(bounds.Width, bounds.Height));
			}
			else
			{
				// For iOS 9 and 10, we need to do weird stuff with the initial frame.
				// The 0 width: Prevents flickers
				// The 44 height: Gives a valid default size that will be reused (god knows why) even after setting the height later on.
				Frame = new CGRect(new CGPoint(0, 0), new CGSize(0, 44));
			}
		}

		protected override Size MeasureOverride(Size availableSize)
		{
			if (_blockReentrantMeasure)
			{
				// In some cases setting the Frame below can prompt iOS to call SizeThatFits with 0 height, which would screw up the desired 
				// size of children if we're within LayoutSubviews(). In this case simply return the existing measure.
				return _childSize;
			}

			try
			{
				_blockReentrantMeasure = true;

				_lastAvailableSize = availableSize;

				// By default, iOS will horizontally center the TitleView inside the UINavigationBar,
				// ignoring the size of the left and right buttons.

				_childSize = base.MeasureOverride(availableSize);

				if (Child is IFrameworkElement frameworkElement
					&& frameworkElement.HorizontalAlignment == HorizontalAlignment.Stretch)
				{
					// To make the content stretch horizontally (instead of being centered),
					// we can set HorizontalAlignment.Stretch on it.
					// This will force the TitleView to take all available horizontal space.
					_childSize.Width = availableSize.Width;
				}
				else
				{
					if (!_childSize.Width.IsNaN()
						&& !_childSize.Height.IsNaN()
						&& _childSize.Height != 0
						&& _childSize.Width != 0
						&& (Frame.Width != _childSize.Width
						|| Frame.Height != _childSize.Height))
					{
						// Set the frame size to the child size so that the OS centers properly.
						Frame = new CGRect(Frame.X, Frame.Y, _childSize.Width, _childSize.Height);
					}
				}

				return _childSize;
			}
			finally
			{
				_blockReentrantMeasure = false;
			}
		}

		public override CGRect Frame
		{
			get { return base.Frame; }
			set
			{
				if (!UIDevice.CurrentDevice.CheckSystemVersion(11, 0)
					// iOS likes to mix things up by calling SizeThatFits with zero Height for no apparent reason (eg when navigating back to a page). When
					// this happens, we need to remeasure with the correct size to ensure children are laid out correctly.
					|| (_lastAvailableSize?.Height == 0 && value.Height != 0))
				{
					// This allows text trimming when there are more AppBarButtons
					var availableSize = value.Size;
					base.MeasureOverride(availableSize);
				}

				base.Frame = value;
			}
		}
	}
}
