#nullable enable

using System;
using System.Globalization;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Uno.UI.Samples.Controls;
#if __CROSSRUNTIME__
using Uno.UI.NativeElementHosting;
#endif

namespace UITests.Shared.Wasm;

[Sample("Wasm",
	Name = nameof(Wasm_ViewportOrientationTelemetry),
	Description = "Compares the Uno Platform window size against the browser's documentElement size after device rotation. The banner turns STALE if the app-measured size lags the settled viewport size.",
	IsManualTest = true,
	IgnoreInSnapshotTests = true)]
public sealed partial class Wasm_ViewportOrientationTelemetry : Page
{
	private const string InstallCountersScript = """
		(function() {
			let t = globalThis.__unoViewportTelemetry;
			if (!t) {
				t = { resize: 0, orientation: 0 };
				globalThis.__unoViewportTelemetry = t;
				window.addEventListener("resize", function() { t.resize++; });
				window.addEventListener("orientationchange", function() { t.orientation++; });
			}
			return "ok";
		})();
		""";

	private const string PollScript = """
		(function() {
			const r = document.documentElement.getBoundingClientRect();
			const t = globalThis.__unoViewportTelemetry || { resize: 0, orientation: 0 };
			return Math.round(r.width) + "," + Math.round(r.height) + "," + t.resize + "," + t.orientation;
		})();
		""";

	private readonly DispatcherTimer _pollTimer;

#if __CROSSRUNTIME__
	private BrowserHtmlElement? _jsGateway;
	private string? _telemetryError;
#endif

	public Wasm_ViewportOrientationTelemetry()
	{
		this.InitializeComponent();

		// Poll on an interval rather than only on SizeChanged so a stuck stale
		// size is visibly stuck instead of sampled once right after rotation.
		_pollTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(100) };
		_pollTimer.Tick += (_, _) => UpdateTelemetry();

		Loaded += OnPageLoaded;
		Unloaded += OnPageUnloaded;
	}

	private void OnPageLoaded(object sender, RoutedEventArgs e)
	{
#if __CROSSRUNTIME__
		if (OperatingSystem.IsBrowser())
		{
			try
			{
				_jsGateway = BrowserHtmlElement.CreateHtmlElement("div");
				_jsGateway.ExecuteJavascript(InstallCountersScript);
			}
			catch (Exception ex)
			{
				DropGateway(ex.Message);
			}
		}
#endif

		_pollTimer.Start();
		UpdateTelemetry();
	}

	private void OnPageUnloaded(object sender, RoutedEventArgs e)
	{
		_pollTimer.Stop();

#if __CROSSRUNTIME__
		_jsGateway?.Dispose();
		_jsGateway = null;
#endif
	}

	private void UpdateTelemetry()
	{
		var unoSize = XamlRoot?.Size ?? default;
		UnoBoundsText.Text = $"{unoSize.Width:0} x {unoSize.Height:0}";

#if __CROSSRUNTIME__
		if (_jsGateway is not null)
		{
			try
			{
				var result = _jsGateway.ExecuteJavascript(PollScript);
				if (result.Split(',') is [var width, var height, var resize, var orientation]
					&& double.TryParse(width, NumberStyles.Float, CultureInfo.InvariantCulture, out var docWidth)
					&& double.TryParse(height, NumberStyles.Float, CultureInfo.InvariantCulture, out var docHeight))
				{
					DocRectText.Text = $"{docWidth:0} x {docHeight:0}";
					EventCountsText.Text = $"{resize} resize / {orientation} orientationchange";

					var stale = Math.Abs(unoSize.Width - docWidth) > 2 || Math.Abs(unoSize.Height - docHeight) > 2;
					SetStatus(stale ? "STALE" : "OK", isError: stale);
					return;
				}

				DropGateway($"unexpected telemetry format: {result}");
			}
			catch (Exception ex)
			{
				DropGateway(ex.Message);
			}
		}

		if (_telemetryError is not null)
		{
			DocRectText.Text = $"Telemetry failed: {_telemetryError}";
			EventCountsText.Text = "-";
			SetStatus("ERROR", isError: true);
			return;
		}
#endif

		DocRectText.Text = "DOM telemetry is available only when running in a browser";
		EventCountsText.Text = "-";
		StatusText.Text = "N/A";
	}

#if __CROSSRUNTIME__
	// Surface the failure on the page instead of throwing every timer tick — the
	// diagnostic must diagnose itself.
	private void DropGateway(string error)
	{
		_telemetryError = error;
		_jsGateway?.Dispose();
		_jsGateway = null;
	}
#endif

	// Allocate a brush only when the status actually transitions (the timer ticks at 10 Hz).
	private void SetStatus(string status, bool isError = false)
	{
		if (StatusText.Text != status)
		{
			StatusText.Text = status;
			StatusBorder.Background = new SolidColorBrush(isError ? Microsoft.UI.Colors.IndianRed : Microsoft.UI.Colors.MediumSeaGreen);
		}
	}
}
