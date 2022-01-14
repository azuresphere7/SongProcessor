﻿using System.Text;

namespace SongProcessor.Utils;

public static class FileUtils
{
	private const string NUMBER_PATTERN = " ({0})";
	private static readonly HashSet<char> InvalidChars
		= new(Path.GetInvalidFileNameChars().Concat(Path.GetInvalidPathChars()));

	public static string? EnsureAbsoluteFile(string dir, string? file)
	{
		if (file is null)
		{
			return null;
		}

		return Path.IsPathFullyQualified(file) ? file : Path.Combine(dir, file);
	}

	public static string? GetRelativeOrAbsoluteFile(string dir, string? file)
	{
		if (file is null)
		{
			return null;
		}

		// Windows paths are case insensitive
		var comparison = OperatingSystem.IsWindows()
			? StringComparison.OrdinalIgnoreCase
			: StringComparison.CurrentCulture;

		// If the directory contains the info directory just return the nested file path
		// Otherwise return the absolute path
		return file.StartsWith(dir, comparison) ? file[(dir.Length + 1)..] : file;
	}

	public static string NextAvailableFile(string file)
	{
		static string GetNextFilename(string pattern)
		{
			var tmp = string.Format(pattern, 1);
			if (tmp == pattern)
			{
				throw new ArgumentException("The pattern must include an index place-holder", nameof(pattern));
			}

			if (!File.Exists(tmp))
			{
				return tmp; // short-circuit if no matches
			}

			int min = 1, max = 2; // min is inclusive, max is exclusive/untested
			while (File.Exists(string.Format(pattern, max)))
			{
				min = max;
				max *= 2;
			}

			while (max != min + 1)
			{
				var pivot = (max + min) / 2;
				if (File.Exists(string.Format(pattern, pivot)))
				{
					min = pivot;
				}
				else
				{
					max = pivot;
				}
			}
			return string.Format(pattern, max);
		}

		// Short-cut if already available
		if (!File.Exists(file))
		{
			return file;
		}

		// If path has extension then insert the number pattern just before the extension
		// and return next filename
		var pattern = Path.HasExtension(file)
			? file.Insert(file.LastIndexOf(Path.GetExtension(file)), NUMBER_PATTERN)
			: file + NUMBER_PATTERN;

		// Otherwise just append the pattern to the path and return next filename
		return GetNextFilename(pattern);
	}

	public static string SanitizePath(string path)
	{
		var sb = new StringBuilder();
		foreach (var c in path)
		{
			if (!InvalidChars.Contains(c))
			{
				sb.Append(c);
			}
		}
		return sb.ToString();
	}
}