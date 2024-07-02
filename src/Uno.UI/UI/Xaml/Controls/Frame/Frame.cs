using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Uno.Disposables;
using System.Text;
using Uno.Extensions;
using System.Collections.Specialized;
using Windows.UI.Xaml.Navigation;
using Windows.UI.Xaml.Media.Animation;
using Uno;
using Windows.UI.Xaml.Media;
using Uno.UI;
using System.Runtime.CompilerServices;
using Windows.UI.Xaml.Input;
using Uno.UI.Xaml.Core;
using Uno.UI.Helpers;
using Uno.Foundation.Logging;

namespace Windows.UI.Xaml.Controls
{
	public partial class Frame : ContentControl
	{
		private bool _isNavigating;

		private string _navigationState;

		private static readonly PagePool _pool = new PagePool();

		public Frame()
		{
			var backStack = new ObservableCollection<PageStackEntry>();
			var forwardStack = new ObservableCollection<PageStackEntry>();

			backStack.CollectionChanged += (s, e) =>
			{
				CanGoBack = BackStack.Any();
				BackStackDepth = BackStack.Count;
			};

			forwardStack.CollectionChanged += (s, e) => CanGoForward = ForwardStack.Any();

			BackStack = backStack;
			ForwardStack = forwardStack;

			DefaultStyleKey = typeof(Frame);
		}

		internal PageStackEntry CurrentEntry { get; set; }

		protected override void OnContentChanged(object oldValue, object newValue)
		{
			base.OnContentChanged(oldValue, newValue);

			// Make sure we void CurrentEntry when someone sets Frame.Content = null;
			if (newValue == null)
			{
				CurrentEntry = null;
			}
			else if (CurrentEntry is not null)
			{
				// This is to support hot reload scenarios - the PageStackEntry 
				// is used when navigating back to this page as it's maintained in the BackStack
				CurrentEntry.Instance = newValue as Page;
				CurrentEntry.SourcePageType = newValue.GetType();
			}
		}

		#region BackStackDepth DependencyProperty

		public int BackStackDepth
		{
			get { return (int)GetValue(BackStackDepthProperty); }
			private set { this.SetValue(BackStackDepthProperty, value); }
		}

		// Using a DependencyProperty as the backing store for BackStackDepth.  This enables animation, styling, binding, etc...
		public static DependencyProperty BackStackDepthProperty { get; } =
			DependencyProperty.Register("BackStackDepth", typeof(int), typeof(Frame), new FrameworkPropertyMetadata(0, (s, e) => ((Frame)s)?.OnBackStackDepthChanged(e)));


		protected virtual void OnBackStackDepthChanged(DependencyPropertyChangedEventArgs e)
		{

		}

		#endregion

		#region BackStack DependencyProperty

		public IList<PageStackEntry> BackStack
		{
			get { return (IList<PageStackEntry>)GetValue(BackStackProperty); }
			set { SetValue(BackStackProperty, value); }
		}

		// Using a DependencyProperty as the backing store for BackStack.  This enables animation, styling, binding, etc...
		public static DependencyProperty BackStackProperty { get; } =
			DependencyProperty.Register("BackStack", typeof(IList<PageStackEntry>), typeof(Frame), new FrameworkPropertyMetadata(null, (s, e) => ((Frame)s)?.OnBackStackChanged(e)));

		private void OnBackStackChanged(DependencyPropertyChangedEventArgs e)
		{
		}

		#endregion

		#region CacheSize DependencyProperty

		public int CacheSize
		{
			get { return (int)GetValue(CacheSizeProperty); }
			set { SetValue(CacheSizeProperty, value); }
		}

		// Using a DependencyProperty as the backing store for CacheSize.  This enables animation, styling, binding, etc...
		public static DependencyProperty CacheSizeProperty { get; } =
			DependencyProperty.Register("CacheSize", typeof(int), typeof(Frame), new FrameworkPropertyMetadata(0, (s, e) => ((Frame)s)?.OnCacheSizeChanged(e)));


		private void OnCacheSizeChanged(DependencyPropertyChangedEventArgs e)
		{
		}

		#endregion

		#region CanGoBack DependencyProperty

