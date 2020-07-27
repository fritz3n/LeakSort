using System;
using System.Collections.Concurrent;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace LeakSort
{
    class LeakStreamWriter : IDisposable
    {
        public string Path { get; }
        private StreamWriter streamWriter = null;
        private Task Writer;
        private ConcurrentQueue<string> queue;
        public bool exit = false;

        public int LastUsed { get; private set; }
        public const int MaxQueueCount = 1000;

        public LeakStreamWriter(string path)
        {
            Path = path;
            queue = new ConcurrentQueue<string>();
        }

        public void Open()
        {
            streamWriter = new StreamWriter(new FileStream(Path,FileMode.Append, FileAccess.Write, FileShare.None,bufferSize: 4096, useAsync: true));
            streamWriter.AutoFlush = true;
            Writer = new Task(async ()=> 
            {
                while (!exit || queue.Count > 0)
                {
                    //wait for a new line
                    string line;
                    while (!queue.TryDequeue(out line))
                    {
                        await Task.Delay(10);
                    }
                    await streamWriter.WriteLineAsync(line);
                }
            });
            Writer.Start();
        }

        public async Task WriteLine(string line, int index)
        {
            if(queue.Count >= MaxQueueCount)
            {
                await Task.Delay(100);
            }
            queue.Enqueue(line);
        }

        public void Dispose()
        {
            exit = true;
            Writer.Wait();
            Thread.Sleep(5);
            streamWriter.Flush();
            streamWriter.Dispose();
            GC.SuppressFinalize(this);
        }

        public async Task DisposeAsync()
        {
            exit = true;
            await Writer;
            await streamWriter.FlushAsync();
            await streamWriter.DisposeAsync();
            GC.SuppressFinalize(this);
        }
    }
}
