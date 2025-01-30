using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Numerics;
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

		internal CompareResult Compare(string[] artifacts)
		{
			var testResult = new CompareResult(_platform);

			var path = _basePath;
			var resultsId = $"{DateTime.Now:yyyyMMdd-hhmmss}";
			var diffPath = Path.Combine(_basePath, $"Results-{_platform}-{resultsId}");
			var resultsFilePath = Path.Combine(_basePath, $"Results-{_platform}-{resultsId}.html");

			Directory.CreateDirectory(path);
			Directory.CreateDirectory(diffPath);

			if (artifacts == null)
			{
				var q1 = from directory in Directory.EnumerateDirectories(_artifactsBasePath)
						 let info = new DirectoryInfo(directory)
						 orderby info.FullName descending
						 select directory;

				var orderedDirectories = from directory in q1
										 orderby directory
										 select directory;

				artifacts = orderedDirectories.ToArray();
			}

			var q = from directory in artifacts.Select((v, i) => new { Index = i, Path = v })
					let files = from sample in EnumerateFiles(Path.Combine(directory.Path, _artifactsInnerBasePath, _platform), "*.png").AsParallel()
								where CanBeUsedForCompare(sample)
								select new { File = sample, Id = BuildSha1(sample) }
					select new
					{
						Path = directory.Path,
						Index = directory.Index,
						Files = files.AsParallel().ToArray(),
					};

			var allFolders = LogForeach(q, i => Helpers.WriteLineWithTime($"Processed {i.Path}")).ToArray();

			testResult.Folders.AddRange(allFolders.Select((p, index) => (index, p.Path)).AsParallel());

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

									var otherMatch = a.Reverse().FirstOrDefault(i => i.IdSha != null);
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

				var hasChanges = increments.Any(i => i.Count(v => v.IdSha != null) - 1 > 1);

				var compareResultFile = new CompareResultFile();
				compareResultFile.HasChanged = hasChanges;
				compareResultFile.TestName = testFile;

				testResult.Tests.Add(compareResultFile);

				var changeResult = increments[0];

				var firstFolder = changeResult.Where(i => i.FolderIndex != -1).Min(i => i.FolderIndex);

				for (var folderIndex = 0; folderIndex < allFolders.Length; folderIndex++)
				{
					var folderInfo = changeResult.FirstOrDefault(inc => inc.FolderIndex == folderIndex);

					if (folderInfo != null)
					{
						var hasChangedFromPrevious = folderIndex != firstFolder && folderInfo.IdSha != null;

						var compareResultFileRun = new CompareResultFileRun();
						compareResultFile.ResultRun.Add(compareResultFileRun);

						compareResultFileRun.ImageId = folderInfo.Id;
						compareResultFileRun.ImageSha = folderInfo.IdSha;
						compareResultFileRun.FilePath = folderInfo.Path;
						compareResultFileRun.FolderIndex = folderIndex;

						compareResultFileRun.HasChanged = hasChangedFromPrevious;

						var previousFolderInfo = changeResult.FirstOrDefault(inc => inc.FolderIndex == folderIndex - 1);
						if (hasChangedFromPrevious && previousFolderInfo != null)
						{
							try
							{
								var currentImage = DecodeImage(folderInfo.Path);
								var previousImage = DecodeImage(previousFolderInfo.Path);

								if (currentImage.pixels.Length == previousImage.pixels.Length)
								{
									var diff = DiffImages(currentImage.pixels, previousImage.pixels, currentImage.frame.Format.BitsPerPixel / 8);

									var diffFilePath = Path.Combine(diffPath, $"{folderInfo.Id}-{folderInfo.CompareeId}.png");
									WriteImage(diffFilePath, diff, currentImage.frame, currentImage.stride);

									compareResultFileRun.DiffResultImage = diffFilePath;
								}

								changedList.Add(testFile);
							}
							catch (Exception ex)
							{
								Helpers.WriteLineWithTime($"[ERROR] Failed to process [{folderInfo.Path}] and [{previousFolderInfo.Path}]\n({ex})");
							}
						}

						GC.Collect(2, GCCollectionMode.Forced);
						GC.WaitForPendingFinalizers();
					}
				}
			}

			testResult.UnchangedTests = allFiles.Length - changedList.Distinct().Count();
			testResult.TotalTests = allFiles.Length;

			return testResult;
		}

		private bool CanBeUsedForCompare(string sample)
		{
			if (ReadScreenshotMetadata(sample) is { } options)
			{
				if (options.TryGetValue("IgnoreInSnapshotCompare", out var ignore) && ignore.Equals("true", StringComparison.OrdinalIgnoreCase))
				{
					return false;
				}
			}

			return true;
		}

		private static IDictionary<string, string> ReadScreenshotMetadata(string sample)
		{
			var metadataFile = Path.Combine(Path.GetDirectoryName(sample),
				$"{Path.GetFileNameWithoutExtension(sample)}.metadata");

			if (File.Exists(metadataFile))
			{
				var lines = File.ReadAllLines(metadataFile);

				return lines.Select(l => l.Split('=')).ToDictionary(p => p[0], p => p[1]);
			}

			return null;
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
				using (var file = File.OpenRead($@"\\?\{sample}"))
				{
					var data = sha1.ComputeHash(file);

					// Create a new Stringbuilder to collect the bytes
					// and create a string.
					var sBuilder = new StringBuilder();

					// Loop through each byte of the hashed data 
					// and format each one as a hexadecimal string.
					for (var i = 0; i < data.Length; i++)
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
				return [];
			}
		}

		private void WriteImage(string diffPath, byte[] diff, BitmapFrame frameInfo, int stride)
		{
			using (var stream = new FileStream(diffPath, FileMode.Create))
			{
				var encoder = new PngBitmapEncoder();

				encoder.Interlace = PngInterlaceOption.On;

				var frame = BitmapSource.Create(
					pixelWidth: (int)frameInfo.PixelWidth,
					pixelHeight: (int)frameInfo.PixelHeight,
					dpiX: frameInfo.DpiX,
					dpiY: frameInfo.DpiY,
					pixelFormat: frameInfo.Format,
					palette: frameInfo.Palette,
					pixels: diff,
					stride: stride
				);

				encoder.Frames.Add(BitmapFrame.Create(frame));
				encoder.Save(stream);
			}
		}

		private byte[] DiffImages(ReadOnlySpan<byte> currentImage, ReadOnlySpan<byte> previousImage, int pixelSize)
		{
			var length = currentImage.Length;
			var resultImage = new byte[length];
			var vectorSize = Vector<byte>.Count;
			var i = 0;

			// Apply XOR with SIMD on blocks of size 'vectorSize'
			for (; i <= length - vectorSize; i += vectorSize)
			{
				var currentVector = new Vector<byte>(currentImage.Slice(i, vectorSize));
				var previousVector = new Vector<byte>(previousImage.Slice(i, vectorSize));
				var resultVector = currentVector ^ previousVector;
				resultVector.CopyTo(resultImage.AsSpan(i, vectorSize));
			}

			// Process remaining pixels without SIMD if necessary
			for (; i < length; i++)
			{
				resultImage[i] = (byte)(currentImage[i] ^ previousImage[i]);
			}

			// If pixelSize is 4, force opacity on every fourth byte
			if (pixelSize == 4)
			{
				for (i = 3; i < length; i += 4)
				{
					resultImage[i] = 0xFF;
				}
			}

			return resultImage;
		}

		private (BitmapFrame frame, byte[] pixels, int stride) DecodeImage(string path1)
		{
			try
			{
				using (Stream imageStreamSource = new FileStream($@"\\?\{path1}", FileMode.Open, FileAccess.Read, FileShare.Read))
				{
					var decoder = new PngBitmapDecoder(imageStreamSource, BitmapCreateOptions.PreservePixelFormat, BitmapCacheOption.Default);

					var f = decoder.Frames[0];
					var sourceBytesPerPixels = f.Format.BitsPerPixel / 8;
					var sourceStride = f.PixelWidth * sourceBytesPerPixels;
					sourceStride += (4 - sourceStride % 4);

					var image = new byte[sourceStride * (f.PixelHeight * sourceBytesPerPixels)];
					f.CopyPixels(image, (int)sourceStride, 0);

					return (f, image, sourceStride);
				}
			}
			catch (Exception ex)
			{
				throw new InvalidOperationException($"Unable to decode image {path1}", ex);
			}
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
