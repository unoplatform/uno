using Microsoft.UI.Xaml.Controls;
using Uno.UI.Samples.Controls;
using System;
using DisplayInfo = Windows.Graphics.Display.DisplayInformation;
using Uno.UI.Samples.UITests.Helpers;
using Windows.UI.Core;
using System.Windows.Input;
using System.Collections.ObjectModel;
using Uno.Disposables;
using Windows.Graphics.Display;

namespace UITests.Shared.Windows_Graphics_Display
{
	[Sample("Windows.Graphics.Display", "DisplayInformation", Description: "Shows the values and events from DisplayInformation class properties. N/A for not implemented.", ViewModelType: typeof(DisplayInformationTestsViewModel))]
	public sealed partial class DisplayInformationTests : Page
	{
		public DisplayInformationTests()
		{
			this.InitializeComponent();

			Loaded += (_, _) =>
			{
				if (DataContext is DisplayInformationTestsViewModel vm)
				{
#if HAS_UNO
					vm.SetDisplayInformation(Microsoft.UI.Xaml.XamlRoot.GetDisplayInformation(XamlRoot));
#else
					vm.SetDisplayInformation(DisplayInformation.GetForCurrentView());
#endif
				}
			};
		}
	}

	internal class DisplayInformationTestsViewModel : ViewModelBase
	{
		private bool _dpiChangesOn = false;
		private bool _orientationChangesOn = false;
		private DisplayInformation _displayInformation;

		public DisplayInformationTestsViewModel(Private.Infrastructure.UnitTestDispatcherCompat dispatcher) : base(dispatcher)
		{
			RefreshDisplayInformation();
			Disposables.Add(Disposable.Create(() =>
			{
				if (_displayInformation is { })
				{
					_displayInformation.DpiChanged -= DpiChanged;
					_displayInformation.OrientationChanged -= OrientationChanged;
				}
			}));
		}

		public void SetDisplayInformation(DisplayInformation displayInformation)
		{
			_displayInformation ??= displayInformation;
		}

		public ICommand RefreshCommand => GetOrCreateCommand(Refresh);

		private void Refresh() => RefreshDisplayInformation();

		public bool DpiChangesOn
		{
			get => _dpiChangesOn;
			set
			{
				_dpiChangesOn = value;
				RaisePropertyChanged();
			}
		}

		public bool OrientationChangesOn
		{
			get => _orientationChangesOn;
			set
			{
				_orientationChangesOn = value;
				RaisePropertyChanged();
			}
		}

		public string LastDpiChangeInfo { get; private set; }

		public string LastOrientationChangeInfo { get; private set; }

		public ObservableCollection<PropertyInformation> Properties { get; set; } = new ObservableCollection<PropertyInformation>();

		public ICommand ObserveDpiChangesCommand => GetOrCreateCommand(ObserveDpiChanges);

		private void ObserveDpiChanges()
		{
			if (_displayInformation is { } info)
			{
				if (DpiChangesOn)
				{
					info.DpiChanged += DpiChanged;
				}
				else
				{
					info.DpiChanged -= DpiChanged;
				}
			}
		}

		public ICommand ObserveOrientationChangesCommand => GetOrCreateCommand(ObserveOrientationChanges);

		private void ObserveOrientationChanges()
		{
			if (_displayInformation is { } info)
			{
				if (OrientationChangesOn)
				{
					info.OrientationChanged += OrientationChanged;
				}
				else
				{
					info.OrientationChanged -= OrientationChanged;
				}
			}
		}

		private void DpiChanged(DisplayInfo sender, object args)
		{
			LastDpiChangeInfo = $"{DateTime.Now.ToLongTimeString()} ({sender.LogicalDpi})";
			RaisePropertyChanged(nameof(LastDpiChangeInfo));
		}

		private void OrientationChanged(DisplayInfo sender, object args)
		{
			LastOrientationChangeInfo = $"{DateTime.Now.ToLongTimeString()} ({sender.CurrentOrientation})";
			RaisePropertyChanged(nameof(LastOrientationChangeInfo));
		}

		private void RefreshDisplayInformation()
		{
			if (_displayInformation is { } info)
			{
				var properties = new PropertyInformation[]
				{
					new PropertyInformation(nameof(DisplayInfo.AutoRotationPreferences), SafeGetValue(()=>DisplayInfo.AutoRotationPreferences)),
					new PropertyInformation(nameof(info.CurrentOrientation), SafeGetValue(()=>info.CurrentOrientation)),
					new PropertyInformation(nameof(info.NativeOrientation), SafeGetValue(()=>info.NativeOrientation)),
					new PropertyInformation(nameof(info.ScreenHeightInRawPixels), SafeGetValue(()=>info.ScreenHeightInRawPixels)),
					new PropertyInformation(nameof(info.ScreenWidthInRawPixels), SafeGetValue(()=>info.ScreenWidthInRawPixels)),
					new PropertyInformation(nameof(info.LogicalDpi), SafeGetValue(()=>info.LogicalDpi)),
					new PropertyInformation(nameof(info.DiagonalSizeInInches), SafeGetValue(()=>info.DiagonalSizeInInches)),
					new PropertyInformation(nameof(info.RawPixelsPerViewPixel), SafeGetValue(()=>info.RawPixelsPerViewPixel)),
					new PropertyInformation(nameof(info.RawDpiX), SafeGetValue(()=>info.RawDpiX)),
					new PropertyInformation(nameof(info.RawDpiY), SafeGetValue(()=>info.RawDpiY)),
					new PropertyInformation(nameof(info.ResolutionScale), SafeGetValue(()=>info.ResolutionScale)),
				};
				Properties = new ObservableCollection<PropertyInformation>(properties);
				RaisePropertyChanged(nameof(Properties));
			}
		}

		private string SafeGetValue<T>(Func<T> getter)
		{
			try
			{
				var value = getter();
				if (value == null)
				{
					return "(null)";
				}
				return Convert.ToString(value);
			}
			catch (NotImplementedException)
			{
				return "(Not implemented)";
			}
			catch (NotSupportedException)
			{
				return "(Not supported)";
			}
		}

		public class PropertyInformation
		{
			public PropertyInformation(string name, string value)
			{
				Name = name;
				Value = value;
			}

			public string Name { get; set; }

			public string Value { get; set; }

		}
	}
}
