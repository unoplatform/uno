using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;
using System.Threading.Tasks;

using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using Microsoft.UI.Xaml.Media.Animation;
using Android.Views.Animations;
using Uno.Disposables;
using Uno.Extensions;
using Uno.UI.Extensions;

namespace Uno.UI.Controls
{
	public partial class NativeFramePresenter : Grid // Inheriting from Grid is a hack to remove 1 visual layer (Android 4.4 stack size limits)
	{
		private static DependencyProperty BackButtonVisibilityProperty = ToolkitHelper.GetProperty("Uno.UI.Toolkit.CommandBarExtensions, Uno.UI.Toolkit", "BackButtonVisibility");

		private Frame _frame;
		private bool _isUpdatingStack;
		private (Page page, NavigationTransitionInfo transitionInfo) _currentPage;
		private readonly Queue<(Page page, NavigationEventArgs args)> _stackUpdates = new Queue<(Page, NavigationEventArgs)>();
		private CompositeDisposable _subscriptions;

		public NativeFramePresenter()
		{
		}

		private protected override void OnLoaded()
		{
			base.OnLoaded();

			Initialize(this.GetTemplatedParent() as Frame);
		}

		private void Initialize(Frame frame)
		{
			if (_frame == frame)
			{
				return;
			}

			global::System.Diagnostics.Debug.Assert(_subscriptions is null);
			_subscriptions = new CompositeDisposable();

			_frame = frame;
			_frame.Navigated += OnNavigated;
			_subscriptions.Add(Disposable.Create(() => _frame.Navigated -= OnNavigated));

			if (_frame.BackStack is ObservableCollection<PageStackEntry> backStack)
			{
				backStack.CollectionChanged += OnBackStackChanged;
				_subscriptions.Add(Disposable.Create(() => backStack.CollectionChanged -= OnBackStackChanged));
			}

			if (_frame.Content is Page startPage)
			{
				_stackUpdates.Enqueue((_frame.Content as Page, new NavigationEventArgs(_frame.Content, NavigationMode.New, null, null, null, null)));
				_ = InvalidateStack();
			}
		}

		private protected override void OnUnloaded()
		{
			base.OnUnloaded();
			_subscriptions?.Dispose();
			_subscriptions = null;
		}

		private void OnBackStackChanged(object sender, NotifyCollectionChangedEventArgs e)
		{
			UpdateBackButtonVisibility();
		}

		private void UpdateBackButtonVisibility()
		{
			var backButtonVisibility = (_frame?.CanGoBack ?? false)
				? Visibility.Visible
				: Visibility.Collapsed;

			var page = _frame?.CurrentEntry?.Instance;
			var commandBar = page?.TopAppBar as CommandBar ?? page.FindFirstDescendant<CommandBar>(
				x => x is not Frame, // prevent looking into the nested page
				_ => true
			);

			commandBar?.SetValue(BackButtonVisibilityProperty, backButtonVisibility);
		}

		private void OnNavigated(object sender, NavigationEventArgs e)
		{
			_stackUpdates.Enqueue((_frame.Content as Page, e));

			_ = InvalidateStack();
		}

		private async Task InvalidateStack()
		{
			if (_isUpdatingStack)
			{
				return;
			}

			_isUpdatingStack = true;

			while (_stackUpdates.Any())
			{
				var navigation = _stackUpdates.Dequeue();
				await UpdateStack(navigation.page, navigation.args);
			}

			_isUpdatingStack = false;
		}

