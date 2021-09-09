﻿using SongProcessor.Models;

namespace SongProcessor.Warnings
{
	public sealed class VideoIsNull : IWarning
	{
		public IAnime Anime { get; }

		public VideoIsNull(IAnime anime)
		{
			Anime = anime;
		}

		public override string ToString()
			=> $"Video info is null: {Anime.Name}";
	}
}