using System;
using System.Collections.Generic;
using System.Text;
using Uno.Extensions;
using Windows.UI.Core;
using Microsoft.UI.Xaml.Controls;
using Uno.Disposables;
using System.Threading.Tasks;

namespace UITests.Shared.Windows_UI_Xaml_Controls.ListView
{
	/// <summary>
	/// Tracks number of times Measure and Arrange are called.
	/// </summary>
	public partial class MeasureDetectorControl : ContentControl
	{
		private TextBlock _measureCountTextBlock;
		private TextBlock _arrangeCountTextBlock;
		private TextBlock _stateTextBlock;

		private readonly SerialDisposable _stateSetterDisposable = new SerialDisposable();

		private int _measureCount;
		private int _arrangeCount;

		protected override void OnApplyTemplate()
		{
			base.OnApplyTemplate();
			_measureCountTextBlock = GetTemplateChild("MeasureCountTextBlock") as TextBlock;
			_arrangeCountTextBlock = GetTemplateChild("ArrangeCountTextBlock") as TextBlock;
			_stateTextBlock = GetTemplateChild("StateTextBlock") as TextBlock;
			var inner = GetTemplateChild("MeasureDetectorInner") as MeasureDetectorInner;

			var wr = new WeakReference<MeasureDetectorControl>(this);

			inner.WasMeasured += async () =>
			{
				var that = wr.GetTarget();
				that._measureCount++;
				await that.Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
				{
					that._measureCountTextBlock.Text = that._measureCount.ToString();
					that._stateTextBlock.Text = "Measuring";
				});

				await UpdateState(that);
			};

			inner.WasArranged += async () =>
			{
				var that = wr.GetTarget();
				that._arrangeCount++;
				await that.Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
				{
					that._arrangeCountTextBlock.Text = that._arrangeCount.ToString();
					that._stateTextBlock.Text = "Measuring";
				});

				await UpdateState(that);
			};
		}

		private static async Task UpdateState(MeasureDetectorControl that)
		{
			var cd = new CancellationDisposable();
			that._stateSetterDisposable.Disposable = cd;
			var ct = cd.Token;
			await Task.Delay(50);
			await that.Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
			{
				if (!ct.IsCancellationRequested)
				{
					that._stateTextBlock.Text = "Measured";
				}
			});
		}
	}
}
