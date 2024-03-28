using System;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Uno.UI.Samples.Controls;
using Uno.UI.Samples.UITests.Helpers;
using Windows.UI.Core;
using Windows.UI.Xaml.Controls;
using System.Windows.Input;
#if __WASM__
using Uno.Web.Http;
#endif

namespace UITests.Uno_Web.Http
{
#if __WASM__
	[Sample("Uno.Web", ViewModelType = typeof(CookieManagerTestsViewModel))]
#endif
	public sealed partial class CookieManagerTests : Page
	{
		public CookieManagerTests()
		{
			this.InitializeComponent();
			this.DataContextChanged += CookieManagerTests_DataContextChanged;
		}

		private void CookieManagerTests_DataContextChanged(Windows.UI.Xaml.DependencyObject sender, Windows.UI.Xaml.DataContextChangedEventArgs args)
		{
			ViewModel = args.NewValue as CookieManagerTestsViewModel;
		}

		internal CookieManagerTestsViewModel ViewModel { get; private set; }
	}

	internal class CookieManagerTestsViewModel : ViewModelBase
	{
		private string _cookiesText = string.Empty;

		public CookieManagerTestsViewModel(Private.Infrastructure.UnitTestDispatcherCompat dispatcher) : base(dispatcher)
		{
		}

#if __WASM__
		public string Name { get; set; }

		public string Value { get; set; }

		public string Path { get; set; }

		public string Domain { get; set; }

		public int MaxAge { get; set; }

		public DateTimeOffset Expires { get; set; } = DateTimeOffset.UtcNow.AddDays(1);

		public bool Secure { get; set; }

		public CookieSameSite[] SameSiteOptions { get; } = Enum.GetValues<CookieSameSite>();

		public CookieSameSite SameSite { get; set; }

		public string CookiesText
		{
			get => _cookiesText;
			set => Set(ref _cookiesText, value);
		}

		public ICommand SetCommand => GetOrCreateCommand(SetCookie);

		private void SetCookie()
		{
			var request = new SetCookieRequest(new Cookie(Name, Value));
			if (!string.IsNullOrEmpty(Path))
			{
				request.Path = Path;
			}

			if (!string.IsNullOrEmpty(Domain))
			{
				request.Domain = Domain;
			}

			if (MaxAge > 0)
			{
				request.MaxAge = MaxAge;
			}

			if (Expires >= DateTimeOffset.UtcNow)
			{
				request.Expires = Expires;
			}

			if (SameSite != CookieSameSite.Lax)
			{
				request.SameSite = SameSite;
			}

			request.Secure = Secure;

			CookieManager.GetDefault().SetCookie(request);
		}

		public ICommand GetCommand => GetOrCreateCommand(GetCookies);

		private void GetCookies()
		{
			var cookies = CookieManager.GetDefault().GetCookies();
			CookiesText = string.Join(",", cookies.Select(cookie => $"{cookie.Name}:{cookie.Value}"));
		}

		public ICommand DeleteCommand => GetOrCreateCommand(DeleteCookie);

		private void DeleteCookie()
		{
			CookieManager.GetDefault().DeleteCookie(Name, !string.IsNullOrEmpty(Path) ? Path : null);
		}
#endif
	}
}
