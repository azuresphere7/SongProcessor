﻿using SongProcessor.Converters;
using SongProcessor.FFmpeg.Jobs;
using SongProcessor.Models;
using SongProcessor.Utils;

using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;

namespace SongProcessor.FFmpeg;

public sealed class SourceInfoGatherer : ISourceInfoGatherer
{
	private const string PROPERTY = "property";
	private const string VALUE = "value";
	private const string VOLUME_DETECT_PATTERN =
		@"\[Parsed_volumedetect_0 @ .*?\] " + // Starts with a method/caller and a hex #
		$"(?<{PROPERTY}>.*?): " + // Property name is first and has a colon right after
		$"(?<{VALUE}>.*?)$"; // Value is second

	private static readonly JsonSerializerOptions _Options = new();
	private static readonly char[] _SplitChars = new[] { '_', 'd' };
	private static readonly Dictionary<string, string> _VolumeArgs = new()
	{
		["vn"] = "",
		["sn"] = "",
		["dn"] = "",
		["f"] = "null",
	};
	private static readonly Dictionary<string, string> _VolumeAudioFilters = new()
	{
		["volumedetect"] = "",
	};
	private static readonly Regex VolumeDetectRegex =
		new(VOLUME_DETECT_PATTERN, RegexOptions.Compiled | RegexOptions.ExplicitCapture);

	static SourceInfoGatherer()
	{
		_Options.NumberHandling = JsonNumberHandling.AllowReadingFromString;
		_Options.Converters.Add(new AspectRatioJsonConverter());
		_Options.Converters.Add(new ParseJsonConverter<bool>(bool.Parse));
	}

	public Task<AudioInfo> GetAudioInfoAsync(string file, int track = 0)
		=> GetInfoAsync<AudioInfo>('a', file, track);

	public Task<VideoInfo> GetVideoInfoAsync(string file, int track = 0)
		=> GetInfoAsync<VideoInfo>('v', file, track);

	public async Task<VolumeInfo> GetVolumeInfoAsync(string file, int track = 0)
	{
		if (!File.Exists(file))
		{
			throw FileNotFound(file, 'a');
		}

		var args = new FFmpegArgs(
			Inputs: new FFmpegInput[]
			{
				new(file, null),
			},
			Mapping: new[]
			{
				$"0:a:{track}",
			},
			Args: _VolumeArgs,
			AudioFilters: _VolumeAudioFilters,
			VideoFilters: null,
			OutputFile: "-"
		);
		using var process = ProcessUtils.FFmpeg.CreateProcess(args.ToString());

		var histograms = new Dictionary<int, int>();
		var maxVolume = 0.00;
		var meanVolume = 0.00;
		var nSamples = 0;
		process.ErrorDataReceived += (s, e) =>
		{
			if (e.Data is null)
			{
				return;
			}

			var match = VolumeDetectRegex.Match(e.Data);
			if (!match.Success)
			{
				return;
			}

			var property = match.Groups[PROPERTY].Value;
			var value = match.Groups[VALUE].Value;
			switch (property)
			{
				case "n_samples":
					nSamples = int.Parse(value);
					break;

				case "mean_volume":
					meanVolume = VolumeModifer.Parse(value).Value;
					break;

				case "max_volume":
					maxVolume = VolumeModifer.Parse(value).Value;
					break;

				default: // histogram_#db
					var db = int.Parse(property.Split(_SplitChars)[1]);
					histograms[db] = int.Parse(value);
					break;
			}
		};

		var code = await process.RunAsync(OutputMode.Async).ConfigureAwait(false);
		if (code != SongJob.FFMPEG_SUCCESS)
		{
			var e = new InvalidOperationException($"FFmpeg returned error {code} via '{args}'.");
			throw new SourceInfoGatheringException(file, 'a', e);
		}

		return new(
			File: file,
			Histograms: histograms,
			MaxVolume: maxVolume,
			MeanVolume: meanVolume,
			NSamples: nSamples
		);
	}

	private static SourceInfoGatheringException FileNotFound(string file, char stream)
		=> new(file, stream, new FileNotFoundException("File does not exist", file));

	private static async Task<T> GetInfoAsync<T>(char stream, string file, int track)
		where T : SourceInfo
	{
		if (!File.Exists(file))
		{
			throw FileNotFound(file, stream);
		}

		var args = new FFmpegArgs(
			Inputs: Array.Empty<FFmpegInput>(),
			Mapping: Array.Empty<string>(),
			Args: new Dictionary<string, string>
			{
				["v"] = "quiet",
				["print_format"] = "json",
				["show_streams"] = "",
				["select_streams"] = $"{stream}:{track}",
			},
			AudioFilters: null,
			VideoFilters: null,
			OutputFile: file
		);
		using var process = ProcessUtils.FFprobe.CreateProcess(args.ToString());

		process.StartInfo.StandardOutputEncoding = Encoding.UTF8;
		process.StartInfo.RedirectStandardOutput = true;
		await process.RunAsync(OutputMode.Sync).ConfigureAwait(false);
		// Call WaitForExit otherwise the JSON may be incomplete
		await process.WaitForExitAsync().ConfigureAwait(false);

		T info;
		try
		{
			var output = await JsonSerializer.DeserializeAsync<Output<T>>(
				process.StandardOutput.BaseStream,
				_Options
			).ConfigureAwait(false);
			if (output?.Streams?.SingleOrDefault() is not T temp)
			{
				throw new JsonException($"FFmpeg returned invalid JSON via '{args}'.");
			}
			info = temp;
		}
		catch (Exception e)
		{
			throw new SourceInfoGatheringException(file, stream, e);
		}

		return info with
		{
			File = file
		};
	}

	private sealed record Output<T>(
		[property: JsonPropertyName("streams")]
		T[] Streams
	);
}