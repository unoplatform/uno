#nullable disable

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NUnit.Framework;

namespace SamplesApp.UITests.TestFramework
{
	public partial class RetryTests
	{
		static int When_Retry_On_Timeout_Count = 0;

		[Test]
		[AutoRetry]
		[Timeout(2000)]
		public void When_Retry_On_Timeout()
		{
			Console.WriteLine($"When_Retry_On_Timeout: {++When_Retry_On_Timeout_Count}");
			if (When_Retry_On_Timeout_Count < AutoRetryAttribute.AutoRetryDefaultCount)
			{
				System.Threading.Thread.Sleep(4000);
			}
		}

		static int When_Retry_On_Unhandled_Exception_Count = 0;

		[Test]
		[AutoRetry]
		public void When_Retry_On_Unhandled_Exception()
		{
			Console.WriteLine($"When_Retry_On_Unhandled_Exception {++When_Retry_On_Unhandled_Exception_Count}");

			if (When_Retry_On_Unhandled_Exception_Count < AutoRetryAttribute.AutoRetryDefaultCount)
			{
				throw new NotImplementedException();
			}
		}

	}

	public partial class RetrySetup
	{
		static int Setup_Count = 0;

		[SetUp]
		public void Setup()
		{
			Console.WriteLine($"Setup: {++Setup_Count}");

			if (Setup_Count < AutoRetryAttribute.AutoRetryDefaultCount)
			{
				throw new NotImplementedException();
			}
		}

		[Test]
		[AutoRetry]
		public void When_Success()
		{
			Console.WriteLine($"When_Success: {++Setup_Count}");
		}
	}

	public partial class RetryTearDown
	{
		static int TearDown_Count = 0;

		[TearDown]
		public void TearDown()
		{
			Console.WriteLine($"TearDown: {++TearDown_Count}");

			if (TearDown_Count < AutoRetryAttribute.AutoRetryDefaultCount)
			{
				throw new NotImplementedException();
			}
		}

		[Test]
		[AutoRetry]
		public void When_Success()
		{
			Console.WriteLine($"When_Success: {++TearDown_Count}");
		}
	}
}
