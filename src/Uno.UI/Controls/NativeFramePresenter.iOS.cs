using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using UIKit;
using Uno.Extensions;
using Uno.UI.Helpers;
using Uno.Foundation.Logging;
using Windows.Foundation;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Navigation;
using Windows.UI.Xaml.Media.Animation;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Threading.Tasks;
using ObjCRuntime;

namespace Uno.UI.Controls
{
	public partial class NativeFramePresenter : FrameworkElement
	{
		/* Architecture
		 *
		 * This class deals with the navigations events from its Frame and UINavigationController.
		 * There are 2 main event routes: events from the Frame, and events from the UINavigationController.
		 * 
		 * 1. From the Frame
		 * 
		 *    - Frame.Navigating --> Frame.Navigated
		 *                       --> Frame.NavigationStopped
		 *                     
		 *    - Frame.BackStack.CollectionChanged
		 *
		 * 2. From the UINavigationController
		 * 
		 *    - UINavigationControllerDelegate.WillShowViewController --> UINavigationControllerDelegate.DidShowViewController
		 *
		 * 
		 * Scenarios:
		 * 
		 * 1. Someone uses Frame.Navigate() or Frame.GoBack().
		 *    1.1 Frame.Navigating is raised and a NavigationRequest is added in _frameToControllerRequests.
		 *    1.2 Frame.Navigated is raised and NavigationController.PushViewController() (or PopViewController) is called to replicate the Frame operation in the native view.
		 *    1.3 UINavigationControllerDelegate.WillShowViewController is called.
		 *    1.4 UINavigationControllerDelegate.DidShowViewController is called and the NavigationRequest is removed from _frameToControllerRequests.
		 *
		 * 2. Someone uses a native back swipe or the back button from a native CommandBar.
		 *    2.1 UINavigationControllerDelegate.WillShowViewController is called and _controllerToFrameRequest is instanciated.
		 *        While _controllerToFrameRequest isn't null, all Frame.Navigating events that don't correlated with a back navigation are cancelled.
		 *        Frame.GoBack is called from UINavigationControllerDelegate.WillShowViewController.
		 *    2.2 Frame.Navigating is raised.
		 *    2.3 Frame.Navigated is raised and _controllerToFrameRequest is marked as handled by the frame.
		 *    2.4 UINavigationControllerDelegate.WillShowViewController ends.
		 *    2.5 UINavigationControllerDelegate.DidShowViewController is called and _controllerToFrameRequest is set to null.
		 *
		 * 3. Someone removes an item from Frame.BackStack
		 *    3.1 Frame.BackStack.CollectionChanged is raised and NavigationController.SetViewController is called to replicate the change in the native view.
		 *    3.2 UINavigationControllerDelegate.WillShowViewController is called.
		 *    3.3 UINavigationControllerDelegate.DidShowViewController is called.
		 */

		private Frame _frame;
		private ControllerDelegate _controllerDelegate;

		/// <summary>
		/// The requests created by the Frame to replicate in the UINavigationController.
		/// The frame adds requests at the beginning of the list.
		/// The NavigationController removes requests at the end of the list.
		/// </summary>
		private readonly LinkedList<NavigationRequest> _frameToControllerRequests = new LinkedList<NavigationRequest>();

		/// <summary>
		/// The request created by the UINavigationController to replicate in the Frame.
		/// This is not a list because there can only be 1 at a time. That's because this type of request gets created from the WillShowViewController and deleted from DidShowViewController.
		/// On top of that, the associated frame operation runs synchronously from WillShowViewController.
		/// </summary>
		private NavigationRequest _controllerToFrameRequest;

		public NativeFramePresenter()
		{
			NavigationController = new FrameNavigationController();

			SizeChanged += NativeFramePresenter_SizeChanged;

			// Hide the NavigationBar by default. Only show if navigating to a Page that contains a CommandBar.
			NavigationController.NavigationBarHidden = true;
		}

		protected override Size MeasureOverride(Size availableSize)
			=> MeasureFirstChild(availableSize);

