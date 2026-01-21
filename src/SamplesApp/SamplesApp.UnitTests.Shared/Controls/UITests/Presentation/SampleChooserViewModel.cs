// #define TRACK_REFS

using System;
using System.Threading;
using System.Threading.Tasks;
using System.Reflection;
using System.Linq;
using System.Collections.Generic;
using System.Diagnostics;
using SampleControl.Entities;
using Uno.UI.Samples.Controls;
using Uno.UI.Samples.Entities;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Globalization;
using Windows.Storage;
using Microsoft.UI.Xaml;
using System.IO;
using Uno.Extensions;
using Uno.UI.Samples.Tests;

#if HAS_UNO
using Uno.Foundation.Logging;
using MUXControlsTestApp;
#else
using Microsoft.Extensions.Logging;
using Uno.Logging;
#endif

using Microsoft.UI.Xaml.Controls;
using Windows.Graphics.Imaging;
using Windows.Graphics.Display;
using SamplesApp;
using Uno.UI.Extensions;
using Private.Infrastructure;
using System.Reflection.Metadata;
using UITests.Shared.Helpers;
using Uno.UI.Samples.UITests.Helpers;
using System.Runtime.CompilerServices;
using System.ComponentModel.DataAnnotations;
using System.Text;
using UITests.Playground;
using SamplesApp.Samples.Help;

namespace SampleControl.Presentation
{
	/// <summary>
	/// A UI Samples chooser ViewModel
	/// </summary>
	/// <remarks>
	/// This class does not use any MVVM framework to avoid circular package dependencies with Uno.UI.
	/// </remarks>

	public partial class SampleChooserViewModel
	{
		private const string TestGroupVariable = "UITEST_RUNTIME_TEST_GROUP";
		private const string TestGroupCountVariable = "UITEST_RUNTIME_TEST_GROUP_COUNT";
		private const string TestsFilterRawVariable = "UITEST_RUNTIME_TESTS_FILTER";

#if DEBUG
		private const int _numberOfRecentSamplesVisible = 10;
#else
		private const int _numberOfRecentSamplesVisible = 0;
#endif

#if HAS_UNO
		private Logger _log = Uno.Foundation.Logging.LogExtensionPoint.Log(typeof(SampleChooserViewModel));
#else
		private static readonly ILogger _log = Uno.Extensions.LogExtensionPoint.Log(typeof(SampleChooserViewModel));
#endif

		private Window _window;

		private List<SampleChooserCategory> _categories;
		private List<SampleChooserCategory> _allCategories;
		private List<SampleChooserCategory> _visibleCategories;
		private List<SampleChooserCategory> _manualTestsCategories;

		private readonly Uno.Threading.AsyncLock _fileLock = new Uno.Threading.AsyncLock();
#if !__NETSTD_REFERENCE__
		private readonly string SampleChooserFileAddress = "SampleChooserFileAddress.";
#endif

		private readonly string SampleChooserLRUConstant = "Samples.LRU";
		private readonly string SampleChooserFavoriteConstant = "Samples.Favorites";
		private const string SampleChooserLatestCategoryConstant = "Samples.LatestCategory";

		private Section _lastSection = Section.Library;
		private readonly Stack<Section> _previousSections = new Stack<Section>();

		private readonly UnitTestDispatcherCompat _dispatcher;

		// A static instance used during UI Testing automation
		public static SampleChooserViewModel Instance { get; private set; }

		public static bool IsDebug
		{
#if DEBUG
			get { return true; }
#else
			get { return false; }
#endif
		}

		public SampleChooserControl Owner { get; }

		public SampleChooserViewModel(SampleChooserControl owner)
		{
			Instance = this;
			Owner = owner;
			_dispatcher = UnitTestDispatcherCompat.From(owner);

#if TRACK_REFS
			Uno.UI.DataBinding.BinderReferenceHolder.IsEnabled = true;
#endif

#if HAS_UNO
			// Disable all pooling so that controls get collected quickly.
			Microsoft.UI.Xaml.FrameworkTemplatePool.IsPoolingEnabled = false;
#endif
			UseFluentStyles = true;

			// FPS indicator visibility is persisted across app sessions.
			var localSettings = ApplicationData.Current.LocalSettings;
			if (localSettings.Values.TryGetValue(nameof(ShowFpsIndicator), out var value) && value is bool boolValue)
			{
				ShowFpsIndicator = boolValue;
			}

			InitializeCommands();
			ObserveChanges();

			InitializeCategories();

			if (_log.IsEnabled(LogLevel.Information))
			{
				_log.Info($"Found {_categories.SelectMany(c => c.SamplesContent).Distinct().Count()} sample(s) in {_categories.Count} categories.");
			}

			_ = _dispatcher.RunAsync(
					async () =>
					{
						// Initialize favorites and recents list as soon as possible.
						if (FavoriteSamples == null || !FavoriteSamples.Any())
						{
							FavoriteSamples = await GetFavoriteSamples(CancellationToken.None, true);
						}
						if (RecentSamples == null || !RecentSamples.Any())
						{
							RecentSamples = await GetRecentSamples(CancellationToken.None);
						}
					}
				);
		}

