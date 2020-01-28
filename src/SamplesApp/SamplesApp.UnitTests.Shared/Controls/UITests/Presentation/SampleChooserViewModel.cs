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
using Windows.UI.Xaml.Data;
using Windows.ApplicationModel.Core;
using Windows.UI.Core;
using Windows.Storage;
using Uno.Extensions;
using Uno.Logging;
using Microsoft.Extensions.Logging;
using Windows.UI.Xaml;
using System.IO;
using Uno.Disposables;

#if XAMARIN || NETSTANDARD2_0
using Windows.UI.Xaml.Controls;
#else
using Windows.Graphics.Imaging;
using Windows.Graphics.Display;
using Windows.UI.Xaml.Media;
using Windows.UI;
using Windows.UI.Xaml.Controls;
#endif

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
		private const int _numberOfRecentSamplesVisible = 10;
		private List<SampleChooserCategory> _categories;

		private readonly Uno.Threading.AsyncLock _fileLock = new Uno.Threading.AsyncLock();
#if !NETSTANDARD2_0
		private readonly string SampleChooserFileAddress = "SampleChooserFileAddress.";
#endif

		private readonly string SampleChooserLRUConstant = "Samples.LRU";
		private readonly string SampleChooserFavoriteConstant = "Samples.Favorites";
		private const string SampleChooserLatestCategoryConstant = "Samples.LatestCategory";

		private Section _lastSection = Section.Library;
		private readonly Stack<Section> _previousSections = new Stack<Section>();

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

		public SampleChooserViewModel()
		{
			Instance = this;

#if HAS_UNO
			// Disable all pooling so that controls get collected quickly.
			Windows.UI.Xaml.FrameworkTemplatePool.IsPoolingEnabled = false;
#endif
			InitializeCommands();
			ObserveChanges();

			_categories = GetSamples();
		}

		/// <summary>
		/// Displays a new page depending on the parameter that was sent.
		/// </summary>
		/// <param name="ct"></param>
		/// <param name="section">The page to go to.</param>
		/// <returns></returns>
		private async Task ShowNewSection(CancellationToken ct, Section section)
		{
			Console.WriteLine($"Section changed: {section}");

			switch (section)
			{
				// Sections
				case Section.Library:
				case Section.Samples:
				case Section.Recents:
				case Section.Favorites:
				case Section.Search:
					_previousSections.Push(_lastSection);
					await ShowSelectedList(ct, section);

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

		/// <summary>
		/// Goes back one page.
		/// </summary>
		/// <param name="ct"></param>
		/// <returns></returns>
		private async Task ShowPreviousSection(CancellationToken ct)
		{
			if (_previousSections.Count == 0)
			{
				_lastSection = Section.Library;
				await ShowSelectedList(ct, Section.Library);
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
					await ShowSelectedList(ct, priorPage);
					break;

				default:
					break;
			}
		}

		private async Task ShowSelectedList(CancellationToken ct, Section section)
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
			await Window.Current.Dispatcher.RunAsync(
				CoreDispatcherPriority.Normal,
				async () =>
				{
					var currentContent = ContentPhone as Control;

					if (currentContent == null)
					{
						this.Log().Debug($"No current Sample Control selected.");
						return;
					}

					var builder = new System.Text.StringBuilder();
					builder.AppendLine($"Dump for '{currentContent.GetType().FullName}':");
					PrintViewHierarchy(currentContent, builder);
					var toLog = builder.ToString();
					this.Log().Debug(toLog);
				});
		}


		private void OnContentVisibilityChanged()
		{
			IsAnyContentVisible = ContentVisibility;
		}

		private class SampleInfo
		{
			public SampleChooserCategory Category { get; set; }
			public SampleChooserContent Sample { get; set; }

			public bool Matches(string path)
			{
				var pathMembers = path.Split(new char[] { '.' });
				return Matches(category: pathMembers.ElementAtOrDefault(0), sampleName: pathMembers.ElementAtOrDefault(1));
			}

			private bool Matches(string category, string sampleName)
			{
				return category.HasValue() &&
						Category.Category.Equals(category, StringComparison.OrdinalIgnoreCase) &&
						(sampleName.IsNullOrEmpty() || (Sample?.ControlName?.Equals(sampleName, StringComparison.OrdinalIgnoreCase) ?? false));
			}
		}

		// Targets can either be a Category or a Sample (formatted as [Category].[SampleName])
		private const string _firstTargetToRun = "";

		private static readonly string[] _targetsToSkip =
		{
			/*Will be fixed along with bug #29117 */
			"GridView.GridViewEmptyGroups",
			"GridView.GridViewGrouped",
			"GridView.GridViewGroupedMaxRowsTwo",

			/*Will be fixed along with bug #29132 */
			"ListView.ListViewGrouped_ItemContainerStyleSelector",

			/*Will be fixed along with bug #29134 */
			"TimePicker.TimePickerSelector_Simple",

			"ScrollViewer.ScrollViewer_Padding",

			/* Will befixed along with bug #118190 */
			"Animations.DoubleAnimation_TranslateX",
			"Animations.DoubleAnimationUsingKeyFrames_TranslateX",
			"Animations.EasingDoubleKeyFrame_CompositeTransform",
		};

		private async Task RecordAllTests(CancellationToken ct)
		{
			try
			{
				IsSplitVisible = false;

				var folderName = "UITests-" + DateTime.Now.ToString("yyyyMMdd-hhmmssfff", CultureInfo.InvariantCulture);

				await DumpOutputFolderName(ct, folderName);


				await Window.Current.Dispatcher.RunAsync(
					CoreDispatcherPriority.Normal,
					async () =>
				{
#if XAMARIN
					var initialInactiveStats = Uno.UI.DataBinding.BinderReferenceHolder.GetInactiveViewReferencesStats();
					var initialActiveStats = Uno.UI.DataBinding.BinderReferenceHolder.GetReferenceStats();
#endif
					var testQuery = from category in _categories
									from sample in category.SamplesContent
									where !sample.IgnoreInSnapshotTests
									// where sample.ControlName.Equals("GridViewVerticalGrouped")
									select new SampleInfo
									{
										Category = category,
										Sample = sample,
									};

					Debug.Assert(
						_firstTargetToRun.IsNullOrEmpty() || testQuery.Any(testInfo => testInfo.Matches(_firstTargetToRun)),
						"First target to run must be either a Category or a Sample that is present in the app."
					);

					Debug.Assert(
						_targetsToSkip.Where(target => !target.IsNullOrWhiteSpace()).None(target => target.Equals(_firstTargetToRun, StringComparison.OrdinalIgnoreCase)),
						"First test to run cannot be skipped"
					);

					var tests = testQuery
						.SkipWhile(testInfo => _firstTargetToRun.HasValue() && !testInfo.Matches(_firstTargetToRun))
						.Where(testInfo => _targetsToSkip.None(testInfo.Matches))
						.ToArray();

					if (this.Log().IsEnabled(LogLevel.Debug))
					{
						this.Log().Debug($"Generating tests for {tests.Count()} test in {folderName}");
					}

					foreach (var sample in tests)
					{
						try
						{
#if XAMARIN
							var inactiveStats = Uno.UI.DataBinding.BinderReferenceHolder.GetInactiveViewReferencesStats();
							var activeStats = Uno.UI.DataBinding.BinderReferenceHolder.GetReferenceStats();
#endif

							var fileName = $"{sample.Category.Category}-{sample.Sample.ControlName}.png";

							try
							{
								LogMemoryStatistics();

								if (this.Log().IsEnabled(LogLevel.Debug))
								{
									this.Log().Debug($"Generating {folderName}\\{fileName}");
								}

								await ShowNewSection(ct, Section.SamplesContent);

								SelectedLibrarySample = sample.Sample;

								var content = await UpdateContent(ct, sample.Sample) as FrameworkElement;

								ContentPhone = content;

								await Task.Delay(500, ct);

								await GenerateBitmap(ct, folderName, fileName, content);
							}
							catch (Exception e)
							{
								this.Log().Error($"Failed to execute test for {fileName}", e);
							}

#if XAMARIN
							Uno.UI.DataBinding.BinderReferenceHolder.LogInactiveViewReferencesStatsDiff(inactiveStats);
							Uno.UI.DataBinding.BinderReferenceHolder.LogActiveViewReferencesStatsDiff(activeStats);
#endif
						}
						catch (Exception e)
						{
							if (this.Log().IsEnabled(LogLevel.Error))
							{
								this.Log().Error("Exception", e);
							}
						}
					}

					ContentPhone = null;

					if (this.Log().IsEnabled(LogLevel.Debug))
					{
						this.Log().Debug($"Final binder reference stats");
					}

#if XAMARIN
					Uno.UI.DataBinding.BinderReferenceHolder.LogInactiveViewReferencesStatsDiff(initialInactiveStats);
					Uno.UI.DataBinding.BinderReferenceHolder.LogActiveViewReferencesStatsDiff(initialActiveStats);
#endif
				});
			}
			catch (Exception e)
			{
				if (this.Log().IsEnabled(LogLevel.Error))
				{
					this.Log().Error("Exception", e);
				}
			}
			finally
			{
				IsSplitVisible = true;
			}
		}

		partial void LogMemoryStatistics();

		private void ObserveChanges()
		{
			PropertyChanged += (s, e) =>
			{

				void Update(SampleChooserContent newContent)
				{
					var unused = Window.Current.Dispatcher.RunAsync(
						CoreDispatcherPriority.Normal,
						async () =>
						{
							CurrentSelectedSample = newContent;

							if (CurrentSelectedSample != null)
							{
								ContentPhone = await UpdateContent(CancellationToken.None, newContent);
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

			var unused = Window.Current.Dispatcher.RunAsync(
				CoreDispatcherPriority.Normal, async () =>
				{
					await Task.Delay(500);

					if (!currentSearch.IsCancellationRequested)
					{
						FilteredSamples = UpdateSearch(search, Categories);
					}
				}
			);
		}

		private List<SampleChooserContent> UpdateSearch(string search, List<SampleChooserCategory> categories)
		{
			if (string.IsNullOrEmpty(search))
			{
				return new List<SampleChooserContent>();
			}

			var starts = categories
				.SelectMany(cat => cat.SamplesContent)
				.Where(content => content.ControlName.StartsWith(search, StringComparison.OrdinalIgnoreCase));

			var contains = categories
				.SelectMany(cat => cat.SamplesContent)
				.Where(content => !starts.Contains(content) && content.ControlName.Contains(search));

			// Order the results by showing the "start with" results
			// followed by results that "contain" the search term
			return starts.Concat(contains).ToList();
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
				if (this.Log().IsEnabled(LogLevel.Warning))
				{
					this.Log().Warn("Get last run tests failed, returning empty list", e);
				}
				return new List<SampleChooserContent>();
			}
		}

		private async Task<SampleChooserCategory> GetLatestCategory(CancellationToken ct)
		{
			var latestSelected = await Get(SampleChooserLatestCategoryConstant, () => (string)null);
			return _categories.FirstOrDefault(c => c.Category == latestSelected) ??
				_categories.FirstOrDefault();
		}

		private static Assembly[] GetAllAssembies()
		{
#if NETFX_CORE
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

		/// <summary>
		/// This method retreives all the categories and sample contents associated with them throughout the app.
		/// </summary>
		/// <returns></returns>
		private List<SampleChooserCategory> GetSamples()
		{
			var query = from assembly in GetAllAssembies()
						from type in FindDefinedAssemblies(assembly)
						let sampleAttribute = FindSampleAttribute(type)
						where sampleAttribute != null
						select (type, attribute: sampleAttribute);

			query = query.ToArray();

			var categories = new SortedSet<SampleChooserCategory>();

			foreach (var control in query)
			{
				var categoryStr = control.attribute.Category ?? control.type.Namespace.Split('.').Last();


				var sampleControl = new SampleChooserContent
				{
					ControlName = control.attribute.ControlName ?? control.type.Name,
					ViewModelType = control.attribute.ViewModelType,
					Description = control.attribute.Description,
					ControlType = control.type.AsType(),
					IgnoreInSnapshotTests = control.attribute.IgnoreInSnapshotTests
				};

				var category = categories.SingleOrDefault(c=>c.Category == categoryStr);
				if(category == null)
				{
					category = new SampleChooserCategory {Category = categoryStr};
					categories.Add(category);
				}

				category.SamplesContent.Add(sampleControl);
			}

			this.Log().Info($"Found {query.Count()} sample(s) in {categories.Count} categorie(s).");

			return categories.ToList();
		}

		private static IEnumerable<TypeInfo> FindDefinedAssemblies(Assembly assembly)
		{
			try
			{
				return assembly.DefinedTypes.ToArray();
			}
			catch (Exception)
			{
				return new TypeInfo[0];
			}
		}

		private static SampleControlInfoAttribute FindSampleAttribute(TypeInfo type)
		{
			try
			{
				if (!(type.Namespace?.StartsWith("System.Windows") ?? true))
				{
					return type?.GetCustomAttributes()
						.OfType<SampleControlInfoAttribute>()
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
					.OrderBy(s => s.IsFavorite)
					.ToList();
			}
		}

		private async Task UpdateFavorites(CancellationToken ct, bool getAllSamples = false, List<SampleChooserContent> favoriteSamples = null)
		{
			// If true, load all samples and not just those of a selected category
			var samples = getAllSamples
				? _categories.SelectMany(cat => cat.SamplesContent).ToList()
				: SampleContents;

			var favorites = (favoriteSamples != null)
								? favoriteSamples           // Use the parameter if it exists
								: FavoriteSamples;    // Use the DynamicProperty

			foreach (var sample in samples)
			{
				await UpdateFavoriteForSample(ct, sample, favorites.Contains(sample));
			}

			SampleContents = samples;
		}

		private async Task ToggleFavorite(CancellationToken ct, SampleChooserContent sample)
		{
			var favorites = await GetFavoriteSamples(ct);

			if (favorites.Contains(sample))
			{
				favorites.Remove(sample);
			}
			else
			{
				await UpdateFavoriteForSample(ct, sample, true);
				favorites.Add(sample);
			}

			await SetFile(SampleChooserFavoriteConstant, favorites.ToArray());

			FavoriteSamples = favorites;

			await UpdateFavorites(ct);
		}

		private async Task LoadPreviousTest(CancellationToken ct)
		{
			if (PreviousSample != null)
			{
				ContentPhone = await UpdateContent(ct, PreviousSample);
			}
		}

		private async Task ReloadCurrentTest(CancellationToken ct)
		{
			if (CurrentSelectedSample != null)
			{
				ContentPhone = await UpdateContent(ct, CurrentSelectedSample);
			}
		}

		private async Task LoadNextTest(CancellationToken ct)
		{
			if (NextSample != null)
			{
				ContentPhone = await UpdateContent(ct, NextSample);
			}
		}

		private async Task UpdateFavoriteForSample(CancellationToken ct, SampleChooserContent sample, bool isFavorite)
		{
			// Have to update favorite on UI thread for the INotifyPropertyChanged in SampleChooserControl
			await Window.Current.Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () => sample.IsFavorite = isFavorite);
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
				await UpdateFavorites(ct, getAllSamples, favoriteSamples);

				return favoriteSamples;
			}
			catch (Exception e)
			{
				if (this.Log().IsEnabled(LogLevel.Warning))
				{
					this.Log().Warn("Get favorite samples failed, returning empty list", e);
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
		public async Task<object> UpdateContent(CancellationToken ct, SampleChooserContent newContent)
		{
			//Activator is used here in order to generate the view and bind it directly with the proper view model
			var control = Activator.CreateInstance(newContent.ControlType);

			if (control is ContentControl controlAsContentControl && !(controlAsContentControl.Content is Uno.UI.Samples.Controls.SampleControl))
			{
				control = new Uno.UI.Samples.Controls.SampleControl
				{
					Content = control,
					SampleDescription = newContent.Description
				};
			}

			var container = new Border { Child = control as UIElement };

			if (newContent.ViewModelType != null)
			{
				var vm = Activator.CreateInstance(newContent.ViewModelType, container.Dispatcher);
				container.DataContext = vm;

				if (vm is IDisposable disposable)
				{
					void Dispose(object snd, RoutedEventArgs e)
					{
						container.Unloaded -= Dispose;
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
			var selectedCategory = SelectedCategory ?? await GetCategory(newContent);

			if (selectedCategory != null)
			{
				await Set(SampleChooserLatestCategoryConstant, selectedCategory.Category);
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

			return container;
		}

		private async Task<SampleChooserCategory> GetCategory(SampleChooserContent content)
		{
			return _categories.FirstOrDefault(cat =>
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
			var q = from category in _categories
					from test in category.SamplesContent
					where !test.IgnoreInSnapshotTests
					select test.ControlType.FullName;

			return string.Join(";", q);
		}

		public async Task SetSelectedSample(CancellationToken ct, string metadataName)
		{
			Console.WriteLine($"Searching sample {metadataName}");

			try
			{
				var q = from category in _categories
						from test in category.SamplesContent
						where test.ControlType.FullName == metadataName
						select test;

				var sample = q.FirstOrDefault();

				if (sample != null)
				{
					IsSplitVisible = false;

					await ShowNewSection(ct, Section.SamplesContent);

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

		private async Task Set<T>(string key, T value)
		{
#if !NETSTANDARD2_0
			var json = Newtonsoft.Json.JsonConvert.SerializeObject(value);
			ApplicationData.Current.LocalSettings.Values[key] = json;
#endif
		}

		private async Task<T> Get<T>(string key, Func<T> d = null)
		{
#if !NETSTANDARD2_0
			var json = (string)ApplicationData.Current.LocalSettings.Values[key];
			return Newtonsoft.Json.JsonConvert.DeserializeObject<T>(json);
#else
			return default;
#endif
		}

		private async Task SetFile<T>(string key, T value)
		{
#if !NETSTANDARD2_0
			var json = Newtonsoft.Json.JsonConvert.SerializeObject(value);

			using (await _fileLock.LockAsync(CancellationToken.None))
			{
				try
				{
					var folderPath = ApplicationData.Current.LocalFolder.Path;
					File.WriteAllText(Path.Combine(folderPath, SampleChooserFileAddress + key), json);
				}
				catch (IOException e)
				{
					this.Log().Error(e.Message);
				}
			}
#endif
		}

		private async Task<T> GetFile<T>(string key, Func<T> defaultValue = null)
		{
#if !NETSTANDARD2_0
			string json = null;

			using (await _fileLock.LockAsync(CancellationToken.None))
			{
				try
				{
					var folderPath = ApplicationData.Current.LocalFolder.Path;
					json = File.ReadAllText(Path.Combine(folderPath, SampleChooserFileAddress + key));
				}
				catch (IOException e)
				{
					this.Log().Error(e.Message);
				}
			}

			if (json.HasValueTrimmed())
			{
				return Newtonsoft.Json.JsonConvert.DeserializeObject<T>(json);
			}
			else
#endif
			{
				return defaultValue != null ? defaultValue() : default(T);
			}
		}
	}
}
