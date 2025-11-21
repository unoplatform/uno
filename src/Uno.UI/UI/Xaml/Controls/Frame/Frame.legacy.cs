using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Uno.Disposables;
using System.Text;
using Uno.Extensions;
using System.Collections.Specialized;
using Microsoft.UI.Xaml.Navigation;
using Microsoft.UI.Xaml.Media.Animation;
using Uno;
using Microsoft.UI.Xaml.Media;
using Uno.UI;
using System.Runtime.CompilerServices;
using Microsoft.UI.Xaml.Input;
using Uno.UI.Xaml.Core;
using Uno.UI.Helpers;
using Uno.Foundation.Logging;
using System.Diagnostics.CodeAnalysis;

namespace Microsoft.UI.Xaml.Controls;

public partial class Frame : ContentControl
{
	private bool _isNavigating;

	private string _navigationState;

	private static PagePool _pool;

	private void CtorLegacy()
	{
		_pool = new PagePool();

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
			SetSourcePageType(CurrentEntry, newValue.GetType());
		}

		[UnconditionalSuppressMessage("Trimming", "IL2067")]
		// 'value' argument does not satisfy 'DynamicallyAccessedMemberTypes.PublicConstructors' in call to 'PageStackEntry.SourcePageType.set'.
		static void SetSourcePageType(PageStackEntry entry, Type type)
		{
			entry.SourcePageType = type;
		}
	}

	private string GetNavigationStateLegacy() => _navigationState;

	private void GoBackLegacy() => GoBackWithTransitionInfoLegacy(null);

	private void GoBackWithTransitionInfoLegacy(NavigationTransitionInfo transitionInfoOverride)
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

	private void GoForwardLegacy()
	{
		if (CanGoForward)
		{
			InnerNavigate(ForwardStack.Last(), NavigationMode.Forward);
		}
	}

	private bool NavigateLegacy(Type sourcePageType) => Navigate(sourcePageType, null, null);

	private bool NavigateLegacy(Type sourcePageType, object parameter) => Navigate(sourcePageType, parameter, null);

	[UnconditionalSuppressMessage("Trimming", "IL2072", Justification = "Types manipulated here have been marked earlier")]
	[UnconditionalSuppressMessage("Trimming", "IL2067", Justification = "Types manipulated here have been marked earlier")]
	private bool NavigateWithTransitionInfoLegacy(Type sourcePageType, object parameter, NavigationTransitionInfo infoOverride)
	{
		var entry = new PageStackEntry(sourcePageType.GetReplacementType(), parameter, infoOverride);
		return InnerNavigate(entry, NavigationMode.New);
	}

	private bool NavigateToTypeLegacy(Type sourcePageType, object parameter, FrameNavigationOptions frameNavigationOptions)
	{
		NavigationTransitionInfo transitionInfoOverride = null;

		if (frameNavigationOptions is not null)
		{
			transitionInfoOverride = frameNavigationOptions.TransitionInfoOverride;
		}

		return NavigateWithTransitionInfoLegacy(sourcePageType, parameter, transitionInfoOverride);
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
			bool InternalNavigate(PageStackEntry entry, NavigationMode mode)
			{
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

					throw;
				}
			}

			return InternalNavigate(entry, mode);
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

		SetContent(CurrentEntry.Instance);

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

	private void SetNavigationStateLegacy(string navigationState) => _navigationState = navigationState;

	private void SetNavigationStateWithNavigationControlLegacy(string navigationState, bool suppressNavigate) => _navigationState = navigationState;

	internal Page EnsurePageInitializedLegacy(PageStackEntry entry)
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

	private void OnSourcePageTypeChangedLegacy(DependencyPropertyChangedEventArgs e)
	{
		if (!_isNavigating)
		{
			if (e.NewValue == null)
			{
				throw new ArgumentNullException(
					"SourcePageType cannot be set to null. Set Content to null instead.");
			}
			Navigate((Type)e.NewValue);
		}
	}
}
