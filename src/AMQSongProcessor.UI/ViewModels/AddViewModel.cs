﻿using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Reactive;
using System.Runtime.Serialization;
using System.Threading.Tasks;

using AMQSongProcessor.Gatherers;
using AMQSongProcessor.Models;
using AMQSongProcessor.UI.Models;

using ReactiveUI;

using Splat;

namespace AMQSongProcessor.UI.ViewModels
{
	[DataContract]
	public class AddViewModel : ReactiveObject, IRoutableViewModel
	{
		private static readonly SaveNewOptions _SaveOptions = new
		(
			AddShowNameDirectory: true,
			AllowOverwrite: false,
			CreateDuplicateFile: true
		);

		private readonly IEnumerable<IAnimeGatherer> _Gatherers;
		private readonly IScreen? _HostScreen;
		private readonly ISongLoader _Loader;
		private readonly IMessageBoxManager _MessageBoxManager;
		private bool _AddEndings = true;
		private bool _AddInserts = true;
		private bool _AddOpenings = true;
		private bool _AddSongs = true;
		private string? _Directory;
		private Exception? _Exception;
		private int _Id = 1;
		private string _SelectedGathererName;

		[DataMember]
		public bool AddEndings
		{
			get => _AddEndings;
			set => this.RaiseAndSetIfChanged(ref _AddEndings, value);
		}
		[DataMember]
		public bool AddInserts
		{
			get => _AddInserts;
			set => this.RaiseAndSetIfChanged(ref _AddInserts, value);
		}
		[DataMember]
		public bool AddOpenings
		{
			get => _AddOpenings;
			set => this.RaiseAndSetIfChanged(ref _AddOpenings, value);
		}
		[DataMember]
		public bool AddSongs
		{
			get => _AddSongs;
			set => this.RaiseAndSetIfChanged(ref _AddSongs, value);
		}
		public ObservableCollection<IAnime> Anime { get; } = new();
		[DataMember]
		public string? Directory
		{
			get => _Directory;
			set => this.RaiseAndSetIfChanged(ref _Directory, value);
		}
		public Exception? Exception
		{
			get => _Exception;
			set => this.RaiseAndSetIfChanged(ref _Exception, value);
		}
		public IEnumerable<string> GathererNames { get; }
		public IScreen HostScreen => _HostScreen ?? Locator.Current.GetService<IScreen>();
		[DataMember]
		public int Id
		{
			get => _Id;
			set => this.RaiseAndSetIfChanged(ref _Id, value);
		}
		[DataMember]
		public string SelectedGathererName
		{
			get => _SelectedGathererName;
			set => this.RaiseAndSetIfChanged(ref _SelectedGathererName, value);
		}
		public string UrlPathSegment => "/add";

		#region Commands
		public ReactiveCommand<Unit, Unit> Add { get; }
		public ReactiveCommand<IAnime, Unit> DeleteAnime { get; }
		public ReactiveCommand<Unit, Unit> SelectDirectory { get; }
		#endregion Commands

		public AddViewModel() : this(null)
		{
		}

		public AddViewModel(IScreen? screen)
		{
			_HostScreen = screen;
			_Loader = Locator.Current.GetService<ISongLoader>();
			_MessageBoxManager = Locator.Current.GetService<IMessageBoxManager>();
			_Gatherers = Locator.Current.GetService<IEnumerable<IAnimeGatherer>>();
			_SelectedGathererName = _Gatherers.First().Name;
			GathererNames = _Gatherers.Select(x => x.Name);

			var canAdd = this.WhenAnyValue(
				x => x.Directory,
				x => x.Id,
				(directory, id) => System.IO.Directory.Exists(directory) && id > 0);
			Add = ReactiveCommand.CreateFromTask(PrivateAdd, canAdd);
			DeleteAnime = ReactiveCommand.CreateFromTask<IAnime>(PrivateDeleteAnime);
			SelectDirectory = ReactiveCommand.CreateFromTask(PrivateSelectDirectory);
		}

		private async Task PrivateAdd()
		{
			try
			{
				var gatherer = _Gatherers.Single(x => x.Name == SelectedGathererName);
				var model = await gatherer.GetAsync(Id, new
				(
					AddEndings: AddEndings,
					AddInserts: AddInserts,
					AddOpenings: AddOpenings,
					AddSongs: AddSongs)
				).ConfigureAwait(true);
				var file = await _Loader.SaveAsync(Directory!, model, _SaveOptions).ConfigureAwait(true);
				Anime.Add(new ObservableAnime(new Anime(file!, model, null)));
				Exception = null;
			}
			catch (Exception e)
			{
				Exception = e;
			}
		}

		private async Task PrivateDeleteAnime(IAnime anime)
		{
			var result = await _MessageBoxManager.ConfirmAsync(new()
			{
				Text = $"Are you sure you want to delete {anime.Name}?",
				Title = "Anime Deletion",
			}).ConfigureAwait(true);
			if (!result)
			{
				return;
			}

			Anime.Remove(anime);
			File.Delete(anime.AbsoluteInfoPath);
		}

		private async Task PrivateSelectDirectory()
		{
			var path = await _MessageBoxManager.GetDirectoryAsync(Directory).ConfigureAwait(true);
			if (string.IsNullOrWhiteSpace(path))
			{
				return;
			}

			Directory = path;
		}
	}
}