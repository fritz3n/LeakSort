using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace LeakSort
{
	class LeakSaver : IDisposable
	{
		private const string legalCharacters = "abcdefghijklmnopqrstuvwxyz0123456789";

		private Dictionary<string, LeakStreamWriter> fileStreams = new Dictionary<string, LeakStreamWriter>();
		private HashSet<string> directories = new HashSet<string>();
		private int writeCount;
		private readonly int layers;
		private readonly string basePath;

		public int WriteCount { get => writeCount; private set => writeCount = value; }

		public LeakSaver(int layers, string basePath)
		{
			this.layers = layers;
			this.basePath = basePath;
		}

		public async Task SaveLine(string line)
		{
			Interlocked.Increment(ref writeCount);
			string directory = GetDirectory(line, layers); //describes the dir in which the file belongs
			string path = GetFilePath(line, layers);

			if (fileStreams.TryGetValue(path, out LeakStreamWriter outStream))
			{
				await outStream.WriteLine(line, WriteCount);
				return;
			}
			else
			{
				CreateDirectory(directory);

				LeakStreamWriter stream = new LeakStreamWriter(Path.Combine(basePath, path));
				fileStreams.Add(path, stream);
				stream.Open();
				await stream.WriteLine(line, WriteCount);
			}
		}

		private void CreateDirectory(string directory)
		{
			if (directories.Contains(directory))
				return;
			string[] subPaths = directory.Split(Path.AltDirectorySeparatorChar,
							  Path.DirectorySeparatorChar);

			string curPath = string.Empty;

			for (int i = 0; i < subPaths.Count(); i++)
			{
				curPath = Path.Combine(curPath, subPaths[i]);
				if (!directories.Contains(curPath))
				{
					Directory.CreateDirectory(Path.Combine(basePath, curPath));
					directories.Add(curPath);
				}
			}
		}

		public string GetPathOfComponentsAbsolute(string text, int layers) => Path.Combine(basePath, GetPathOfComponents(text, layers));

		/// <summary>
		/// Computes the Path of a piece of text
		/// </summary>
		/// <param name="text">The text</param>
		/// <param name="layers">The number of layers in the path</param>
		/// <returns>The relative Path of the text</returns>
		public static string GetPathOfComponents(string text, int layers)
		{
			int layerCount = Math.Min(layers, text.Length);

			string[] components = new string[layerCount + 1];
			components[0] = string.Empty;
			for (int i = 0; i < layerCount; i++)
			{
				if (legalCharacters.IndexOf(text[i], StringComparison.InvariantCultureIgnoreCase) != -1)
				{
					components[i + 1] = text[i].ToString().ToLowerInvariant();
				}
				else
				{
					components[i + 1] = "symbols";
				}
			}

			return Path.Combine(components);
		}

		public static string GetPathOfComponents(string basePath, string text, int layers) => Path.Combine(basePath, GetPathOfComponents(text, layers));
		public static string GetDirectory(string line, int layers) => GetPathOfComponents(line, layers - 1);
		public static string GetFilePath(string line, int layers) => GetPathOfComponents(line, layers) + ".txt";
		public static string GetFilePath(string basePath, string line, int layers) => GetPathOfComponents(basePath, line, layers) + ".txt";


		public void Dispose()
		{
			foreach (KeyValuePair<string, LeakStreamWriter> kv in fileStreams)
			{
				kv.Value.Dispose();
			}
			GC.SuppressFinalize(this);
		}

		public async Task DisposeAsync()
		{
			foreach (KeyValuePair<string, LeakStreamWriter> kv in fileStreams)
			{
				await kv.Value.DisposeAsync();
			}
			GC.SuppressFinalize(this);
		}
	}
}