		protected override Size ArrangeOverride(Size finalSize)
			=> ArrangeFirstChild(finalSize);

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

			frame.Navigating += OnFrameNavigating;
			frame.Navigated += OnFrameNavigated;
			frame.NavigationStopped += OnFrameNavigationStopped;
			if (frame.BackStack is ObservableCollection<PageStackEntry> backStack)
			{
				backStack.CollectionChanged += OnFrameBackStackChanged;
			}

			NavigationController.View.AutoresizingMask = UIViewAutoresizing.All;
			NavigationController.View.Frame = Frame;

			AddSubview(NavigationController.View);

			if (_frame.Content is Page startPage)
			{
				// When the frame already has content, we add a NavigationRequest in the PageViewController's AssociatedRequests.
				// Not doing this results in log errors from WillShowViewController and DidShowViewController (the ones about AssociatedRequests being empty).
				// Then, we push the PageViewController without animations (because the page is already present in the Frame). 

				var pageViewController = new PageViewController(startPage);
				var navigationEventArgs = new NavigationEventArgs(
					_frame.CurrentEntry.Instance,
					NavigationMode.New,
					_frame.CurrentEntry.NavigationTransitionInfo,
					_frame.CurrentEntry.Parameter,
					_frame.CurrentEntry.SourcePageType,
					null
				);
				pageViewController.AssociatedRequests.Add(new NavigationRequest(_frame, navigationEventArgs));
				NavigationController.PushViewController(pageViewController, false);
			}

			_controllerDelegate = new ControllerDelegate(this);
			NavigationController.Delegate = _controllerDelegate;
		}

		private void NativeFramePresenter_SizeChanged(object sender, SizeChangedEventArgs args)
		{
			NavigationController.View.Frame = Frame;
		}

		/// <summary>
		/// This is called on <see cref="Frame.Navigating"/>.
		/// We use this handler to cancel the navigation when the request conflicts with the <see cref="NavigationController"/>.
		/// </summary>
		private void OnFrameNavigating(object sender, NavigatingCancelEventArgs e)
		{
			if (e.Cancel)
			{
				// If something cancelled the navigation, we simply ignore the event.
				return;
			}

			var frameRequest = new NavigationRequest(_frame, e);

			if (_controllerToFrameRequest != null)
			{
				// We get here when the UINavigationController initiated a navigation (like a back swipe) that is being executed by the Frame.
				if (NavigationRequest.Correlates(frameRequest, _controllerToFrameRequest))
				{
					// We queue the request so that we can handle it in OnFrameNavigated and ignore it in OnFrameBackStackChanged.
					_frameToControllerRequests.AddFirst(_controllerToFrameRequest);
				}
				else
				{
					// When the Frame's request doesn't matche the UINavigationController's request. We cancel the Frame's request.
					// Ex: The UINavigationController is doing a native back, but the Frame wants to go forward.
					//     This sequencing can happen when you press back during an ViewModel operation that usually ends with a navigation.
					e.Cancel = true;

					if (this.Log().IsEnabled(LogLevel.Debug))
					{
						this.Log().Debug("Cancelled frame navigating request because a native navigation is in progress.");
					}
				}
			}
			else
			{
				// We queue the request so that we can handle it in OnFrameNavigated and ignore it in OnFrameBackStackChanged.
				_frameToControllerRequests.AddFirst(frameRequest);
			}
		}

