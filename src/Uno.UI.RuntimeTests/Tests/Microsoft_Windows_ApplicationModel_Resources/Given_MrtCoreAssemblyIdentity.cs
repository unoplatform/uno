#nullable enable

using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.Windows.ApplicationModel.Resources;

namespace Uno.UI.RuntimeTests.Tests.Microsoft_Windows_ApplicationModel_Resources;

/// <summary>
/// Guards the BC75 packaging contract: the MRT Core surface ships from Uno.UWP (assembly
/// "Uno"), not Uno.UI. Consumers binding these types by assembly-qualified name depend on it.
/// </summary>
[TestClass]
public class Given_MrtCoreAssemblyIdentity
{
	private const string ExpectedAssemblyName = "Uno";

	[TestMethod]
	[DataRow(typeof(IResourceContext))]
	[DataRow(typeof(IResourceManager))]
	[DataRow(typeof(KnownResourceQualifierName))]
	[DataRow(typeof(MrtCoreContract))]
	[DataRow(typeof(ResourceCandidate))]
	[DataRow(typeof(ResourceCandidateKind))]
	[DataRow(typeof(ResourceContext))]
	[DataRow(typeof(ResourceLoader))]
	[DataRow(typeof(ResourceManager))]
	[DataRow(typeof(ResourceMap))]
	[DataRow(typeof(ResourceNotFoundEventArgs))]
	public void When_TypeIdentity_Then_ShipsFromUnoUwp(Type type)
		=> Assert.AreEqual(ExpectedAssemblyName, type.Assembly.GetName().Name);
}
