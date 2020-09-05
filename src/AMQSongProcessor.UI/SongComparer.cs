﻿using System;
using System.Collections.Generic;

using AMQSongProcessor.Models;
using AMQSongProcessor.Utils;

namespace AMQSongProcessor.UI
{
	public sealed class SongComparer : Comparer<ISong>
	{
		public override int Compare(ISong? x, ISong? y)
		{
			if (x != null)
			{
				if (y != null)
				{
					return CompareNonNull(x, y);
				}
				return 1;
			}
			return y != null ? -1 : 0;
		}

		private int CompareNonNull(ISong x, ISong y)
		{
			// Every song has a type, so we can safely always sort by that
			var type = x.Type.CompareTo(y.Type);
			if (type != 0)
			{
				return type;
			}

			// Not every song has an episode, so we can't always sort by episode
			if (x.Episode.HasValue && y.Episode.HasValue)
			{
				var episode = x.Episode.Value.CompareTo(y.Episode.Value);
				if (episode != 0)
				{
					return episode;
				}
			}

			// Not every song has a timestamp yet, so we can't always sort by start
			if (x.HasTimeStamp() && y.HasTimeStamp())
			{
				var start = x.Start.CompareTo(y.Start);
				if (start != 0)
				{
					return start;
				}
			}

			// We don't want the list to be in alphabetical order instead of semi-chronological order
			//return string.Compare(x.GetFullName(), y.GetFullName());
			return 0;
		}
	}
}