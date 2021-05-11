using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Uno.UI.Tests.ResourceLoader.Controls;
using _ResourceLoader = Windows.ApplicationModel.Resources.ResourceLoader;

namespace Uno.UI.Tests.ResourceLoaderTests
{
	[TestClass]
	public class Given_ResourceLoader
	{
		[TestInitialize]
		public void Init()
		{
			CultureInfo.CurrentUICulture = new CultureInfo("en-US");
			_ResourceLoader.DefaultLanguage = "en-US";
			_ResourceLoader.AddLookupAssembly(GetType().Assembly);
		}

		[TestCleanup]
		public void Cleanup()
		{
			CultureInfo.CurrentUICulture = new CultureInfo("en-US");
			_ResourceLoader.DefaultLanguage = "en-US";
		}

		[TestMethod]
		public void When_ResourceFile_Neutral()
		{
			_ResourceLoader.DefaultLanguage = "en";

			Assert.AreEqual("App70-en", _ResourceLoader.GetForCurrentView().GetString("ApplicationName"));
		}

		[TestMethod]
		public void When_ResourceFile_Neutral_Both()
		{
			void setResources(string language)
			{
				CultureInfo.CurrentUICulture = new CultureInfo(language);
				_ResourceLoader.DefaultLanguage = language;
			}

			setResources("fr");
			Assert.AreEqual("App70-fr", _ResourceLoader.GetForCurrentView().GetString("ApplicationName"));

			setResources("fr-FR");
			Assert.AreEqual("App70-fr", _ResourceLoader.GetForCurrentView().GetString("ApplicationName"));

			setResources("en");
			Assert.AreEqual("App70-en", _ResourceLoader.GetForCurrentView().GetString("ApplicationName"));
		}

		[TestMethod]
		public void When_MissingLocalizedResource_FallbackOnParent()
		{
			var SUT = _ResourceLoader.GetForCurrentView();

			CultureInfo.CurrentUICulture = new CultureInfo("fr-FR");
			Assert.AreEqual(@"Text in 'fr'", SUT.GetString("Given_ResourceLoader/When_LocalizedResource"));
		}

		[TestMethod]
		public void When_MissingLocalizedResource_FallbackOnDefault()
		{
			var SUT = _ResourceLoader.GetForCurrentView();

			CultureInfo.CurrentUICulture = new CultureInfo("de-DE");
			Assert.AreEqual(@"Text in 'en'", SUT.GetString("Given_ResourceLoader/When_LocalizedResource"));
		}

		[TestMethod]
		public void When_Collection_And_InlineProperty()
		{
			var SUT = new When_Collection_And_InlineProperty();

			Assert.AreEqual(@"Header in 'en'", SUT.rb.Header);
		}
	}
}