		public bool CanGoBack
		{
			get { return (bool)GetValue(CanGoBackProperty); }
			private set { SetValue(CanGoBackProperty, value); }
		}

		// Using a DependencyProperty as the backing store for CanGoBack.  This enables animation, styling, binding, etc...
		public static DependencyProperty CanGoBackProperty { get; } =
			DependencyProperty.Register("CanGoBack", typeof(bool), typeof(Frame), new FrameworkPropertyMetadata(false, (s, e) => ((Frame)s)?.OnCanGoBackChanged(e)));


		private void OnCanGoBackChanged(DependencyPropertyChangedEventArgs e)
		{
		}

		#endregion

		#region CanGoForward DependencyProperty

		public bool CanGoForward
		{
			get { return (bool)GetValue(CanGoForwardProperty); }
			private set { SetValue(CanGoForwardProperty, value); }
		}

		// Using a DependencyProperty as the backing store for CanGoForward.  This enables animation, styling, binding, etc...
		public static DependencyProperty CanGoForwardProperty { get; } =
			DependencyProperty.Register("CanGoForward", typeof(bool), typeof(Frame), new FrameworkPropertyMetadata(true, (s, e) => ((Frame)s)?.OnCanGoForwardChanged(e)));


		private void OnCanGoForwardChanged(DependencyPropertyChangedEventArgs e)
		{

		}

		#endregion

		#region CurrentSourcePageType DependencyProperty

		public Type CurrentSourcePageType => (Type)GetValue(CurrentSourcePageTypeProperty);

		public static DependencyProperty CurrentSourcePageTypeProperty { get; } =
			DependencyProperty.Register(nameof(CurrentSourcePageType), typeof(Type), typeof(Frame), new FrameworkPropertyMetadata(null, (s, e) => ((Frame)s)?.OnCurrentSourcePageTypeChanged(e)));


		private void OnCurrentSourcePageTypeChanged(DependencyPropertyChangedEventArgs e)
		{

		}

		#endregion

		#region ForwardStack DependencyProperty

		public IList<PageStackEntry> ForwardStack
		{
			get { return (IList<PageStackEntry>)GetValue(ForwardStackProperty); }
			private set { SetValue(ForwardStackProperty, value); }
		}

		// Using a DependencyProperty as the backing store for ForwardStack.  This enables animation, styling, binding, etc...
		public static DependencyProperty ForwardStackProperty { get; } =
			DependencyProperty.Register("ForwardStack", typeof(IList<PageStackEntry>), typeof(Frame), new FrameworkPropertyMetadata(null, (s, e) => ((Frame)s)?.OnForwardStackChanged(e)));


		private void OnForwardStackChanged(DependencyPropertyChangedEventArgs e)
		{
		}

		#endregion

		#region SourcePageType DependencyProperty

		public Type SourcePageType
		{
			get => (Type)GetValue(SourcePageTypeProperty);
			set => SetValue(SourcePageTypeProperty, value);
		}

		public static DependencyProperty SourcePageTypeProperty { get; } =
			DependencyProperty.Register(nameof(SourcePageType), typeof(Type), typeof(Frame), new FrameworkPropertyMetadata(null, (s, e) => ((Frame)s)?.OnSourcePageTypeChanged(e)));

		private void OnSourcePageTypeChanged(DependencyPropertyChangedEventArgs e)
		{
			if (!_isNavigating)
			{
				if (e.NewValue == null)
				{
					throw new InvalidOperationException(
						"SourcePageType cannot be set to null. Set Content to null instead.");
				}
				Navigate((Type)e.NewValue);
			}
		}

		#endregion

		#region IsNavigationStackEnabled DependencyProperty

		public bool IsNavigationStackEnabled
		{
			get { return (bool)GetValue(IsNavigationStackEnabledProperty); }
			set { SetValue(IsNavigationStackEnabledProperty, value); }
		}

		public static DependencyProperty IsNavigationStackEnabledProperty { get; } =
			DependencyProperty.Register(nameof(IsNavigationStackEnabled), typeof(bool), typeof(Frame), new FrameworkPropertyMetadata(true));

		#endregion

