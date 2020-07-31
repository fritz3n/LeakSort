using LeakSort;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Importer
{
    class Program
    {
        const string path = @"D:\leaks";
        const int layers = 3;
        static async Task Main(string[] args)
        {

            string lockPath = Path.Combine(path, "lock");
            if (File.Exists(lockPath))
            {
                Console.WriteLine("Leak in use");
                return;
            }
            else
            {
                File.Create(lockPath).Close();
            }

            try
            {
                CancellationTokenSource cts = new CancellationTokenSource();
                Task t = Task.Run(async () =>
                {
                    // Find all .input files in current folder
                    string[] files = Directory.GetFiles(Directory.GetCurrentDirectory(), "*.input");
                    //import them one by one and rename to .done
                    foreach (string file in files)
                    {
                        Console.WriteLine("Importing {0}.", Path.GetFileName(file));
                        using (LeakSaver ls = new LeakSaver(layers, path))
                        using (LeakReader lr = new LeakReader(layers, path))
                        using (LeakInput li = new LeakInput(file, ls, lr))
                        {
                            await li.SortAllAsync(cts.Token);
                        }
                        Console.WriteLine("Finished importing {0}", Path.GetFileName(file));
                        File.Move(file, file + ".done");
                    }
                    Console.WriteLine("Finished");
                });

                Console.ReadLine();
                cts.Cancel();
                Console.WriteLine("Stopping");
                await t;
            }
            finally
            {
                File.Delete(lockPath);
            }
        }
    }
}
