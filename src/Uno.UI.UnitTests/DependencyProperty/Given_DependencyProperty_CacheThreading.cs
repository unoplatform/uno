#nullable enable

using System;
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
		DependencyProperty? first = null;
		DependencyProperty? second = null;
		Exception? firstError = null;
		Exception? secondError = null;
		var firstThread = StartThread(
			() => first = DependencyProperty.GetProperty(typeof(FirstCacheOwner), FirstPropertyName),
			error => firstError = error);
		var secondThread = StartThread(
			() => second = DependencyProperty.GetProperty(typeof(SecondCacheOwner), SecondPropertyName),
			error => secondError = error);

		Assert.IsTrue(firstThread.Join(TimeSpan.FromSeconds(10)));
		Assert.IsTrue(secondThread.Join(TimeSpan.FromSeconds(10)));
		Assert.IsNull(firstError, firstError?.ToString());
		Assert.IsNull(secondError, secondError?.ToString());
		Assert.AreSame(firstExpected, first);
		Assert.AreSame(secondExpected, second);
		Assert.AreSame(firstExpected, DependencyProperty.GetProperty(typeof(FirstCacheOwner), FirstPropertyName));
		Assert.AreSame(secondExpected, DependencyProperty.GetProperty(typeof(SecondCacheOwner), SecondPropertyName));
	}

	[TestMethod]
	public void When_Property_Is_Registered_While_Lookup_Is_In_Flight()
	{
		using var registryPublished = new ManualResetEventSlim();
		using var allowCacheInvalidation = new ManualResetEventSlim();
		DependencyProperty.RegisterPropertyPublishedTestHook =
			(type, propertyName) =>
			{
				if (type == typeof(RegistrationCacheOwner) && propertyName == RegistrationPropertyName)
				{
					registryPublished.Set();
					Assert.IsTrue(allowCacheInvalidation.Wait(TimeSpan.FromSeconds(5)));
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
			Assert.IsTrue(registryPublished.Wait(TimeSpan.FromSeconds(5)));

			var lookupThread = StartThread(
				() => resolved = DependencyProperty.GetProperty(typeof(RegistrationCacheOwner), RegistrationPropertyName),
				error => lookupError = error);
			Assert.IsTrue(lookupThread.Join(TimeSpan.FromSeconds(10)));
			allowCacheInvalidation.Set();
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
			DependencyProperty.RegisterPropertyPublishedTestHook = null;
			allowCacheInvalidation.Set();
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
	private static readonly Barrier ResolutionBoundary = new(2);

	private sealed class FirstCacheOwner
	{
		static FirstCacheOwner()
			=> Assert.IsTrue(ResolutionBoundary.SignalAndWait(TimeSpan.FromSeconds(5)));
	}

	private sealed class SecondCacheOwner
	{
		static SecondCacheOwner()
			=> Assert.IsTrue(ResolutionBoundary.SignalAndWait(TimeSpan.FromSeconds(5)));
	}

	private sealed class RegistrationCacheOwner
	{
	}
}
