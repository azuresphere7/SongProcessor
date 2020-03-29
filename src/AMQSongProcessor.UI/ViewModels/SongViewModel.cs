﻿using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive.Linq;
using System.Runtime.Serialization;
using System.Windows.Input;

using AdvorangesUtils;

using AMQSongProcessor.Models;
using Avalonia.Controls;
using Avalonia.Controls.Templates;
using Avalonia.LogicalTree;
using Avalonia.Threading;
using Avalonia.VisualTree;
using DynamicData.Binding;
using ReactiveUI;

using Splat;

namespace AMQSongProcessor.UI.ViewModels
{
	[DataContract]
	public class SongViewModel : ReactiveObject, IRoutableViewModel
	{
		private const string LOAD = "Load";
		private const string UNLOAD = "Unload";

		private readonly IScreen? _HostScreen;
		private readonly SongLoader _Loader;
		private readonly ISongProcessor _Processor;
		private string? _Directory;
		private string _DirectoryButtonText = LOAD;

		public ICommand AddSong { get; }
		public ObservableCollection<Anime> Anime { get; } = new ObservableCollection<Anime>();

		public ICommand CloseAll { get; }
		public ICommand CopyANNID { get; }

		[DataMember]
		public string? Directory
		{
			get => _Directory;
			set => this.RaiseAndSetIfChanged(ref _Directory, value);
		}

		public string DirectoryButtonText
		{
			get => _DirectoryButtonText;
			set => this.RaiseAndSetIfChanged(ref _DirectoryButtonText, value);
		}

		public ICommand EditSong { get; }
		public ICommand ExpandAll { get; }
		public ICommand ExportFixes { get; }
		public IScreen HostScreen => _HostScreen ?? Locator.Current.GetService<IScreen>();
		public ICommand Load { get; }
		public ICommand ProcessSongs { get; }
		public ICommand RemoveSong { get; }
		public string UrlPathSegment => "/songs";

		public SongViewModel(IScreen? screen = null)
		{
			_HostScreen = screen;
			_Loader = Locator.Current.GetService<SongLoader>();
			_Processor = Locator.Current.GetService<ISongProcessor>();
			CopyANNID = ReactiveCommand.CreateFromTask<int>(async id =>
			{
				await Avalonia.Application.Current.Clipboard.SetTextAsync(id.ToString()).CAF();
			});

			var canLoad = this
				.WhenAnyValue(x => x.Directory)
				.Select(System.IO.Directory.Exists);
			Load = ReactiveCommand.CreateFromTask(async () =>
			{
				var shouldAttemptToLoad = !Anime.Any();
				Anime.Clear();

				if (shouldAttemptToLoad)
				{
					await foreach (var anime in _Loader.LoadAsync(Directory!))
					{
						Anime.Add(anime);
					}
				}

				DirectoryButtonText = Anime.Any() ? UNLOAD : LOAD;
			}, canLoad);

			AddSong = ReactiveCommand.Create<Anime>(anime =>
			{
				var song = new Song();
				anime.Songs.Add(song);

				var vm = new EditViewModel(song);
				HostScreen.Router.Navigate.Execute(vm);
			});

			EditSong = ReactiveCommand.Create<Song>(song =>
			{
				var vm = new EditViewModel(song);
				HostScreen.Router.Navigate.Execute(vm);
			});

			RemoveSong = ReactiveCommand.Create<Song>(song =>
			{
				var anime = song.Anime;
				anime.Songs.Remove(song);
			});

			var hasChildren = this
				.WhenAnyValue(x => x.Anime.Count)
				.Select(x => x > 0);
			ExpandAll = ReactiveCommand.Create<TreeView>(tree =>
			{
				foreach (TreeViewItem item in tree.GetLogicalChildren())
				{
					item.IsExpanded = true;
				}
			}, hasChildren);

			CloseAll = ReactiveCommand.Create<TreeView>(tree =>
			{
				foreach (TreeViewItem item in tree.GetLogicalChildren())
				{
					item.IsExpanded = false;
				}
			}, hasChildren);

			ExportFixes = ReactiveCommand.CreateFromTask(async () =>
			{
				await _Processor.ExportFixesAsync(Directory!, Anime).CAF();
			}, hasChildren);

			ProcessSongs = ReactiveCommand.CreateFromTask(async () =>
			{
				await _Processor.ProcessAsync(Anime).CAF();
			}, hasChildren);
		}
	}
}