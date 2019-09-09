using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;

namespace Uno.UI.TestComparer.Comparer
{
	class TestFilesComparer
	{
		public TestFilesComparer(string basePath, string artifactsBasePath, string artifactsInnerBasePath, string platform)
		{
			_basePath = basePath;
			_artifactsBasePath = artifactsBasePath;
			_artifactsInnerBasePath = artifactsInnerBasePath;
			_platform = platform;
		}

		internal CompareResult Compare()
		{
			var testResult = new CompareResult();

			string path = _basePath;
			var resultsId = $"{DateTime.Now:yyyyMMdd-hhmmss}";
			string diffPath = Path.Combine(_basePath, $"Results-{_platform}-{resultsId}");
			string resultsFilePath = Path.Combine(_basePath, $"Results-{_platform}-{resultsId}.html");

			Directory.CreateDirectory(path);
			Directory.CreateDirectory(diffPath);

			var q1 = from directory in Directory.EnumerateDirectories(_artifactsBasePath)
					 let info = new DirectoryInfo(directory)
					 orderby info.CreationTime descending
					 select directory;

			q1 = q1.ToArray();

			var orderedDirectories = from directory in q1
									 orderby directory
									 select directory;

			var q = from directory in orderedDirectories.Select((v, i) => new { Index = i, Path = v })
					let files = from sample in EnumerateFiles(Path.Combine(directory.Path, _artifactsInnerBasePath, _platform), "*.png").AsParallel()
								select new { File = sample, Id = BuildSha1(sample) }
					select new
					{
						Path = directory.Path,
						Index = directory.Index,
						Files = files.ToArray(),
					};

			var allFolders = LogForeach(q, i => Console.WriteLine($"Processed {i.Path}")).ToArray();

			testResult.Folders.AddRange(allFolders.Select((p, index) => (index, p.Path)));

			var allFiles = allFolders
				.SelectMany(folder => folder
					.Files.Select(file => Path.GetFileName(file.File))
				)
				.Distinct()
				.ToArray();

			var changedList = new List<string>();
            foreach (var testFile in allFiles)
            {
                var testFileIncrements = from folder in allFolders
                                         orderby folder.Index ascending
                                         select new
                                         {
                                             Folder = folder.Path,
                                             FolderIndex = folder.Index,
                                             Files = new[] {
                                                new { FileInfo = folder.Files.FirstOrDefault(f => Path.GetFileName(f.File) == testFile) }
                                             }
                                         };

                var increments =
                    (
                        from platformIndex in Enumerable.Range(0, 1)
                        select testFileIncrements
                            .Aggregate(
                                new[] { new { FolderIndex = -1, Path = "", IdSha = "", Id = -1, CompareeId = -1 } },
                                (a, f) =>
                                {
                                    var platformFiles = f.Files[platformIndex];

                                    if (platformFiles?.FileInfo == null)
                                    {
                                        return a;
                                    }

                                    var otherMatch = a.Reverse().Where(i => i.IdSha != null).FirstOrDefault();
                                    if (platformFiles.FileInfo?.Id.sha != otherMatch?.IdSha)
                                    {
                                        return a
                                            .Concat(new[] { new { FolderIndex = f.FolderIndex, Path = platformFiles.FileInfo.File, IdSha = platformFiles.FileInfo?.Id.sha, Id = platformFiles.FileInfo?.Id.index ?? -1, CompareeId = otherMatch.Id } })
                                            .ToArray();
                                    }
                                    else
                                    {
                                        return a
                                            .Concat(new[] { new { FolderIndex = f.FolderIndex, Path = platformFiles.FileInfo.File, IdSha = (string)null, Id = platformFiles.FileInfo.Id.index, CompareeId = -1 } })
                                            .ToArray();
                                    }
                                }
                            )
                    ).ToArray();

                var hasChanges = increments.Any(i => i.Where(v => v.IdSha != null).Count() - 1 > 1);

                var compareResultFile = new CompareResultFile();
                compareResultFile.HasChanged = hasChanges;
                compareResultFile.TestName = testFile;

                testResult.Tests.Add(compareResultFile);

                var changeResult = increments[0];

                var firstFolder = changeResult.Where(i => i.FolderIndex != -1).Min(i => i.FolderIndex);

                for (int folderIndex = 0; folderIndex < allFolders.Length; folderIndex++)
                {
                    var folderInfo = changeResult.FirstOrDefault(inc => inc.FolderIndex == folderIndex);

                    if (folderInfo != null)
                    {
                        var hasChangedFromPrevious = folderIndex != firstFolder && folderInfo?.IdSha != null;

                        var compareResultFileRun = new CompareResultFileRun();
                        compareResultFile.ResultRun.Add(compareResultFileRun);

                        compareResultFileRun.ImageId = folderInfo.Id;
                        compareResultFileRun.ImageSha = folderInfo.IdSha;
                        compareResultFileRun.FilePath = folderInfo.Path;

                        compareResultFileRun.HasChanged = hasChangedFromPrevious;

                        if (folderInfo != null)
                        {
                            var previousFolderInfo = changeResult.FirstOrDefault(inc => inc.FolderIndex == folderIndex - 1);
                            if (hasChangedFromPrevious && previousFolderInfo != null)
                            {
                                var currentImage = DecodeImage(folderInfo.Path);
                                var previousImage = DecodeImage(previousFolderInfo.Path);

                                var diff = DiffImages(currentImage.pixels, previousImage.pixels);

                                var diffFilePath = Path.Combine(diffPath, $"{folderInfo.Id}-{folderInfo.CompareeId}.png");
                                WriteImage(diffFilePath, diff, currentImage.frame);

                                compareResultFileRun.DiffResultImage = diffFilePath;
                            }
                        }
                    }

                    changedList.Add(testFile);
                }
            }

			testResult.UnchangedTests = allFiles.Length - changedList.Count;
			testResult.TotalTests = allFiles.Length;

			return testResult;
		}

