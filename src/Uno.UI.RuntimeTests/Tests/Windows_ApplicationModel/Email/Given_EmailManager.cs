#if WINAPPSDK || __IOS__ || __ANDROID__
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Windows.ApplicationModel.Email;

namespace Uno.UI.RuntimeTests.Tests.Windows_ApplicationModel.Email
{
	[TestClass]
	public class Given_EmailManager
	{
		[TestMethod]
		public async Task When_EmailMessage_Is_Null()
		{
			await Assert.ThrowsExceptionAsync<ArgumentNullException>(
				async () => await EmailManager.ShowComposeNewEmailAsync(null));
		}
	}
}
#endif