		public event EventHandler SampleChanging;

		/// <summary>
		/// Displays a new page depending on the parameter that was sent.
		/// </summary>
		/// <param name="ct"></param>
		/// <param name="section">The page to go to.</param>
		/// <returns></returns>
		private void ShowNewSection(CancellationToken ct, Section section)
		{
			Console.WriteLine($"Section changed: {section} ({GetMemoryStats()})");

			switch (section)
			{
				// Sections
				case Section.Library:
				case Section.Samples:
				case Section.Recents:
				case Section.Favorites:
				case Section.Search:
					_previousSections.Push(_lastSection);
					ShowSelectedList(ct, section);

					return; // Return after a Tab is shown

				// Section contents
				case Section.SamplesContent:
				case Section.RecentsContent:
				case Section.FavoritesContent:
				case Section.SearchContent:
				default:
					break;
			}
		}

		private string GetMemoryStats()
		{
			var totalMemory = GC.GetTotalMemory(false);
			return $"GC Heap: {totalMemory / 1024.0 / 1024.0:0.00} MB";
		}

		/// <summary>
		/// Goes back one page.
		/// </summary>
		/// <param name="ct"></param>
		/// <returns></returns>
		private void ShowPreviousSection(CancellationToken ct)
		{
			if (_previousSections.Count == 0)
			{
				_lastSection = Section.Library;
				ShowSelectedList(ct, Section.Library);
				return;
			}

			var priorPage = _previousSections.Pop();

			switch (priorPage)
			{
				case Section.Library:
				case Section.Samples:
				case Section.Recents:
				case Section.Favorites:
				case Section.Search:
					ShowSelectedList(ct, priorPage);
					break;

				default:
					break;
			}
		}

		private void ShowSelectedList(CancellationToken ct, Section section)
		{
			CategoryVisibility = section == Section.Library;
			CategoriesSelected = section == Section.Library || section == Section.Samples;

			RecentsVisibility = section == Section.Recents;
			FavoritesVisibility = section == Section.Favorites;
			SearchVisibility = section == Section.Search;
			SampleVisibility = section == Section.Samples;

			_lastSection = section;
		}

		private async Task LogViewDump(CancellationToken ct)
		{
			await RunOnUIThreadAsync(
				() =>
				{
					var currentContent = ContentPhone as Control;

					if (currentContent == null)
					{
						_log.Debug($"No current Sample Control selected.");
						return;
					}

					var builder = new System.Text.StringBuilder();
					builder.AppendLine($"Dump for '{currentContent.GetType().FullName}':");
					PrintViewHierarchy(currentContent, builder);
					var toLog = builder.ToString();
					_log.Debug(toLog);
				});
		}


		private void OnContentVisibilityChanged()
		{
			IsAnyContentVisible = ContentVisibility;
		}

		internal async Task RecordAllTests(CancellationToken ct, string screenShotPath = "", int totalGroups = 1, int currentGroupIndex = 0, Action doneAction = null)
		{
			try
			{
				IsRecordAllTests = true;
				IsSplitVisible = false;

				var folderName = Path.Combine(screenShotPath, "UITests-" + DateTime.Now.ToString("yyyyMMdd-hhmmssfff", CultureInfo.InvariantCulture));

				await DumpOutputFolderName(ct, folderName);

				await RunOnUIThreadAsync(
					async () =>
					{
						try
						{
							await RecordAllTestsInner(folderName, totalGroups, currentGroupIndex, ct, doneAction);
						}
						finally
						{
							IsRecordAllTests = false;
						}
					});
			}
			catch (Exception e)
			{
				if (_log.IsEnabled(LogLevel.Error))
				{
					_log.Error("RecordAllTests exception", e);
				}
			}
		}

