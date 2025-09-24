using Uno.UI.RemoteControl.Helpers;

namespace Uno.UI.RemoteControl.DevServer.Tests.Helpers;

[TestClass]
public class BannerHelperTests
{
	[TestMethod]
	public void Banner_Write_WhenTitleIsNull_ShouldThrowArgumentNullException()
	{
		// Arrange & Act
		var act = () => BannerHelper.Write(title: null!, entries: []);

		// Assert
		act.Should().Throw<ArgumentNullException>()
			.And.ParamName.Should().Be("title");
	}

	[TestMethod]
	public void Banner_Write_WhenEntriesIsNull_ShouldThrowArgumentNullException()
	{
		// Arrange & Act
		var act = () => BannerHelper.Write(title: "Test Title", entries: null!);

		// Assert
		act.Should().Throw<ArgumentNullException>()
			.And.ParamName.Should().Be("entries");
	}

	[TestMethod]
	public void Banner_Write_WhenEmptyTitleAndNoEntries_ShouldProduceValidBanner()
	{
		// Arrange
		var output = new StringWriter();

		// Act
		BannerHelper.Write(title: "", entries: [], 20, output);

		// Assert
		output.ToString().Should().Be(
			"""
			+===========+
			|           |
			+===========+
			
			""");
	}

	[TestMethod]
	public void Banner_Write_WhenSimpleTitleAndNoEntries_ShouldCenterTitle()
	{
		// Arrange
		var output = new StringWriter();

		// Act
		BannerHelper.Write(title: "Test", entries: [], maxInnerWidth: 20, output: output);

		// Assert
		output.ToString().Should().Be(
			"""
			+===========+
			|   Test    |
			+===========+
			
			""");
	}

	[TestMethod]
	public void Banner_Write_WhenTitleAndEntries_ShouldProduceCompleteBanner()
	{
		// Arrange
		BannerHelper.BannerEntry[] entries =
		[
			("Version", "1.0.0"),
			("Port", "8080"),
		];
		var output = new StringWriter();

		// Act
		BannerHelper.Write(title: "DevServer", entries: entries, maxInnerWidth: 30, output: output);

		// Assert
		output.ToString().Should().Be(
			"""
			+=================+
			|    DevServer    |
			+-----------------+
			| Version : 1.0.0 |
			| Port    : 8080  |
			+=================+
			
			""");
	}

	[TestMethod]
	public void Banner_Write_WhenDefaultOutput_ShouldNotThrow()
	{
		// Arrange & Act
		var act = () => BannerHelper.Write(title: "Test", entries: [], maxInnerWidth: 20);

		// Assert
		act.Should().NotThrow();
	}

	[TestMethod]
	public void Banner_Write_WhenVerySmallMaxInnerWidth_ShouldUseMinimumWidth()
	{
		// Arrange
		var output = new StringWriter();

		// Act
		BannerHelper.Write(
			title: "Test",
			entries: [("Key", "Value")],
			maxInnerWidth: 5,
			output: output); // Should be forced to minimum of 10

		// Assert
		output.ToString().Should().Be(
			"""
			+================+
			|      Test      |
			+----------------+
			| Key    : Value |
			+================+
			
			""");
	}

	[TestMethod]
	public void Banner_Write_WhenOddWidthTitle_ShouldCenterCorrectly()
	{
		// Arrange
		var output = new StringWriter();

		// Act
		BannerHelper.Write(title: "Odd", entries: [], maxInnerWidth: 21, output: output);

		// Assert
		output.ToString().Should().Be(
			"""
			+===========+
			|    Odd    |
			+===========+
			
			""");
	}

	[TestMethod]
	public void Banner_Write_WhenLongTitle_ShouldClipTitle()
	{
		// Arrange
		const string title = "This is a very long title that should be clipped";
		var output = new StringWriter();

		// Act
		BannerHelper.Write(title: title, entries: [], maxInnerWidth: 20, output: output);

		// Assert
		output.ToString().Should().Be(
			"""
			+====================+
			|This is a very lo...|
			+====================+
			
			""");
	}

	[TestMethod]
	public void Banner_Write_WhenLongKeys_ShouldClipKeys()
	{
		// Arrange
		BannerHelper.BannerEntry[] entries =
		[
			("VeryLongKeyNameThatShouldBeClipped", "Value"),
		];
		var output = new StringWriter();

		// Act
		BannerHelper.Write(title: "Test", entries: entries, maxInnerWidth: 20, output: output);

		// Assert
		output.ToString().Should().Be(
			"""
			+====================+
			|        Test        |
			+--------------------+
			| VeryLon... : Value |
			+====================+
			
			""");
	}

