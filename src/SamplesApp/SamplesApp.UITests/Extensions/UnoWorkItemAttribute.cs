using System;

namespace SamplesApp.UITests;

[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class, AllowMultiple = true)]
public sealed class UnoWorkItemAttribute : Attribute
{
	public UnoWorkItemAttribute(string url)
	{
		Url = url;
	}

	public string Url { get; }
}