		public event NavigatedEventHandler Navigated;

		public event NavigatingCancelEventHandler Navigating;

		public event NavigationFailedEventHandler NavigationFailed;

		public event NavigationStoppedEventHandler NavigationStopped;

		public string GetNavigationState() => _navigationState;

		public void GoBack() => GoBack(null);

		public void GoBack(NavigationTransitionInfo transitionInfoOverride)
		{
			if (CanGoBack)
			{

				var entry = BackStack.Last();
				if (transitionInfoOverride != null)
				{
					entry.NavigationTransitionInfo = transitionInfoOverride;
				}
				else
				{
					// Fallback to the page forward navigation transition info
					entry.NavigationTransitionInfo = CurrentEntry.NavigationTransitionInfo;
				}

				InnerNavigate(entry, NavigationMode.Back);
			}
		}

		public void GoForward()
		{
			if (CanGoForward)
			{
				InnerNavigate(ForwardStack.Last(), NavigationMode.Forward);
			}
		}

		public bool Navigate(Type sourcePageType) => Navigate(sourcePageType, null, null);

		public bool Navigate(Type sourcePageType, object parameter) => Navigate(sourcePageType, parameter, null);

		public bool Navigate(Type sourcePageType, object parameter, NavigationTransitionInfo infoOverride)
		{
			var entry = new PageStackEntry(sourcePageType.GetReplacementType(), parameter, infoOverride);
			return InnerNavigate(entry, NavigationMode.New);
		}

		private bool InnerNavigate(PageStackEntry entry, NavigationMode mode)
		{
			if (_isNavigating)
			{
				if (this.Log().IsEnabled(LogLevel.Warning))
				{
					this.Log().LogWarning(
						"Frame is already navigating, ignoring the navigation request." +
						"Please delay the navigation with await Task.Yield().");
				}
				return false;
			}

			try
			{
				_isNavigating = true;

				return InnerNavigateUnsafe(entry, mode);
			}
			catch (Exception exception)
			{
				NavigationFailed?.Invoke(this, new NavigationFailedEventArgs(entry.SourcePageType, exception));

				if (NavigationFailed == null)
				{
					Application.Current.RaiseRecoverableUnhandledException(new InvalidOperationException("Navigation failed", exception));
				}

				return false;
			}
			finally
			{
				_isNavigating = false;
			}
		}

		/// <remarks>
		/// This method contains or is called by a try/catch containing method and
		/// can be significantly slower than other methods as a result on WebAssembly.
		/// See https://github.com/dotnet/runtime/issues/56309
		/// </remarks>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private bool InnerNavigateUnsafe(PageStackEntry entry, NavigationMode mode)
		{
			// Navigating
			var navigatingFromArgs = new NavigatingCancelEventArgs(
				mode,
				entry.NavigationTransitionInfo,
				entry.Parameter,
				entry.SourcePageType
			);

			Navigating?.Invoke(this, navigatingFromArgs);

			if (navigatingFromArgs.Cancel)
			{
				// Frame canceled
				OnNavigationStopped(entry, mode);
				return false;
			}

			CurrentEntry?.Instance?.OnNavigatingFrom(navigatingFromArgs);

			if (navigatingFromArgs.Cancel)
			{
				// Page canceled
				OnNavigationStopped(entry, mode);
				return false;
			}

			// Navigate
			var previousEntry = CurrentEntry;
			CurrentEntry = entry;

			if (mode == NavigationMode.New)
			{
				// Doing this first allows CurrentEntry to reuse existing page if pooling is enabled
				ReleasePages(ForwardStack);
			}

			if (CurrentEntry.Instance == null)
			{
				if (EnsurePageInitialized(entry) is not { } page)
				{
					return false;
				}

				CurrentEntry.Instance = page;
			}

			MoveFocusFromCurrentContent();

			Content = CurrentEntry.Instance;

			if (IsNavigationStackEnabled)
			{
				switch (mode)
				{
					case NavigationMode.New:
						ForwardStack.Clear();
						if (previousEntry != null)
						{
							BackStack.Add(previousEntry);
						}
						break;
					case NavigationMode.Back:
						ForwardStack.Add(previousEntry);
						BackStack.Remove(CurrentEntry);
						break;
					case NavigationMode.Forward:
						BackStack.Add(previousEntry);
						ForwardStack.Remove(CurrentEntry);
						break;
					case NavigationMode.Refresh:
						break;
				}
			}

			// Navigated
			var navigationEvent = new NavigationEventArgs(
				CurrentEntry.Instance,
				mode,
				entry.NavigationTransitionInfo,
				entry.Parameter,
				entry.SourcePageType,
				null
			);

			SetValue(SourcePageTypeProperty, entry.SourcePageType);
			SetValue(CurrentSourcePageTypeProperty, entry.SourcePageType);

			Navigated?.Invoke(this, navigationEvent);

			previousEntry?.Instance.OnNavigatedFrom(navigationEvent);
			CurrentEntry.Instance.OnNavigatedTo(navigationEvent);

			return true;
		}

