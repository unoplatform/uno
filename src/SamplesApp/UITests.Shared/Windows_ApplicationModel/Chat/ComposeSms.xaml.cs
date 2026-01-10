using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices.WindowsRuntime;
using Uno.UI.Samples.Controls;
using Windows.ApplicationModel.Chat;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;

namespace UITests.Shared.Windows_ApplicationModel.Chat
{
	[Sample("Windows.ApplicationModel", "ChatMessageManager", ignoreInSnapshotTests: true, description: "Test the ChatMessageManager.ShowComposeSmsMessageAsync API.")]
	public sealed partial class ComposeSms : UserControl, System.ComponentModel.INotifyPropertyChanged
	{
		private string _phoneNumber;
		private string _body;

		public ComposeSms()
		{
			this.InitializeComponent();
		}

		public ObservableCollection<string> PhoneNumbers { get; } = new ObservableCollection<string>();

		public string PhoneNumber
		{
			get => _phoneNumber;
			set
			{
				_phoneNumber = value;
				OnPropertyChanged();
			}
		}

		public string Body
		{
			get => _body;
			set
			{
				_body = value;
				OnPropertyChanged();
			}
		}

		public event System.ComponentModel.PropertyChangedEventHandler PropertyChanged;

		public void AddNumber_Click(object sender, RoutedEventArgs e)
		{
			PhoneNumbers.Add(PhoneNumber);
			PhoneNumber = "";
		}

		public void ClearNumbers_Click(object sender, RoutedEventArgs e)
		{
			PhoneNumbers.Clear();
		}

		public async void Compose_Click(object sender, RoutedEventArgs e)
		{
			var chatMessage = new ChatMessage()
			{
				Body = Body
			};
			foreach (var number in PhoneNumbers)
			{
				chatMessage.Recipients.Add(number);
			}
			await ChatMessageManager.ShowComposeSmsMessageAsync(chatMessage);
		}

		public void OnPropertyChanged([CallerMemberName] string propertyName = null)
		{
			PropertyChanged?.Invoke(this, new System.ComponentModel.PropertyChangedEventArgs(propertyName));
		}
	}
}
