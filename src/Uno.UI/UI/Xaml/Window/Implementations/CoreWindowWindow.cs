#nullable enable

using System;
using Uno.UI.Xaml.Core;
using Windows.UI.Core;
using Windows.UI.Xaml;
using WinUICoreServices = Uno.UI.Xaml.Core.CoreServices;

namespace Uno.UI.Xaml.Controls;

internal partial class CoreWindowWindow : BaseWindowImplementation
{
	private readonly ContentManager _contentManager;

	public CoreWindowWindow(Window window) : base(window)
	{
		_contentManager = new ContentManager(window, true);
		CoreWindow = CoreWindow.GetOrCreateForCurrentThread();

		InitializeNativeWindow();
	}

#pragma warning disable CS0067
	public override CoreWindow CoreWindow { get; }

	public override UIElement? Content
	{
		get => _contentManager.Content;
		set => _contentManager.Content = value;
	}

	public override XamlRoot? XamlRoot => WinUICoreServices.Instance.MainVisualTree?.GetOrCreateXamlRoot();

	public override void Activate()
	{
		if (WinUICoreServices.Instance.MainVisualTree is null)
		{
			throw new InvalidOperationException("Main visual tree is not initialized.");
		}
		ContentManager.TryLoadRootVisual(WinUICoreServices.Instance.MainVisualTree.GetOrCreateXamlRoot());
	}
}