		private async Task RecordAllTestsInner(string folderName, int totalGroups, int currentGroupIndex, CancellationToken ct, Action doneAction = null)
		{
			try
			{
				var rootFolder = await GetStorageFolderFromNameOrCreate(ct, folderName);
#if TRACK_REFS
				var initialInactiveStats = Uno.UI.DataBinding.BinderReferenceHolder.GetInactiveViewReferencesStats();
				var initialActiveStats = Uno.UI.DataBinding.BinderReferenceHolder.GetReferenceStats();
#endif

				var tests = GetSampleChooserContentsForSnapshotTests().ToArray();

				if (_log.IsEnabled(LogLevel.Debug))
				{
					_log.Debug($"Generating tests for {tests.Length} test in {folderName}");
				}

				var target = new Microsoft.UI.Xaml.Media.Imaging.RenderTargetBitmap();

				for (int i = 0; i < tests.Length; i++)
				{
					if ((i % totalGroups) != currentGroupIndex)
					{
						continue;
					}

					var sample = tests[i];

					try
					{
#if TRACK_REFS
						var inactiveStats = Uno.UI.DataBinding.BinderReferenceHolder.GetInactiveViewReferencesStats();
						var activeStats = Uno.UI.DataBinding.BinderReferenceHolder.GetReferenceStats();
#endif

						var fileName = $"{SanitizeScreenshotFileName(sample.ControlName)}.png";

						try
						{
							Console.WriteLine($"Creating control for {fileName}");

							LogMemoryStatistics();

							if (_log.IsEnabled(LogLevel.Debug))
							{
								_log.Debug($"Generating {folderName}\\{fileName}");
							}

							ShowNewSection(ct, Section.SamplesContent);

							SelectedLibrarySample = sample;

							var (content, control) = await UpdateContent(ct, sample);

							ContentPhone = content;

							if (control is IWaitableSample waitableSample)
							{
								await waitableSample.SamplePreparedTask;
							}

#if HAS_UNO
							await _dispatcher.RunIdleAsync(_ => { });
							await _dispatcher.RunIdleAsync(_ => { });
#else
							await Task.Delay(500, ct);
#endif

							Console.WriteLine($"Generating screenshot for {fileName}");
							var file = await rootFolder.CreateFileAsync(fileName + ".png",
								CreationCollisionOption.ReplaceExisting
								).AsTask(ct);
							await GenerateBitmap(ct, target, file, content);

							try
							{
								SetRootTheme(ElementTheme.Dark);
								file = await rootFolder.CreateFileAsync(fileName + "-dark.png",
									CreationCollisionOption.ReplaceExisting
									).AsTask(ct);
								await GenerateBitmap(ct, target, file, content);
							}
							finally
							{
								SetRootTheme(ElementTheme.Default);
							}
						}
						catch (Exception e)
						{
							_log.Error($"Failed to execute test for {fileName}", e);
						}

#if TRACK_REFS
						Uno.UI.DataBinding.BinderReferenceHolder.LogInactiveViewReferencesStatsDiff(inactiveStats);
						Uno.UI.DataBinding.BinderReferenceHolder.LogActiveViewReferencesStatsDiff(activeStats);
#endif
						if (_log.IsEnabled(LogLevel.Debug))
						{
							_log.Debug($"Initial diff");
						}
#if TRACK_REFS
						Uno.UI.DataBinding.BinderReferenceHolder.LogInactiveViewReferencesStatsDiff(initialInactiveStats);
						Uno.UI.DataBinding.BinderReferenceHolder.LogActiveViewReferencesStatsDiff(initialActiveStats);
#endif
					}
					catch (Exception e)
					{
						if (_log.IsEnabled(LogLevel.Error))
						{
							_log.Error("Exception", e);
						}
					}
				}

				ContentPhone = null;

				if (_log.IsEnabled(LogLevel.Debug))
				{
					_log.Debug($"Final binder reference stats");
				}

#if TRACK_REFS
				Uno.UI.DataBinding.BinderReferenceHolder.LogInactiveViewReferencesStatsDiff(initialInactiveStats);
				Uno.UI.DataBinding.BinderReferenceHolder.LogActiveViewReferencesStatsDiff(initialActiveStats);
#endif
			}
			catch (Exception e)
			{
				if (_log.IsEnabled(LogLevel.Error))
				{
					_log.Error("RecordAllTests exception", e);
				}
			}
			finally
			{
				// Done action is needed as awaiting the task is not enough to determine the end of this method.
				doneAction?.Invoke();

				IsSplitVisible = true;
			}
		}

		private object SanitizeScreenshotFileName(string fileName) =>
			fileName
				.Replace(":", "_")
				.Replace("/", "_")
				.Replace("\\", "_")
				.Replace("\\", "_");

		internal void CreateNewWindow()
		{
#if HAS_UNO || WINAPPSDK //TODO: Enable UWP-style new window #8978
			var newWindow = new Window();
			var page = new MainPage();
			page.ViewModel.SetWindow(newWindow);
			newWindow.Content = page;
			newWindow.Activate();
#endif
		}

		internal void SetWindow(Window window) => _window = window;

		internal void OpenPlayground()
		{
			_ = SetSelectedSample(CancellationToken.None, typeof(Playground).FullName);
		}

		internal async Task OpenHelp(CancellationToken ct)
		{
			IsSplitVisible = false;

			_ = SetSelectedSample(CancellationToken.None, typeof(HelpPage).FullName);
		}

		internal async Task OpenSample(CancellationToken ct, SampleChooserContent content)
		{
			await SetSelectedSample(ct, content.ControlType.FullName);
		}

		internal async Task OpenRuntimeTests(CancellationToken ct)
		{
			IsSplitVisible = false;

			var runtimeTests = GetContent(typeof(SamplesApp.Samples.UnitTests.UnitTestsPage).GetTypeInfo());

			if (runtimeTests == null)
			{
				throw new InvalidOperationException($"Unable to find UnitTestsPage");
			}

			var (content, _) = await UpdateContent(ct, runtimeTests);

			if (!Equals(SelectedLibrarySample, runtimeTests))
			{
				SelectedLibrarySample = null;
			}

			if (!Equals(SelectedRecentSample, runtimeTests))
			{
				SelectedRecentSample = null;
			}

			if (!Equals(SelectedFavoriteSample, runtimeTests))
			{
				SelectedFavoriteSample = null;
			}
			ContentPhone = content;
		}

