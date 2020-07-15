﻿using System.Threading.Tasks;

using AMQSongProcessor.Models;

namespace AMQSongProcessor
{
	public interface ISongLoader
	{
		string Extension { get; set; }

		Task<Anime?> LoadAsync(string file);

		Task SaveAsync(Anime anime, SaveNewOptions? options = null);
	}
}