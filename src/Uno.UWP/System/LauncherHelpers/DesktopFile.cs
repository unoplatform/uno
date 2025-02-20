using System;
using System.Collections.Generic;
using System.IO;


namespace Uno.UI.Runtime.Skia.Extensions.System.LauncherHelpers
{
	// Implementation based on this: https://specifications.freedesktop.org/desktop-entry-spec/desktop-entry-spec-latest.html
	internal class DesktopFile
	{
		private readonly Dictionary<string, Dictionary<string, string>> _groups = new Dictionary<string, Dictionary<string, string>>();
		public Dictionary<string, string> DesktopEntry => _groups["Desktop Entry"];

		public DesktopFile(string desktopFileId)
		{
			var XdgDataDirs = Environment.GetEnvironmentVariable("XDG_DATA_DIRS");
			if (string.IsNullOrEmpty(XdgDataDirs))
			{
				XdgDataDirs = "/usr/local/share/:/usr/share/";
			}
			desktopFileId = desktopFileId.Replace('-', '/');

			foreach (var dir in XdgDataDirs.Split(':'))
			{
				var path = Path.Combine(dir, "applications", desktopFileId);
				if (!File.Exists(path))
				{
					continue;
				}
				ParseFile(path);
				return;
			}

			throw new FileNotFoundException($"{desktopFileId} does not exist");
		}

		public DesktopFile(FileInfo fileInfo)
		{
			ParseFile(fileInfo.FullName);
		}

		private void ParseFile(string path)
		{
			using var reader = File.OpenText(path);
			string currentHeaderName = null;
			while (!reader.EndOfStream)
			{
				var line = reader.ReadLine().Trim();
				if (string.IsNullOrEmpty(line))
				{
					continue;
				}
				if (line.StartsWith('#'))
				{
					continue;
				}
				if (line.StartsWith('[') && line.EndsWith(']'))
				{
					currentHeaderName = line.Substring(1, line.Length - 2);
					if (!_groups.ContainsKey(currentHeaderName))
					{
						_groups.Add(currentHeaderName, new Dictionary<string, string>());
					}
					continue;
				}

				var splitPos = line.IndexOf('=');
				var key = line.Substring(0, splitPos).Trim();
				var value = line.Substring(splitPos + 1).Trim();

				var group = _groups[currentHeaderName];
				group.TryAdd(key, value);
			}
		}
	}
}
