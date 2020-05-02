using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Extensions.Logging;
using UIKit;
using Uno.Extensions;
using Uno.UI.Helpers;
using Uno.Logging;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Navigation;
using Windows.UI.Xaml.Media.Animation;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Threading.Tasks;

namespace Uno.UI.Controls
{
	public partial class NativeFramePresenter : FrameworkElement
	{
		private Frame _frame;
		private ControllerDelegate _controllerDelegate;
		private Queue<UIViewController> _requestedViewControllers = new Queue<UIViewController>();
		private UIViewController _currentViewController;

		public NativeFramePresenter()
		{
			NavigationController = new FrameNavigationController();

			SizeChanged += NativeFramePresenter_SizeChanged;

			// Hide the NavigationBar by default. Only show if navigating to a Page that contains a CommandBar.
			NavigationController.NavigationBarHidden = true;
		}

		internal protected override void OnTemplatedParentChanged(DependencyPropertyChangedEventArgs e)
		{
			base.OnTemplatedParentChanged(e);
			InitializeController(TemplatedParent as Frame);
		}

		/// <summary>
		/// Exposes the underlying <see cref="UINavigationController"/> instance used to display this frame presenter.
		/// </summary>
		public UINavigationController NavigationController { get; }

		private void InitializeController(Frame frame)
		{
			if (_frame == frame)
			{
				return;
			}

			_frame = frame;

			frame.Navigated += (s, e) => UpdateNavigationStack();
			if (frame.BackStack is ObservableCollection<PageStackEntry> backStack)
			{
				backStack.CollectionChanged += (s, e) => UpdateNavigationStack();
			}

			NavigationController.View.AutoresizingMask = UIViewAutoresizing.All;
			NavigationController.View.Frame = Frame;

			AddSubview(NavigationController.View);

			if (_frame.Content is Page startPage)
			{
				var viewController = new PageViewController(startPage);
				NavigationController.PushViewController(viewController, false);
			}

			_controllerDelegate = new ControllerDelegate(this);
			NavigationController.Delegate = _controllerDelegate;
		}

		private void NativeFramePresenter_SizeChanged(object sender, SizeChangedEventArgs args)
		{
			NavigationController.View.Frame = Frame;
		}

		private void UpdateNavigationStack()
		{
			var viewControllers = _frame
				.BackStack
				.Concat(_frame.CurrentEntry)
				.Distinct()
				.OfType<PageStackEntry>()
				.Select(entry => entry.Instance.FindViewController() ?? new PageViewController(entry.Instance))
				.ToArray();

			var requestedViewController = viewControllers.LastOrDefault();
			var isCurrentViewController = requestedViewController == NavigationController.TopViewController;
			var isLastRequestedViewController = _requestedViewControllers.FirstOrDefault() == requestedViewController;
			if ((isCurrentViewController || isLastRequestedViewController) &&
				viewControllers.SequenceEqual(NavigationController.ViewControllers)
			)
			{
				return;
			}

			var isAnimated = GetIsAnimated(_frame.CurrentEntry);
			var alreadyRequested = _requestedViewControllers.Contains(requestedViewController);
			_requestedViewControllers.Enqueue(requestedViewController);

			if (viewControllers.Length == NavigationController.ViewControllers.Length + 1 &&
				viewControllers.Take(viewControllers.Length - 1).SequenceEqual(NavigationController.ViewControllers) &&
				// Ensure not to call PushViewController if one has already been potentially called for the same controller and is still pending, since iOS gets all like this: "NSInvalidArgumentException Reason: Pushing the same view controller instance more than once is not supported "
				!alreadyRequested
			)
			{
				// Use Push/Pop when possible because they're animated more nicely by iOS
				NavigationController.PushViewController(requestedViewController, isAnimated);
			}
			else if (viewControllers.Length == NavigationController.ViewControllers.Length - 1 &&
				NavigationController.ViewControllers.Take(NavigationController.ViewControllers.Length - 1).SequenceEqual(viewControllers)
			)
			{
				NavigationController.PopViewController(isAnimated);
			}
			else
			{
				NavigationController.SetViewControllers(viewControllers, isAnimated);
			}
		}

		private bool GetIsAnimated(PageStackEntry entry)
		{
			return !(entry?.NavigationTransitionInfo is SuppressNavigationTransitionInfo);

			// TODO: Explicitly handle all navigation transitions:
			// - DrillInNavigationTransitionInfo
			// - NavigationTransitionInfo
			// - SlideNavigationTransitionInfo
			// - SuppressNavigationTransitionInfo
			// - EntranceNavigationTransitionInfo
			// - ContinuumNavigationTransitionInfo
			// - CommonNavigationTransitionInfo
		}

