using System;
using System.Linq;
using System.Windows.Input;
using Uno.UI.Samples.Controls;
using Uno.UI.Samples.UITests.Helpers;
using Windows.ApplicationModel.Contacts;
using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;


namespace UITests.Windows_ApplicationModel.Contacts
{
	[Sample("Windows.ApplicationModel", Name = "Contacts_Pick", ViewModelType = typeof(PickContactViewModel))]
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

		internal PickContactViewModel Model { get; private set; }

		private void PickContact_DataContextChanged(DependencyObject sender, DataContextChangedEventArgs args)
		{
			Model = args.NewValue as PickContactViewModel;
		}
	}

	internal class PickContactViewModel : ViewModelBase
	{
		private string _status = "";
		private Contact[] _pickedContacts = Array.Empty<Contact>();
		private Contact _selectedContact = null;

		public PickContactViewModel(Private.Infrastructure.UnitTestDispatcherCompat dispatcher) : base(dispatcher)
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

		public Contact[] PickedContacts
		{
			get => _pickedContacts;
			set
			{
				_pickedContacts = value;
				RaisePropertyChanged();
			}
		}

		public Contact SelectedContact
		{
			get => _selectedContact;
			set
			{
				_selectedContact = value;
				RaisePropertyChanged();
			}
		}

		public ICommand PickCommand => GetOrCreateCommand(Pick);

		public ICommand PickMultipleCommand => GetOrCreateCommand(PickMultiple);

		private async void Pick()
		{
			if (!IsAvailable)
			{
				return;
			}

			var picker = new ContactPicker();
			var contact = await picker.PickContactAsync();
			if (contact != null)
			{
				PickedContacts = new[] { contact };
				SelectedContact = contact;
			}
			else
			{
				PickedContacts = Array.Empty<Contact>();
			}
		}

		private async void PickMultiple()
		{
			if (!IsAvailable)
			{
				return;
			}

			var picker = new ContactPicker();
			PickedContacts = (await picker.PickContactsAsync()).ToArray();
			SelectedContact = PickedContacts.FirstOrDefault();
		}
	}
}