		/// <summary>
		/// This is called on <see cref="Frame.Navigated"/>.
		/// We use this handler to create requets for the <see cref="NavigationController"/>.
		/// </summary>
		private void OnFrameNavigated(object sender, NavigationEventArgs e)
		{
			// We create a request object from the current state. We only use this object to correlate it with existing requests.
			var request = new NavigationRequest(_frame, e);

			if (TryGetFirst(_frameToControllerRequests, out var frameRequest) && NavigationRequest.Correlates(request, frameRequest))
			{
				// Mark the request as handled by the frame because we're in the Navigated handler.
				frameRequest.WasHandledByFrame = true;

				if (frameRequest == _controllerToFrameRequest)
				{
					// If the request is the one created by the NavigationController, we don't have to do anything at this point.
					// The DidShowViewController method will simply remove it from the list once it gets called.
				}
				else
				{
					// Get the page from the event args.
					var page = e.Content as Page;

					// Use that page to get the native ViewController.
					var viewController = page.FindViewController() ?? new PageViewController(page);

					// If that ViewController is a PageViewController, we add the request to its list.
					(viewController as PageViewController)?.AssociatedRequests.Add(frameRequest);

					// We get the isAnimated flag from the transition info.
					var isAnimated = GetIsAnimated(frameRequest.TransitionInfo);

					switch (frameRequest.NavigationMode)
					{
						case NavigationMode.Back:
							if (this.Log().IsEnabled(LogLevel.Debug))
							{
								this.Log().Debug("Poping ViewController to replicate Frame's back navigation.");
							}
							NavigationController.PopViewController(isAnimated);
							break;
						case NavigationMode.Forward:
						case NavigationMode.New:
							if (this.Log().IsEnabled(LogLevel.Debug))
							{
								this.Log().Debug($"Pushing ViewController ({page.GetType().Name}) to replicate Frame's forward navigation.");
							}
							NavigationController.PushViewController(viewController, isAnimated);
							break;
						case NavigationMode.Refresh:
						default:
							// Refresh currently doesn't have an effect.
							break;
					}
				}
			}
			else
			{
				// We shouldn't get here because the frame events are synchronous.
				if (frameRequest == null)
				{
					this.Log().Error($"Can't process OnFrameNavigated because the request queue is empty.");
				}
				else
				{
					this.Log().Error($"Can't process OnFrameNavigated because the request in queue doesn't match the current request.");
				}
			}
		}

		/// <summary>
		/// This is called on <see cref="Frame.NavigationStopped"/>.
		/// We use this handler to remove requests cancelled by <see cref="NavigatingCancelEventArgs.Cancel"/>.
		/// </summary>
		private void OnFrameNavigationStopped(object sender, NavigationEventArgs e)
		{
			var request = new NavigationRequest(_frame, e);
			if (TryGetFirst(_frameToControllerRequests, out var frameToControllerRequest) && NavigationRequest.Correlates(request, frameToControllerRequest))
			{
				if (this.Log().IsEnabled(LogLevel.Debug))
				{
					this.Log().Debug("Aborted navigation request because the Frame.Navigating event was cancelled.");
				}

				_frameToControllerRequests.RemoveFirst();
			}
			else
			{
				// We shouldn't get here because the frame events are synchronous.
				this.Log().Error($"Can't process OnFrameNavigationStopped because the request in queue doesn't match the current request.");
			}
		}

