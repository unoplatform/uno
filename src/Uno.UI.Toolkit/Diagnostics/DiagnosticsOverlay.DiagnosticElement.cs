#nullable enable
#pragma warning disable IDE0055 // Invalid suggestion for switch statement
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Uno.Foundation.Logging;

namespace Uno.Diagnostics.UI;

public sealed partial class DiagnosticsOverlay
{
	private record DiagnosticElement(DiagnosticsOverlay Overlay, IDiagnosticViewProvider Provider, IDiagnosticViewContext Coordinator) : IDisposable
	{
		private UIElement? _preview;
		private CancellationTokenSource? _details;

		public UIElement Preview => _preview ??= CreatePreview();

		private UIElement CreatePreview()
		{
			try
			{
				var preview = Provider.GetPreview(Coordinator);
				var element = preview as UIElement ?? DiagnosticViewHelper.CreateText(preview.ToString());

				if (ToolTipService.GetToolTip(element) is null)
				{
					ToolTipService.SetToolTip(element, Provider.Name);
				}

				if (element is not ButtonBase)
				{
					element.Tapped += (snd, e) => ShowDetails();
				}

				return element;
			}
			catch (Exception e)
			{
				this.Log().Error($"Failed to get preview for {Provider.Name}.", e);

				var element = DiagnosticViewHelper.CreateText("**");
				ToolTipService.SetToolTip(element, $"Failed to get preview for {Provider.Name}.");
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
					Interlocked.Exchange(ref _details, ct)?.Cancel();

					var details = await Provider.GetDetailsAsync(Coordinator, ct.Token);
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
							flyout.ShowAt(Preview, new FlyoutShowOptions());
							ct.Token.Register(flyout.Hide);
							break;
						}

						default:
						{
							var dialog = new ContentDialog
							{
								XamlRoot = Overlay._root,
								Title = Provider.Name,
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
					this.Log().Error($"Failed to show details for {Provider.Name}.", e);
				}
			}
		}

		public void HideDetails()
			=> Interlocked.Exchange(ref _details, new CancellationTokenSource())?.Cancel();

		public void Dispose()
			=> Interlocked.Exchange(ref _details, new CancellationTokenSource())?.Cancel();
	}
}
