﻿using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;

using AdvorangesUtils;

namespace AMQSongProcessor.Utils
{
	[Flags]
	public enum OutputMode : uint
	{
		Sync = 0,
		Async = 1U << 0,
	}

	public readonly struct Program
	{
		public string Path { get; }

		public Program(string path)
		{
			Path = path;
		}

		public Process CreateProcess(string args)
			=> ProcessUtils.CreateProcess(Path, args);
	}

	public static class ProcessUtils
	{
		private static readonly bool IsWindows =
			Environment.OSVersion.Platform.ToString().CaseInsContains("win");
		private static readonly IReadOnlyList<Environment.SpecialFolder> SpecialFolders =
			GetValues<Environment.SpecialFolder>();
		public static Program FFmpeg { get; } = FindProgram("ffmpeg");
		public static Program FFprobe { get; } = FindProgram("ffprobe");

		public static Process CreateProcess(string program, string args)
		{
			return new()
			{
				StartInfo = new()
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

		public static Task<int> RunAsync(this Process process, OutputMode mode)
		{
			var tcs = new TaskCompletionSource<int>();

			process.EnableRaisingEvents = true;
			process.WithCleanUp(null, c => tcs.SetResult(c));

			var started = process.Start();
			if (!started)
			{
				throw new InvalidOperationException("Could not start process: " + process);
			}

			if ((mode & OutputMode.Async) != 0)
			{
				process.BeginOutputReadLine();
				process.BeginErrorReadLine();
			}

			return tcs.Task;
		}

		public static Process WithCleanUp(
			this Process process,
			EventHandler? onCancel,
			Action<int>? onComplete,
			CancellationToken? token = null)
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
				onCancel?.Invoke(sender, args);
			}

			//If the program gets canceled, shut down the process
			Console.CancelKeyPress += Cancel;
			//If the program gets shut down, make sure it also shuts down the process
			AppDomain.CurrentDomain.ProcessExit += Cancel;
			//If an unhandled exception occurs, also attempt to shut down the process
			AppDomain.CurrentDomain.UnhandledException += Cancel;
			//Same if a cancellation token is canceled
			var registration = token?.Register(() => Cancel(token, EventArgs.Empty));
			process.Exited += (s, e) =>
			{
				Console.CancelKeyPress -= Cancel;
				AppDomain.CurrentDomain.ProcessExit -= Cancel;
				AppDomain.CurrentDomain.UnhandledException -= Cancel;
				registration?.Dispose();
				onComplete?.Invoke(process.ExitCode);
			};
			return process;
		}

		private static Program FindProgram(string program)
		{
			program = IsWindows ? program + ".exe" : program;
			//Look through every directory and any subfolders they have called bin
			foreach (var dir in GetDirectories(program))
			{
				if (TryGetProgram(dir, program, out var path))
				{
					return new Program(path);
				}
				else if (TryGetProgram(Path.Combine(dir, "bin"), program, out path))
				{
					return new Program(path);
				}
			}
			throw new InvalidOperationException($"Unable to find {program}.");
		}

		private static IEnumerable<string> GetDirectories(string program)
		{
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
			foreach (var folder in SpecialFolders)
			{
				yield return Path.Combine(Environment.GetFolderPath(folder), program);
			}
		}

		private static IReadOnlyList<T> GetValues<T>() where T : Enum
		{
			var uncast = Enum.GetValues(typeof(T));
			var cast = new T[uncast.Length];
			for (var i = 0; i < uncast.Length; ++i)
			{
				cast[i] = (T)uncast.GetValue(i)!;
			}
			return cast;
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