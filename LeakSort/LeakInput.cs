using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;

namespace LeakSort
{
    class LeakInput
    {
        private StreamReader streamReader;
        private readonly LeakSaver leakSaver;

        public string BasePath { get; }

        public LeakInput(string path, LeakSaver leakSaver)
        {
            BasePath = path;
            this.leakSaver = leakSaver;
            streamReader = new StreamReader(new FileStream(BasePath,
                FileMode.Open, FileAccess.Read, FileShare.None,
                bufferSize: 4096, useAsync: true));
        }

        public LeakInput(string path, LeakSaver leakSaver, LeakReader lr) : this(path, leakSaver)
        {
            //hack to get around no use of async await
            Task t = Task.Run(async () =>
            {
                //do a 'binary search' in the stream
                long lowerBound = 0;
                long upperBound = streamReader.BaseStream.Length;
                const long minDist = 5 * 1000;//start looking line by line at 5kb
                while (upperBound - lowerBound > minDist)
                {
                    long middle = (lowerBound + upperBound) / 2;

                    // print a nice graphic
                    const long glen = 100;
                    long maxLen = streamReader.BaseStream.Length;
                    long gLpos = (glen * lowerBound) / maxLen;
                    long gMpos = (glen * middle) / maxLen;
                    long gUpos = (glen * upperBound) / maxLen;
                    char[] gdata = new string(' ', (int)glen + 2).ToCharArray();
                    gdata[0] = '[';
                    gdata[gdata.Length - 1] = ']';
                    if (gLpos != gMpos && gMpos != gUpos)
                    {
                        gdata[gLpos + 1] = '<';
                        gdata[gMpos + 1] = '.';
                        gdata[gUpos + 1] = '>';
                        Console.WriteLine(new string(gdata));
                    }




                    streamReader.BaseStream.Position = middle;
                    streamReader.DiscardBufferedData();
                    // read partial line
                    await streamReader.ReadLineAsync();
                    string line = await streamReader.ReadLineAsync();
                    if (await lr.HasResult(line))
                    {
                        lowerBound = middle - 1000; // offset because partial lines
                    }
                    else
                    {
                        upperBound = middle + 1000; // offset because partial lines
                    }
                }
                streamReader.BaseStream.Position = lowerBound;
                streamReader.DiscardBufferedData();
                await streamReader.ReadLineAsync();

                while (true)
                {
                    string line = await streamReader.ReadLineAsync();
                    if (!(await lr.HasResult(line)))
                    {
                        await leakSaver.SaveLine(line);
                        break;
                    }
                }
                Console.WriteLine("Skipped aprox. {0}MB", streamReader.BaseStream.Position / (1000 * 1000));
            });
            t.Wait();
        }

        public async Task<InputProgress> SortAllAsync(CancellationToken cancellationToken)
        {
            while (!cancellationToken.IsCancellationRequested && !streamReader.EndOfStream)
            {
                string line = streamReader.ReadLine();
                await leakSaver.SaveLine(line, cancellationToken);
            }
            return new InputProgress(streamReader.BaseStream);
        }

        public long GetPosition() => streamReader.BaseStream.Position;
        public long GetLength() => streamReader.BaseStream.Length;
    }
}
