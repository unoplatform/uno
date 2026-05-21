namespace Uno.Licensing.TestContracts;

/// <summary>
/// Test stand-in for Uno.Licensing.Sdk.Contracts::ILicensingService.
/// Named to match the "Uno.Licensing.*.dll" glob in
/// HostAssemblyResolution.AddInSharedAssemblyPatterns so the cross-add-in
/// type-sharing regression test exercises the real fix path.
/// </summary>
public interface ILicensingTestContract { }

public sealed class DefaultLicensingTestContract : ILicensingTestContract { }
