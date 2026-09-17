#nullable enable

namespace Microsoft.UI.Xaml.Controls;

partial class StackLayoutState
{
	internal double Uno_LastReportedExtentMajorStart = double.NaN;
	internal uint Uno_ExtentMajorStartConfigurationVersion = uint.MaxValue;

	internal void Uno_ResetExtentMajorStart()
	{
		Uno_LastReportedExtentMajorStart = double.NaN;
		Uno_ExtentMajorStartConfigurationVersion = uint.MaxValue;
	}
}
