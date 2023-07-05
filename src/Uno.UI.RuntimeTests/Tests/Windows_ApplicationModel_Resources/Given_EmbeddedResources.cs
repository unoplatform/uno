#nullable disable

using FluentAssertions.Execution;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using System.Threading;
using System.Linq;
using Windows.ApplicationModel.Resources;
using Windows.ApplicationModel.Resources.Core;
using Windows.UI.Xaml;

namespace Uno.UI.RuntimeTests.Tests
{
	[TestClass]
	public class Given_EmbeddedResources
	{
		[TestMethod]
#if __MACOS__
		[Ignore("Currently fails on macOS, part of #9282 epic")]
#elif __IOS__ || __ANDROID__
		[Ignore("Currently fails https://github.com/unoplatform/uno/issues/9080")]
#endif
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
