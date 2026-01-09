using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Microsoft.UI.Xaml.Navigation;

[assembly: UnconditionalSuppressMessage("Trimming", "IL2111", Scope = "member",
	Target = "M:Microsoft.UI.Xaml.Controls.Frame.#cctor()",
	Justification = "From the `CurrentSourcePageTypeProperty`, `SourcePageTypeProperty` assignment; `typeof(Type)` triggers IL2111 regarding `Type.TypeInitializer`, but Uno doesn't use `Type.TypeInitializer`!")]

namespace Microsoft.UI.Xaml.Controls;

partial class Frame
{
	/// <summary>
	/// Gets a collection of PageStackEntry instances representing the backward navigation history of the Frame.
	/// </summary>
	public IList<PageStackEntry> BackStack
	{
		get => (IList<PageStackEntry>)GetValue(BackStackProperty);
		set => SetValue(BackStackProperty, value); // TODO: Setter should not be public
	}

	/// <summary>
	/// Identifies the BackStack dependency property.
	/// </summary>
	public static DependencyProperty BackStackProperty { get; } =
		DependencyProperty.Register(
			nameof(BackStack),
			typeof(IList<PageStackEntry>),
			typeof(Frame),
			new FrameworkPropertyMetadata(null));

	/// <summary>
	/// Gets the number of entries in the navigation back stack
	/// </summary>
	public int BackStackDepth
	{
		get => (int)GetValue(BackStackDepthProperty);
		internal set => SetValue(BackStackDepthProperty, value);
	}

	/// <summary>
	/// Identifies the BackStackDepth dependency property.
	/// </summary>
	public static DependencyProperty BackStackDepthProperty { get; } =
		DependencyProperty.Register(
			nameof(BackStackDepth),
			typeof(int),
			typeof(Frame),
			new FrameworkPropertyMetadata(0, (s, e) => ((Frame)s)?.OnBackStackDepthChanged(e)));

	protected virtual void OnBackStackDepthChanged(DependencyPropertyChangedEventArgs e)
	{
	}

	/// <summary>
	/// Gets or sets the number of pages in the navigation history that can be cached for the frame.
	/// </summary>
	public int CacheSize
	{
		get => (int)GetValue(CacheSizeProperty);
		set => SetValue(CacheSizeProperty, value);
	}

	/// <summary>
	/// Identifies the CacheSize dependency property.
	/// </summary>
	public static DependencyProperty CacheSizeProperty { get; } =
		DependencyProperty.Register(
			nameof(CacheSize),
			typeof(int),
			typeof(Frame),
			new FrameworkPropertyMetadata(InitialTransientCacheSize));

	/// <summary>
	/// Gets a value that indicates whether there is at least one entry in back navigation history.
	/// </summary>
	public bool CanGoBack
	{
		get => (bool)GetValue(CanGoBackProperty);
		internal set => SetValue(CanGoBackProperty, value);
	}

	/// <summary>
	/// Identifies the CanGoBack dependency property.
	/// </summary>
	public static DependencyProperty CanGoBackProperty { get; } =
		DependencyProperty.Register(
			nameof(CanGoBack),
			typeof(bool),
			typeof(Frame),
			new FrameworkPropertyMetadata(false));

	/// <summary>
	/// Gets a value that indicates whether there is at least one entry in forward navigation history.
	/// </summary>
	public bool CanGoForward
	{
		get => (bool)GetValue(CanGoForwardProperty);
		internal set => SetValue(CanGoForwardProperty, value);
	}

	/// <summary>
	/// Identifies the CanGoForward dependency property.
	/// </summary>
	public static DependencyProperty CanGoForwardProperty { get; } =
		DependencyProperty.Register(
			nameof(CanGoForward),
			typeof(bool),
			typeof(Frame),
			new FrameworkPropertyMetadata(true));

	/// <summary>
	/// Gets a type reference for the content that is currently displayed.
	/// </summary>
	public Type CurrentSourcePageType
	{
		get => (Type)GetValue(CurrentSourcePageTypeProperty);
		internal set => SetValue(CurrentSourcePageTypeProperty, value);
	}

