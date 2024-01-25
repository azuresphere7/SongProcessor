﻿using SongProcessor.Models;
using SongProcessor.Results;
using SongProcessor.Utils;

using System.Collections.Immutable;

namespace SongProcessor.FFmpeg.Jobs;

public abstract class SongJob : ISongJob
{
	public const int FFMPEG_ABORTED = -1;
	public const int FFMPEG_SUCCESS = 0;

	public bool AlreadyExists => File.Exists(GetSanitizedPath());
	public IAnime Anime { get; }
	public ISong Song { get; }

	protected static IReadOnlyDictionary<string, string> Args { get; } = new Dictionary<string, string>()
	{
		["v"] = "level+error", // Only output errors to stderr
		["nostats"] = "", // Do not output the default stats
		["progress"] = "pipe:1", // Output the stats to stdout in the easier to parse format
		["sn"] = "", // No subtitles
		["map_metadata"] = "-1", // No metadata
		["map_chapters"] = "-1", // No chapters
		["b:a"] = "320k", // Set the audio bitrate to 320k
	}.ToImmutableDictionary();

	public event Action<ProcessingData>? ProcessingDataReceived;

	protected SongJob(IAnime anime, ISong song)
	{
		Anime = anime;
		Song = song;
	}

	public async Task<IResult> ProcessAsync(CancellationToken? token = null)
	{
		var file = GetSanitizedPath();
		if (File.Exists(file))
		{
			return new FileAlreadyExists(file);
		}

		using var process = ProcessUtils.FFmpeg.CreateProcess(GenerateArgs());
		process.OnCancel((_, _) =>
		{
			// We can't just send 'q' to FFmpeg and have it quit gracefully because
			// sometimes the path never gets released and then can't get deleted
			process.Kill(entireProcessTree: true);
			if (!process.WaitForExit(250))
			{
				return;
			}

			try
			{
				File.Delete(file);
			}
			catch { } // Nothing we can do
		}, token);
		// FFmpeg will output the information we want to std:out
		var progressBuilder = new ProgressBuilder();
		process.OutputDataReceived += (_, e) =>
		{
			if (e.Data is null)
			{
				return;
			}

			if (progressBuilder.IsNextProgressReady(e.Data, out var progress))
			{
				ProcessingDataReceived?.Invoke(new(Song.GetLength(), file, progress));
			}
		};
		// Since we set the loglevel to error we don't need to filter
		var errors = default(List<string>);
		process.ErrorDataReceived += (_, e) =>
		{
			if (e.Data is null)
			{
				return;
			}

			errors ??= [];
			errors.Add(e.Data);
		};

		var code = await process.RunAsync(OutputMode.Async).ConfigureAwait(false);
		return code switch
		{
			FFMPEG_SUCCESS => Success.Instance,
			FFMPEG_ABORTED => Canceled.Instance,
			_ => new Error(code, errors ?? []),
		};
	}

	protected abstract string GenerateArgs();

	protected virtual string GetSanitizedPath()
	{
		var path = GetUnsanitizedPath();
		var dir = Path.GetDirectoryName(path)!;
		var file = FileUtils.SanitizePath(Path.GetFileName(path));
		return Path.Combine(dir, file);
	}

	protected abstract string GetUnsanitizedPath();
}