		private Dictionary<string, int> _fileHashesTable = new Dictionary<string, int>();
		private readonly string _basePath;
		private readonly string _artifactsBasePath;
		private readonly string _artifactsInnerBasePath;
		private readonly string _platform;

		private (string sha, int index) BuildSha1(string sample)
		{
			using (var sha1 = SHA1.Create())
			{
				using (var file = File.OpenRead(sample))
				{
					var data = sha1.ComputeHash(file);

					// Create a new Stringbuilder to collect the bytes
					// and create a string.
					var sBuilder = new StringBuilder();

					// Loop through each byte of the hashed data 
					// and format each one as a hexadecimal string.
					for (int i = 0; i < data.Length; i++)
					{
						sBuilder.Append(data[i].ToString("x2", CultureInfo.InvariantCulture));
					}

					var sha = sBuilder.ToString();

					lock (_fileHashesTable)
					{
						if (!_fileHashesTable.TryGetValue(sha, out var index))
						{
							index = _fileHashesTable.Count;
							_fileHashesTable[sha] = index;
						}

						return (sha, index);
					}
				}
			}
		}

		private IEnumerable<string> EnumerateFiles(string path, string pattern)
		{
			if (Directory.Exists(path))
			{
				return Directory.EnumerateFiles(path, pattern, SearchOption.AllDirectories);
			}
			else
			{
				return new string[0];
			}
		}

		private void WriteImage(string diffPath, byte[] diff, BitmapFrame frameInfo)
		{
			using (var stream = new FileStream(diffPath, FileMode.Create))
			{
				var encoder = new PngBitmapEncoder();

				encoder.Interlace = PngInterlaceOption.On;

				var frame = BitmapSource.Create(
					pixelWidth: (int)frameInfo.Width,
					pixelHeight: (int)frameInfo.Height,
					dpiX: frameInfo.DpiX,
					dpiY: frameInfo.DpiY,
					pixelFormat: frameInfo.Format,
					palette: frameInfo.Palette,
					pixels: diff,
					stride: (int)(frameInfo.Width * 4)
				);

				encoder.Frames.Add(BitmapFrame.Create(frame));
				encoder.Save(stream);
			}
		}

		private byte[] DiffImages(byte[] currentImage, byte[] previousImage)
		{
			var result = new byte[currentImage.Length];

			for (int i = 0; i < result.Length; i++)
			{
				result[i] = (byte)(currentImage[i] ^ previousImage[i]);
			}

			// Force result to be opaque
			for (int i = 0; i < result.Length; i += 4)
			{
				result[i+3] = 0xFF;
			}

			return result;
		}

		private (BitmapFrame frame, byte[] pixels) DecodeImage(string path1)
		{
			Stream imageStreamSource = new FileStream(path1, FileMode.Open, FileAccess.Read, FileShare.Read);
			var decoder = new PngBitmapDecoder(imageStreamSource, BitmapCreateOptions.PreservePixelFormat, BitmapCacheOption.Default);

			byte[] image = new byte[(int)(decoder.Frames[0].Width * decoder.Frames[0].Height * 4)];
			decoder.Frames[0].CopyPixels(image, (int)(decoder.Frames[0].Width * 4), 0);

			return (decoder.Frames[0], image);
		}

		private static IEnumerable<T> LogForeach<T>(IEnumerable<T> q, Action<T> action)
		{
			foreach (var item in q)
			{
				action(item);
				yield return item;
			}
		}

	}
}
