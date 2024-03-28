using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using System.Windows.Input;
using Uno.Disposables;
using Uno.UI.Samples.Controls;
using Uno.UI.Samples.UITests.Helpers;
using Windows.ApplicationModel.DataTransfer;
using Windows.UI.Core;
using Windows.UI.Xaml.Controls;
using Private.Infrastructure;

namespace UITests.Windows_ApplicationModel.DataTransfer
{
	[Sample("Windows.ApplicationModel", Name = nameof(DataTransferManager), ViewModelType = typeof(DataTransferManagerTestsViewModel))]
	public sealed partial class DataTransferManagerTests : Page
	{
		public DataTransferManagerTests()
		{
			this.InitializeComponent();
			this.DataContextChanged += DataTransferManagerTests_DataContextChanged;
		}

		internal DataTransferManagerTestsViewModel ViewModel { get; private set; }

		private void DataTransferManagerTests_DataContextChanged(Windows.UI.Xaml.DependencyObject sender, Windows.UI.Xaml.DataContextChangedEventArgs args)
		{
			ViewModel = args.NewValue as DataTransferManagerTestsViewModel;
		}
	}

	internal class DataTransferManagerTestsViewModel : ViewModelBase
	{
		private readonly DataTransferManager _dataTransferManager;
		private string _title = null;
		private string _description = null;
		private string _text = null;
		private string _uriText = null;
		private string _applicationLink = null;
		private string _webLink = null;
		private bool? _setDarkTheme = null;

		public DataTransferManagerTestsViewModel(Private.Infrastructure.UnitTestDispatcherCompat dispatcher) : base(dispatcher)
		{
			if (DataTransferManager.IsSupported())
			{
				_dataTransferManager = DataTransferManager.GetForCurrentView();
				_dataTransferManager.DataRequested += DataRequested;
				Disposables.Add(Disposable.Create(() =>
				{
					_dataTransferManager.DataRequested -= DataRequested;
				}));
			}
		}

		public ObservableCollection<string> EventLog { get; } = new ObservableCollection<string>();

		public ICommand ShowUICommand => GetOrCreateCommand(ShowUI);

		public ICommand ClearEventLogCommand => GetOrCreateCommand(ClearEventLog);

		public string Title
		{
			get => _title;
			set
			{
				_title = value;
				RaisePropertyChanged();
			}
		}

		public string Description
		{
			get => _description;
			set
			{
				_description = value;
				RaisePropertyChanged();
			}
		}

		public bool? SetDarkTheme
		{
			get => _setDarkTheme;
			set
			{
				_setDarkTheme = value;
				RaisePropertyChanged();
			}
		}

		public string Text
		{
			get => _text;
			set
			{
				_text = value;
				RaisePropertyChanged();
			}
		}

		public string UriText
		{
			get => _uriText;
			set
			{
				_uriText = value;
				RaisePropertyChanged();
			}
		}

		public string ApplicationLink
		{
			get => _applicationLink;
			set
			{
				_applicationLink = value;
				RaisePropertyChanged();
			}
		}

		public string WebLink
		{
			get => _webLink;
			set
			{
				_webLink = value;
				RaisePropertyChanged();
			}
		}

		private void ShowUI() => DataTransferManager.ShowShareUI(new ShareUIOptions()
		{
			Theme = GetTheme()
		});

		private ShareUITheme GetTheme()
		{
			if (SetDarkTheme == null)
			{
				return ShareUITheme.Default;
			}
			else if (SetDarkTheme == true)
			{
				return ShareUITheme.Dark;
			}
			else
			{
				return ShareUITheme.Light;
			}
		}

		private void ClearEventLog() => EventLog.Clear();

		private async void DataRequested(DataTransferManager sender, DataRequestedEventArgs args)
		{
			var deferral = args.Request.GetDeferral();

			LogEvent(nameof(DataTransferManager.DataRequested));

			// arbitrary delay to verify deferral functionality
			await Task.Delay(500);

			args.Request.Data.ShareCompleted += Data_ShareCompleted;
			args.Request.Data.ShareCanceled += Data_ShareCanceled;

			if (!string.IsNullOrEmpty(Title))
			{
				args.Request.Data.Properties.Title = Title;
			}

			if (!string.IsNullOrEmpty(Description))
			{
				args.Request.Data.Properties.Description = Description;
			}

			if (!string.IsNullOrEmpty(Text))
			{
				args.Request.Data.SetText(Text);
			}

			if (!string.IsNullOrEmpty(UriText) && Uri.TryCreate(UriText, UriKind.RelativeOrAbsolute, out var uri))
			{
#pragma warning disable CS0618 // Type or member is obsolete
				args.Request.Data.SetUri(uri);
#pragma warning restore CS0618 // Type or member is obsolete
			}

			if (!string.IsNullOrEmpty(WebLink) && Uri.TryCreate(WebLink, UriKind.RelativeOrAbsolute, out var webLink))
			{
				args.Request.Data.SetWebLink(webLink);
			}

			if (!string.IsNullOrEmpty(ApplicationLink) && Uri.TryCreate(ApplicationLink, UriKind.RelativeOrAbsolute, out var applicationLink))
			{
				args.Request.Data.SetApplicationLink(applicationLink);
			}

			deferral.Complete();
		}

		private void Data_ShareCanceled(DataPackage sender, object args)
		{
			sender.ShareCanceled -= Data_ShareCanceled;
			LogEvent(nameof(DataPackage.ShareCanceled));
		}

		private void Data_ShareCompleted(DataPackage sender, ShareCompletedEventArgs args)
		{
			sender.ShareCompleted -= Data_ShareCompleted;
			LogEvent(nameof(DataPackage.ShareCompleted), $"{args.ShareTarget?.AppUserModelId}, {args.ShareTarget?.ShareProvider?.Title}");
		}

		private async void LogEvent(string eventName, string args = null)
		{
			var logText = DateTime.UtcNow.ToLongTimeString() + ": " + eventName;
			if (args != null)
			{
				logText += $" ({args})";
			}

			await Dispatcher.RunAsync(UnitTestDispatcherCompat.Priority.Normal, () =>
			{
				EventLog.Insert(0, logText);
			});
		}
	}
}
