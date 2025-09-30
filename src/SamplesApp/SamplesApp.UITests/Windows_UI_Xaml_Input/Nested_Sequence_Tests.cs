using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Windows.Devices.Input;
using AwesomeAssertions.Execution;
using NUnit.Framework;
using SamplesApp.UITests.TestFramework;
using Uno.Testing;
using Uno.UITest.Helpers;
using Uno.UITest.Helpers.Queries;

namespace SamplesApp.UITests.Windows_UI_Xaml_Input
{
	[TestFixture]
	internal class Nested_Sequence_Tests : SampleControlUITestBase
	{
		private const string _sample = "UITests.Windows_UI_Input.PointersTests.Nested_Sequence";

#pragma warning disable SYSLIB1045
		private static readonly Regex _resultRegex = new Regex(@"\s*\[(?<element>\w+)\]\s+(?<evt>\w+)\s*(\((?<param>(?<key>\w+)\s*=\s*(?<value>\w+)|\s*\|\s*)+\))?");
#pragma warning restore SYSLIB1045

		private static KeyValuePair<string, string> _inRange = new("in_range", "true");
		private static KeyValuePair<string, string> _inContact = new("in_contact", "true");
		private static KeyValuePair<string, string> _notInRange = new("in_range", "false");
		private static KeyValuePair<string, string> _notInContact = new("in_contact", "false");

		[Test]
		[AutoRetry]
		[ActivePlatforms(Platform.Android, Platform.iOS)]
		[InjectedPointer(PointerDeviceType.Touch)]
#if IS_RUNTIME_UI_TESTS
		[Uno.UI.RuntimeTests.RequiresFullWindow]
#endif
		public async Task When_PressOnNestedAndReleaseOnContainer_Touch()
		{
			await RunAsync(_sample);

			var target = App.WaitForElement("_container").Single().Rect;
			App.DragCoordinates(target.CenterX, target.CenterY, target.Right - 10, target.CenterY);

			var result = App.Marked("_result").GetDependencyPropertyValue<string>("Text");
			var actual = Parse(result);
			var expected = new Expected[]
			{
				new("NESTED", "Entered", _inRange, _inContact),
				new("INTERMEDIATE", "Entered", _inRange, _inContact),
				new("CONTAINER", "Entered", _inRange, _inContact),
				new("NESTED", "Pressed", _inRange, _inContact),
				new("INTERMEDIATE", "Pressed", _inRange, _inContact),
				new("CONTAINER", "Pressed", _inRange, _inContact),
				new("NESTED", "Exited", _inRange, _inContact),
				new("INTERMEDIATE", "Exited", _inRange, _inContact),
				new("CONTAINER", "Released", _notInRange, _notInContact),
				new("CONTAINER", "Exited", _notInRange, _notInContact),
			};

			actual.Should().BeEquivalentTo(expected);
		}

		[Test]
		[AutoRetry]
#if !__SKIA__
		[Ignore("Inputs simulated by selenium are directly appreaing at the start location and wrongly inserting an exit.")]
		//[ActivePlatforms(Platform.Browser)]
#endif
#if IS_RUNTIME_UI_TESTS
		[Uno.UI.RuntimeTests.RequiresFullWindow]
#endif
		[InjectedPointer(PointerDeviceType.Mouse)]
		public async Task When_PressOnNestedAndReleaseOnContainer_Mouse()
		{
			await RunAsync(_sample);

			var target = App.WaitForElement("_container").Single().Rect;
			App.DragCoordinates(target.CenterX, target.CenterY, target.Right - 10, target.CenterY);

			var result = App.Marked("_result").GetDependencyPropertyValue<string>("Text");
			var actual = Parse(result);
			var expected = new Expected[]
			{
				new("CONTAINER", "Entered", _inRange, _notInContact),
				new("NESTED", "Entered", _inRange, _notInContact),
				new("INTERMEDIATE", "Entered", _inRange, _notInContact),
				new("NESTED", "Pressed", _inRange, _inContact),
				new("INTERMEDIATE", "Pressed", _inRange, _inContact),
				new("CONTAINER", "Pressed", _inRange, _inContact),
				new("NESTED", "Exited", _inRange, _inContact),
				new("INTERMEDIATE", "Exited", _inRange, _inContact),
				new("CONTAINER", "Released", _inRange, _notInContact)
			};

			actual.Should().BeEquivalentTo(expected);
		}