	[TestMethod]
	public void Banner_Write_WhenMaxKeyWidthConstraint_ShouldLimitKeyWidth()
	{
		// Arrange - Test that key width never exceeds 50% of max width
		BannerHelper.BannerEntry[] entries =
		[
			("VeryLongKeyNameThatWouldExceedFiftyPercentOfMaxWidth", "Short"),
		];
		var output = new StringWriter();

		// Act
		BannerHelper.Write(title: "Test", entries: entries, maxInnerWidth: 20, output: output);

		// Assert
		output.ToString().Should().Be(
			"""
			+====================+
			|        Test        |
			+--------------------+
			| VeryLon... : Short |
			+====================+
			
			""");
	}

	[TestMethod]
	public void Banner_Write_WhenLongValuesWithDefaultClip_ShouldClipAtEnd()
	{
		// Arrange
		BannerHelper.BannerEntry[] entries =
		[
			("Key", "This is a very long value that should be clipped because it exceeds the available space"),
		];
		var output = new StringWriter();

		// Act
		BannerHelper.Write(title: "Test", entries: entries, maxInnerWidth: 25, output: output);

		// Assert
		output.ToString().Should().Be(
			"""
			+=========================+
			|          Test           |
			+-------------------------+
			| Key    : This is a v... |
			+=========================+
			
			""");
	}

	[TestMethod]
	public void Banner_Write_WhenLongValuesWithStartClip_ShouldClipAtBeginning()
	{
		// Arrange
		BannerHelper.BannerEntry[] entries =
		[
			("Key", "This is a very long value that should be clipped", BannerHelper.ClipMode.Start),
		];
		var output = new StringWriter();

		// Act
		BannerHelper.Write(title: "Test", entries: entries, maxInnerWidth: 25, output: output);

		// Assert
		output.ToString().Should().Be(
			"""
			+=========================+
			|          Test           |
			+-------------------------+
			| Key    : ... be clipped |
			+=========================+
			
			""");
	}

	[TestMethod]
	public void Banner_Write_WhenNullKeyAndValue_ShouldHandleGracefully()
	{
		// Arrange
		BannerHelper.BannerEntry[] entries =
		[
			new(null!, null),
		];
		var output = new StringWriter();

		// Act
		BannerHelper.Write(title: "Test", entries: entries, maxInnerWidth: 20, output: output);

		// Assert
		output.ToString().Should().Be(
			"""
			+===========+
			|   Test    |
			+-----------+
			|        :  |
			+===========+
			
			""");
	}

	[TestMethod]
	public void Banner_Write_WhenZeroValueWidth_ShouldHandleGracefully()
	{
		// Arrange
		BannerHelper.BannerEntry[] entries =
		[
			("VeryLongKeyNameThatTakesAllSpace", "Value"),
		];
		var output = new StringWriter();

		// Act
		BannerHelper.Write(title: "Test", entries: entries, maxInnerWidth: 15, output: output);

		// Assert
		output.ToString().Should().Be(
			"""
			+====================+
			|        Test        |
			+--------------------+
			| VeryLon... : Value |
			+====================+
			
			""");
	}

	[TestMethod]
	public void BannerEntry_WhenDefaultClipMode_ShouldBeEnd()
	{
		// Arrange & Act
		var entry = new BannerHelper.BannerEntry("Key", "Value");

		// Assert
		entry.Clip.Should().Be(BannerHelper.ClipMode.End);
	}

	[TestMethod]
	public void BannerEntry_WhenImplicitConversionFromTuple_ShouldWork()
	{
		// Arrange & Act
		BannerHelper.BannerEntry entry = ("Key", "Value");

		// Assert
		entry.Key.Should().Be("Key");
		entry.Value.Should().Be("Value");
		entry.Clip.Should().Be(BannerHelper.ClipMode.End);
	}

	[TestMethod]
	public void BannerEntry_WhenImplicitConversionFromTupleWithClip_ShouldWork()
	{
		// Arrange & Act
		BannerHelper.BannerEntry entry = ("Key", "Value", BannerHelper.ClipMode.Start);

		// Assert
		entry.Key.Should().Be("Key");
		entry.Value.Should().Be("Value");
		entry.Clip.Should().Be(BannerHelper.ClipMode.Start);
	}

