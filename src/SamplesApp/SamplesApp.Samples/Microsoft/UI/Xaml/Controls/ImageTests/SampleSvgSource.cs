using System;

namespace UITests.Windows_UI_Xaml_Controls.ImageTests;

public class SampleSvgSource
{
	public SampleSvgSource(string name, Uri uri)
	{
		Name = name;
		Uri = uri;
	}

	public string Name { get; }

	public Uri Uri { get; }
}