		private void DidShowViewController(UINavigationController navigationController, UIViewController viewController, bool animated)
		{
			bool isNavigatingToSameFrame = _currentViewController == viewController;
			_currentViewController = viewController;

			// Do not go back if this method was called back-to-back with the same
			// view controller (can occur when displaying modal windows)
			if (isNavigatingToSameFrame)
			{
				return;
			}

			// Here, we detect whether the newly pushed viewController was triggered by:
			// - Frame events (the viewController will be part of the _requestedViewControllers queue)
			// - UINavigationController user interactions (e.g., back button, back swipe)

			// Iterate through the _requestedViewControllers queue to find the newly pushed viewController
			while (_requestedViewControllers.Any())
			{
				if (_requestedViewControllers.Peek() == viewController)
				{
					// The viewController is next in queue, everything is in order.
					// We keep the viewController in the queue (Peek) in case DidShowViewController is called twice 
					// with the same viewController (this can happen when dismissing a modal view controller).
					return;
				}

				_requestedViewControllers.Dequeue();
			}

			// The viewController wasn't part of the _requestedViewControllers queue.
			// We assume the user manually triggered a back navigation on the UINavigationController (e.g., back button, back swipe).
			// We sync the state of the UINavigationController with the Frame.
			_frame.GoBack();
		}

		private partial class PageViewController : UIViewController
		{
			public PageViewController(Page page)
			{
				// Apply this check if we are compiling with the iOS 11 SDK or above
				if (!ScrollViewer.UseContentInsetAdjustmentBehavior)
				{
					// This is deprecated on iOS 11+, we only set it on older versions.
					AutomaticallyAdjustsScrollViewInsets = false;
				}

				// Allows Page content to extend under the UINavigationBar (even if opaque)
				ExtendedLayoutIncludesOpaqueBars = true;

				Page = page;
				View = Page;

				CommandBarHelper.PageCreated(this);
			}

			public override void ViewWillAppear(bool animated)
			{
				try
				{
					base.ViewWillAppear(animated);

					CommandBarHelper.PageWillAppear(this);
				}
				catch (Exception e)
				{
					Application.Current.RaiseRecoverableUnhandledException(e);
				}
			}

			public override void ViewDidDisappear(bool animated)
			{
				try
				{
					base.ViewDidDisappear(animated);

					CommandBarHelper.PageDidDisappear(this);
				}
				catch (Exception e)
				{
					Application.Current.RaiseRecoverableUnhandledException(e);
				}
			}

			public Page Page { get; }

			internal CommandBar GetCommandBar()
			{
				return Page.TopAppBar as CommandBar ?? Page.FindFirstChild<CommandBar>();
			}
		}

		private partial class FrameNavigationController : UINavigationController
		{
			private PageViewController LowerController => ViewControllers.Length > 1 ? ViewControllers[ViewControllers.Length - 2] as PageViewController : null;

			public FrameNavigationController() : base(typeof(UnoNavigationBar), typeof(UIToolbar))
			{
			}

			public override UIViewController PopViewController(bool animated)
			{
				var lowerCommandBar = LowerController?.GetCommandBar();
				var renderer = lowerCommandBar?.GetRenderer(() => (CommandBarRenderer)null);
				if (renderer != null)
				{
					// Set navigation bar properties for page about to become visible. This gives a nice animation and works around bug on 
					// iOS 11.2 where TitleTextAttributes aren't updated properly (https://openradar.appspot.com/37567828)
					renderer.Native = NavigationBar;
				}

				return base.PopViewController(animated);
			}
		}

		private class ControllerDelegate : UINavigationControllerDelegate
		{
			private readonly WeakReference _owner;

			public ControllerDelegate(NativeFramePresenter owner)
				=> _owner = new WeakReference(owner);

			private NativeFramePresenter Owner => _owner.Target as NativeFramePresenter;

			public override void DidShowViewController(UINavigationController navigationController, UIViewController viewController, bool animated)
			{
				try
				{
					Owner?.DidShowViewController(navigationController, viewController, animated);
				}
				catch (Exception e)
				{
					Application.Current.RaiseRecoverableUnhandledException(e);
				}
			}
		}
	}
}
