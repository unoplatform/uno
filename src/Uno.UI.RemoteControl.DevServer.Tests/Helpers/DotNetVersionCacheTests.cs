using System;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging.Abstractions;
using Uno.UI.DevServer.Cli.Helpers;

namespace Uno.UI.RemoteControl.DevServer.Tests.Helpers;

[TestClass]
public class DotNetVersionCacheTests
{
	private string _tempDir = null!;
	private string _cachePath = null!;
	private string _globalJsonPath = null!;

	[TestInitialize]
	public void TestInitialize()
	{
		_tempDir = Path.Combine(Path.GetTempPath(), $"dotnet-version-cache-tests-{Guid.NewGuid()}");
		Directory.CreateDirectory(_tempDir);
		_cachePath = Path.Combine(_tempDir, "dotnet-version-cache.json");
		_globalJsonPath = Path.Combine(_tempDir, "global.json");
	}

	[TestCleanup]
	public void TestCleanup()
	{
		try
		{
			if (Directory.Exists(_tempDir))
			{
				Directory.Delete(_tempDir, true);
			}
		}
		catch
		{
			// Best-effort cleanup
		}
	}

	private DotNetVersionCache CreateCache(
		Func<Task<(string? rawVersion, string? tfm)>>? provider = null)
	{
		var actualProvider = provider ?? (() => Task.FromResult<(string?, string?)>(("10.0.100", "net10.0")));
		var logger = NullLoggerFactory.Instance.CreateLogger<DotNetVersionCache>();
		return new DotNetVersionCache(logger, actualProvider, cachePathOverride: _cachePath);
	}

	private void WriteGlobalJson(string? sdkVersion = null)
	{
		var content = sdkVersion is not null
			? $$$"""{"sdk": {"version": "{{{sdkVersion}}}"}, "msbuild-sdks": {"Uno.Sdk": "5.6.100"}}"""
			: """{"msbuild-sdks": {"Uno.Sdk": "5.6.100"}}""";
		File.WriteAllText(_globalJsonPath, content);
	}

	private void WriteCacheFile(string rawVersion, string tfm, string sdkVersionKey, DateTime? timestamp = null)
	{
		var ts = timestamp ?? DateTime.UtcNow;
		var json = JsonSerializer.Serialize(new
		{
			version = 1,
			rawVersion,
			tfm,
			sdkVersionKey,
			timestamp = ts.ToString("O")
		});
		File.WriteAllText(_cachePath, json);
	}

	[TestMethod]
	public async Task CacheMiss_CallsProviderAndWritesCache()
	{
		var callCount = 0;
		var cache = CreateCache(provider: () =>
		{
			callCount++;
			return Task.FromResult<(string?, string?)>(("10.0.100", "net10.0"));
		});

		var result = await cache.GetOrRefreshAsync(globalJsonPath: null);

		result.rawVersion.Should().Be("10.0.100");
		result.tfm.Should().Be("net10.0");
		callCount.Should().Be(1);
		File.Exists(_cachePath).Should().BeTrue();
	}

	[TestMethod]
	public async Task CacheHit_ReturnsWithoutCallingProvider()
	{
		WriteCacheFile("10.0.100", "net10.0", "");

		var callCount = 0;
		var cache = CreateCache(provider: () =>
		{
			callCount++;
			return Task.FromResult<(string?, string?)>(("10.0.200", "net10.0"));
		});

		var result = await cache.GetOrRefreshAsync(globalJsonPath: null);

		result.rawVersion.Should().Be("10.0.100");
		result.tfm.Should().Be("net10.0");
		callCount.Should().Be(0);
	}

	[TestMethod]
	public async Task CacheInvalidBySdkVersionKey_Refreshes()
	{
		WriteGlobalJson("10.0.100");
		WriteCacheFile("9.0.100", "net9.0", "9.0.100");

		var cache = CreateCache(provider: () =>
			Task.FromResult<(string?, string?)>(("10.0.100", "net10.0")));

		var result = await cache.GetOrRefreshAsync(globalJsonPath: _globalJsonPath);

		result.rawVersion.Should().Be("10.0.100");
		result.tfm.Should().Be("net10.0");
	}

	[TestMethod]
	public async Task CacheExpired_Refreshes()
	{
		WriteCacheFile("10.0.100", "net10.0", "", timestamp: DateTime.UtcNow.AddHours(-25));

		var cache = CreateCache(provider: () =>
			Task.FromResult<(string?, string?)>(("10.0.200", "net10.0")));

		var result = await cache.GetOrRefreshAsync(globalJsonPath: null);

		result.rawVersion.Should().Be("10.0.200");
	}

	[TestMethod]
	public async Task Force_RefreshesEvenIfCacheValid()
	{
		WriteCacheFile("10.0.100", "net10.0", "");

		var cache = CreateCache(provider: () =>
			Task.FromResult<(string?, string?)>(("10.0.200", "net10.0")));

		var result = await cache.GetOrRefreshAsync(globalJsonPath: null, force: true);

		result.rawVersion.Should().Be("10.0.200");
	}

	[TestMethod]
	public async Task CorruptedCache_RefreshesWithoutCrashing()
	{
		File.WriteAllText(_cachePath, "this is not json");

		var cache = CreateCache(provider: () =>
			Task.FromResult<(string?, string?)>(("10.0.100", "net10.0")));

		var result = await cache.GetOrRefreshAsync(globalJsonPath: null);

		result.rawVersion.Should().Be("10.0.100");
		result.tfm.Should().Be("net10.0");
	}

	[TestMethod]
	public async Task GlobalJsonAbsent_UsesEmptySdkVersionKey()
	{
		WriteCacheFile("10.0.100", "net10.0", "");

		var callCount = 0;
		var cache = CreateCache(provider: () =>
		{
			callCount++;
			return Task.FromResult<(string?, string?)>(("10.0.200", "net10.0"));
		});

		var result = await cache.GetOrRefreshAsync(globalJsonPath: null);

		result.rawVersion.Should().Be("10.0.100");
		callCount.Should().Be(0);
	}

	[TestMethod]
	public void TryGetSdkVersionFromGlobalJson_ReturnsSdkVersion()
	{
		WriteGlobalJson("10.0.100");

		var result = DotNetVersionCache.TryGetSdkVersionFromGlobalJson(_globalJsonPath);

		result.Should().Be("10.0.100");
	}

	[TestMethod]
	public void TryGetSdkVersionFromGlobalJson_NoSdkSection_ReturnsNull()
	{
		File.WriteAllText(_globalJsonPath, """{"msbuild-sdks": {"Uno.Sdk": "5.6.100"}}""");

		var result = DotNetVersionCache.TryGetSdkVersionFromGlobalJson(_globalJsonPath);

		result.Should().BeNull();
	}

	[TestMethod]
	public void TryGetSdkVersionFromGlobalJson_NullPath_ReturnsNull()
	{
		var result = DotNetVersionCache.TryGetSdkVersionFromGlobalJson(null);

		result.Should().BeNull();
	}
}