		/// <summary>
		/// This is called on <see cref="Frame.BackStack"/> changed.
		/// We use this handler to detect BackStack manipulations (like removing previous pages) and reset the <see cref="UINavigationController.ViewControllers"/> when applicable.
		/// </summary>
		private void OnFrameBackStackChanged(object sender, NotifyCollectionChangedEventArgs e)
		{
			var collection = _frame.BackStack;
			if (CorrelatesNavigatingRequest())
			{
				if (this.Log().IsEnabled(LogLevel.Trace))
				{
					this.Log().Trace("Correlated Frame.BackStack changed event to Frame.Navigating event.");
				}

				// We don't do anything; the OnFrameNavigated method will deal with the Navigating event.
			}
			else
			{
				if (this.Log().IsEnabled(LogLevel.Trace))
				{
					this.Log().Trace("Detected Frame.BackStack change not related to Frame.Navigating event.");
				}

				// When someone manipulates the Frame's BackStack (like removing entries), we reflect those changes on the NavigationController.
				ForceFrameStateIntoNavigationController();
			}

			bool CorrelatesNavigatingRequest()
			{
				if (TryGetFirst(_frameToControllerRequests, out var frameRequest))
				{
					switch (e.Action)
					{
						case NotifyCollectionChangedAction.Add:
							// The only "Add" on the BackStack that can correlate a Navigating event is a forward navigation.

							// Check whether the Add is at the end of the list.
							var newItem = e.NewItems[0] as PageStackEntry;
							if (newItem != null
								&& e.NewStartingIndex == (collection.Count - 1)
								&& (frameRequest.NavigationMode == NavigationMode.New || frameRequest.NavigationMode == NavigationMode.Forward)
								&& newItem.SourcePageType == (frameRequest.BackStackPageTypes.Count == 0 ? null : frameRequest.BackStackPageTypes[frameRequest.BackStackPageTypes.Count - 1]))
							{
								return true;
							}
							else
							{
								// Any Insert operation isn't caused by the Frame's navigation methods.
								return false;
							}
						case NotifyCollectionChangedAction.Remove:
							// The only "Remove" on the BackStack that can correlate a Navigating event is a back navigation.

							if (e.OldStartingIndex == collection.Count
								&& frameRequest.NavigationMode == NavigationMode.Back)
							{
								return true;
							}
							else
							{
								return false;
							}
						default:
							return false;
					}
				}
				else
				{
					// If there were no Navigating event, then the BackStackChanged isn't caused by regular navigation.
					return false;
				}
			}
		}

		private void ForceFrameStateIntoNavigationController()
		{
			var viewControllers = _frame
				.BackStack
				.Concat(_frame.CurrentEntry)
				.Where(entry => entry != null)
				.Distinct()
				.OfType<PageStackEntry>()
				.Select(FindOrCreateViewController)
				.ToArray();

			if (!viewControllers.SequenceEqual(NavigationController.ViewControllers))
			{
				if (this.Log().IsEnabled(LogLevel.Debug))
				{
					this.Log().Debug("Resetting all ViewControllers based on Frame's state.");
				}

				// TODO: iOS 14 introduced a bug where calling this method is getting unpleasant results (https://developer.apple.com/forums/thread/656524). (https://developer.apple.com/forums/thread/656524)
				// A workaround for this is removing the animation as this seems to be related to the root cause.
				// Note: This change should not affect consumer navigation since this is only used when resetting the stack.
				if (UIDevice.CurrentDevice.CheckSystemVersion(14, 0))
				{
					NavigationController.SetViewControllers(viewControllers, animated: false);
				}
				else
				{
					NavigationController.SetViewControllers(viewControllers, animated: true);
				}
			}
		}

		private UIViewController FindOrCreateViewController(PageStackEntry entry)
		{
			if (entry.Instance is { } pageInstance)
			{
				return pageInstance.FindViewController() ?? new PageViewController(pageInstance);
			}

			var page = _frame.EnsurePageInitialized(entry);
			return new PageViewController(page);
		}

		private bool GetIsAnimated(NavigationTransitionInfo transitionInfo)
		{
			return !(transitionInfo is SuppressNavigationTransitionInfo);

			// TODO: Explicitly handle all navigation transitions:
			// - DrillInNavigationTransitionInfo
			// - NavigationTransitionInfo
			// - SlideNavigationTransitionInfo
			// - SuppressNavigationTransitionInfo
			// - EntranceNavigationTransitionInfo
			// - ContinuumNavigationTransitionInfo
			// - CommonNavigationTransitionInfo
		}

