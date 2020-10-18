#nullable enable

using System;
using System.Threading.Tasks;
using System.Windows.Input;
using Uno.UI.Samples.Controls;
using Uno.UI.Samples.UITests.Helpers;
using Windows.ApplicationModel.Contacts;
using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;


namespace UITests.Windows_ApplicationModel.Contacts
{
	[Sample("Windows.ApplicationModel", Name = "Contacts_PickContact", ViewModelType = typeof(PickContactViewModel))]
	public sealed partial class PickContact : Page
	{
		public PickContact()
		{
			this.InitializeComponent();
			this.DataContextChanged += PickContact_DataContextChanged;
			this.Loaded += PickContact_Loaded;
		}

		private void PickContact_Loaded(object sender, RoutedEventArgs e)
		{
			Model?.Load();
		}

		public PickContactViewModel? Model { get; private set; }

		private void PickContact_DataContextChanged(DependencyObject sender, DataContextChangedEventArgs args)
		{
			Model = args.NewValue as PickContactViewModel;
		}
	}

	public class PickContactViewModel : ViewModelBase
	{
		private string _status = "";
		private Contact? _contact = null;

		public PickContactViewModel(CoreDispatcher dispatcher) : base(dispatcher)
		{
		}

		public async void Load()
		{
			IsAvailable = await ContactPicker.IsSupportedAsync();
			RaisePropertyChanged(nameof(IsAvailable));
			if (!IsAvailable)
			{
				Status = "ContactPicker is not supported on this platform";
			}
			else
			{
				Status = "ContactPicker is supported";
			}
		}

		public bool IsAvailable { get; private set; }

		public string Status
		{
			get => _status;
			set
			{
				_status = value;
				RaisePropertyChanged();
			}
		}

		public Contact? Contact
		{
			get => _contact;
			set
			{
				_contact = value;
				RaisePropertyChanged();
			}
		}

		public ICommand PickCommand => GetOrCreateCommand(Pick);

		private async void Pick()
		{
			if (!IsAvailable)
			{
				return;
			}

			var picker = new ContactPicker();
			Contact = await picker.PickContactAsync();
		}
	}
}
