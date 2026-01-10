using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using System.Windows.Input;
using Uno.UI.Samples.Controls;
using Uno.UI.Samples.UITests.Helpers;
using Windows.ApplicationModel.Email;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Core;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;

using ICommand = System.Windows.Input.ICommand;
using EventHandler = System.EventHandler;

namespace UITests.Shared.Windows_ApplicationModel.Email
{
	/// <summary>
	/// An empty page that can be used on its own or navigated to within a Frame.
	/// </summary>
	[Sample(
		"Windows.ApplicationModel",
		"EmailManager",
		description: "Test the EmailManager.ShowComposeNewEmailAsync API.",
		viewModelType: typeof(EmailManagerViewModel))]
	public sealed partial class EmailManagerTests : Page
	{
		public EmailManagerTests()
		{
			this.InitializeComponent();
		}
	}

	internal class EmailManagerViewModel : ViewModelBase
	{
		private string _to = "";
		private string _cc = "";
		private string _bcc = "";
		private string _subject = "";
		private string _body = "";
		private string _errorMessage = "";

		public EmailManagerViewModel(Private.Infrastructure.UnitTestDispatcherCompat dispatcher)
			: base(dispatcher)
		{
		}

		public string To
		{
			get => _to;
			set
			{
				_to = value;
				RaisePropertyChanged();
			}
		}

		public string CC
		{
			get => _cc;
			set
			{
				_cc = value;
				RaisePropertyChanged();
			}
		}

		public string Bcc
		{
			get => _bcc;
			set
			{
				_bcc = value;
				RaisePropertyChanged();
			}
		}

		public string Subject
		{
			get => _subject;
			set
			{
				_subject = value;
				RaisePropertyChanged();
			}
		}

		public string Body
		{
			get => _body;
			set
			{
				_body = value;
				RaisePropertyChanged();
			}
		}

		public string ErrorMessage
		{
			get => _errorMessage;
			set
			{
				_errorMessage = value;
				RaisePropertyChanged();
			}
		}

		public ICommand ComposeCommand => GetOrCreateCommand(Compose);

		private async void Compose()
		{
			try
			{
				var toAddresses = To.Split(',');
				var email = new EmailMessage
				{
					Subject = Subject,
					Body = Body
				};
				AddAddresses(To.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries), email.To);
				AddAddresses(CC.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries), email.CC);
				AddAddresses(Bcc.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries), email.Bcc);

				await EmailManager.ShowComposeNewEmailAsync(email);
			}
			catch (Exception ex)
			{
				ErrorMessage = $"Error occured - {ex.Message}";
			}
		}

		private void AddAddresses(string[] addresses, IList<EmailRecipient> target)
		{
			foreach (var address in addresses)
			{
				target.Add(new EmailRecipient(address));
			}
		}
	}
}