		internal async Task RunRuntimeTests(CancellationToken ct, string testResultsFilePath, Action doneAction = null)
		{
			try
			{
				await OpenRuntimeTests(ct);

				if (ContentPhone is FrameworkElement fe
					&& fe.FindName("UnitTestsRootControl") is Uno.UI.Samples.Tests.UnitTestsControl unitTests)
				{
#if IS_CI
					// Used to disable showing the test output visually
					unitTests.IsRunningOnCI = true;
#endif
					var engineConfig = new UnitTestEngineConfig();

					// Used to perform test grouping on CI to reduce the impact of re-runs
					if (!string.IsNullOrEmpty(Environment.GetEnvironmentVariable(TestGroupVariable)))
					{
						unitTests.CITestGroup = int.Parse(Environment.GetEnvironmentVariable(TestGroupVariable));
						unitTests.CITestGroupCount = int.Parse(Environment.GetEnvironmentVariable(TestGroupCountVariable));
					}

					// Read the environment variable first
					var rawFilter = Environment.GetEnvironmentVariable(TestsFilterRawVariable);

					// Read the CI set variable, through Uno.UITest, used for Wasm DOM tests.
					rawFilter = string.IsNullOrWhiteSpace(rawFilter) ? unitTests.CITestFilter : rawFilter;

					if (!string.IsNullOrWhiteSpace(rawFilter))
					{
						// Replace the "!" with "==" that can be replaced when the variable
						// value has been provided through an URL in wasm. (`=` is parsed as a key/value separator)
						rawFilter = rawFilter.Replace("!", "=");

						// read the filter generated by Uno.NUnitTransformTool with list-failed
						var filter = Encoding.UTF8.GetString(Convert.FromBase64String(rawFilter));

						engineConfig.Filters = filter
							.Split("|")
							.Select(s => s.Trim())
							.Where(s => !string.IsNullOrEmpty(s) && s != "invalid-test-for-retry") // skip marker from tests scripts
							.ToArray();

						Console.WriteLine($"Using filters: {string.Join(", ", engineConfig.Filters)}");
					}

					await Task.Run(() => unitTests.RunTests(ct, engineConfig));

					await SkiaSamplesAppHelper.SaveFile(testResultsFilePath, unitTests.NUnitTestResultsDocument, ct);
				}
			}
			finally
			{
				doneAction?.Invoke();
			}
		}

		partial void LogMemoryStatistics();

		private void ObserveChanges()
		{
			PropertyChanged += (s, e) =>
			{

				void Update(SampleChooserContent newContent)
				{
					if (_isRecordAllTests)
					{
						return;
					}
					_ = UnitTestDispatcherCompat
						.From(Owner)
						.RunAsync(
						async () =>
						{
							if (newContent != null)
							{
								CurrentSelectedSample = newContent;
								(ContentPhone, _) = await UpdateContent(CancellationToken.None, newContent);
							}
						}
					);
				}

				void UpdateFavorite()
				{
					IsFavoritedSample = CurrentSelectedSample != null ? FavoriteSamples.Contains(CurrentSelectedSample) : false;
				}

				switch (e.PropertyName)
				{
					case nameof(SelectedRecentSample):
						Update(SelectedRecentSample);
						break;

					case nameof(SelectedLibrarySample):
						Update(SelectedLibrarySample);
						break;

					case nameof(SelectedFavoriteSample):
						Update(SelectedFavoriteSample);
						break;

					case nameof(SelectedSearchSample):
						Update(SelectedSearchSample);
						break;

					case nameof(CurrentSelectedSample):
						UpdateFavorite();
						break;

					case nameof(FavoriteSamples):
						UpdateFavorite();
						break;

					case nameof(Categories):
						TryUpdateSearchResults();
						break;

					case nameof(SearchTerm):
						TryUpdateSearchResults();
						break;
				}
			};
		}

		CancellationTokenSource _pendingSearch;

		private void TryUpdateSearchResults()
		{
			_pendingSearch?.Cancel();

			var currentSearch = _pendingSearch = new CancellationTokenSource();

			var search = SearchTerm;

			_ = RunOnUIThreadAsync(
				async () =>
				{
					// Delay the search to allow the user to type more characters
					await Task.Delay(400);

					if (currentSearch.IsCancellationRequested)
					{
						return;
					}

					var results = await SearchAsync(search, _allCategories, currentSearch.Token);

					if (results is null || currentSearch.IsCancellationRequested)
					{
						return;
					}

					FilteredSamples = results;
				}
			);
		}

