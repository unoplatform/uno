using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Input;
using Uno.Extensions;
using Uno.UI.Samples.Controls;
using Uno.UI.Samples.UITests.Helpers;
using Windows.UI;
using Windows.UI.Core;
using Windows.UI.ViewManagement;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Private.Infrastructure;

namespace UITests.Windows_UI_ViewManagement
{
	[Sample("Windows.UI.ViewManagement", ViewModelType = typeof(UISettingsTestsViewModel))]
	public sealed partial class UISettingsTests : Page
	{
		public UISettingsTests()
		{
			this.InitializeComponent();
			this.DataContextChanged += UISettingsTests_DataContextChanged;
		}

		internal UISettingsTestsViewModel ViewModel { get; private set; }

		private void UISettingsTests_DataContextChanged(DependencyObject sender, DataContextChangedEventArgs args)
		{
			ViewModel = (UISettingsTestsViewModel)args.NewValue;
		}
	}

	internal class UISettingsTestsViewModel : ViewModelBase
	{
		private readonly UISettings _uiSettings = new UISettings();

		public UISettingsTestsViewModel(Private.Infrastructure.UnitTestDispatcherCompat dispatcher) : base(dispatcher)
		{
			_uiSettings.ColorValuesChanged += UiSettings_ColorValuesChanged;
			UpdateColors();
		}

		private async void UiSettings_ColorValuesChanged(UISettings sender, object args)
		{
			await Dispatcher.RunAsync(UnitTestDispatcherCompat.Priority.Normal, () => UpdateColors());
		}

		public string AnimationsEnabled => GetValueSafe(() => _uiSettings.AnimationsEnabled);

		public ObservableCollection<UIColorTypeListItem> UIColors { get; } = new ObservableCollection<UIColorTypeListItem>();

		public ICommand GetUIColorsCommand => GetOrCreateCommand(UpdateColors);

		private void UpdateColors()
		{
			UIColors.Clear();
			foreach (var colorType in Enum.GetValues<UIColorType>())
			{
				try
				{
					var color = _uiSettings.GetColorValue(colorType);
					UIColors.Add(new UIColorTypeListItem(colorType, color));
				}
				catch (ArgumentException)
				{
					// Color is not available - fallback
					UIColors.Add(new UIColorTypeListItem(colorType, Colors.Transparent));
				}
			}
		}

		private string GetValueSafe(Func<object> getter)
		{
			try
			{
				var value = getter();
				return value?.ToString() ?? "(null)";
			}
			catch (NotImplementedException)
			{
				return "N/A";
			}
		}
	}

	public class UIColorTypeListItem
	{
		public UIColorTypeListItem(UIColorType colorType, Color color)
		{
			ColorType = colorType;
			Color = color;
		}

		public UIColorType ColorType { get; }

		public Color Color { get; }
	}
}