	/// <summary>
	/// Identifies the CurrentSourcePageType dependency property.
	/// </summary>
	[UnconditionalSuppressMessage("Trimming", "IL2111", Justification = "`typeof(Type)` triggers IL2111 regarding `Type.TypeInitializer`, but Uno doesn't use `Type.TypeInitializer`!")]
	// warning IL2111: Method 'System.Type.TypeInitializer.get' with parameters or return value with `DynamicallyAccessedMembersAttribute` is accessed via reflection. Trimmer can't guarantee availability of the requirements of the method.
	public static DependencyProperty CurrentSourcePageTypeProperty { get; } =
		DependencyProperty.Register(
			nameof(CurrentSourcePageType),
			typeof(Type),
			typeof(Frame),
			new FrameworkPropertyMetadata(null));

	/// <summary>
	/// Gets a collection of PageStackEntry instances representing the forward navigation history of the Frame.
	/// </summary>
	public IList<PageStackEntry> ForwardStack
	{
		get => (IList<PageStackEntry>)GetValue(ForwardStackProperty);
		private set => SetValue(ForwardStackProperty, value);
	}

	/// <summary>
	/// Identifies the ForwardStack dependency property.
	/// </summary>
	public static DependencyProperty ForwardStackProperty { get; } =
		DependencyProperty.Register(
			nameof(ForwardStack),
			typeof(IList<PageStackEntry>),
			typeof(Frame),
			new FrameworkPropertyMetadata(null));

	/// <summary>
	/// Gets or sets a value that indicates whether navigation is recorded in the Frame's ForwardStack or BackStack.
	/// </summary>
	public bool IsNavigationStackEnabled
	{
		get => (bool)GetValue(IsNavigationStackEnabledProperty);
		set => SetValue(IsNavigationStackEnabledProperty, value);
	}

	/// <summary>
	/// Identifies the IsNavigationStackEnabled dependency property.
	/// </summary>
	public static DependencyProperty IsNavigationStackEnabledProperty { get; } =
		DependencyProperty.Register(
			nameof(IsNavigationStackEnabled),
			typeof(bool),
			typeof(Frame),
			new FrameworkPropertyMetadata(true));

	/// <summary>
	/// Gets or sets a type reference of the current content, or the content that should be navigated to.
	/// </summary>
	public Type SourcePageType
	{
		get => (Type)GetValue(SourcePageTypeProperty);
		set => SetValue(SourcePageTypeProperty, value);
	}

	/// <summary>
	/// Identifies the SourcePageType dependency property.
	/// </summary>
	[UnconditionalSuppressMessage("Trimming", "IL2111", Justification = "`typeof(Type)` triggers IL2111 regarding `Type.TypeInitializer`, but Uno doesn't use `Type.TypeInitializer`!")]
	// warning IL2111: Method 'System.Type.TypeInitializer.get' with parameters or return value with `DynamicallyAccessedMembersAttribute` is accessed via reflection. Trimmer can't guarantee availability of the requirements of the method.
	public static DependencyProperty SourcePageTypeProperty { get; } =
		DependencyProperty.Register(
			nameof(SourcePageType),
			typeof(Type),
			typeof(Frame),
			new FrameworkPropertyMetadata(null, (s, e) => ((Frame)s)?.OnSourcePageTypeChanged(e)));

	private void OnSourcePageTypeChanged(DependencyPropertyChangedEventArgs e)
	{
		if (!_useWinUIBehavior)
		{
			OnSourcePageTypeChangedLegacy(e);
		}
	}

	/// <summary>
	/// Occurs when the content that is being navigated to has been found and is available
	/// from the Content property, although it may not have completed loading.
	/// </summary>
	public event NavigatedEventHandler Navigated;

	/// <summary>
	/// Occurs when a new navigation is requested.
	/// </summary>
	public event NavigatingCancelEventHandler Navigating;

	/// <summary>
	/// Occurs when an error is raised while navigating to the requested content.
	/// </summary>
	public event NavigationFailedEventHandler NavigationFailed;

	/// <summary>
	/// Occurs when a new navigation is requested while a current navigation is in progress.
	/// </summary>
	public event NavigationStoppedEventHandler NavigationStopped;
}