		private async Task<List<SampleChooserContent>> SearchAsync(string search, List<SampleChooserCategory> categories, CancellationToken cancellationToken)
		{
			if (string.IsNullOrEmpty(search))
			{
				return [];
			}

			return await Task.Run(() =>
			{
				var starts = categories
					.SelectMany(cat => cat.SamplesContent)
					.Where(content => content.ControlName.StartsWith(search, StringComparison.OrdinalIgnoreCase));

				if (cancellationToken.IsCancellationRequested)
				{
					return null;
				}

				var contains = categories
					.SelectMany(cat => cat.SamplesContent)
					.Where(content => content.ControlName.IndexOf(search, StringComparison.OrdinalIgnoreCase) >= 0);

				// Order the results by showing the "start with" results
				// followed by results that "contain" the search term
				return starts.Concat(contains).OrderBy(s => s.ControlName).Distinct().ToList();
			});
		}

		public bool TryOpenSingleSearchResult()
		{
			if (FilteredSamples is { } samples
				&& samples.Count is 1)
			{
				SelectedSearchSample = samples[0];
				return true;
			}

			return false;
		}

		/// <summary>
		/// This method is used to get the list of samplechoosercontent that is present in the settings.
		/// </summary>
		/// <param name="ct"></param>
		/// <returns></returns>
		private async Task<List<SampleChooserContent>> GetRecentSamples(CancellationToken ct)
		{
			try
			{
				return await GetFile(SampleChooserLRUConstant, () => new List<SampleChooserContent>());
			}
			catch (Exception e)
			{
				if (_log.IsEnabled(LogLevel.Warning))
				{
					_log.Warn("Get last run tests failed, returning empty list", e);
				}
				return new List<SampleChooserContent>();
			}
		}

		private SampleChooserCategory GetLatestCategory(CancellationToken ct)
		{
			var latestSelected = Get(SampleChooserLatestCategoryConstant, () => (string)null);
			return _categories.FirstOrDefault(c => c.Category == latestSelected) ??
				_categories.FirstOrDefault();
		}

		private static Assembly[] GetAllAssembies()
		{
#if WINAPPSDK
			var assemblies = new List<Assembly>();

			var files = Windows.ApplicationModel.Package.Current.InstalledLocation.GetFilesAsync().AsTask().Result;
			if (files == null)
			{
				return assemblies.ToArray();
			}

			foreach (var file in files.Where(file => file.FileType == ".dll" || file.FileType == ".exe"))
			{
				try
				{
					assemblies.Add(Assembly.Load(new AssemblyName(file.DisplayName)));
				}
				catch (Exception ex)
				{
					System.Diagnostics.Debug.WriteLine(ex.Message);
				}
			}

			return assemblies.ToArray();
#else
			return AppDomain.CurrentDomain.GetAssemblies();
#endif
		}

		private void UpdateCategoryList()
		{
			if (_manualTestsOnly)
			{
				Categories = _manualTestsCategories;
			}
			else
			{
				Categories = _visibleCategories;
			}
		}

		private IEnumerable<SampleChooserContent> GetSampleChooserContentsForSnapshotTests()
		{
			foreach (var sample in _allSamples)
			{
				var typeInfo = sample.GetTypeInfo();
				var sampleAttribute = FindSampleAttribute(typeInfo);
				if (sampleAttribute is { IgnoreInSnapshotTests: false })
				{
					yield return GetContent(typeInfo, sampleAttribute);
				}
			}
		}

		/// <summary>
		/// This method retrieves all the categories and sample contents associated with them throughout the app.
		/// </summary>
		private void InitializeCategories()
		{
			// Get all samples and their SampleAttribute.
			var samples =
				from type in _allSamples
				let sampleAttribute = FindSampleAttribute(type.GetTypeInfo())
				select (type, sampleAttribute);

			// Group samples into categories.
			var categories =
				from sample in samples
				let content = GetContent(sample.type.GetTypeInfo(), sample.sampleAttribute)
				from category in content.Categories
				group content by category into contentByCategory
				orderby contentByCategory.Key.ToLower(CultureInfo.CurrentUICulture)
				select new SampleChooserCategory(contentByCategory);

			_allCategories = categories.ToList();
			_visibleCategories = _allCategories.Where(c => !c.Category.StartsWith('_')).ToList();
			_manualTestsCategories = _allCategories
				.Select(cat => new SampleChooserCategory(cat.Category, cat.SamplesContent.Where(s => s.IsManualTest)))
				.Where(cat => cat.Count > 0)
				.ToList();

			UpdateCategoryList();
		}

		private static SampleChooserContent GetContent(TypeInfo type)
			=> GetContent(type, FindSampleAttribute(type));

		private static SampleChooserContent GetContent(TypeInfo type, SampleAttribute attribute)
			=> new SampleChooserContent
			{
				ControlName = attribute.Name ?? type.Name,
				Categories = attribute.Categories?.Where(c => !string.IsNullOrWhiteSpace(c)).Any() ?? false
					? attribute.Categories.Where(c => !string.IsNullOrWhiteSpace(c)).ToArray()
					: new[] { type.Namespace.Split('.').Last().TrimStart("Windows_UI_Xaml").TrimStart("Windows_UI") },
				ViewModelType = attribute.ViewModelType,
				Description = attribute.Description,
				ControlType = type.AsType(),
				IgnoreInSnapshotTests = attribute.IgnoreInSnapshotTests,
				IsManualTest = attribute.IsManualTest,
				UsesFrame = attribute.UsesFrame,
				DisableKeyboardShortcuts = attribute.DisableKeyboardShortcuts
			};

