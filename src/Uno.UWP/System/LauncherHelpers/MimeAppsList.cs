using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using Uno.Extensions;
using Uno.Foundation.Logging;

namespace Uno.UI.Runtime.Skia.Extensions.System.LauncherHelpers
{
	internal class MimeAppsList
	{
		private const string fileName = "mimeapps.list";
		private enum MimeAppSection
		{
			DefaultApplications,
			AddedAssociations,
			RemovedAssociations
		}
		private static readonly Dictionary<string, MimeAppSection> SectionMap =
			new Dictionary<string, MimeAppSection>(StringComparer.InvariantCultureIgnoreCase)
		{
			{ "[Default Applications]", MimeAppSection.DefaultApplications },
			{ "[Added Associations]", MimeAppSection.AddedAssociations },
			{ "[Removed Associations]", MimeAppSection.RemovedAssociations }
		};

		public Dictionary<string, HashSet<string>> DefaultApplications;
		public Dictionary<string, HashSet<string>> AddedAssociations;
		public Dictionary<string, HashSet<string>> RemovedAssociations;
		public Dictionary<string, HashSet<string>> DesktopFileAssociations;

		public MimeAppsList()
		{
			Reload();
		}

		public void Reload()
		{
			static void Add(Dictionary<string, HashSet<string>> target, string key, string value)
			{
				if (target.ContainsKey(key))
				{
					target[key].Add(value);
				}
				else
				{
					target.Add(key, new HashSet<string>() { value });
				}
			}

			var locations = new List<string>();
			DefaultApplications = new Dictionary<string, HashSet<string>>();
			AddedAssociations = new Dictionary<string, HashSet<string>>();
			RemovedAssociations = new Dictionary<string, HashSet<string>>();
			DesktopFileAssociations = new Dictionary<string, HashSet<string>>();

			// About the location: https://specifications.freedesktop.org/mime-apps-spec/mime-apps-spec-latest.html#file
			// About the variable names: https://specifications.freedesktop.org/basedir-spec/basedir-spec-latest.html#variables
			var Home = Environment.GetEnvironmentVariable("HOME");
			var XdgConfigHome = Environment.GetEnvironmentVariable("XDG_CONFIG_HOME");
			if (string.IsNullOrEmpty(XdgConfigHome))
			{
				XdgConfigHome = Path.Combine(Home, ".config");
			}

			var XdgConfigDirs = Environment.GetEnvironmentVariable("XDG_CONFIG_DIRS");
			if (string.IsNullOrEmpty(XdgConfigDirs))
			{
				XdgConfigDirs = "/etc/xdg";
			}

			var XdgDataHome = Environment.GetEnvironmentVariable("XDG_DATA_HOME");
			if (string.IsNullOrEmpty(XdgDataHome))
			{
				XdgDataHome = Path.Combine(Home, ".local/share");
			}

			var XdgDataDirs = Environment.GetEnvironmentVariable("XDG_DATA_DIRS");
			if (string.IsNullOrEmpty(XdgDataDirs))
			{
				XdgDataDirs = "/usr/local/share/:/usr/share/";
			}

			locations.Add(XdgConfigHome);
			locations.AddRange(XdgConfigDirs.Split(':'));
			locations.Add(Path.Combine(XdgDataHome, "applications"));
			locations.AddRange(XdgDataDirs.Split(':').Select(path => Path.Combine(path, "applications")));

			foreach (var location in locations)
			{
				ParseFile(location);
			}

			foreach (var dir in XdgDataDirs.Split(':'))
			{
				var desktopFileDir = Path.Combine(dir, "applications");
				var directoryInfo = new DirectoryInfo(desktopFileDir);

				if (!directoryInfo.Exists)
				{
					continue;
				}

				foreach (var fileInfo in directoryInfo.GetFiles("*.desktop"))
				{
					var desktopFile = new DesktopFile(fileInfo);
					if (!desktopFile.DesktopEntry.ContainsKey("MimeType"))
					{
						continue;
					}
					var mimeTypes = desktopFile.DesktopEntry["MimeType"].Split(';');
					foreach (var mimeType in mimeTypes)
					{
						Add(DesktopFileAssociations, mimeType, fileName);
					}
				}
			}
		}

		public bool Supports(string mimeType)
		{
			bool Check(Dictionary<string, HashSet<string>> target)
			{
				return target.ContainsKey(mimeType) && target[mimeType].Count > 0;
			}
			return Check(DefaultApplications) || Check(AddedAssociations) || Check(DesktopFileAssociations);
		}

		private void ParseFile(string location)
		{
			static void Add(Dictionary<string, HashSet<string>> target, string key, IEnumerable<string> values)
			{
				if (target.ContainsKey(key))
				{
					target[key].AddRange(values);
				}
				else
				{
					target.Add(key, new HashSet<string>(values));
				}
			}
			// Using algorithm here: https://specifications.freedesktop.org/mime-apps-spec/mime-apps-spec-latest.html#ordering
			try
			{
				using var reader = File.OpenText(Path.Combine(location, fileName));

				MimeAppSection? currentSection = null;

				var tempBlackList = new Dictionary<string, HashSet<string>>();

				while (!reader.EndOfStream)
				{
					var line = reader.ReadLine().Trim();
					if (string.IsNullOrEmpty(line))
					{
						continue;
					}

					if (SectionMap.ContainsKey(line))
					{
						currentSection = SectionMap[line];
					}
					else
					{
						if (!currentSection.HasValue)
						{
							continue;
						}

						var key = line.Substring(0, line.IndexOf('=')).Trim();
						var values = line.Substring(line.IndexOf('=') + 1).Trim().Split(';');

						switch (currentSection)
						{
							case MimeAppSection.DefaultApplications:
								{
									IEnumerable<string> toAdd = null;
									if (RemovedAssociations.ContainsKey(key))
									{
										var currentRemoved = RemovedAssociations[key];
										toAdd = values.Where(val => !currentRemoved.Contains(val));
									}
									else
									{
										toAdd = values;
									}

									Add(DefaultApplications, key, toAdd);
								}
								break;
							case MimeAppSection.AddedAssociations:
								{
									IEnumerable<string> toAdd = null;
									if (RemovedAssociations.ContainsKey(key))
									{
										var currentRemoved = RemovedAssociations[key];
										toAdd = values.Where(val => !currentRemoved.Contains(val));
									}
									else
									{
										toAdd = values;
									}

									Add(AddedAssociations, key, toAdd);
								}
								break;
							case MimeAppSection.RemovedAssociations:
								{
									Add(tempBlackList, key, values);
								}
								break;
						}
					}
				}

				foreach (var kvp in tempBlackList)
				{
					Add(RemovedAssociations, kvp.Key, kvp.Value);
				}
			}
			catch (Exception exception)
			{
				if (typeof(MimeAppsList).Log().IsEnabled(LogLevel.Error))
				{
					typeof(MimeAppsList).Log().Error($"Failed to {nameof(ParseFile)}.", exception);
				}
			}
		}
	}
}