		private async Task UpdateStack(Page newPage, NavigationEventArgs args)
		{
			// When AndroidUnloadInactivePages is false, we keep the pages that are still a part of the navigation history
			// (i.e. in BackStack or ForwardStack) as children and make then invisible instead of removing them. The order
			// of these "hidden" children is not necessarily similar to BackStack.Concat(ForwardStack), since the
			// back and forward stacks can be manipulated explicitly beside navigating. We could attempt to maintain
			// a correspondence between the "hidden" children and the back and forward stacks by listening to their
			// CollectionChanged events, but this breaks our optimization attempts since these events fire before
			// Navigated events are fired. For example, in a GoBack action, the BackStack is updated first and we would
			// remove the element that corresponds to the previously-last element in the BackStack, and then respond to
			// the Navigated event by making the newly-navigated-to page visible, except that the element we just removed
			// is the one we want. Therefore, we treat the "hidden" children as a list that we have to walk through to
			// remove items that are no longer a part of the navigation history. Although costly, navigation is not
			// a heavily-automated action and is mostly bottlenecked by human reaction times, so it's fine.

			var oldPage = _currentPage.page;
			var oldTransitionInfo = _currentPage.transitionInfo;
			_currentPage = (newPage, args.NavigationTransitionInfo);

			if (newPage is not null)
			{
				if (Children.Contains(newPage))
				{
					newPage.Visibility = Visibility.Visible;
				}
				else
				{
					if (args.NavigationMode is NavigationMode.Back)
					{
						// insert the new page underneath the old one so that
						// when the old one animates out you see the new page
						// without a time slice in between where neither
						// the old nor the new page is visible.
						Children.Insert(0, newPage);
					}
					else
					{
						Children.Add(newPage);
					}
				}
				if (args.NavigationMode is not NavigationMode.Back && GetIsAnimated(args.NavigationTransitionInfo))
				{
					await newPage.AnimateAsync(GetEnterAnimation());
					newPage.ClearAnimation();
				}
			}

			if (oldPage is not null)
			{
				if (args.NavigationMode is NavigationMode.Back && GetIsAnimated(oldTransitionInfo))
				{
					await oldPage.AnimateAsync(GetExitAnimation());
					oldPage.ClearAnimation();
				}
				if (FeatureConfiguration.NativeFramePresenter.AndroidUnloadInactivePages)
				{
					Children.Remove(oldPage);
				}
				else
				{
					oldPage.Visibility = Visibility.Collapsed;
				}
			}

			if (!FeatureConfiguration.NativeFramePresenter.AndroidUnloadInactivePages)
			{
				var pagesStillInHistory = _frame.BackStack.Select(entry => entry.Instance).ToHashSet();
				pagesStillInHistory.AddRange(_frame.ForwardStack.Select(entry => entry.Instance));
				pagesStillInHistory.Add(newPage);
				Children.Remove(element => !pagesStillInHistory.Contains(element));
			}
		}

		private static bool GetIsAnimated(NavigationTransitionInfo transitionInfo)
		{
			return !(transitionInfo is SuppressNavigationTransitionInfo);
		}

		private static Animation GetEnterAnimation()
		{
			// Source:
			// https://android.googlesource.com/platform/frameworks/base/+/android-cts-7.1_r5/core/res/res/anim/activity_open_enter.xml

			var enterAnimation = new AnimationSet(false)
			{
				ZAdjustment = ContentZorder.Top,
				FillAfter = true
			};

			enterAnimation.AddAnimation(new AlphaAnimation(0, 1)
			{
				Interpolator = new DecelerateInterpolator(2), // DecelerateQuart
				FillEnabled = true,
				FillBefore = false,
				FillAfter = true,
				Duration = 200,
			});

			enterAnimation.AddAnimation(new TranslateAnimation(
				Dimension.Absolute, 0,
				Dimension.Absolute, 0,
				Dimension.RelativeToSelf, 0.08f,
				Dimension.Absolute, 0
			)
			{
				FillEnabled = true,
				FillBefore = true,
				FillAfter = true,
				Interpolator = new DecelerateInterpolator(2.5f), // DecelerateQuint
				Duration = 350,
			});

			return enterAnimation;
		}

		private static Animation GetExitAnimation()
		{
			// Source:
			// https://android.googlesource.com/platform/frameworks/base/+/android-cts-7.1_r5/core/res/res/anim/activity_close_exit.xml

			var exitAnimation = new AnimationSet(false)
			{
				ZAdjustment = ContentZorder.Top,
				FillAfter = true
			};

			exitAnimation.AddAnimation(new AlphaAnimation(1, 0)
			{
				Interpolator = new LinearInterpolator(),
				FillEnabled = true,
				FillBefore = false,
				FillAfter = true,
				StartOffset = 100,
				Duration = 150,
			});

			exitAnimation.AddAnimation(new TranslateAnimation(
				Dimension.Absolute, 0,
				Dimension.Absolute, 0,
				Dimension.RelativeToSelf, 0.0f,
				Dimension.RelativeToSelf, 0.08f
			)
			{
				FillEnabled = true,
				FillBefore = true,
				FillAfter = true,
				Interpolator = new AccelerateInterpolator(2), // AccelerateQuart
				Duration = 250,
			});

			return exitAnimation;
		}
	}
}
