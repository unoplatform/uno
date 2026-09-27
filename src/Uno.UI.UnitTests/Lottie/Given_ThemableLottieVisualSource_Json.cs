#nullable enable
using System;
using System.IO;
using System.Text.Json;
using Windows.Storage.Streams;
using Windows.UI;
using Microsoft.VisualStudio.TestTools.UnitTesting;

#if HAS_UNO_WINUI
using CommunityToolkit.WinUI.Lottie;
#else
using Microsoft.Toolkit.Uwp.UI.Lottie;
#endif

namespace Uno.UI.Tests.Lottie
{
	/// <summary>
	/// Covers the JSON parse/serialize contract of <see cref="ThemableLottieVisualSource"/>, whose
	/// output is handed to Skottie as a string.
	/// </summary>
	[TestClass]
	public class Given_ThemableLottieVisualSource_Json
	{
		[TestMethod]
		public void When_Color_Applied_Then_Components_Are_Single_Precision()
		{
			var sut = new ThemableLottieVisualSource();
			sut.LoadForTests(GetStream(), "cache-key", (_, _) => { });

			sut.SetColorThemeProperty("Foreground", Color.FromArgb(4, 1, 2, 3));

			using var document = JsonDocument.Parse(sut.GetJson()!);
			var components = document.RootElement
				.GetProperty("layers")[0].GetProperty("shapes")[0].GetProperty("it")[1]
				.GetProperty("c").GetProperty("k");

			// Widening these to double would emit 0.00392156862745098 instead.
			components[0].GetSingle().Should().Be(1 / 255f);
			components[1].GetSingle().Should().Be(2 / 255f);
			components[2].GetSingle().Should().Be(3 / 255f);
			components[3].GetSingle().Should().Be(4 / 255f);
		}

		[TestMethod]
		public void When_Serialized_Then_Output_Is_Compact()
		{
			var sut = new ThemableLottieVisualSource();
			sut.LoadForTests(GetStream(), "cache-key", (_, _) => { });

			var json = sut.GetJson()!;

			// JsonNode.ToString() pretty-prints; only ToJsonString() stays compact.
			json.Should().NotContain("\n");
			json.Length.Should().BeLessThan(GetText("animation.json").Length);
		}

		[TestMethod]
		public void When_Members_Are_Null_Then_Document_Loads()
		{
			var sut = new ThemableLottieVisualSource();
			var results = 0;

			sut.LoadForTests(GetStream("animation-null-members.json"), "cache-key", (_, _) => results++);

			results.Should().Be(1);
			sut.GetJson().Should().NotBeNull();
		}

		[TestMethod]
		public void When_Object_Has_Trailing_Comma_Then_Document_Loads()
		{
			var sut = new ThemableLottieVisualSource();
			var results = 0;

			sut.LoadForTests(GetStream("animation-trailing-comma.json"), "cache-key", (_, _) => results++);

			results.Should().Be(1);
			sut.GetJson().Should().NotBeNull();
		}

		[TestMethod]
		public void When_Numbers_Are_Extreme_Then_Text_Is_Preserved()
		{
			var sut = new ThemableLottieVisualSource();
			sut.LoadForTests(GetStream("animation-numbers.json"), "cache-key", (_, _) => { });

			var json = sut.GetJson()!;

			// A lossy parser rewrites these to "Infinity" and 1.2345678901234568E+29.
			json.Should().Contain("1e999");
			json.Should().Contain("123456789012345678901234567890");
		}

		private static string GetText(string name)
		{
			using var stream = GetRawStream(name);
			using var reader = new StreamReader(stream);
			return reader.ReadToEnd();
		}

		private static IInputStream GetStream(string name = "animation.json")
			=> GetRawStream(name).AsInputStream();

		private static Stream GetRawStream(string name)
		{
			var resourceName = "Uno.UI.Tests.Lottie." + name;
			var stream = typeof(Given_ThemableLottieVisualSource_Json).Assembly.GetManifestResourceStream(resourceName);

			if (stream == null)
			{
				throw new InvalidOperationException("Unable to find embedded resource named " + resourceName);
			}

			return stream;
		}
	}
}
