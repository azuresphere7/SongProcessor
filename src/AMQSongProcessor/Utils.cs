﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

using AdvorangesUtils;

namespace AMQSongProcessor
{
	public static class Utils
	{
		private static readonly bool IsWindows = Environment.OSVersion.Platform.ToString().CaseInsContains("win");
		public static string FFmpeg { get; } = FindProgram("ffmpeg");
		public static string FFprobe { get; } = FindProgram("ffprobe");

		public static Process CreateProcess(string program, string args)
		{
			return new Process
			{
				StartInfo = new ProcessStartInfo
				{
					FileName = program,
					Arguments = args,
					UseShellExecute = false,
					CreateNoWindow = true,
					RedirectStandardOutput = true,
					RedirectStandardError = true,
				},
				EnableRaisingEvents = true,
			};
		}

		public static Task<int> RunAsync(this Process process, bool write)
		{
			var tcs = new TaskCompletionSource<int>();

			process.EnableRaisingEvents = true;
			process.WithCleanUp((s, e) => { }, c => tcs.SetResult(c));

			if (write)
			{
				process.OutputDataReceived += (s, e) => Console.WriteLine(e.Data);
				process.ErrorDataReceived += (s, e) => Console.WriteLine(e.Data);
			}

			var started = process.Start();
			if (!started)
			{
				throw new InvalidOperationException("Could not start process: " + process);
			}

			process.BeginOutputReadLine();
			process.BeginErrorReadLine();

			return tcs.Task;
		}

		public static T[] ToArray<T>(this IEnumerable<T> source, int count)
		{
			if (source == null)
			{
				throw new ArgumentNullException(nameof(source));
			}
			if (count < 0)
			{
				throw new ArgumentOutOfRangeException(nameof(count));
			}

			var array = new T[count];
			var i = 0;
			foreach (var item in source)
			{
				array[i++] = item;
			}
			return array;
		}

		public static async Task<List<T>> ToListAsync<T>(this IAsyncEnumerable<T> enumerable)
		{
			var list = new List<T>();
			await foreach (var value in enumerable)
			{
				list.Add(value);
			}
			return list;
		}

		public static T ToObject<T>(this JsonElement element, JsonSerializerOptions? options = null)
		{
			var json = element.GetRawText();
			return JsonSerializer.Deserialize<T>(json, options);
		}

		public static T ToObject<T>(this JsonDocument document, JsonSerializerOptions? options = null)
		{
			if (document == null)
			{
				throw new ArgumentNullException(nameof(document));
			}
			return document.RootElement.ToObject<T>(options);
		}

		public static void WithCleanUp(this Process process, EventHandler cancel, Action<int> finish, CancellationToken? token = null)
		{
			if (!process.EnableRaisingEvents)
			{
				throw new ArgumentException("Must be able to raise events.", nameof(process));
			}

			var isCanceled = false;
			void Cancel(object? sender, EventArgs args)
			{
				if (isCanceled)
				{
					return;
				}

				isCanceled = true;
				cancel(sender, args);
			}

			//If the program gets shut down, make sure it also shuts down the ffmpeg process
			AppDomain.CurrentDomain.ProcessExit += Cancel;
			var registration = token?.Register(() => Cancel(null, EventArgs.Empty));
			process.Exited += (s, e) =>
			{
				AppDomain.CurrentDomain.ProcessExit -= Cancel;
				registration?.Dispose();
				finish(process.ExitCode);
			};
		}

		private static string FindProgram(string program)
		{
			program = IsWindows ? program + ".exe" : program;
			//Look through every directory and any subfolders they have called bin
			foreach (var dir in GetDirectories(program))
			{
				if (TryGetProgram(dir, program, out var path))
				{
					return path;
				}
				else if (TryGetProgram(Path.Combine(dir, "bin"), program, out path))
				{
					return path;
				}
			}
			throw new InvalidOperationException($"Unable to find {program}.");
		}

		private static IEnumerable<string> GetDirectories(string program)
		{
			static IReadOnlyList<T> GetValues<T>() where T : Enum
			{
				var uncast = Enum.GetValues(typeof(T));
				var cast = new T[uncast.Length];
				for (var i = 0; i < uncast.Length; ++i)
				{
					cast[i] = (T)uncast.GetValue(i)!;
				}
				return cast;
			}

			//Check where the program is stored
			if (Assembly.GetExecutingAssembly().Location is string assembly)
			{
				yield return Path.GetDirectoryName(assembly)!;
			}
			//Check path variables
			if (Environment.GetEnvironmentVariable("PATH") is string path)
			{
				foreach (var part in path.Split(IsWindows ? ';' : ':'))
				{
					yield return part.Trim();
				}
			}
			//Check every special folder
			foreach (var folder in GetValues<Environment.SpecialFolder>())
			{
				yield return Path.Combine(Environment.GetFolderPath(folder), program);
			}
		}

		private static bool TryGetProgram(
			string directory,
			string program,
			[NotNullWhen(true)] out string? path)
		{
			if (!Directory.Exists(directory))
			{
				path = null;
				return false;
			}

			var files = Directory.EnumerateFiles(directory, program, SearchOption.TopDirectoryOnly);
			path = files.FirstOrDefault();
			return path != null;
		}
	}
}