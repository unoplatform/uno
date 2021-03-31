using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Windows.Web.Http;

namespace Uno.UI.RuntimeTests.Tests.Windows_Web.Http
{
	[TestClass]
	public class Given_HttpCookie
    {
        [TestMethod]
		public void When_Basic_Cookie_Serialized()
		{
			var cookie = new HttpCookie("test", "a.com", "/");
			var serialized = cookie.ToString();
			Assert.AreEqual("test=; Path=/; Domain=a.com", serialized);
		}

		[TestMethod]
		public void When_Cookie_With_Value_Serialized()
		{
			var cookie = new HttpCookie("test", "a.com", "/")
			{
				Value = "abc"
			};
			var serialized = cookie.ToString();
			Assert.AreEqual("test=abc; Path=/; Domain=a.com", serialized);
		}

		[TestMethod]
		public void When_Cookie_With_Expires_Serialized()
		{
			var cookie = new HttpCookie("test", "a.com", "/")
			{
				Expires = new DateTimeOffset(2021, 12, 4, 3, 5, 13, TimeSpan.Zero),
			};
			var serialized = cookie.ToString();
			Assert.AreEqual("test=; Path=/; Domain=a.com; Expires=Sat, 04 Dec 2021 03:05:13 GMT", serialized);
		}

		[TestMethod]
		public void When_Cookie_With_Secure_Serialized()
		{
			var cookie = new HttpCookie("test", "a.com", "/")
			{
				Secure = true
			};
			var serialized = cookie.ToString();
			Assert.AreEqual("test=; Path=/; Domain=a.com; Secure", serialized);
		}

		[TestMethod]
		public void When_Cookie_With_HttpOnly_Serialized()
		{
			var cookie = new HttpCookie("test", "a.com", "/")
			{
				HttpOnly = true
			};
			var serialized = cookie.ToString();
			Assert.AreEqual("test=; Path=/; Domain=a.com; HttpOnly", serialized);
		}

		[TestMethod]
		public void When_Cookie_With_All_Properties_Serialized()
		{
			var cookie = new HttpCookie("test", "subdomain.example.com", "/some/path")
			{
				Value = "abc",
				Expires = new DateTimeOffset(2021, 12, 4, 3, 5, 13, TimeSpan.Zero),
				HttpOnly = true,
				Secure = true,
			};
			var serialized = cookie.ToString();
			Assert.AreEqual("test=abc; Path=/some/path; Domain=subdomain.example.com; Secure; HttpOnly; Expires=Sat, 04 Dec 2021 03:05:13 GMT", serialized);
		}

		[TestMethod]
		public void When_Cookie_Value_Invalid_Characters()
		{
			var cookie = new HttpCookie("test", "a.com", "/")
			{
				Value = ";;==;;"
			};
			var serialized = cookie.ToString();
			Assert.AreEqual("test=;;==;;; Path=/; Domain=a.com", serialized);
		}
	}
}
