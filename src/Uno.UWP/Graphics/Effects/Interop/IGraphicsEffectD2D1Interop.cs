#nullable enable

using System;
using System.Runtime.InteropServices;

namespace Windows.Graphics.Effects.Interop
{
	[Guid("2FC57384-A068-44D7-A331-30982FCF7177")]
	public interface IGraphicsEffectD2D1Interop
	{
		Guid GetEffectId();
		void GetNamedPropertyMapping(string name, out uint index, out GraphicsEffectPropertyMapping mapping);
		uint GetPropertyCount();
		object? GetProperty(uint index); // This should be IPropertyValue but because PropertyValue and WinRT type boxing behaves differently under .NET than on native runtimes/languages (e.g. C++) we have to use object instead, for more info check Windows::Foundation::ValueFactory::CreateInspectableArray(...) at "onecore\com\winrt\wintypes\valuestore\valuefactory.cpp"
		IGraphicsEffectSource? GetSource(uint index);
		uint GetSourceCount();
	}
}