		private void WillShowViewController(UINavigationController navigationController, [Transient] UIViewController viewController, bool animated)
		{
			TraceViewControllers(nameof(WillShowViewController), viewController);

			if (!(viewController is PageViewController pageViewController))
			{
				// When the ViewController isn't a PageViewController, it means it doesn't have anything to do with the Frame.
				// It's possibly a modal ViewController.
				// We just ignore it.
				return;
			}

			var lastRequest = pageViewController.AssociatedRequests.LastOrDefault();
			if (lastRequest != null)
			{
				if (lastRequest.WasHandledByController)
				{
					// When the last request for this controller is already handled, it means the NavigationController is requesting something.
					// It means this method was not called as a result of a Frame operation, but rather a native operation.

					if (this.Log().IsEnabled(LogLevel.Trace))
					{
						this.Log().Trace("Detected native navigation.");
					}

					var frameControllers = _frame
						.BackStack
						.Concat(_frame.CurrentEntry)
						.Distinct()
						.OfType<PageStackEntry>()
						.Select(entry => entry.Instance.FindViewController() ?? new PageViewController(entry.Instance))
						.ToArray();

					// Check if the native operation is a native back
					if (frameControllers.Length - 1 == navigationController.ViewControllers.Length
						&& frameControllers.Take(frameControllers.Length - 1).SequenceEqual(navigationController.ViewControllers))
					{
						var coordinator = navigationController.TopViewController?.GetTransitionCoordinator();
						var isBackSwipe = coordinator != null && coordinator.InitiallyInteractive;

						// Assigning this field will prevent new navigations request (except this one) from processing in Frame.Navigating 
						_controllerToFrameRequest = new NavigationRequest(_frame, pageViewController);

						if (isBackSwipe)
						{
							HandleBackSwipe(coordinator);
						}
						else
						{
							// If the back isn't a swipe, it's probably the CommandBar's back.
							RequestFrameBack();
						}
					}
					else
					{
						// Only natives backs are currently supported.
						this.Log().Error($"Can't process WillShowViewController because of an unsupported native operation.");
					}
				}
				else
				{
					// It's normal for requests not to be handled by the NavigationController at this point because that flag is set in the next method (DidShowViewController).
				}
			}
			else
			{
				this.Log().Error($"Can't process WillShowViewController because the current PageViewController's AssociatedRequests list is empty.");
			}

			void HandleBackSwipe(IUIViewControllerTransitionCoordinator coordinator)
			{
				if (this.Log().IsEnabled(LogLevel.Trace))
				{
					this.Log().Trace("Detected back swipe.");
				}

				// When the coordinator is initially interactive, it means that we're probably detecting a back swipe.
				// Because the back gesture can be cancelled, we don't proceed with the back just yet; we wait for the gesture to end.
				coordinator.NotifyWhenInteractionChanges(context =>
				{
					if (context.IsCancelled)
					{
						if (this.Log().IsEnabled(LogLevel.Trace))
						{
							this.Log().Trace("Cancelled back swipe.");
						}

						// If the back swipe gesture is cancelled, we void the controller request.
						_controllerToFrameRequest = null;
					}
					else if (context.IsInteractive == false)
					{
						if (this.Log().IsEnabled(LogLevel.Trace))
						{
							this.Log().Trace("Finished back swipe.");
						}

						// If the back swipe gesture completes, we proceed with the back action.
						RequestFrameBack();
					}
				});
			}

			void RequestFrameBack()
			{
				if (this.Log().IsEnabled(LogLevel.Debug))
				{
					this.Log().Debug("Native back performed. Calling Frame.GoBack() to synchronize the frame's state with the native state.");
				}

				pageViewController.AssociatedRequests.Add(_controllerToFrameRequest);

				// GoBack is synchronous, so the frame's state will be updated before we exit this method.
				_frame.GoBack();
			}
		}

