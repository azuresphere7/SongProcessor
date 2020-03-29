﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Reactive;
using System.Reactive.Linq;
using System.Windows.Input;

using AdvorangesUtils;

using AMQSongProcessor.Models;
using Avalonia.Controls;
using Newtonsoft.Json;

using ReactiveUI;
using ReactiveUI.Validation.Abstractions;
using ReactiveUI.Validation.Contexts;
using ReactiveUI.Validation.Extensions;

using Splat;

namespace AMQSongProcessor.UI.ViewModels
{
	//Never serialize this view/viewmodel since this data is related to folder structure
	[JsonConverter(typeof(NewtonsoftJsonSkipThis))]
	public class EditViewModel : ReactiveObject, IRoutableViewModel, IValidatableViewModel
	{
		private readonly IScreen? _HostScreen;
		private readonly SongLoader _Loader;
		private readonly Song _Song;
		private string _Artist;
		private int _AudioTrack;
		private string _CleanPath;
		private string _End;
		private int _Episode;
		private bool _Has480p;
		private bool _Has720p;
		private bool _HasMp3;
		private bool _IsSubmitted;
		private string _Name;
		private bool _ShouldIgnore;
		private int _SongPosition;
		private SongType _SongType;
		private string _Start;
		private int _VideoTrack;
		private int _VolumeModifier;

		public static IReadOnlyCollection<SongType> SongTypes { get; } = new[]
		{
			SongType.Opening,
			SongType.Ending,
			SongType.Insert,
		};

		public string Artist
		{
			get => _Artist;
			set => this.RaiseAndSetIfChanged(ref _Artist, value);
		}

		public int AudioTrack
		{
			get => _AudioTrack;
			set => this.RaiseAndSetIfChanged(ref _AudioTrack, value);
		}

		public string CleanPath
		{
			get => _CleanPath;
			set => this.RaiseAndSetIfChanged(ref _CleanPath, value);
		}

		public string End
		{
			get => _End;
			set => this.RaiseAndSetIfChanged(ref _End, value);
		}

		public int Episode
		{
			get => _Episode;
			set => this.RaiseAndSetIfChanged(ref _Episode, value);
		}

		public bool Has480p
		{
			get => _Has480p;
			set => this.RaiseAndSetIfChanged(ref _Has480p, value);
		}

		public bool Has720p
		{
			get => _Has720p;
			set => this.RaiseAndSetIfChanged(ref _Has720p, value);
		}

		public bool HasMp3
		{
			get => _HasMp3;
			set => this.RaiseAndSetIfChanged(ref _HasMp3, value);
		}

		public IScreen HostScreen => _HostScreen ?? Locator.Current.GetService<IScreen>();

		public bool IsSubmitted
		{
			get => _IsSubmitted;
			set => this.RaiseAndSetIfChanged(ref _IsSubmitted, value);
		}

		public string Name
		{
			get => _Name;
			set => this.RaiseAndSetIfChanged(ref _Name, value);
		}

		public ReactiveCommand<Unit, Unit> Save { get; }

		public bool ShouldIgnore
		{
			get => _ShouldIgnore;
			set => this.RaiseAndSetIfChanged(ref _ShouldIgnore, value);
		}

		public int SongPosition
		{
			get => _SongPosition;
			set => this.RaiseAndSetIfChanged(ref _SongPosition, value);
		}

		public SongType SongType
		{
			get => _SongType;
			set => this.RaiseAndSetIfChanged(ref _SongType, value);
		}

		public string Start
		{
			get => _Start;
			set => this.RaiseAndSetIfChanged(ref _Start, value);
		}

		public string UrlPathSegment => "/edit";

		public ValidationContext ValidationContext { get; } = new ValidationContext();

		public int VideoTrack
		{
			get => _VideoTrack;
			set => this.RaiseAndSetIfChanged(ref _VideoTrack, value);
		}

