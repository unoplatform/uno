#nullable enable

using System;
using System.Collections.Generic;
using Windows.Foundation;
using Uno.UI;
using Uno;
using Uno.UI.Controls;
using Microsoft.UI.Xaml;

namespace Windows.UI.ViewManagement;

public partial class InputPane
{
	private static readonly Dictionary<XamlRoot, InputPane> _instances = new();
	private static InputPane? _fallbackInstance;
	private Rect _occludedRect = new Rect(0, 0, 0, 0);
	private XamlRoot? _xamlRoot;

	private InputPane()
	{
		InitializePlatform();
	}

	partial void InitializePlatform();

	public event TypedEventHandler<InputPane, InputPaneVisibilityEventArgs>? Hiding;

	public event TypedEventHandler<InputPane, InputPaneVisibilityEventArgs>? Showing;

	/// <summary>
	/// The XamlRoot associated with this InputPane instance (null for the fallback instance).
	/// </summary>
	internal XamlRoot? AssociatedXamlRoot => _xamlRoot;

	public Rect OccludedRect
	{
		get => _occludedRect;
		internal set
		{
			if (_occludedRect != value)
			{
				_occludedRect = value;
				OnOccludedRectChanged();
			}
		}
	}

	public bool Visible
	{
		get => OccludedRect.Height > 0;
		set
		{
			if (value)
			{
				TryShow();
			}
			else
			{
				TryHide();
			}
		}
	}

	/// <summary>
	/// Returns the InputPane for the initial window. Backward-compatible with existing callers.
	/// </summary>
	public static InputPane GetForCurrentView()
	{
		var xamlRoot = Microsoft.UI.Xaml.Window.InitialWindow?.Content?.XamlRoot;
		if (xamlRoot != null)
		{
			return GetForXamlRoot(xamlRoot);
		}

		// Fallback for early calls before window is set up
		_fallbackInstance ??= new InputPane();
		return _fallbackInstance;
	}

	/// <summary>
	/// Returns the InputPane for a specific XamlRoot. Used internally for multi-window support.
	/// </summary>
	internal static InputPane GetForXamlRoot(XamlRoot xamlRoot)
	{
		if (!_instances.TryGetValue(xamlRoot, out var inputPane))
		{
			inputPane = new InputPane();
			inputPane._xamlRoot = xamlRoot;
			_instances[xamlRoot] = inputPane;
		}

		return inputPane;
	}

#if __APPLE_UIKIT__
	[NotImplemented]
#endif
	public bool TryShow()
	{
		if (Visible)
		{
			return false;
		}

		return TryShowPlatform();
	}

	public bool TryHide()
	{
		if (!Visible)
		{
			return false;
		}

		return TryHidePlatform();
	}

	internal void OnOccludedRectChanged()
	{
		var args = new InputPaneVisibilityEventArgs(OccludedRect);

		if (Visible)
		{
			Showing?.Invoke(this, args);
		}
		else
		{
			Hiding?.Invoke(this, args);
		}

		if (!args.EnsuredFocusedElementInView)
		{
			// Wait for proper element to be focused
			_ = UI.Core.CoreDispatcher.Main.RunAsync(
				UI.Core.CoreDispatcherPriority.Normal,
				() => EnsureFocusedElementInViewPartial()
			);
		}
	}

	partial void EnsureFocusedElementInViewPartial();
}
