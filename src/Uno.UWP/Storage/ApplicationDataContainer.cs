using System.Diagnostics.CodeAnalysis;
using Windows.Foundation.Collections;

namespace Windows.Storage;

public partial class ApplicationDataContainer
{
	internal ApplicationDataContainer(ApplicationData owner, string name, ApplicationDataLocality locality)
	{
		Locality = locality;
		Name = name;

		InitializePartial(owner);
	}

	partial void InitializePartial(ApplicationData owner);

	public ApplicationDataLocality Locality { get; }

	public string Name { get; }

	public IPropertySet Values { get; private set; }
#if __NETSTD_REFERENCE__ || IS_UNIT_TESTS
		// initialized properly to non-null on Skia and Wasm in InitializePartial.
		// For unit tests, it's not initialized and we don't care that much.
		= null!;
#endif
}
