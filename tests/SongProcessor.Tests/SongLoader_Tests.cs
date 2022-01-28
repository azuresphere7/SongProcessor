﻿using FluentAssertions;

using Microsoft.VisualStudio.TestTools.UnitTesting;

using SongProcessor.FFmpeg;
using SongProcessor.Models;
using SongProcessor.Tests.FFmpeg;
using SongProcessor.Utils;

using System.Text.Json;

namespace SongProcessor.Tests;

[TestClass]
public sealed class SongLoader_Tests : FFmpeg_TestsBase
{
	private readonly ISongLoader _Loader = new SongLoader(new SourceInfoGatherer());

	[TestMethod]
	public async Task LoadEmptyFile_Test()
	{
		using var temp = new TempDirectory();
		var path = Path.Combine(temp.Dir, "info.amq");
		File.Create(path).Dispose();

		Func<Task> load = () => _Loader.LoadAsync(path);
		await load.Should().ThrowAsync<JsonException>().ConfigureAwait(false);
	}

	[TestMethod]
	public async Task LoadFakeFile_Test()
	{
		using var temp = new TempDirectory();
		var path = Path.Combine(temp.Dir, "info.amq");

		var actual = await _Loader.LoadAsync(path).ConfigureAwait(false);
		actual.Should().BeNull();
	}

	[TestMethod]
	public async Task LoadInvalidFile_Test()
	{
		using var temp = new TempDirectory();
		var path = Path.Combine(temp.Dir, "info.amq");
		await File.WriteAllTextAsync(path, "asdf").ConfigureAwait(false);

		Func<Task> load = () => _Loader.LoadAsync(path);
		await load.Should().ThrowAsync<JsonException>().ConfigureAwait(false);
	}

	[TestMethod]
	[TestCategory(FFMPEG_CATEGORY)]
	public async Task LoadInvalidVideo_Test()
	{
		using var temp = new TempDirectory();
		var anime = CreateAnime(temp.Dir);

		await _Loader.SaveAsync(anime.AbsoluteInfoPath, new AnimeBase(anime)
		{
			Source = anime.AbsoluteInfoPath,
		}).ConfigureAwait(false);

		Func<Task> load = () => _Loader.LoadAsync(anime.AbsoluteInfoPath);
		await load.Should().ThrowAsync<SourceInfoGatheringException>().ConfigureAwait(false);
	}

	[TestMethod]
	[TestCategory(FFMPEG_CATEGORY)]
	public async Task SaveAndLoad_Test()
	{
		using var temp = new TempDirectory();
		var expected = CreateAnime(temp.Dir);
		expected.Songs.AddRange(new Song[]
		{
			new()
			{
				Name = "Song1",
				Artist = "Artist1",
				Type = SongType.Op.Create(1),
			},
			new()
			{
				Name = "Song2",
				Artist = "Artist2",
				Type = SongType.Ed.Create(1),
			},
		});

		await _Loader.SaveAsync(expected).ConfigureAwait(false);
		var actual = await _Loader.LoadAsync(expected.AbsoluteInfoPath).ConfigureAwait(false);
		actual.Should().BeEquivalentTo(expected);
	}

	[TestMethod]
	public async Task SaveFailure_Test()
	{
		using var temp = new TempDirectory();
		var anime = CreateAnime(temp.Dir);
		using var fs = File.Create(anime.AbsoluteInfoPath);

		Func<Task> save = () => _Loader.SaveAsync(anime);
		await save.Should().ThrowAsync<IOException>().ConfigureAwait(false);
	}

	[TestMethod]
	public async Task SaveNew_Test()
	{
		using var temp = new TempDirectory();
		var anime = CreateAnime(temp.Dir);

		var actual = await _Loader.SaveNewAsync(temp.Dir, anime, new(
			AddShowNameDirectory: false,
			AllowOverwrite: false,
			CreateDuplicateFile: false
		)).ConfigureAwait(false);
		actual.Should().Be(Path.Combine(
			temp.Dir,
			"info.amq"
		));
	}

	[TestMethod]
	public async Task SaveNewAddShowNameDirectory_Test()
	{
		using var temp = new TempDirectory();
		var anime = CreateAnime(temp.Dir);

		var actual = await _Loader.SaveNewAsync(temp.Dir, anime, new(
			AddShowNameDirectory: true,
			AllowOverwrite: false,
			CreateDuplicateFile: false
		)).ConfigureAwait(false);
		actual.Should().Be(Path.Combine(
			temp.Dir,
			$"[{anime.Year}] {anime.Name}",
			"info.amq"
		));
	}

	[TestMethod]
	public async Task SaveNewCreateDuplicate_Test()
	{
		using var temp = new TempDirectory();
		var anime = CreateAnime(temp.Dir);
		File.Create(anime.AbsoluteInfoPath).Close();

		var actual = await _Loader.SaveNewAsync(temp.Dir, anime, new(
			AddShowNameDirectory: false,
			AllowOverwrite: false,
			CreateDuplicateFile: true
		)).ConfigureAwait(false);
		actual.Should().Be(Path.Combine(
			temp.Dir,
			"info (1).amq"
		));
	}

	[TestMethod]
	public async Task SaveNewDisallowOverwrite_Test()
	{
		using var temp = new TempDirectory();
		var anime = CreateAnime(temp.Dir);
		File.Create(anime.AbsoluteInfoPath).Close();

		var actual = await _Loader.SaveNewAsync(temp.Dir, anime, new(
			AddShowNameDirectory: false,
			AllowOverwrite: false,
			CreateDuplicateFile: false
		)).ConfigureAwait(false);
		actual.Should().BeNull();
	}
}