		private void DidShowViewController(UINavigationController navigationController, UIViewController viewController, bool animated)
		{
			TraceViewControllers(nameof(DidShowViewController), viewController);

			if (!(viewController is PageViewController pageViewController))
			{
				// When the ViewController isn't a PageViewController, it means it doesn't have anything to do with the Frame.
				// It's possibly a modal ViewController.
				// We just ignore it.
				return;
			}

			var lastRequest = pageViewController.AssociatedRequests.LastOrDefault();
			if (lastRequest != null)
			{
				// Mark the request as handled by the NavigationController.
				lastRequest.WasHandledByController = true;

				if (lastRequest == _controllerToFrameRequest)
				{
					_frameToControllerRequests.Remove(_controllerToFrameRequest);
					_controllerToFrameRequest = null;
				}
				else
				{
					if (TryGetLast(_frameToControllerRequests, out var frameRequest))
					{
						if (NavigationRequest.Correlates(lastRequest, frameRequest))
						{
							// Now that the NavigationController handled the frame request, we remove it from the list.
							_frameToControllerRequests.RemoveLast();
						}
						else
						{
							// Note for the future: We might be able to improve this by reseting the Frame to the NavigationController's content.
							// However, this case doesn't seem to really happen.

							// Something bad happened. We clear the request queue to try to recover.
							_frameToControllerRequests.Clear();
							this.Log().Error($"Can't process DidShowViewController because the last request doesn't match the current request.");
						}
					}
					else
					{
						// It's possible that the NavigationController is the source of this event. When that's the case, the list of Frame requests is possibly empty.
					}
				}
			}
			else
			{
				this.Log().Error($"Can't process DidShowViewController because the current PageViewController's AssociatedRequests list is empty.");
			}
		}

		private static bool TryGetFirst(LinkedList<NavigationRequest> navigationRequests, out NavigationRequest firstValue)
		{
			firstValue = navigationRequests.FirstOrDefault();
			return firstValue != null;
		}

		private static bool TryGetLast(LinkedList<NavigationRequest> navigationRequests, out NavigationRequest lastValue)
		{
			lastValue = navigationRequests.LastOrDefault();
			return lastValue != null;
		}

		/// <summary>
		/// This represents a navigation request.
		/// We can create this object from <see cref="Frame"/> events (<see cref="Frame.Navigating"/> & <see cref="Frame.Navigated"/>)
		/// or from <see cref="UINavigationController"/> events (<see cref="ControllerDelegate.WillShowViewController(UINavigationController, UIViewController, bool)"/> & <see cref="ControllerDelegate.WillShowViewController(UINavigationController, UIViewController, bool)"/>).
		/// We use this class to correlate requests from the frame with requests from the navigation controller.
		/// </summary>
		private class NavigationRequest
		{
			/// <summary>
			/// Constructor for the <see cref="Frame.Navigating"/> event.
			/// </summary>
			public NavigationRequest(Frame frame, NavigatingCancelEventArgs e)
			{
				NavigationMode = e.NavigationMode;
				PageType = e.SourcePageType;
				TransitionInfo = e.NavigationTransitionInfo;

				// Here we build the BackStack that we would have after the Navigated event.
				switch (e.NavigationMode)
				{
					case NavigationMode.New:
					case NavigationMode.Forward:
						var backStackPageType = frame.BackStack
							.Select(p => p.SourcePageType)
							.ToList();
						if (frame.Content is Page page)
						{
							backStackPageType.Add(page.GetType());
						}
						BackStackPageTypes = backStackPageType;
						break;
					case NavigationMode.Back:
						BackStackPageTypes = frame.BackStack
							.Take(Math.Max(0, frame.BackStack.Count - 1))
							.Select(p => p.SourcePageType)
							.ToList();
						break;
					case NavigationMode.Refresh:
					default:
						BackStackPageTypes = frame.BackStack.Select(p => p.SourcePageType).ToList();
						break;

				}

				WasHandledByFrame = false;
				WasHandledByController = false;
			}

			/// <summary>
			/// Constructor for the <see cref="Frame.Navigated"/> event.
			/// </summary>
			public NavigationRequest(Frame frame, NavigationEventArgs e)
			{
				NavigationMode = e.NavigationMode;
				PageType = e.SourcePageType;
				TransitionInfo = e.NavigationTransitionInfo;
				BackStackPageTypes = frame.BackStack.Select(p => p.SourcePageType).ToList();

				WasHandledByFrame = true;
				WasHandledByController = false;
			}