		[Test]
		[AutoRetry]
#if !__SKIA__
		[Ignore("Does not work due to the 'implicit capture'")]
#endif
		[InjectedPointer(PointerDeviceType.Touch)]
		public async Task When_PressOnContainerAndReleaseOnNested_Touch()
		{
			await RunAsync(_sample);

			var target = App.WaitForElement("_container").Single().Rect;
			App.DragCoordinates(target.X + 10, target.CenterY, target.CenterX, target.CenterY);

			var result = App.Marked("_result").GetDependencyPropertyValue<string>("Text");
			var actual = Parse(result);
			var expected = new Expected[]
			{
				new("CONTAINER", "Entered", _inRange, _inContact),
				new("CONTAINER", "Pressed", _inRange, _inContact),
				new("NESTED", "Entered", _inRange, _inContact),
				new("INTERMEDIATE", "Entered", _inRange, _inContact),
				new("NESTED", "Released", _notInRange, _notInContact),
				new("INTERMEDIATE", "Released", _notInRange, _notInContact),
				new("CONTAINER", "Released", _notInRange, _notInContact),
				new("NESTED", "Exited", _notInRange, _notInContact),
				new("INTERMEDIATE", "Exited", _notInRange, _notInContact),
				new("CONTAINER", "Exited", _notInRange, _notInContact),
			};

			actual.Should().BeEquivalentTo(expected);
		}

		[Test]
		[AutoRetry]
		[Ignore("Doesnt work on CI")]
		[ActivePlatforms(Platform.Browser)]
		[InjectedPointer(PointerDeviceType.Mouse)]
		public async Task When_PressOnContainerAndReleaseOnNested_Mouse()
		{
			await RunAsync(_sample);

			var target = App.WaitForElement("_container").Single().Rect;
			App.DragCoordinates(target.X + 10, target.CenterY, target.CenterX, target.CenterY);

			var result = App.Marked("_result").GetDependencyPropertyValue<string>("Text");
			var actual = Parse(result);
			var expected = new Expected[]
			{
				new("CONTAINER", "Entered", _inRange, _notInContact),
				new("CONTAINER", "Pressed", _inRange, _inContact),
				new("NESTED", "Entered", _inRange, _inContact),
				new("INTERMEDIATE", "Entered", _inRange, _inContact),
				new("NESTED", "Released", _inRange, _notInContact),
				new("INTERMEDIATE", "Released", _inRange, _notInContact),
				new("CONTAINER", "Released", _inRange, _notInContact),
			};

			actual.Should().BeEquivalentTo(expected);
		}

		private IEnumerable<PointerEventInfo> Parse(string text)
		{
			foreach (Match match in _resultRegex.Matches(text))
			{
				var line = new PointerEventInfo(match.Groups["element"].Value, match.Groups["evt"].Value);

				var keys = match.Groups["key"].Captures;
				var values = match.Groups["value"].Captures;
				Assert.AreEqual(keys.Count, values.Count);

				for (var i = 0; i < keys.Count; i++)
				{
					line[keys[i].Value.ToLowerInvariant()] = values[i].Value.ToLowerInvariant();
				}

				yield return line;
			}
		}

		private class PointerEventInfo : Dictionary<string, string>, IEquatable<Expected>
		{
			public PointerEventInfo(string element, string @event)
			{
				Element = element;
				Event = @event;
			}

			public string Element { get; }

			public string Event { get; }

			/// <inheritdoc />
			public bool Equals(Expected expected)
			{
				var success = true;
				if (!Element.Equals(expected.Element))
				{
					AssertionScope.Current.AddPreFormattedFailure($"Not the same element (expected: {expected.Element} | actual: {Element})");
					success = false;
				}

				if (!Event.Equals(expected.Event))
				{
					AssertionScope.Current.AddPreFormattedFailure($"Not the same event (expected: {expected.Event} | actual: {Event})");
					success = false;
				}

				foreach (var parameter in expected)
				{
					if (!TryGetValue(parameter.Key, out var value))
					{
						AssertionScope.Current.AddPreFormattedFailure($"Key {parameter.Key} is not set");
						success = false;
					}
					else if (!value.Equals(parameter.Value))
					{
						AssertionScope.Current.AddPreFormattedFailure($"Value of '{parameter.Key}' is not the same (expected: {parameter.Value} | actual: {value})");
						success = false;
					}
				}

				return success;
			}

			/// <inheritdoc />
			public override bool Equals(object obj)
				=> obj is Expected expected ? Equals(expected) : base.Equals(obj);

			public override int GetHashCode()
				=> Element.GetHashCode() ^ Event.GetHashCode();
		}

		private class Expected : PointerEventInfo
		{
			public Expected(string element, string @event, params KeyValuePair<string, string>[] parameters) : base(element, @event)
			{
				foreach (var parameter in parameters)
				{
					Add(parameter.Key, parameter.Value);
				}
			}
		}
	}
}