		private static IEnumerable<TypeInfo> FindDefinedAssemblies(Assembly assembly)
		{
			try
			{
				return assembly.DefinedTypes.ToArray();
			}
			catch (Exception)
			{
				return Array.Empty<TypeInfo>();
			}
		}

		private static SampleAttribute FindSampleAttribute(TypeInfo type)
		{
			try
			{
				if (!(type.Namespace?.StartsWith("System.Windows") ?? true))
				{
					return type?.GetCustomAttributes()
						.OfType<SampleAttribute>()
						.FirstOrDefault();
				}
				return null;
			}
			catch (Exception)
			{
				return null;
			}
		}

		private void OnSelectedCategoryChanged()
		{
			if (SelectedCategory != null)
			{
				SampleContents = SelectedCategory
					.SamplesContent
					.Safe()
					.OrderByDescending(s => s.IsFavorite)
					.ThenBy(s => s.ControlName)
					.ToList();
			}
		}

		private void UpdateFavorites(bool getAllSamples = false, List<SampleChooserContent> favoriteSamples = null)
		{
			// If true, load all samples and not just those of a selected category
			var samples = getAllSamples
				? _allCategories.SelectMany(cat => cat.SamplesContent).ToList()
				: SampleContents;

			var favorites = (favoriteSamples != null)
								? favoriteSamples           // Use the parameter if it exists
								: FavoriteSamples;    // Use the DynamicProperty

			foreach (var sample in samples)
			{
				UpdateFavoriteForSample(sample, favorites.Contains(sample));
			}

			SampleContents = samples;
		}

		private async Task ToggleFavorite(CancellationToken ct, SampleChooserContent sample)
		{
			if (sample is null)
			{
				return;
			}

			var favorites = await GetFavoriteSamples(ct);

			if (favorites.Contains(sample))
			{
				favorites.Remove(sample);
			}
			else
			{
				UpdateFavoriteForSample(sample, true);
				favorites.Add(sample);
			}

			await SetFile(SampleChooserFavoriteConstant, favorites.ToArray());

			FavoriteSamples = favorites;

			OnSelectedCategoryChanged();
			UpdateFavorites();
		}

		private async Task LoadPreviousTest(CancellationToken ct)
		{
			if (PreviousSample != null)
			{
				(ContentPhone, _) = await UpdateContent(ct, PreviousSample);
			}
		}

		private async Task ReloadCurrentTest(CancellationToken ct)
		{
			if (CurrentSelectedSample != null)
			{
				(ContentPhone, _) = await UpdateContent(ct, CurrentSelectedSample);
			}
		}

		private async Task LoadNextTest(CancellationToken ct)
		{
			if (NextSample != null)
			{
				(ContentPhone, _) = await UpdateContent(ct, NextSample);
			}
		}

		private void UpdateFavoriteForSample(SampleChooserContent sample, bool isFavorite)
		{
			// Have to update favorite on UI thread for the INotifyPropertyChanged in SampleChooserControl
			sample.IsFavorite = isFavorite;
		}

		/// <summary>
		/// This method is used to get the list of samplechoosercontent that is present in the settings for favorites.
		/// </summary>
		/// <param name="ct"></param>
		/// <param name="getAllSamples">If true, will load favorites based on all samples and not just based on selected category</param>
		/// <returns></returns>
		private async Task<List<SampleChooserContent>> GetFavoriteSamples(CancellationToken ct, bool getAllSamples = false)
		{
			try
			{
				var favoriteSamples = await GetFile(SampleChooserFavoriteConstant, () => new List<SampleChooserContent>());

				// Update the Sample List to set the IsFavorite to True
				UpdateFavorites(getAllSamples, favoriteSamples);

				return favoriteSamples;
			}
			catch (Exception e)
			{
				if (_log.IsEnabled(LogLevel.Warning))
				{
					_log.Warn("Get favorite samples failed, returning empty list", e);
				}
				return new List<SampleChooserContent>();
			}
		}

