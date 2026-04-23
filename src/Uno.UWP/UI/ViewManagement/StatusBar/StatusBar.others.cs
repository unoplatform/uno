#if !__ANDROID__ && !__IOS__
using Windows.Foundation;
using Windows.UI;

namespace Windows.UI.ViewManagement
{
	/// <summary>
	/// Provides methods and properties for interacting with the status bar on a window (app view).
	/// </summary>
	/// <remarks>
	/// This is a stub for platforms where StatusBar is not natively supported.
	/// The generated stub was removed because StatusBar is no longer part of the WinUI API surface.
	/// </remarks>
	[global::Uno.NotImplemented("__TVOS__", "IS_UNIT_TESTS", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__")]
	public sealed partial class StatusBar
	{
		internal StatusBar()
		{
		}

		[global::Uno.NotImplemented("__TVOS__", "IS_UNIT_TESTS", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__")]
		public Color? ForegroundColor
		{
			get => throw new global::System.NotImplementedException("The member Color? StatusBar.ForegroundColor is not implemented. For more information, visit https://aka.platform.uno/notimplemented#m=Color%3F%20StatusBar.ForegroundColor");
			set => global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.ViewManagement.StatusBar", "Color? StatusBar.ForegroundColor");
		}

		[global::Uno.NotImplemented("__TVOS__", "IS_UNIT_TESTS", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__")]
		public double BackgroundOpacity
		{
			get => throw new global::System.NotImplementedException("The member double StatusBar.BackgroundOpacity is not implemented. For more information, visit https://aka.platform.uno/notimplemented#m=double%20StatusBar.BackgroundOpacity");
			set => global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.ViewManagement.StatusBar", "double StatusBar.BackgroundOpacity");
		}

		[global::Uno.NotImplemented("__TVOS__", "IS_UNIT_TESTS", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__")]
		public Color? BackgroundColor
		{
			get => throw new global::System.NotImplementedException("The member Color? StatusBar.BackgroundColor is not implemented. For more information, visit https://aka.platform.uno/notimplemented#m=Color%3F%20StatusBar.BackgroundColor");
			set => global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.ViewManagement.StatusBar", "Color? StatusBar.BackgroundColor");
		}

		[global::Uno.NotImplemented("__TVOS__", "IS_UNIT_TESTS", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__")]
		public Rect OccludedRect
		{
			get => throw new global::System.NotImplementedException("The member Rect StatusBar.OccludedRect is not implemented. For more information, visit https://aka.platform.uno/notimplemented#m=Rect%20StatusBar.OccludedRect");
		}

		[global::Uno.NotImplemented("__TVOS__", "IS_UNIT_TESTS", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__")]
		public StatusBarProgressIndicator ProgressIndicator
		{
			get => throw new global::System.NotImplementedException("The member StatusBarProgressIndicator StatusBar.ProgressIndicator is not implemented. For more information, visit https://aka.platform.uno/notimplemented#m=StatusBarProgressIndicator%20StatusBar.ProgressIndicator");
		}

		[global::Uno.NotImplemented("__TVOS__", "IS_UNIT_TESTS", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__")]
		public IAsyncAction ShowAsync()
		{
			throw new global::System.NotImplementedException("The member IAsyncAction StatusBar.ShowAsync() is not implemented. For more information, visit https://aka.platform.uno/notimplemented#m=IAsyncAction%20StatusBar.ShowAsync%28%29");
		}

		[global::Uno.NotImplemented("__TVOS__", "IS_UNIT_TESTS", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__")]
		public IAsyncAction HideAsync()
		{
			throw new global::System.NotImplementedException("The member IAsyncAction StatusBar.HideAsync() is not implemented. For more information, visit https://aka.platform.uno/notimplemented#m=IAsyncAction%20StatusBar.HideAsync%28%29");
		}

		[global::Uno.NotImplemented("__TVOS__", "IS_UNIT_TESTS", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__")]
		public static StatusBar GetForCurrentView()
		{
			throw new global::System.NotImplementedException("The member StatusBar StatusBar.GetForCurrentView() is not implemented. For more information, visit https://aka.platform.uno/notimplemented#m=StatusBar%20StatusBar.GetForCurrentView%28%29");
		}

		[global::Uno.NotImplemented("__TVOS__", "IS_UNIT_TESTS", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__")]
		public event TypedEventHandler<StatusBar, object> Hiding
		{
			[global::Uno.NotImplemented("__TVOS__", "IS_UNIT_TESTS", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__")]
			add => global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.ViewManagement.StatusBar", "event TypedEventHandler<StatusBar, object> StatusBar.Hiding");
			[global::Uno.NotImplemented("__TVOS__", "IS_UNIT_TESTS", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__")]
			remove => global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.ViewManagement.StatusBar", "event TypedEventHandler<StatusBar, object> StatusBar.Hiding");
		}

		[global::Uno.NotImplemented("__TVOS__", "IS_UNIT_TESTS", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__")]
		public event TypedEventHandler<StatusBar, object> Showing
		{
			[global::Uno.NotImplemented("__TVOS__", "IS_UNIT_TESTS", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__")]
			add => global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.ViewManagement.StatusBar", "event TypedEventHandler<StatusBar, object> StatusBar.Showing");
			[global::Uno.NotImplemented("__TVOS__", "IS_UNIT_TESTS", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__")]
			remove => global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.ViewManagement.StatusBar", "event TypedEventHandler<StatusBar, object> StatusBar.Showing");
		}
	}
}
#endif
