﻿using System;
using System.Diagnostics;
using System.Text.Json.Serialization;

using AMQSongProcessor.Utils;

namespace AMQSongProcessor.Models
{
	[DebuggerDisplay("{DebuggerDisplay,nq}")]
	public class Song
	{
		public static readonly TimeSpan UnknownTime = TimeSpan.FromSeconds(-1);

		[JsonIgnore]
		public Anime Anime { get; set; } = null!;
		public string Artist { get; set; } = null!;
		public string? CleanPath { get; set; }
		public TimeSpan End { get; set; }
		public int? Episode { get; set; }
		public string FullName => $"{Name} ({Artist})";
		public bool HasTimeStamp => Start != UnknownTime;
		public bool IsCompleted => !IsMissing(Status.Res480 | Status.Res720);
		public bool IsIncompleted => !(IsCompleted || IsUnsubmitted);
		public bool IsUnsubmitted => Status == Status.NotSubmitted;
		public TimeSpan Length => End - Start;
		public string Name { get; set; } = null!;
		public int OverrideAudioTrack { get; set; }
		public int OverrideVideoTrack { get; set; }
		public bool ShouldIgnore { get; set; }
		public TimeSpan Start { get; set; }
		public Status Status { get; set; }
		public SongTypeAndPosition Type { get; set; }
		public VolumeModifer? VolumeModifier { get; set; }
		private string DebuggerDisplay => FullName;

		public Song()
		{
		}

		public Song(string name, string artist, TimeSpan start, TimeSpan end, SongTypeAndPosition type, Status status)
		{
			Artist = artist;
			Name = name;
			Start = start;
			End = end;
			Type = type;
			Status = status;
		}

		public string? GetCleanSongPath()
			=> FileUtils.GetFile(Anime.Directory, CleanPath);

		public string GetMp3Path()
			=> FileUtils.GetFile(Anime.Directory, $"[{Anime.Id}] {Name}.mp3")!;

		public string GetVideoPath(int resolution)
			=> FileUtils.GetFile(Anime.Directory, $"[{Anime.Id}] {Name} [{resolution}p].webm")!;

		public bool IsMissing(Status status)
			=> (Status & status) == 0;

		public void SetCleanPath(string? path)
			=> CleanPath = FileUtils.StoreRelativeOrAbsolute(Anime.Directory, path);
	}
}