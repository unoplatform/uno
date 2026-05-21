namespace Uno.Licensing.TestContracts;

/// <summary>
/// Test stand-in for <c>Uno.Licensing.Sdk.Contracts::ILicensingService</c>.
/// The cross-add-in type-sharing regression test relies on this assembly being
/// physically present in the provider add-in's directory but absent from every
/// add-in's deps.json — exercising the file-system probe (step 4) in
/// <c>AddInLoadContext.Load</c>.
/// </summary>
public interface ILicensingTestContract { }

public sealed class DefaultLicensingTestContract : ILicensingTestContract { }