		/// <summary>
		/// This method receives a newContent and returns a newly built content. It also adds the content to the settings for the latest ran tests.
		/// </summary>
		/// <param name="ct"></param>
		/// <param name="newContent"></param>
		/// <returns>The updated content</returns>
		public async Task<(FrameworkElement Content, object Control)> UpdateContent(CancellationToken ct, SampleChooserContent newContent)
		{
			SampleChanging?.Invoke(this, EventArgs.Empty);

			FrameworkElement container = null;

			object control;
			var frameRequested =
				newContent.UsesFrame &&
				typeof(Page).IsAssignableFrom(newContent.ControlType);
			if (frameRequested)
			{
				var frame = new Frame();
				frame.Navigate(newContent.ControlType);
				container = frame;
				control = frame.Content;
			}
			else
			{
				//Activator is used here in order to generate the view and bind it directly with the proper view model
				control = Activator.CreateInstance(newContent.ControlType);

				if (control is ContentControl controlAsContentControl && !(controlAsContentControl.Content is Uno.UI.Samples.Controls.SampleControl))
				{
					control = new Uno.UI.Samples.Controls.SampleControl
					{
						Content = control,
						SampleDescription = newContent.Description
					};
				}

				container = new Border { Child = control as UIElement };
			}

			if (newContent.ViewModelType != null)
			{
				var constructors = newContent.ViewModelType.GetConstructors();
				object vm;
				if (constructors.Any(c => c.GetParameters().Length == 1))
				{
					vm = Activator.CreateInstance(newContent.ViewModelType, UnitTestDispatcherCompat.From(container));
				}
				else
				{
					vm = Activator.CreateInstance(newContent.ViewModelType);
				}
				container.DataContext = vm;

				if (vm is IDisposable disposable)
				{
					void Dispose(object snd, RoutedEventArgs e)
					{
						container.DataContext = null;
						container.Unloaded -= Dispose;
						container.DataContext = null;
						disposable.Dispose();
					}

					container.Unloaded += Dispose;
				}
			}
			else
			{
				// Don't propagate parent's DataContext to children
				container.DataContext = null;
			}

			var recents = await GetRecentSamples(ct);

			// Get the selected category, else if null find it using the SampleContent passed in
			var selectedCategory = SelectedCategory ?? GetCategory(newContent);

			if (selectedCategory != null)
			{
				Set(SampleChooserLatestCategoryConstant, selectedCategory.Category);
			}

			CurrentSelectedSample = newContent;

			if (!recents.Contains(newContent))
			{
				recents.Insert(0, newContent);

				if (recents.Count > _numberOfRecentSamplesVisible)
				{
					recents.RemoveAt(_numberOfRecentSamplesVisible);
				}

				await SetFile(SampleChooserLRUConstant, recents.ToArray());

				RecentSamples = recents;
			}

			return (container, control);
		}

		private SampleChooserCategory GetCategory(SampleChooserContent content)
		{
			return _allCategories.FirstOrDefault(cat =>
						cat.SamplesContent.Any(
							sample => sample.Equals(content)));
		}

		// Simple function to convert string to Enum value since specifying
		// CommandParameters as objects in Xaml didn't work
		private Section ConvertSectionEnum(string value)
		{
			Section section = Section.Library;
			Enum.TryParse(value, true, out section);
			return section;
		}

		public string GetAllSamplesNames()
		{
			// TODO: This might not be returning samples without a category (i.e, attributed just with [Sample] without any arguments)
			var q = from category in _allCategories
					from test in category.SamplesContent
					where !test.IgnoreInSnapshotTests && !test.IsManualTest
					select test.ControlType.FullName;

			return string.Join(";", q.Distinct());
		}

		public void SetSelectedSample(CancellationToken token, string categoryName, string sampleName)
		{
			var category = _allCategories.FirstOrDefault(
				c => c.Category != null &&
				c.Category.Equals(categoryName, StringComparison.InvariantCultureIgnoreCase));

			if (category == null)
			{
				return;
			}

			var sample = category.SamplesContent.FirstOrDefault(
				s => s.ControlName != null && s.ControlName.Equals(sampleName, StringComparison.InvariantCultureIgnoreCase));

			if (sample == null)
			{
				return;
			}

			ShowNewSection(token, Section.SamplesContent);

			SelectedLibrarySample = sample;
		}

		public async Task SetSelectedSample(CancellationToken ct, string metadataName)
		{
			Console.WriteLine($"Searching sample {metadataName}");

			try
			{
				var q = from category in _allCategories
						from test in category.SamplesContent
						where test.ControlType.FullName == metadataName
						select test;

				var sample = q.FirstOrDefault();

				if (sample != null)
				{
					IsSplitVisible = false;

					ShowNewSection(ct, Section.SamplesContent);

					SelectedLibrarySample = sample;

					// Wait for the content to render properly
					await Task.Delay(500, ct);
				}
				else
				{
					throw new InvalidOperationException($"The test {metadataName} cannot be found.");
				}
			}
			catch (Exception e)
			{
				Console.WriteLine($"Failed to set selected sample [{metadataName}]: {e}");
			}
		}

		private void Set<T>(string key, T value)
		{
#if !__NETSTD_REFERENCE__
			var json = Newtonsoft.Json.JsonConvert.SerializeObject(value);
			ApplicationData.Current.LocalSettings.Values[key] = json;
#endif
		}

		private T Get<T>(string key, Func<T> d = null)
		{
#if !__NETSTD_REFERENCE__
			var json = (string)ApplicationData.Current.LocalSettings.Values[key];
			return Newtonsoft.Json.JsonConvert.DeserializeObject<T>(json);
#else
			return default;
#endif
		}