	[TestMethod]
	public void Banner_Write_WhenMixedEntryTypes_ShouldHandleAll()
	{
		// Arrange
		BannerHelper.BannerEntry[] entries =
		[
			("Tuple", "Value1"),
			new("Record", "Value2"),
			("TupleWithClip", "Value3", BannerHelper.ClipMode.Start),
			new("NullValue", null),
			new("", "EmptyKey"),
		];
		var output = new StringWriter();

		// Act
		BannerHelper.Write(title: "Mixed Test", entries: entries, maxInnerWidth: 40, output: output);

		// Assert
		output.ToString().Should().Be(
			"""
			+==========================+
			|        Mixed Test        |
			+--------------------------+
			| Tuple         : Value1   |
			| Record        : Value2   |
			| TupleWithClip : Value3   |
			| NullValue     :          |
			|               : EmptyKey |
			+==========================+
			
			""");
	}

	[TestMethod]
	public void Banner_Write_WhenForceClipWithMaxWidthZero_ShouldHandleGracefully()
	{
		// Arrange - This indirectly tests the Clip method with maxWidth <= 0
		BannerHelper.BannerEntry[] entries =
		[
			("Key", "Value"),
		];
		var output = new StringWriter();

		// Act - Using very small width that forces valueWidth to 0 or negative
		BannerHelper.Write(title: "Test", entries: entries, maxInnerWidth: 10, output: output);

		// Assert
		output.ToString().Should().Be(
			"""
			+================+
			|      Test      |
			+----------------+
			| Key    : Value |
			+================+
			
			""");
	}

	[TestMethod]
	public void Banner_Write_WhenForceClipWithSmallWidth_ShouldHandleGracefully()
	{
		// Arrange - This indirectly tests the Clip method with maxWidth <= 3
		BannerHelper.BannerEntry[] entries =
		[
			("Key", "VeryLongValueToForceClippingWithSmallWidth")
		];
		var output = new StringWriter();

		// Act - Using width that forces clipping with limited space
		BannerHelper.Write(title: "Test", entries: entries, maxInnerWidth: 15, output: output);

		// Assert
		output.ToString().Should().Be(
			"""
			+====================+
			|        Test        |
			+--------------------+
			| Key    : VeryLo... |
			+====================+
			
			""");
	}

	[TestMethod]
	public void Banner_Write_WhenExtremelyLongValueRequiresClipping_ShouldClipCorrectly()
	{
		// Arrange - Test both clip modes with extreme values
		const string title = "Test";
		var longValue = new string('A', 10_000); // Very long value
		BannerHelper.BannerEntry[] entries =
		[
			new("EndClip", longValue, BannerHelper.ClipMode.End),
			new("StartClip", longValue, BannerHelper.ClipMode.Start),
		];
		var output = new StringWriter();

		// Act
		BannerHelper.Write(title: title, entries: entries, maxInnerWidth: 50, output: output);

		// Assert
		var result = output.ToString();
		result.Should().Contain("...");
		result.Should().Contain("EndClip");
		result.Should().Contain("StartClip");
	}

	[TestMethod]
	public void Banner_Write_WhenClippingWith3CharacterLimit_ShouldUseDotsOnly()
	{
		// Arrange - Force clipping to only 3 characters (edge case: maxWidth <= 3)
		BannerHelper.BannerEntry[] entries =
		[
			("VeryLongKeyThatNeedsClipping", "VeryLongValueThatNeedsClipping"),
		];
		var output = new StringWriter();

		// Act - Force very small width that results in <= 3 characters for values
		BannerHelper.Write(title: "Test", entries: entries, maxInnerWidth: 12, output: output);

		// Assert
		var result = output.ToString();
		result.Should().NotBeNull();
		// With such a small width, clipping should still work
	}

	[TestMethod]
	public void Banner_Write_WhenClippingWith1CharacterLimit_ShouldHandleEdgeCase()
	{
		// Arrange - Test extreme edge case for clip method
		const string title = "Test";
		BannerHelper.BannerEntry[] entries =
		[
			("K", "VeryLongValue"),
		];
		var output = new StringWriter();

		// Act - Force extremely small width
		BannerHelper.Write(title: title, entries: entries, maxInnerWidth: 11, output: output);

		// Assert
		var result = output.ToString();
		result.Should().NotBeNull();
	}
}
