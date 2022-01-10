﻿using FluentAssertions;

using Microsoft.VisualStudio.TestTools.UnitTesting;

using SongProcessor.Models;

namespace SongProcessor.Tests.FFmpeg.Jobs;

[TestCategory(FFMPEG_CATEGORY)]
public abstract class SongJob_TestsBase : FFmpeg_TestsBase
{
	// The divisor to use for default length of the output
	// The input is around 5.37s long, so the output is around 1.34s
	public const int DIV = 4;

	protected static void AssertValidLength(double value, double expected)
		=> value.Should().BeApproximately(expected, expected * 0.03);

	protected static string GetSingleFile(string directory)
	{
		var files = Directory.GetFiles(directory);
		files.Length.Should().Be(1);
		return files.Single();
	}

	protected static async Task<string> GetSingleFileProducedAsync(string directory, ISongJob job)
	{
		var result = await job.ProcessAsync().ConfigureAwait(false);
		result.IsSuccess.Should().Be(true);
		return GetSingleFile(directory);
	}

	protected Anime GenerateAnime(string directory)
	{
		return new Anime(Path.Combine(directory, "info.amq"), new AnimeBase
		{
			Id = 73,
			Name = "Extremely Long Light Novel Title",
			Songs = new(),
			Source = ValidVideoPath,
			Year = 2500
		}, new(ValidVideoPath, ValidVideoInfo with
		{
			DAR = new(16, 9),
			SAR = AspectRatio.Square,
		}));
	}

	protected T GenerateJob<T>(string directory, Func<Anime, Song, T> factory)
	{
		var anime = GenerateAnime(directory);
		var song = new Song()
		{
			Start = TimeSpan.FromSeconds(0),
			End = TimeSpan.FromSeconds(anime.VideoInfo!.Value.Info.Duration!.Value / DIV),
			Name = Guid.NewGuid().ToString(),
		};
		return factory(anime, song);
	}
}