		private
#if !__NETSTD_REFERENCE__
			async
#endif
			Task SetFile<T>(string key, T value)
		{
#if !__NETSTD_REFERENCE__
			var json = Newtonsoft.Json.JsonConvert.SerializeObject(value);

			using (await _fileLock.LockAsync(CancellationToken.None))
			{
				try
				{
					var folder = await StorageFolder.GetFolderFromPathAsync(ApplicationData.Current.LocalFolder.Path);
					var file = await folder.OpenStreamForWriteAsync(SampleChooserFileAddress + key, CreationCollisionOption.ReplaceExisting);
					using (var writer = new StreamWriter(file, encoding: System.Text.Encoding.UTF8))
					using (var jsonWriter = new Newtonsoft.Json.JsonTextWriter(writer))
					{
						var ser = Newtonsoft.Json.JsonSerializer.CreateDefault();
						ser.Serialize(jsonWriter, value);
						jsonWriter.Flush();
					}
				}
				catch (IOException e)
				{
					_log.LogWarning(e.Message);
				}
			}
#else
			return Task.CompletedTask;
#endif
		}

		private
#if !__NETSTD_REFERENCE__
			async
#endif
			Task<T> GetFile<T>(string key, Func<T> defaultValue = null)
		{
#if !__NETSTD_REFERENCE__
			string json = null;

			using (await _fileLock.LockAsync(CancellationToken.None))
			{
				try
				{
					var folder = ApplicationData.Current.LocalFolder;
					// GetFileAsync ensures the filesystem has been loaded on WASM.
					var file = await folder.GetFileAsync(SampleChooserFileAddress + key);
					json = await FileIO.ReadTextAsync(file);
				}
				catch (IOException e)
				{
					_log.LogWarning(e.Message);
				}
			}

			if (!json.IsNullOrWhiteSpace())
			{
				try
				{
					return Newtonsoft.Json.JsonConvert.DeserializeObject<T>(json);
				}
				catch (Exception ex)
				{
					_log.Error($"Could not deserialize Sample chooser file {key}.", ex);
				}
			}

			return defaultValue != null ? defaultValue() : default(T);
#else
			return Task.FromResult(defaultValue != null ? defaultValue() : default(T));
#endif

		}


		private async Task DumpOutputFolderName(CancellationToken ct, string folderName)
		{
			var folder = await GetStorageFolderFromNameOrCreate(ct, folderName);

			if (_log.IsEnabled(LogLevel.Debug))
			{
				_log.Debug($"Output folder for tests: {folder.Path}");
			}
		}

		private
#if !__WASM__
			async
#endif
			Task GenerateBitmap(CancellationToken ct
			, Microsoft.UI.Xaml.Media.Imaging.RenderTargetBitmap targetBitmap
			, StorageFile file
			, FrameworkElement content)
		{
#if __WASM__
			throw new NotSupportedException($"GenerateBitmap is not supported by this platform");
#else
			var element = Owner.XamlRoot.Content;

			try
			{
				targetBitmap = new Microsoft.UI.Xaml.Media.Imaging.RenderTargetBitmap();

				await targetBitmap.RenderAsync(element).AsTask(ct);

				var pixels = await targetBitmap.GetPixelsAsync().AsTask(ct);

				using (var fileStream = await file.OpenAsync(FileAccessMode.ReadWrite).AsTask(ct))
				{
					var encoder = await BitmapEncoder.CreateAsync(BitmapEncoder.PngEncoderId, fileStream).AsTask(ct);

					encoder.SetPixelData(
						BitmapPixelFormat.Bgra8,
						BitmapAlphaMode.Ignore,
						(uint)targetBitmap.PixelWidth,
						(uint)targetBitmap.PixelHeight,
						content.XamlRoot.RasterizationScale,
						content.XamlRoot.RasterizationScale,
						pixels.ToArray()
					);

					await encoder.FlushAsync().AsTask(ct);
				}
			}
			catch (Exception ex)
			{
				_log.Error(ex.Message);
			}
#endif
		}

		private static string GetRepositoryPath([CallerFilePath] string filePath = null)
		{
			// We could be building WSL app on Windows
			// In which case Path.DirectorySeparatorChar is '/' but filePath is using '\'
			var separator = Path.DirectorySeparatorChar;
			if (filePath.IndexOf(separator) == -1)
			{
				separator = separator == '/' ? '\\' : '/';
			}
			var srcSamplesApp = $"{separator}src{separator}SamplesApp";
			var repositoryPath = filePath;
			if (repositoryPath.IndexOf(srcSamplesApp) is int index && index > 0)
			{
				repositoryPath = repositoryPath.Substring(0, index);
			}

			return repositoryPath;
		}

		private async Task RunOnUIThreadAsync(Action action)
		{
			await _dispatcher.RunAsync(
					UnitTestDispatcherCompat.Priority.Normal,
					() => action());
		}
	}
}
