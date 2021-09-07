﻿using AMQSongProcessor.Models;
using AMQSongProcessor.Results;
using AMQSongProcessor.Utils;

namespace AMQSongProcessor.FFmpeg.Jobs
{
	public abstract class SongJob : ISongJob
	{
		public const int FFMPEG_SUCCESS = 0;
		public static readonly AspectRatio SquareSAR = new(1, 1);

		public bool AlreadyExists => File.Exists(GetSanitizedPath());
		public IAnime Anime { get; }
		public ISong Song { get; }

		public event Action<ProcessingData>? ProcessingDataReceived;

		protected SongJob(IAnime anime, ISong song)
		{
			Anime = anime;
			Song = song;
		}

		public async Task<IResult> ProcessAsync(CancellationToken? token = null)
		{
			var path = GetSanitizedPath();
			if (File.Exists(path))
			{
				return new FileAlreadyExistsResult(path);
			}

			using var process = ProcessUtils.FFmpeg.CreateProcess(GenerateArgs());
			process.OnCancel((_, _) =>
			{
				process.Kill();
				process.Dispose();
				// Without this sleep the file is not released in time and an exception happens
				Thread.Sleep(25);
				File.Delete(path);
			}, token);

			// ffmpeg will output the information we want to std:out
			var progressBuilder = new ProgressBuilder();
			process.OutputDataReceived += (_, e) =>
			{
				if (e.Data is null)
				{
					return;
				}

				if (progressBuilder.IsNextProgressReady(e.Data, out var progress))
				{
					var data = new ProcessingData(Song.GetLength(), path, progress);
					ProcessingDataReceived?.Invoke(data);
				}
			};
			var errors = default(List<string>);
			process.ErrorDataReceived += (_, e) =>
			{
				if (e.Data is null)
				{
					return;
				}

				errors ??= new();
				errors.Add(e.Data);
			};

			var code = await process.RunAsync(OutputMode.Async).ConfigureAwait(false);
			if (code != FFMPEG_SUCCESS)
			{
				errors ??= new();
				return new FFmpegErrorResult(code, errors);
			}
			return FFmpegSuccess.Instance;
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
}