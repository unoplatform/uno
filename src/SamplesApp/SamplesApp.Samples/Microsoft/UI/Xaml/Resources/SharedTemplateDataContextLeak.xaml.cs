using System;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Uno.UI.Samples.Controls;

namespace UITests.Shared.Windows_UI_Xaml.Resources
{
	[Sample("Resources",
		Name = "SharedTemplateDataContextLeak",
		Description = "A shared DataTemplate/Style set on plain custom DPs must not keep the removed owner's DataContext (or the owner) alive.",
		IsManualTest = true,
		IgnoreInSnapshotTests = true)]
	public sealed partial class SharedTemplateDataContextLeak : UserControl
	{
		private int _runs;
		private bool _autoRunStarted;

		public SharedTemplateDataContextLeak()
		{
			this.InitializeComponent();

			// Run once on load, so the sample also works as a non-interactive repro (result is printed to the console).
			Loaded += (_, _) =>
			{
				if (!_autoRunStarted)
				{
					_autoRunStarted = true;
					OnRun(this, null);
				}
			};
		}

		private async void OnRun(object sender, RoutedEventArgs e)
		{
			RunButton.IsEnabled = false;
			try
			{
				var (weakViewModel, weakHolder) = AddHolder();
				await Task.Delay(100); // Let it load, so the DataContext is propagated.

				var sb = new StringBuilder();
				sb.AppendLine($"Run #{++_runs}");

				Host.Children.Clear();
				await Task.Delay(100);

				for (var i = 0; i < 5 && (weakViewModel.IsAlive || weakHolder.IsAlive); i++)
				{
					GC.Collect();
					GC.WaitForPendingFinalizers();
					GC.Collect();
					await Task.Delay(50);
				}

				sb.AppendLine($"  after remove+GC: viewModel alive = {weakViewModel.IsAlive}, control alive = {weakHolder.IsAlive}");
				sb.AppendLine(weakViewModel.IsAlive || weakHolder.IsAlive ? "  RESULT: LEAK" : "  RESULT: OK");

				StatusText.Text = sb.ToString();
				Console.WriteLine("[SharedTemplateDataContextLeak] " + sb.ToString());
			}
			finally
			{
				RunButton.IsEnabled = true;
			}
		}

		[MethodImpl(MethodImplOptions.NoInlining)]
		private (WeakReference ViewModel, WeakReference Holder) AddHolder()
		{
			var viewModel = new LeakViewModel { Name = $"vm{_runs + 1}" };
			var holder = new LeakTemplateHolder
			{
				Style = (Style)Resources["LeakHolderStyle"],
				DataContext = viewModel,
			};
			Host.Children.Add(holder);
			return (new WeakReference(viewModel), new WeakReference(holder));
		}

		private sealed class LeakViewModel
		{
			public string Name { get; set; }

			public byte[] Payload { get; } = new byte[10 * 1024 * 1024];
		}
	}

	/// <summary>
	/// Mirrors a third-party control declaring template/style properties with plain <see cref="PropertyMetadata"/>.
	/// </summary>
	public partial class LeakTemplateHolder : Control
	{
		public static DependencyProperty ItemTemplateProperty { get; } = DependencyProperty.Register(
			nameof(ItemTemplate), typeof(DataTemplate), typeof(LeakTemplateHolder), new PropertyMetadata(null));

		public DataTemplate ItemTemplate
		{
			get => (DataTemplate)GetValue(ItemTemplateProperty);
			set => SetValue(ItemTemplateProperty, value);
		}

		public static DependencyProperty ItemStyleProperty { get; } = DependencyProperty.Register(
			nameof(ItemStyle), typeof(Style), typeof(LeakTemplateHolder), new PropertyMetadata(null));

		public Style ItemStyle
		{
			get => (Style)GetValue(ItemStyleProperty);
			set => SetValue(ItemStyleProperty, value);
		}
	}
}