		/// <summary>
		/// Return pages removed from the stack to the pool, if enabled.
		/// </summary>
		private void ReleasePages(IList<PageStackEntry> pageStackEntries)
		{
			foreach (var entry in pageStackEntries)
			{
				entry.Instance.Frame = null;
			}

			if (!FeatureConfiguration.Page.IsPoolingEnabled)
			{
				return;
			}

			foreach (var entry in pageStackEntries)
			{
				if (entry.Instance != null)
				{
					_pool.EnqueuePage(entry.SourcePageType, entry.Instance);
				}
			}
		}

		public void SetNavigationState(string navigationState) => _navigationState = navigationState;

		internal Page EnsurePageInitialized(PageStackEntry entry)
		{
			if (entry is { Instance: null } &&
				CreatePageInstanceCached(entry.SourcePageType) is { } page)
			{
				page.Frame = this;
				entry.Instance = page;
			}

			return entry?.Instance;
		}

		private static Page CreatePageInstanceCached(Type sourcePageType) => _pool.DequeuePage(sourcePageType);

		internal static Page CreatePageInstance(Type sourcePageType)
		{
			if (Uno.UI.DataBinding.BindingPropertyHelper.BindableMetadataProvider != null)
			{
				var bindableType = Uno.UI.DataBinding.BindingPropertyHelper.BindableMetadataProvider.GetBindableTypeByType(sourcePageType);

				if (bindableType != null)
				{
					return bindableType.CreateInstance()() as Page;
				}
			}

			return Activator.CreateInstance(sourcePageType) as Page;
		}

		private void OnNavigationStopped(PageStackEntry entry, NavigationMode mode)
		{
			NavigationStopped?.Invoke(this, new NavigationEventArgs(
						entry.Instance,
						mode,
						entry.NavigationTransitionInfo,
						entry.Parameter,
						entry.SourcePageType,
						null
					));
		}

		/// <summary>
		/// In case the current page contains a focused element,
		/// we need to move the focus out of the page.
		/// </summary>
		/// <remarks>
		/// In UWP this is done automatically as the elements are unloaded,
		/// but due to the control lifecycle differences in Uno the focus move multiple times
		/// as controls are unloaded in "layers" and it could also not move outside this Frame,
		/// as the Parent would already be unassigned during the OnUnloaded execution.
		/// </remarks>
		private void MoveFocusFromCurrentContent()
		{
			if (Content is not UIElement uiElement)
			{
				return;
			}
			uiElement.IsLeavingFrame = true;
			try
			{
				var focusManager = VisualTree.GetFocusManagerForElement(this);
				if (focusManager?.FocusedElement is not { } focusedElement)
				{
					return;
				}

				var parent = VisualTreeHelper.GetParent(focusedElement);
				while (parent is not null && parent != this)
				{
					parent = VisualTreeHelper.GetParent(parent);
				}

				var inCurrentPage = parent == this;

				if (inCurrentPage)
				{
					// Set the focus on the next focusable element.
					focusManager.SetFocusOnNextFocusableElement(FocusState.Programmatic, true);

					(focusedElement as Control)?.UpdateFocusState(FocusState.Unfocused);
				}
			}
			finally
			{
				uiElement.IsLeavingFrame = false;
			}
		}
	}
}
