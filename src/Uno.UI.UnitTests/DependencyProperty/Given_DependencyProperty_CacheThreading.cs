#nullable enable

using System;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.UI.Xaml;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Uno.UI.Tests.DependencyPropertyTests;

[TestClass]
public partial class Given_DependencyProperty_CacheThreading
{
	[TestMethod]
	public async Task When_Uncached_Property_Is_Resolved_Concurrently()
	{
		for (var iteration = 0; iteration < 20; iteration++)
		{
			var name = $"ConcurrentProperty_{Guid.NewGuid():N}";
			var expected = DependencyProperty.Register(
				name,
				typeof(int),
				typeof(CacheOwner),
				new PropertyMetadata(0));
			using var publish = new Barrier(2);
			SetCacheHook(
				"_getPropertyCachePublishTestHook",
				() => Assert.IsTrue(publish.SignalAndWait(TimeSpan.FromSeconds(5))));

			try
			{
				var actual = await Task.WhenAll(
					Task.Run(() => DependencyProperty.GetProperty(typeof(CacheOwner), name)),
					Task.Run(() => DependencyProperty.GetProperty(typeof(CacheOwner), name)));
				Assert.IsTrue(actual.All(property => property == expected));
			}
			finally
			{
				SetCacheHook("_getPropertyCachePublishTestHook", null);
			}
		}
	}

	[TestMethod]
	public async Task When_Type_Initializer_Registers_The_Requested_Property()
	{
		using var cacheMissed = new ManualResetEventSlim();
		SetCacheHook(
			"_getPropertyCacheMissTestHook",
			() =>
			{
				cacheMissed.Set();
				Assert.IsTrue(
					TypeInitializerRegistrationCompleted.Wait(TimeSpan.FromSeconds(5)),
					"The property cache lock blocked registration from the type initializer.");
			});

		try
		{
			var initialize = Task.Run(
				() => RuntimeHelpers.RunClassConstructor(typeof(InitializingCacheOwner).TypeHandle));
			Assert.IsTrue(TypeInitializerEntered.Wait(TimeSpan.FromSeconds(5)));

			var lookup = Task.Run(
				() => DependencyProperty.GetProperty(typeof(InitializingCacheOwner), InitializingPropertyName));
			Assert.IsTrue(cacheMissed.Wait(TimeSpan.FromSeconds(5)));
			AllowTypeInitializerRegistration.Set();

			await Task.WhenAll(initialize, lookup);
			Assert.AreSame(InitializingCacheOwner.InitializingProperty, lookup.Result);
		}
		finally
		{
			SetCacheHook("_getPropertyCacheMissTestHook", null);
			AllowTypeInitializerRegistration.Set();
		}
	}

	private static void SetCacheHook(string fieldName, Action? hook)
		=> typeof(DependencyProperty)
			.GetField(fieldName, BindingFlags.NonPublic | BindingFlags.Static)!
			.SetValue(null, hook);

	private const string InitializingPropertyName = "Initializing";
	private static readonly ManualResetEventSlim TypeInitializerEntered = new();
	private static readonly ManualResetEventSlim AllowTypeInitializerRegistration = new();
	private static readonly ManualResetEventSlim TypeInitializerRegistrationCompleted = new();

	private sealed partial class CacheOwner : DependencyObject
	{
	}

	private sealed partial class InitializingCacheOwner : DependencyObject
	{
		static InitializingCacheOwner()
		{
			TypeInitializerEntered.Set();
			AllowTypeInitializerRegistration.Wait();
			InitializingProperty = DependencyProperty.Register(
				InitializingPropertyName,
				typeof(int),
				typeof(InitializingCacheOwner),
				new PropertyMetadata(0));
			TypeInitializerRegistrationCompleted.Set();
		}

		internal static DependencyProperty InitializingProperty { get; }
	}
}
