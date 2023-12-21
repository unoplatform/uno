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
	public class Given_EmailRecipient
	{
		[TestMethod]
		public void When_Address_Is_Null()
		{
			Assert.ThrowsException<ArgumentNullException>(
				() => new EmailRecipient(null));
		}

		[TestMethod]
		public void When_Name_Is_Null()
		{
			Assert.ThrowsException<ArgumentNullException>(
				() => new EmailRecipient("someone@example.com", null));
		}

		[TestMethod]
		public void When_Address_Set_Null()
		{
			var emailRecipient = new EmailRecipient();
			Assert.ThrowsException<ArgumentNullException>(
				() => emailRecipient.Address = null);
		}

		[TestMethod]
		public void When_Name_Set_Null()
		{
			var emailRecipient = new EmailRecipient();
			Assert.ThrowsException<ArgumentNullException>(
				() => emailRecipient.Name = null);
		}

		[TestMethod]
		public void When_Address_Default()
		{
			var emailRecipient = new EmailRecipient();
			Assert.AreEqual(string.Empty, emailRecipient.Address);
		}

		[TestMethod]
		public void When_Name_Default()
		{
			var emailRecipient = new EmailRecipient();
			Assert.AreEqual(string.Empty, emailRecipient.Name);
		}

		[TestMethod]
		public void When_Address_Is_Set()
		{
			var email = "someone@example.com";
			var emailRecipient = new EmailRecipient(email);
			Assert.AreEqual(email, emailRecipient.Address);
		}

		[TestMethod]
		public void When_Address_And_Name_Are_Set()
		{
			var email = "someone@example.com";
			var name = "John Doe";
			var emailRecipient = new EmailRecipient(email, name);
			Assert.AreEqual(email, emailRecipient.Address);
			Assert.AreEqual(name, emailRecipient.Name);
		}

		[TestMethod]
		public void When_Empty_Strings_Are_Set()
		{
			var emailRecipient = new EmailRecipient(string.Empty, string.Empty);
			Assert.AreEqual(string.Empty, emailRecipient.Address);
			Assert.AreEqual(string.Empty, emailRecipient.Name);
		}
	}
}
#endif
