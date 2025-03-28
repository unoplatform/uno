#nullable enable
#pragma warning disable IDE0055 // Invalid suggestion for switch statement
#if WINUI || HAS_UNO_WINUI
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Uno.Foundation.Logging;

namespace Uno.Diagnostics.UI;

public sealed partial class DiagnosticsOverlay
{
	private record DiagnosticElement(DiagnosticsOverlay Overlay, IDiagnosticView View, IDiagnosticViewContext Context) : IDisposable
	{
		private UIElement? _value;
		private CancellationTokenSource? _details;

		public UIElement Value => _value ??= CreateValue();

		private UIElement CreateValue()
		{
			try
			{
				var preview = View.GetElement(Context);
				var element = preview as UIElement ?? DiagnosticViewHelper.CreateText(preview.ToString());

				if (ToolTipService.GetToolTip(element) is null)
				{
					ToolTipService.SetToolTip(element, View.Name);
				}

				if (element is not ButtonBase)
				{
					element.Tapped += (snd, e) => ShowDetails();
				}

				return element;
			}
			catch (Exception e)
			{
				this.Log().Error($"Failed to get preview for {View.Name}.", e);

				var element = DiagnosticViewHelper.CreateText("**");
				ToolTipService.SetToolTip(element, $"Failed to get preview for {View.Name}.");
				return element;
			}
		}

		private void ShowDetails()
		{
			_ = Do();

			async ValueTask Do()
			{
				try
				{
					var ct = new CancellationTokenSource();
					if (Interlocked.Exchange(ref _details, ct) is { IsCancellationRequested: false } previous)
					{
						await previous.CancelAsync();
					}

					var details = await View.GetDetailsAsync(Context, ct.Token);
					switch (details)
					{
						case null:
							break;

						case ContentDialog dialog:
							dialog.XamlRoot = Overlay._root;
							await dialog.ShowAsync().AsTask(ct.Token);
							break;

						case UIElement element:
						{
							var flyout = new Flyout { Content = element };
							flyout.ShowAt(Value, new FlyoutShowOptions());
							ct.Token.Register(flyout.Hide);
							break;
						}

						default:
						{
							var dialog = new ContentDialog
							{
								XamlRoot = Overlay._root,
								Title = View.Name,
								Content = details.ToString(),
								CloseButtonText = "Close",
							};
							await dialog.ShowAsync().AsTask(ct.Token);
							break;
						}
					}
				}
				catch (Exception e)
				{
					this.Log().Error($"Failed to show details for {View.Name}.", e);
				}
			}
		}

		public void HideDetails()
			=> Interlocked.Exchange(ref _details, new CancellationTokenSource())?.Cancel();

		public void Dispose()
			=> Interlocked.Exchange(ref _details, new CancellationTokenSource())?.Cancel();
	}
}
#endif
