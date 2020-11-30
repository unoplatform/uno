#nullable enable
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Windows.Storage.Streams;
using Windows.UI;
using FluentAssertions;
using Microsoft.Toolkit.Uwp.UI.Lottie;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Uno.Threading;

namespace Uno.UI.Tests.Lottie
{
	[TestClass]
	public class Given_DynamicReloadedLottieAnimatedVisualSource
	{
		[TestMethod]
		public async Task DynamicReloadedLottieAnimatedVisualSource_SimpleLoading()
		{
			var sut = new DynamicReloadedLottieAnimatedVisualSource();

			var results = new List<string>();

			await sut.LoadForTests(GetStream(), "cache-key", Callback);

			void Callback(string animationjson, string cachekey)
			{
				results.Add(animationjson);
			}

			results.Should().HaveCount(1);
		}

		[TestMethod]
		public async Task DynamicReloadedLottieAnimatedVisualSource_ValueSet_BeforeLoading()
		{
			var reference = new DynamicReloadedLottieAnimatedVisualSource();

			await reference.LoadForTests(GetStream(), "cache-key", (x, y) => { });

			var sut = new DynamicReloadedLottieAnimatedVisualSource();

			sut.SetColorProperty("Foreground", Color.FromArgb(1, 2, 3, 4));

			var results = new List<string>();

			await sut.LoadForTests(GetStream(), "cache-key", Callback);

			void Callback(string animationjson, string cachekey)
			{
				results.Add(animationjson);
			}

			results.Should().HaveCount(1);

			var jsonReference = reference.GetJson();
			sut.GetJson().Should().NotBe(jsonReference);
		}

		[TestMethod]
		public async Task DynamicReloadedLottieAnimatedVisualSource_ValueSet_AfterLoading()
		{
			var reference = new DynamicReloadedLottieAnimatedVisualSource();

			await reference.LoadForTests(GetStream(), "cache-key", (x, y) => { });

			var sut = new DynamicReloadedLottieAnimatedVisualSource();

			var results = new List<string>();

			await sut.LoadForTests(GetStream(), "cache-key", Callback);

			void Callback(string animationjson, string cachekey)
			{
				results.Add(animationjson);
			}

			results.Should().HaveCount(1);

			sut.SetColorProperty("Foreground", Color.FromArgb(1, 2, 3, 4));

			results.Should().HaveCount(2);

			var jsonReference = reference.GetJson();
			sut.GetJson().Should().NotBe(jsonReference);
		}

		private IInputStream GetStream(string name = "animation.json")
		{
			var type = GetType();
			var assembly = type.Assembly;
			var resourceName = "Uno.UI.Tests.Lottie." + name;
			var stream = assembly.GetManifestResourceStream(resourceName);

			if (stream == null)
			{
				throw new InvalidOperationException("Unable to find embedded resource named " + resourceName);
			}

			return stream.AsInputStream();
		}
	}
}
