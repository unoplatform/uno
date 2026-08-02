using System;
using System.Buffers;
using System.IO;
using System.Net.Http;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.UI.Composition;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Uno;
using Uno.Disposables;
using Uno.Extensions;
using Uno.Foundation.Logging;
using Windows.Foundation;
using Windows.Storage;
using Windows.Storage.Streams;

#if !HAS_UNO_WINUI
using Microsoft.UI.Xaml.Controls;
#endif

#if HAS_UNO_WINUI
namespace CommunityToolkit.WinUI.Lottie
#else
namespace Microsoft.Toolkit.Uwp.UI.Lottie
#endif
{
	public abstract partial class LottieVisualSourceBase : DependencyObject, IAnimatedVisualSource, IAnimatedVisualSource3, IDynamicAnimatedVisualSource, IAnimatedVisualSourceWithUri
	{
		public delegate void UpdatedAnimation(string animationJson, string cacheKey);

		private const int MaxAnimationJsonBytes = 4 * 1024 * 1024;
		private const int DefaultReadBufferSize = 80 * 1024;

		private static HttpClient? _httpClient;

		private readonly object _stateGate = new();
		private readonly SerialDisposable _loadRevoker = new();
		private readonly SerialDisposable _animationDataSubscription = new();

		private DispatcherQueue? _dispatcherQueue;
		private Uri? _requestedSource;
		private string? _animationJson;
		private object? _diagnostics;
		private int _loadVersion;
		private bool _hasLoadFailure;
		private bool _isLoading;
		private bool _hasPendingAnimatedVisualInvalidation;
		private Task _pendingAnimatedVisualInvalidation = Task.CompletedTask;
		private Task _currentLoadTask = Task.CompletedTask;

		public static DependencyProperty UriSourceProperty { get; } = DependencyProperty.Register(
			"UriSource",
			typeof(Uri),
			typeof(LottieVisualSourceBase),
			new FrameworkPropertyMetadata(
				default(Uri),
				FrameworkPropertyMetadataOptions.AffectsMeasure | FrameworkPropertyMetadataOptions.AffectsArrange,
				OnUriSourceChanged));

		Uri IAnimatedVisualSourceWithUri.UriSource
		{
			get => UriSource;
			set => UriSource = value;
		}

		public Uri UriSource
		{
			get => (Uri)GetValue(UriSourceProperty);
			set => SetValue(UriSourceProperty, value);
		}

		public static DependencyProperty OptionsProperty { get; } = DependencyProperty.Register(
			"Options", typeof(LottieVisualOptions), typeof(LottieVisualSourceBase), new FrameworkPropertyMetadata(LottieVisualOptions.None));

		[NotImplemented]
		public LottieVisualOptions Options
		{
			get => (LottieVisualOptions)GetValue(OptionsProperty);
			set => SetValue(OptionsProperty, value);
		}

		[NotImplemented]
		public static LottieVisualSource CreateFromString(string uri)
		{
			ArgumentException.ThrowIfNullOrWhiteSpace(uri);

			return new LottieVisualSource
			{
				UriSource = new Uri(uri, UriKind.RelativeOrAbsolute)
			};
		}

		public event TypedEventHandler<IDynamicAnimatedVisualSource, object>? AnimatedVisualInvalidated;

		public IAnimatedVisual? TryCreateAnimatedVisual(Compositor compositor, out object diagnostics)
			=> TryCreateAnimatedVisualCore(compositor, out diagnostics, createAnimations: true);

		public IAnimatedVisual2? TryCreateAnimatedVisual(Compositor compositor, out object diagnostics, bool createAnimations)
			=> TryCreateAnimatedVisualCore(compositor, out diagnostics, createAnimations);

		private IAnimatedVisual2? TryCreateAnimatedVisualCore(Compositor compositor, out object diagnostics, bool createAnimations)
		{
			_dispatcherQueue ??= DispatcherQueue.GetForCurrentThread();

			EnsureLoadRequested();

			string? animationJson;
			object? currentDiagnostics;
			bool hasLoadFailure;
			lock (_stateGate)
			{
				animationJson = _animationJson;
				currentDiagnostics = _diagnostics;
				hasLoadFailure = _hasLoadFailure;
			}

			diagnostics = currentDiagnostics!;

			if (animationJson is null)
			{
				return hasLoadFailure ? null : CreatePendingAnimatedVisual(compositor);
			}

			return TryCreateAnimatedVisualFromJson(compositor, animationJson, createAnimations, out diagnostics);
		}

		private static void OnUriSourceChanged(DependencyObject sender, DependencyPropertyChangedEventArgs args)
		{
			if (sender is LottieVisualSourceBase source)
			{
				source.OnUriSourceChanged();
			}
		}

		public Task SetSourceAsync(Uri sourceUri)
		{
			var previousSource = UriSource;
			UriSource = sourceUri;

			if (previousSource is { } previous && previous.Equals(sourceUri))
			{
				_currentLoadTask = RequestLoad(sourceUri, forceReload: true, raiseInvalidation: true);
			}

			return _currentLoadTask;
		}

		private void OnUriSourceChanged()
		{
			_currentLoadTask = RequestLoad(UriSource, forceReload: false, raiseInvalidation: true);
		}

		private void EnsureLoadRequested()
		{
			_currentLoadTask = RequestLoad(UriSource, forceReload: false, raiseInvalidation: false);
		}

		private Task RequestLoad(Uri? sourceUri, bool forceReload, bool raiseInvalidation)
		{
			if (sourceUri is null)
			{
				ClearLoadState();
				if (raiseInvalidation)
				{
					RaiseAnimatedVisualInvalidated();
				}

				return Task.CompletedTask;
			}

			lock (_stateGate)
			{
				if (!forceReload
					&& _requestedSource is { } requestedSource
					&& requestedSource.Equals(sourceUri)
					&& (_isLoading || _animationJson is not null || _hasLoadFailure))
				{
					return _currentLoadTask;
				}
			}

			StartLoad(sourceUri);

			if (raiseInvalidation)
			{
				RaiseAnimatedVisualInvalidated();
			}

			return _currentLoadTask;
		}

		private void StartLoad(Uri sourceUri)
		{
			_loadRevoker.Disposable = null;
			_animationDataSubscription.Disposable = null;

			CancellationToken cancellationToken;
			int loadVersion;
			var cts = new CancellationTokenSource();

			lock (_stateGate)
			{
				_requestedSource = sourceUri;
				_animationJson = null;
				_diagnostics = null;
				_hasLoadFailure = false;
				_isLoading = true;
				loadVersion = ++_loadVersion;

				_loadRevoker.Disposable = Disposable.Create(() =>
				{
					cts.Cancel();
					cts.Dispose();
				});
				cancellationToken = cts.Token;
			}

			_currentLoadTask = LoadAnimationAsync(sourceUri, loadVersion, cancellationToken);
		}

		private void ClearLoadState()
		{
			_loadRevoker.Disposable = null;
			_animationDataSubscription.Disposable = null;

			lock (_stateGate)
			{
				_hasPendingAnimatedVisualInvalidation = false;
				_pendingAnimatedVisualInvalidation = Task.CompletedTask;
				_requestedSource = null;
				_animationJson = null;
				_diagnostics = null;
				_hasLoadFailure = false;
				_isLoading = false;
			}
		}

		private async Task LoadAnimationAsync(Uri sourceUri, int loadVersion, CancellationToken cancellationToken)
		{
			IDisposable? loadSubscription = null;
			try
			{
				using var jsonSource = await TryOpenJsonSourceAsync(sourceUri, cancellationToken);
				if (jsonSource is null)
				{
					await PublishLoadFailure(sourceUri, loadVersion, new NotSupportedException($"Failed to load animation: {RedactUri(sourceUri)}"));
					return;
				}

				var initialUpdateObserved = 0;
				Task initialInvalidation = Task.CompletedTask;
				void OnAnimationUpdated(string updatedJson, string updatedCacheKey)
				{
					if (!IsCurrentLoad(sourceUri, loadVersion))
					{
						return;
					}

					initialInvalidation = OnAnimationDataChanged(sourceUri, loadVersion, updatedJson, updatedCacheKey);
					Interlocked.Exchange(ref initialUpdateObserved, 1);
				}

				loadSubscription = LoadAndObserveAnimationData(
					jsonSource.Stream,
					jsonSource.CacheKey,
					OnAnimationUpdated);

				if (loadSubscription is AnimationDataLoadSubscription asyncLoad)
				{
					await asyncLoad.InitialLoad.WaitAsync(cancellationToken);
				}

				if (!IsCurrentLoad(sourceUri, loadVersion))
				{
					loadSubscription?.Dispose();
					return;
				}

				if (Interlocked.CompareExchange(ref initialUpdateObserved, 0, 0) == 0)
				{
					loadSubscription?.Dispose();
					await PublishLoadFailure(sourceUri, loadVersion, new InvalidOperationException("The animation source did not publish an initial payload."));
					return;
				}

				_animationDataSubscription.Disposable = loadSubscription;
				loadSubscription = null;
				await initialInvalidation;
			}
			catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
			{
			}
			catch (Exception e)
			{
				await PublishLoadFailure(sourceUri, loadVersion, e);
			}
			finally
			{
				loadSubscription?.Dispose();
			}
		}

		private bool IsCurrentLoad(Uri sourceUri, int loadVersion)
		{
			lock (_stateGate)
			{
				return _requestedSource is { } requestedSource
					&& requestedSource.Equals(sourceUri)
					&& loadVersion == _loadVersion;
			}
		}

		private Task OnAnimationDataChanged(Uri sourceUri, int loadVersion, string updatedJson, string updatedCacheKey)
		{
			lock (_stateGate)
			{
				if (_requestedSource is not { } requestedSource
					|| !requestedSource.Equals(sourceUri)
					|| loadVersion != _loadVersion)
				{
					return Task.CompletedTask;
				}

				_animationJson = updatedJson;
				_diagnostics = null;
				_hasLoadFailure = false;
				_isLoading = false;
			}

			return RaiseAnimatedVisualInvalidatedAsync();
		}

		private async Task PublishLoadFailure(Uri sourceUri, int loadVersion, Exception error)
		{
			lock (_stateGate)
			{
				if (_requestedSource is not { } requestedSource
					|| !requestedSource.Equals(sourceUri)
					|| loadVersion != _loadVersion)
				{
					return;
				}

				_animationJson = null;
				_diagnostics = error;
				_hasLoadFailure = true;
				_isLoading = false;
			}

			if (this.Log().IsEnabled(LogLevel.Error))
			{
				this.Log().Error($"Failed to load animation: {RedactUri(sourceUri)}", error);
			}

			await RaiseAnimatedVisualInvalidatedAsync();
		}

		private void RaiseAnimatedVisualInvalidated()
			=> _ = RaiseAnimatedVisualInvalidatedAsync();

		private Task RaiseAnimatedVisualInvalidatedAsync()
		{
			if (_dispatcherQueue is { HasThreadAccess: true })
			{
				RaiseAnimatedVisualInvalidatedCore();
				return Task.CompletedTask;
			}
			else if (_dispatcherQueue is { } dispatcherQueue)
			{
				TaskCompletionSource<object?> completion;
				lock (_stateGate)
				{
					if (_hasPendingAnimatedVisualInvalidation)
					{
						return _pendingAnimatedVisualInvalidation;
					}

					completion = new TaskCompletionSource<object?>(TaskCreationOptions.RunContinuationsAsynchronously);
					_pendingAnimatedVisualInvalidation = completion.Task;
					_hasPendingAnimatedVisualInvalidation = true;
				}

				if (!dispatcherQueue.TryEnqueue(() =>
				{
					try
					{
						RaiseAnimatedVisualInvalidatedCore();
						completion.TrySetResult(null);
					}
					catch (Exception error)
					{
						completion.TrySetException(error);
					}
				}))
				{
					lock (_stateGate)
					{
						_hasPendingAnimatedVisualInvalidation = false;
						_pendingAnimatedVisualInvalidation = Task.CompletedTask;
					}
					completion.TrySetException(new InvalidOperationException($"Failed to enqueue {nameof(AnimatedVisualInvalidated)}."));
					if (this.Log().IsEnabled(LogLevel.Warning))
					{
						this.Log().Warn($"Failed to enqueue {nameof(AnimatedVisualInvalidated)} for {RedactUri(UriSource)}. The notification will be retried when the source is next used on the UI thread.");
					}
				}

				return completion.Task;
			}
			else
			{
				RaiseAnimatedVisualInvalidatedCore();
				return Task.CompletedTask;
			}
		}

		private void RaiseAnimatedVisualInvalidatedCore()
		{
			lock (_stateGate)
			{
				_hasPendingAnimatedVisualInvalidation = false;
				_pendingAnimatedVisualInvalidation = Task.CompletedTask;
			}
			AnimatedVisualInvalidated?.Invoke(this, null!);
		}

		/// <summary>
		/// If the payload needs to be altered before being feed to the player
		/// </summary>
		protected abstract bool IsPayloadNeedsToBeUpdated { get; }

		/// <summary>
		/// Load the animation json payload
		/// </summary>
		protected virtual IDisposable? LoadAndObserveAnimationData(
			IInputStream sourceJson,
			string sourceCacheKey,
			UpdatedAnimation updateCallback)
		{
			var cts = new CancellationTokenSource();

			return new AnimationDataLoadSubscription(
				LoadAnimationJsonAsync(sourceJson, sourceCacheKey, updateCallback, cts.Token),
				() =>
				{
					cts.Cancel();
					cts.Dispose();
				});
		}

		protected sealed class AnimationDataLoadSubscription : IDisposable
		{
			private Action? _dispose;

			public AnimationDataLoadSubscription(Task initialLoad, Action dispose)
			{
				InitialLoad = initialLoad;
				_dispose = dispose;
			}

			public Task InitialLoad { get; }

			public void Dispose()
			{
				Interlocked.Exchange(ref _dispose, null)?.Invoke();
			}
		}

		protected static async Task<string> ReadAnimationJsonAsync(IInputStream sourceJson, CancellationToken cancellationToken)
		{
			using var _ = sourceJson;
			using var stream = sourceJson.AsStreamForRead(0);

			return await ReadAnimationJsonAsync(stream, stream.CanSeek ? stream.Length : null, cancellationToken);
		}

		protected static async Task<string> ReadAnimationJsonAsync(Stream sourceJson, long? knownLength, CancellationToken cancellationToken)
		{
			if (knownLength is > MaxAnimationJsonBytes)
			{
				throw new InvalidDataException($"Animation JSON exceeds the maximum supported size of {MaxAnimationJsonBytes} bytes.");
			}

			using var bufferStream = knownLength is > 0 and <= MaxAnimationJsonBytes
				? new MemoryStream((int)knownLength.Value)
				: new MemoryStream();

			var buffer = ArrayPool<byte>.Shared.Rent(DefaultReadBufferSize);
			var totalRead = 0;

			try
			{
				while (true)
				{
					var read = await sourceJson.ReadAsync(buffer.AsMemory(0, buffer.Length), cancellationToken);
					if (read == 0)
					{
						break;
					}

					totalRead += read;
					if (totalRead > MaxAnimationJsonBytes)
					{
						throw new InvalidDataException($"Animation JSON exceeds the maximum supported size of {MaxAnimationJsonBytes} bytes.");
					}

					bufferStream.Write(buffer, 0, read);
				}
			}
			finally
			{
				ArrayPool<byte>.Shared.Return(buffer);
			}

			if (bufferStream.Length < 2)
			{
				throw new InvalidDataException("Animation JSON payload is empty.");
			}

			var json = Encoding.UTF8.GetString(bufferStream.GetBuffer(), 0, (int)bufferStream.Length);
			return json.Length > 0 && json[0] == '\uFEFF'
				? json[1..]
				: json;
		}

		private async Task LoadAnimationJsonAsync(
			IInputStream sourceJson,
			string sourceCacheKey,
			UpdatedAnimation updateCallback,
			CancellationToken cancellationToken)
		{
			var json = await ReadAnimationJsonAsync(sourceJson, cancellationToken);
			cancellationToken.ThrowIfCancellationRequested();
			updateCallback(json, sourceCacheKey);
		}

		private partial IAnimatedVisual2 CreatePendingAnimatedVisual(Compositor compositor);
		private partial IAnimatedVisual2? TryCreateAnimatedVisualFromJson(Compositor compositor, string animationJson, bool createAnimations, out object diagnostics);

		private async Task<JsonStreamSource?> TryOpenJsonSourceAsync(Uri uri, CancellationToken ct)
		{
			if (TryLoadEmbeddedJson(uri) is { } embedded)
			{
				return new JsonStreamSource(embedded, uri.OriginalString);
			}

			if (uri.IsLocalResource())
			{
				var file = await StorageFile.GetFileFromApplicationUriAsync(uri).AsTask(ct);
				var value = await file.OpenAsync(FileAccessMode.Read).AsTask(ct);

				return new JsonStreamSource(value, uri.OriginalString);
			}

			if (uri.IsAppData())
			{
				return new JsonStreamSource(OpenValidatedAppDataStream(uri).AsInputStream(), uri.OriginalString);
			}

			return await DownloadJsonFromUri(uri, ct);
		}

		private IInputStream? TryLoadEmbeddedJson(Uri uri)
		{
			if (uri.Scheme != "embedded")
			{
				return null;
			}

			var assemblyName = uri.Host;

			var assembly = assemblyName == "."
				? Application.Current.GetType().Assembly
				: Assembly.Load(assemblyName);

			if (assembly == null)
			{
				return null;
			}

			var resourceName = uri.AbsolutePath.Substring(1).Replace("(assembly)", assembly.GetName().Name);
			var stream = assembly.GetManifestResourceStream(resourceName);
			if (stream == null)
			{
				if (this.Log().IsEnabled(LogLevel.Warning))
				{
					this.Log().Warn($"Unable to find embedded resource named '{resourceName}' to load.");
				}

				return null;
			}

			return stream.AsInputStream();
		}

		private async Task<JsonStreamSource?> DownloadJsonFromUri(Uri uri, CancellationToken ct)
		{
			_httpClient ??= new HttpClient();

			var response = await _httpClient.GetAsync(uri, HttpCompletionOption.ResponseHeadersRead, ct);

			if (!response.IsSuccessStatusCode)
			{
				response.Dispose();
				return null;
			}

			if (response.Content.Headers.ContentLength is { } length)
			{
				if (length < 2)
				{
					response.Dispose();
					return null;
				}

				if (length > MaxAnimationJsonBytes)
				{
					response.Dispose();
					throw new InvalidDataException($"Animation JSON exceeds the maximum supported size of {MaxAnimationJsonBytes} bytes.");
				}
			}

			var stream = await response.Content.ReadAsStreamAsync(ct);
			if (ct.IsCancellationRequested)
			{
				response.Dispose();
				ct.ThrowIfCancellationRequested();
			}

			return new JsonStreamSource(stream.AsInputStream(), uri.OriginalString, response);
		}

		private static Stream OpenValidatedAppDataStream(Uri uri)
		{
			var (declaredRoot, relativePath) = GetAppDataPath(uri);
			var canonicalRoot = Path.GetFullPath(declaredRoot);
			var candidatePath = Path.GetFullPath(Path.Combine(canonicalRoot, relativePath));
			var canonicalRootWithSeparator = canonicalRoot.EndsWith(Path.DirectorySeparatorChar)
				? canonicalRoot
				: canonicalRoot + Path.DirectorySeparatorChar;

			if (!candidatePath.Equals(canonicalRoot, StringComparison.OrdinalIgnoreCase)
				&& !candidatePath.StartsWith(canonicalRootWithSeparator, StringComparison.OrdinalIgnoreCase))
			{
				throw new UnauthorizedAccessException($"The animation source '{RedactUri(uri)}' resolved outside of the declared appdata root.");
			}

			return File.OpenRead(candidatePath);
		}

		private static (string RootPath, string RelativePath) GetAppDataPath(Uri uri)
		{
			var original = uri.OriginalString;
			var schemeSeparatorIndex = original.IndexOf("://", StringComparison.Ordinal);
			if (schemeSeparatorIndex < 0)
			{
				throw new ArgumentOutOfRangeException(nameof(uri), "URI must point to local, roaming or temp folder");
			}

			var afterScheme = original[(schemeSeparatorIndex + 3)..];
			var firstSlashIndex = afterScheme.IndexOf('/');
			if (firstSlashIndex < 0)
			{
				throw new ArgumentOutOfRangeException(nameof(uri), "URI must point to local, roaming or temp folder");
			}

			var rawPath = afterScheme[firstSlashIndex..];
			var queryIndex = rawPath.IndexOfAny(['?', '#']);
			if (queryIndex >= 0)
			{
				rawPath = rawPath[..queryIndex];
			}
			rawPath = Uri.UnescapeDataString(rawPath);

			var segments = rawPath.TrimStart('/', '\\').Split(['/', '\\'], 2, StringSplitOptions.RemoveEmptyEntries);
			if (segments.Length == 0)
			{
				throw new ArgumentOutOfRangeException(nameof(uri), "URI must point to local, roaming or temp folder");
			}

			var rootPath = segments[0].ToLowerInvariant() switch
			{
				"local" => ApplicationData.Current.LocalFolder.Path,
				"roaming" => ApplicationData.Current.RoamingFolder.Path,
				"temp" => ApplicationData.Current.TemporaryFolder.Path,
				_ => throw new ArgumentOutOfRangeException(nameof(uri), "URI must point to local, roaming or temp folder")
			};
			var relativePath = segments.Length > 1 ? segments[1] : string.Empty;

			return (rootPath, relativePath);
		}

		internal static string RedactUri(Uri? uri)
		{
			if (uri is null)
			{
				return "<null>";
			}

			if (!uri.IsAbsoluteUri)
			{
				return uri.OriginalString;
			}

			var builder = new UriBuilder(uri)
			{
				Query = string.Empty,
				Fragment = string.Empty,
				UserName = string.Empty,
				Password = string.Empty
			};

			return builder.Uri.GetLeftPart(UriPartial.Path);
		}

		private sealed class JsonStreamSource : IDisposable
		{
			private readonly IDisposable? _lease;

			public JsonStreamSource(IInputStream stream, string cacheKey, IDisposable? lease = null)
			{
				Stream = stream;
				CacheKey = cacheKey;
				_lease = lease;
			}

			public IInputStream Stream { get; }

			public string CacheKey { get; }

			public void Dispose()
			{
				Stream.Dispose();
				_lease?.Dispose();
			}
		}
	}
}
