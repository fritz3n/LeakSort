using Nito.AsyncEx;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;

namespace LeakSort
{
    public class LeakSaver : IDisposable
    {
        private const string legalCharacters = "abcdefghijklmnopqrstuvwxyz0123456789";

        private readonly Dictionary<string, StreamWriterWrapper> fileStreams = new Dictionary<string, StreamWriterWrapper>();
        private readonly HashSet<string> directories = new HashSet<string>();
        private readonly ConcurrentQueue<string> linequeue = new ConcurrentQueue<string>();
        public Task writerTask;
        private readonly int layers;
        private readonly string basePath;
        private readonly CancellationTokenSource cts = new CancellationTokenSource();
        private readonly AsyncAutoResetEvent addedEvent = new AsyncAutoResetEvent(false);

        public LeakSaver(int layers, string basePath)
        {
            this.layers = layers;
            this.basePath = basePath;

            writerTask = Task.Run(WriteLoop);
        }

        private async Task WriteLoop()
        {
            try
            {
                ulong cycle = 0;
                while (true)
                {
                    if (linequeue.IsEmpty)
                    {
                        await addedEvent.WaitAsync(cts.Token);
                        cts.Token.ThrowIfCancellationRequested();
                    }
                    cycle++;
                    if (linequeue.TryDequeue(out string line))
                    {
                        string directory = GetDirectory(basePath, line, layers); //describes the dir in which the file belongs
                        string path = GetFilePath(basePath, line, layers);

                        if (fileStreams.TryGetValue(path, out StreamWriterWrapper streamWriterW))
                        {
                            await streamWriterW.streamWriter.WriteLineAsync(line);
                        }
                        else
                        {
                            CreateDirectory(directory);

                            StreamWriterWrapper sww = new StreamWriterWrapper(
                                new StreamWriter(new FileStream(path, FileMode.Append, FileAccess.Write, FileShare.None, bufferSize: 4096, useAsync: true)),
                                cycle);

                            fileStreams.Add(path, sww);
                            await sww.streamWriter.WriteLineAsync(line);
                        }

                        //prune the fileStreams if we have to many
                        if (fileStreams.Count > 5000)
                        {
                            KeyValuePair<string, StreamWriterWrapper>[] ar = fileStreams.ToArray();

                            Array.Sort(ar, Compare);

                            await AsyncParallelForEach(ar.Take(750), async (kv) =>
                            {
                                await kv.Value.streamWriter.FlushAsync();
                                await kv.Value.streamWriter.DisposeAsync();
                                lock (fileStreams)
                                {
                                    fileStreams.Remove(kv.Key);
                                }
                            });
                        }
                    }
                }
            }
            catch (OperationCanceledException) { }
            catch (Exception e) { Debugger.Break(); throw e; }
        }

        public async Task SaveLine(string line)
        {
            await SaveLine(line, new CancellationTokenSource().Token);
        }

        public async Task SaveLine(string line, CancellationToken cancellationToken)
        {
            if (linequeue.Count > 10000)
            {
                while (linequeue.Count > 5000)
                {
                    try
                    {
                        await Task.Delay(10, cancellationToken);
                    }
                    catch(TaskCanceledException) { return; }
                }
            }
            linequeue.Enqueue(line);
            addedEvent.Set();
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
        public static string GetDirectory(string basePath, string line, int layers) => GetPathOfComponents(basePath, line, layers - 1);
        public static string GetFilePath(string basePath, string line, int layers) => GetPathOfComponents(basePath, line, layers) + ".txt";


        public void Dispose()
        {
            cts.Cancel();
            writerTask.Wait();
            foreach (KeyValuePair<string, StreamWriterWrapper> kv in fileStreams)
            {
                kv.Value.streamWriter.Flush();
                kv.Value.streamWriter.Dispose();
            }
            GC.SuppressFinalize(this);
        }

        public async Task DisposeAsync()
        {
            cts.Cancel();
            await writerTask;
            foreach (KeyValuePair<string, StreamWriterWrapper> kv in fileStreams)
            {
                await kv.Value.streamWriter.FlushAsync();
                await kv.Value.streamWriter.DisposeAsync();
            }
            GC.SuppressFinalize(this);
        }

        internal sealed class StreamWriterWrapper
        {
            public ulong LastUsed { get; set; }
            public readonly StreamWriter streamWriter;

            public StreamWriterWrapper(StreamWriter sw, ulong cycle) : this(sw)
            {
                LastUsed = cycle;
            }
            public StreamWriterWrapper(StreamWriter sw)
            {
                this.streamWriter = sw;
            }
        }
        internal static int Compare(KeyValuePair<string, StreamWriterWrapper> x, KeyValuePair<string, StreamWriterWrapper> y)
        {
            return x.Value.LastUsed.CompareTo(y.Value.LastUsed);
        }

        public static Task AsyncParallelForEach<T>(IEnumerable<T> source, Func<T, Task> body, int maxDegreeOfParallelism = DataflowBlockOptions.Unbounded, TaskScheduler scheduler = null)
        {
            ExecutionDataflowBlockOptions options = new ExecutionDataflowBlockOptions
            {
                MaxDegreeOfParallelism = maxDegreeOfParallelism
            };
            if (scheduler != null)
                options.TaskScheduler = scheduler;

            ActionBlock<T> block = new ActionBlock<T>(body, options);

            foreach (T item in source)
                block.Post(item);

            block.Complete();
            return block.Completion;
        }
    }
}
