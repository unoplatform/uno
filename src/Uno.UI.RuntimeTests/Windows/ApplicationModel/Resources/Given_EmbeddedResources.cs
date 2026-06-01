using AwesomeAssertions.Execution;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using System.Threading;
using System.Linq;
using Windows.ApplicationModel.Resources;
using Windows.ApplicationModel.Resources.Core;
using Microsoft.UI.Xaml;

namespace Uno.UI.RuntimeTests.Tests
{
	[TestClass]
	public class Given_EmbeddedResources
	{
		[TestMethod]
		public void When_EmbeddedResource()
		{
			var assembly = Application.Current.GetType().Assembly;

			var resourceName = assembly.GetManifestResourceNames().FirstOrDefault(f => f.EndsWith("SubFolder1.SubFolder2.SharedProjectEmbeddedFile.txt"));
			Assert.IsNotNull(resourceName);

			using var s = assembly.GetManifestResourceStream(resourceName);

			Assert.AreNotEqual(0, s.Length);
		}
	}
}
