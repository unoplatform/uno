using System;
using System.Linq;
using Windows.Security.Credentials;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Input;
using Uno.Extensions;
using Uno.UI.Samples.Controls;
using System.IO;
using System.Text;

#if __WASM__
using PasswordVault = UITests.Shared.Windows_Security_Credentials.PasswordVaultTests.PseudoPasswordVault;
#endif

namespace UITests.Shared.Windows_Security_Credentials.PasswordVaultTests
{
	[Sample("Windows.Security", Name = "PasswordVault")]
	public sealed partial class CredentialsPersistence : UserControl
	{
		public CredentialsPersistence()
		{
			this.InitializeComponent();
		}

		private void LoadFromVault(object sender, TappedRoutedEventArgs e)
		{
			_output.Text = new PasswordVault()
				.RetrieveAll()
				.Select(cred =>
				{
					cred.RetrievePassword();
					return $"[{cred.Resource}] {cred.UserName} - {cred.Password}";
				})
				.JoinBy(Environment.NewLine);
		}

		private void WriteToVault(object sender, TappedRoutedEventArgs e)
		{
			new PasswordVault().Add(new PasswordCredential(nameof(CredentialsPersistence), "test-user", Guid.NewGuid().ToString("N")));
		}
	}

#if __WASM__
	public sealed class PseudoPasswordVault : Windows.Security.Credentials.PasswordVault
	{
		public PseudoPasswordVault()
			: base(new UnsecuredPersister(Uno.Foundation.WebAssemblyRuntime.InvokeJS("(function(){ return window.location.hostname; })();")?.ToLowerInvariant()))
		{
		}
	}
#endif
}
