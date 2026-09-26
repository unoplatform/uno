// Exists so the runtime-test suite can build a Func<ResourceDictionary> whose
// Method.DeclaringType.Assembly lives in this secondary-ALC-loaded assembly
// (rather than in the test assembly itself, which lives in the default ALC).
// Used by Given_ResourceResolver_AlcRegistration to validate that
// ResourceResolver.RegisterResourceDictionaryBySource routes non-default-ALC
// registrations to the ALC-scoped registry.

namespace AlcTestApp;

public static class ResourceDictionaryTestFactory
{
	public static ResourceDictionary Create() => new ResourceDictionary();
}