			/// <summary>
			/// Constructor for native backs.
			/// </summary>
			public NavigationRequest(Frame frame, PageViewController pageViewController)
			{
				var requestThatCreatedTheController = pageViewController.AssociatedRequests.First();

				NavigationMode = NavigationMode.Back;
				PageType = requestThatCreatedTheController.PageType;
				TransitionInfo = requestThatCreatedTheController.TransitionInfo;
				BackStackPageTypes = frame.BackStack
					.Take(Math.Max(0, frame.BackStack.Count - 1))
					.Select(p => p.SourcePageType)
					.ToList();

				WasHandledByFrame = false;
				WasHandledByController = false;
			}

			public NavigationMode NavigationMode { get; }

			public Type PageType { get; }

			public NavigationTransitionInfo TransitionInfo { get; }

			public IReadOnlyList<Type> BackStackPageTypes { get; }

			public bool WasHandledByFrame { get; set; }

			public bool WasHandledByController { get; set; }

			public static bool Correlates(NavigationRequest request1, NavigationRequest request2)
			{
				return request1.PageType == request2.PageType
					&& request1.NavigationMode == request2.NavigationMode
					&& request1.BackStackPageTypes.SequenceEqual(request2.BackStackPageTypes);
			}

			public override string ToString()
			{
				return $"{PageType.Name}, NavigationMode.{NavigationMode}, BackStack: [{(BackStackPageTypes.Any() ? string.Join(", ", BackStackPageTypes.Select(t => t.Name)) : ("Empty"))}] {(WasHandledByFrame ? ("WasHandledByFrame") : string.Empty)} {(WasHandledByController ? ("WasHandledByController") : string.Empty)}";
			}
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

			public override void ViewWillDisappear(bool animated)
			{
				try
				{
					base.ViewWillDisappear(animated);
					CommandBarHelper.PageWillDisappear(this);
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

			/// <summary>
			/// This list is used to correlate requests in WillShowViewController DidShowViewController.
			/// </summary>
			public List<NavigationRequest> AssociatedRequests { get; } = new List<NavigationRequest>();

			internal CommandBar GetCommandBar()
			{
				return Page.TopAppBar as CommandBar ?? CommandBarHelper.FindTopCommandBar(Page);
			}

			public override string ToString()
			{
				return $"PageViewController ({Page.GetType().Name})";
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

			public override void WillShowViewController(UINavigationController navigationController, [Transient] UIViewController viewController, bool animated)
			{
				try
				{
					Owner?.WillShowViewController(navigationController, viewController, animated);
				}
				catch (Exception e)
				{
					Application.Current.RaiseRecoverableUnhandledException(e);
				}
			}

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

		#region Tracing helpers
		private void TraceViewControllers(string method, UIViewController viewController)
		{
			if (this.Log().IsEnabled(LogLevel.Trace))
			{
				this.Log().Trace($"{method}: {GetName(viewController)}");

				var frameControllers = _frame
					.BackStack
					.Concat(_frame.CurrentEntry)
					.Where(entry => entry != null)
					.Distinct()
					.OfType<PageStackEntry>()
					.Select(entry => entry.Instance.FindViewController() ?? new PageViewController(entry.Instance))
					.ToArray();

				this.Log().Trace($"│ Frame  ViewControllers: {string.Join(", ", frameControllers.Select(GetName))}");
				this.Log().Trace($"│ Native ViewControllers: {string.Join(", ", NavigationController.ViewControllers.Select(GetName))} ");
				this.Log().Trace($"│            Frame Queue: {string.Join(", ", _frameToControllerRequests.Select(GetName))} ");
				this.Log().Trace($"└     Controller Request: {string.Join(", ", GetName(_controllerToFrameRequest))} ");
			}
		}

		private static string GetName(UIViewController controller)
		{
			if (controller is PageViewController pageViewController)
			{
				return pageViewController.Page.GetType().Name;
			}
			else
			{
				return "non-PageViewController";
			}
		}

		private static string GetName(NavigationRequest request)
		{
			return request?.PageType.Name ?? "null";
		}
		#endregion
	}
}
