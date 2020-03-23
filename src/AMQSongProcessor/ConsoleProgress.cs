﻿using System;

namespace AMQSongProcessor
{
	public sealed class ConsoleProgress : IProgress<string>
	{
		public void Report(string value)
			=> Console.WriteLine(value);
	}
}