using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Uno.Foundation.Extensibility;

namespace Uno.UI.Tests.Foundation
{
	[TestClass]
	public class Given_ApiExtensibility
	{
		// ApiExtensibility._registrations is a process-wide static with no unregister, so each test
		// uses its OWN contract type — otherwise one test's registration would leak into another.
		// Registering the same contract twice is idempotent (first wins) and is robust to the test
		// runner re-executing a method in the same process (a retry sees the first run's registration).
		public interface IContractA
		{
			int Value { get; }
		}

		public interface IContractB
		{
			int Value { get; }
		}

		private sealed class ExtensionA : IContractA
		{
			public ExtensionA(int value) => Value = value;

			public int Value { get; }
		}

		private sealed class ExtensionB : IContractB
		{
			public ExtensionB(int value) => Value = value;

			public int Value { get; }
		}

		private sealed class Owner
		{
		}

		[TestMethod]
		public void When_Register_Called_Twice_Then_No_Throw_And_First_Registration_Wins()
		{
			ApiExtensibility.Register(typeof(IContractA), _ => new ExtensionA(1));

			// A duplicate registration must be an idempotent no-op. Before this fix it threw a
			// duplicate-key ArgumentException — the regression that broke multi-app hosting where a
			// host and a secondary app (in a collectible ALC sharing Uno.Foundation) each register
			// the same framework providers.
			ApiExtensibility.Register(typeof(IContractA), _ => new ExtensionA(2));

			Assert.IsTrue(ApiExtensibility.CreateInstance<IContractA>(new object(), out var instance));
			Assert.IsNotNull(instance);
			Assert.AreEqual(1, instance!.Value, "The first registration must win.");
		}

		[TestMethod]
		public void When_RegisterOfTOwner_Called_Twice_Then_No_Throw_And_First_Registration_Wins()
		{
			ApiExtensibility.Register<Owner>(typeof(IContractB), _ => new ExtensionB(1));
			ApiExtensibility.Register<Owner>(typeof(IContractB), _ => new ExtensionB(2));

			Assert.IsTrue(ApiExtensibility.CreateInstance<IContractB>(new Owner(), out var instance));
			Assert.IsNotNull(instance);
			Assert.AreEqual(1, instance!.Value, "The first registration must win.");
		}
	}
}
