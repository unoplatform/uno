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
	public class Given_EmailMessage
	{
		[TestMethod]
		public void When_Sender_Default()
		{
			var emailMessage = new EmailMessage();
			Assert.IsNotNull(emailMessage.Sender);
		}

		[TestMethod]
		public void When_Recipients_Default()
		{
			var emailMessage = new EmailMessage();
			Assert.AreEqual(0, emailMessage.To.Count);
		}

		[TestMethod]
		public void When_CC_Default()
		{
			var emailMessage = new EmailMessage();
			Assert.AreEqual(0, emailMessage.CC.Count);
		}

		[TestMethod]
		public void When_Bcc_Default()
		{
			var emailMessage = new EmailMessage();
			Assert.AreEqual(0, emailMessage.Bcc.Count);
		}

		[TestMethod]
		public void When_Subject_Default()
		{
			var emailMessage = new EmailMessage();
			Assert.AreEqual(string.Empty, emailMessage.Subject);
		}

		[TestMethod]
		public void When_Body_Default()
		{
			var emailMessage = new EmailMessage();
			Assert.AreEqual(string.Empty, emailMessage.Body);
		}

		[TestMethod]
		public void When_Body_Set_Null()
		{
			var emailMessage = new EmailMessage();
			Assert.ThrowsException<ArgumentNullException>(
				() => emailMessage.Body = null);
		}

		[TestMethod]
		public void When_Subject_Set_Null()
		{
			var emailMessage = new EmailMessage();
			Assert.ThrowsException<ArgumentNullException>(
				() => emailMessage.Subject = null);
		}

		[TestMethod]
		public void When_Sender_Set_Null()
		{
			var emailMessage = new EmailMessage();
			//this should not throw
			emailMessage.Sender = null;
			Assert.IsNull(emailMessage.Sender);
		}

		[TestMethod]
		public void When_Null_To_Recipient_Is_Added()
		{
			var emailMessage = new EmailMessage();
			Assert.ThrowsException<ArgumentNullException>(
				() => emailMessage.To.Add(null));
		}

		[TestMethod]
		public void When_Null_CC_Recipient_Is_Added()
		{
			var emailMessage = new EmailMessage();
			Assert.ThrowsException<ArgumentNullException>(
				() => emailMessage.CC.Add(null));
		}

		[TestMethod]
		public void When_Null_Bcc_Recipient_Is_Added()
		{
			var emailMessage = new EmailMessage();
			Assert.ThrowsException<ArgumentNullException>(
				() => emailMessage.Bcc.Add(null));
		}
	}
}
#endif