		public int VolumeModifier
		{
			get => _VolumeModifier;
			set => this.RaiseAndSetIfChanged(ref _VolumeModifier, value);
		}

		public EditViewModel(Song song, IScreen? screen = null)
		{
			_HostScreen = screen;
			_Loader = Locator.Current.GetService<SongLoader>();
			_Song = song ?? throw new ArgumentNullException(nameof(song));

			_Artist = _Song.Artist;
			_AudioTrack = _Song.OverrideAudioTrack;
			_CleanPath = _Song.CleanPath!;
			_End = _Song.End.ToString();
			_Episode = _Song.Episode ?? 0;
			_Has480p = !song.IsMissing(Status.Res480);
			_Has720p = !song.IsMissing(Status.Res720);
			_HasMp3 = !song.IsMissing(Status.Mp3);
			_IsSubmitted = song.Status != Status.NotSubmitted;
			_Name = _Song.Name;
			_ShouldIgnore = _Song.ShouldIgnore;
			_SongPosition = _Song.Type.Position ?? 0;
			_SongType = _Song.Type.Type;
			_Start = _Song.Start.ToString();
			_VideoTrack = _Song.OverrideVideoTrack;
			_VolumeModifier = _Song.VolumeModifier?.Decibels ?? 0;

			this.ValidationRule(
				x => x.Artist,
				x => !string.IsNullOrWhiteSpace(x),
				"Artist must not be null or empty.");
			this.ValidationRule(
				x => x.CleanPath,
				x => string.IsNullOrEmpty(x) || File.Exists(Path.Combine(song.Anime.Directory, x)),
				"Clean path must be null, empty, or lead to an existing file.");
			this.ValidationRule(
				x => x.Name,
				x => !string.IsNullOrWhiteSpace(x),
				"Name must not be null or empty.");

			var validTimes = this.WhenAnyValue(
				x => x.Start,
				x => x.End,
				(start, end) => new
				{
					ValidStart = TimeSpan.TryParse(start, out var s),
					Start = s,
					ValidEnd = TimeSpan.TryParse(end, out var e),
					End = e,
				})
				.Select(x => x.ValidStart && x.ValidEnd && x.Start <= x.End);
			this.ValidationRule(
				_ => validTimes,
				(_, state) => !state ? "Invalid times supplied or start is less than end." : "");

			Save = ReactiveCommand.CreateFromTask(async () =>
			{
				_Song.Artist = Artist;
				_Song.OverrideAudioTrack = AudioTrack;
				_Song.CleanPath = GetNullIfEmpty(CleanPath);
				_Song.End = TimeSpan.Parse(End);
				_Song.Episode = GetNullIfZero(Episode);
				_Song.Name = Name;
				_Song.Type = new SongTypeAndPosition(SongType, GetNullIfZero(SongPosition));
				_Song.ShouldIgnore = ShouldIgnore;
				_Song.Status = GetStatus();
				_Song.Start = TimeSpan.Parse(Start);
				_Song.OverrideVideoTrack = VideoTrack;
				_Song.VolumeModifier = GetVolumeModifer(VolumeModifier);

				await _Loader.SaveAsync(_Song.Anime).CAF();
			}, this.IsValid());
		}

		private static string? GetNullIfEmpty(string str)
			=> string.IsNullOrEmpty(str) ? null : str;

		private static int? GetNullIfZero(int? num)
			=> num == 0 ? null : num;

		private static VolumeModifer? GetVolumeModifer(int? num)
			=> GetNullIfZero(num) == null ? default(VolumeModifer?) : VolumeModifer.FromDecibels(num!.Value);

		private Status GetStatus()
		{
			var status = Status.NotSubmitted;
			if (HasMp3)
			{
				status |= Status.Mp3;
			}
			if (Has480p)
			{
				status |= Status.Res480;
			}
			if (Has720p)
			{
				status |= Status.Res720;
			}
			if (IsSubmitted && status == Status.NotSubmitted)
			{
				return Status.None;
			}
			return status;
		}
	}
}