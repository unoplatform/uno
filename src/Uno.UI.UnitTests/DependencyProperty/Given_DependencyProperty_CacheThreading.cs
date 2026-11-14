#nullable enable

using System;
using System.Reflection;
using System.Threading;
using Microsoft.UI.Xaml;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Uno.UI.Tests.DependencyPropertyTests;

[TestClass]
[DoNotParallelize]
public class Given_DependencyProperty_CacheThreading
{
	[TestMethod]
	public void When_Uncached_Properties_Are_Resolved_Concurrently()
	{
		var firstExpected = DependencyProperty.Register(
			FirstPropertyName,
			typeof(int),
			typeof(FirstCacheOwner),
			new PropertyMetadata(0));
		var secondExpected = DependencyProperty.Register(
			SecondPropertyName,
			typeof(int),
			typeof(SecondCacheOwner),
			new PropertyMetadata(0));
		using var firstKeyUpdated = new ManualResetEventSlim();
		using var secondLookupStarted = new ManualResetEventSlim();
		using var secondKeyUpdated = new ManualResetEventSlim();
		using var releaseFirstLookup = new ManualResetEventSlim();
		using var releaseSecondLookup = new ManualResetEventSlim();
		DependencyProperty.GetPropertyCacheSearchKeyUpdatedTestHook =
			(type, propertyName) =>
			{
				if (type == typeof(FirstCacheOwner) && propertyName == FirstPropertyName)
				{
					firstKeyUpdated.Set();
					Assert.IsTrue(releaseFirstLookup.Wait(TimeSpan.FromSeconds(5)));
				}
				else if (type == typeof(SecondCacheOwner) && propertyName == SecondPropertyName)
				{
					secondKeyUpdated.Set();
					Assert.IsTrue(releaseSecondLookup.Wait(TimeSpan.FromSeconds(5)));
				}
			};

		try
		{
			DependencyProperty? first = null;
			DependencyProperty? second = null;
			Exception? firstError = null;
			Exception? secondError = null;
			var firstThread = StartThread(
				() => first = DependencyProperty.GetProperty(typeof(FirstCacheOwner), FirstPropertyName),
				error => firstError = error);
			Assert.IsTrue(firstKeyUpdated.Wait(TimeSpan.FromSeconds(5)));

			var secondThread = StartThread(
				() =>
				{
					secondLookupStarted.Set();
					second = DependencyProperty.GetProperty(typeof(SecondCacheOwner), SecondPropertyName);
				},
				error => secondError = error);
			Assert.IsTrue(secondLookupStarted.Wait(TimeSpan.FromSeconds(5)));

			var cacheGate = typeof(DependencyProperty)
				.GetField("_getPropertyCacheGate", BindingFlags.NonPublic | BindingFlags.Static)!
				.GetValue(null)!;
			var cacheGateWasFree = Monitor.TryEnter(cacheGate);
			if (cacheGateWasFree)
			{
				Monitor.Exit(cacheGate);
				Assert.IsTrue(secondKeyUpdated.Wait(TimeSpan.FromSeconds(5)));
			}

			releaseFirstLookup.Set();
			if (cacheGateWasFree)
			{
				Assert.IsTrue(firstThread.Join(TimeSpan.FromSeconds(10)));
			}
			Assert.IsTrue(secondKeyUpdated.Wait(TimeSpan.FromSeconds(5)));
			releaseSecondLookup.Set();
			if (!cacheGateWasFree)
			{
				Assert.IsTrue(firstThread.Join(TimeSpan.FromSeconds(10)));
			}
			Assert.IsTrue(secondThread.Join(TimeSpan.FromSeconds(10)));

			Assert.IsNull(firstError, firstError?.ToString());
			Assert.IsNull(secondError, secondError?.ToString());
			Assert.AreSame(firstExpected, first);
			Assert.AreSame(secondExpected, second);
			Assert.AreSame(firstExpected, DependencyProperty.GetProperty(typeof(FirstCacheOwner), FirstPropertyName));
			Assert.AreSame(secondExpected, DependencyProperty.GetProperty(typeof(SecondCacheOwner), SecondPropertyName));
		}
		finally
		{
			DependencyProperty.GetPropertyCacheSearchKeyUpdatedTestHook = null;
			releaseFirstLookup.Set();
			releaseSecondLookup.Set();
		}
	}

	[TestMethod]
	public void When_Property_Is_Registered_While_Lookup_Is_In_Flight()
	{
		using var cacheInvalidated = new ManualResetEventSlim();
		using var allowRegistration = new ManualResetEventSlim();
		DependencyProperty.GetPropertyCacheResetTestHook =
			(type, propertyName) =>
			{
				if (type == typeof(RegistrationCacheOwner) && propertyName == RegistrationPropertyName)
				{
					cacheInvalidated.Set();
					Assert.IsTrue(allowRegistration.Wait(TimeSpan.FromSeconds(5)));
				}
			};

		try
		{
			DependencyProperty? registered = null;
			DependencyProperty? resolved = null;
			Exception? registrationError = null;
			Exception? lookupError = null;
			var registrationThread = StartThread(
				() => registered = DependencyProperty.Register(
					RegistrationPropertyName,
					typeof(int),
					typeof(RegistrationCacheOwner),
					new PropertyMetadata(0)),
				error => registrationError = error);
			Assert.IsTrue(cacheInvalidated.Wait(TimeSpan.FromSeconds(5)));

			var lookupThread = StartThread(
				() => resolved = DependencyProperty.GetProperty(typeof(RegistrationCacheOwner), RegistrationPropertyName),
				error => lookupError = error);
			Assert.IsTrue(lookupThread.Join(TimeSpan.FromSeconds(10)));
			allowRegistration.Set();
			Assert.IsTrue(registrationThread.Join(TimeSpan.FromSeconds(10)));

			Assert.IsNull(registrationError, registrationError?.ToString());
			Assert.IsNull(lookupError, lookupError?.ToString());
			Assert.IsNotNull(registered);
			Assert.AreSame(registered, resolved);
			Assert.AreSame(
				registered,
				DependencyProperty.GetProperty(typeof(RegistrationCacheOwner), RegistrationPropertyName));
		}
		finally
		{
			DependencyProperty.GetPropertyCacheResetTestHook = null;
			allowRegistration.Set();
		}
	}

	private static Thread StartThread(Action action, Action<Exception> onError)
	{
		var thread = new Thread(
			() =>
			{
				try
				{
					action();
				}
				catch (Exception error)
				{
					onError(error);
				}
			})
		{
			IsBackground = true,
		};
		thread.Start();
		return thread;
	}

	private const string FirstPropertyName = "First";
	private const string SecondPropertyName = "Second";
	private const string RegistrationPropertyName = "RegisteredConcurrently";

	private sealed class FirstCacheOwner
	{
	}

	private sealed class SecondCacheOwner
	{
	}

	private sealed class RegistrationCacheOwner
	{
	}
}
