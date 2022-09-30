using Uno;
using Uno.Foundation;
using Uno.Foundation.Logging;
using Uno.Helpers;
using Windows.Foundation;
using Windows.UI.ViewManagement;

namespace Windows.ApplicationModel.Core;

public partial class CoreApplicationViewTitleBar
{
	private string JsType = "Windows.ApplicationModel.Core.CoreApplicationViewTitleBar";

	private readonly StartStopEventWrapper<TypedEventHandler<CoreApplicationViewTitleBar, object>> _layoutMetricsChangedWrapper;

	public CoreApplicationViewTitleBar()
	{
		_layoutMetricsChangedWrapper = new(StartLayoutMetricsChanged, StopLayoutMetricsChanged);
	}

	public bool ExtendViewIntoTitleBar
	{
		get =>
			bool.TryParse(WebAssemblyRuntime.InvokeJS($"{JsType}.isExtendedIntoTitleBar()"), out var isExtended) &&
			isExtended;
		set
		{
			if (this.Log().IsEnabled(LogLevel.Warning))
			{
				this.Log().LogWarning(
					"On WebAssembly we cannot programmatically move content into titlebar, " +
					"the user must do it manually in the PWA window.");
			}
		}
	}

	public double Height
	{
		get
		{
			var heightString = WebAssemblyRuntime.InvokeJS($"{JsType}.getHeight()");
			return double.TryParse(heightString, out var height) ? height : 0.0;
		}
	}

	public double SystemOverlayLeftInset
	{
		get
		{
			var leftInsetString = WebAssemblyRuntime.InvokeJS($"{JsType}.getLeftInset()");
			return double.TryParse(leftInsetString, out var leftInset) ? leftInset : 0.0;
		}
	}

	public double SystemOverlayRightInset
	{
		get
		{
			var leftInsetString = WebAssemblyRuntime.InvokeJS($"{JsType}.getRightInset()");
			return double.TryParse(leftInsetString, out var leftInset) ? leftInset : 0.0;
		}
	}

	public event TypedEventHandler<CoreApplicationViewTitleBar, object> LayoutMetricsChanged
	{
		add => _layoutMetricsChangedWrapper.AddHandler(value);
		remove => _layoutMetricsChangedWrapper.RemoveHandler(value);
	}

	private void StartLayoutMetricsChanged() => WebAssemblyRuntime.InvokeJS($"{JsType}.startLayoutMetricsChanged()");

	private void StopLayoutMetricsChanged() => WebAssemblyRuntime.InvokeJS($"{JsType}.stopLayoutMetricsChanged()");

	[Preserve]
	public static int DispatchLayoutMetricsChanged()
	{
		var titleBar = CoreApplication.GetCurrentView().TitleBar;
		titleBar.OnLayoutMetricsChanged();
		return 0;
	}

	private void OnLayoutMetricsChanged()
	{
		_layoutMetricsChangedWrapper.Event?.Invoke(this, null);
	}
}
