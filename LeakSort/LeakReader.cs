using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace LeakSort
{
    internal class LeakReader
    {
        const string legalCharacters = "abcdefghijklmnopqrstuvwxyz0123456789";

        private int layers;
        private string basePath;

        public LeakReader(int layers, string basePath)
        {
            this.layers = layers;
            this.basePath = basePath;
        }

        public async Task<IEnumerable<string>> Lookup(string data)
        {
            string path = LeakSaver.GetFilePath(basePath, data, layers);
            LinkedList<string> items = new LinkedList<string>();
            using (StreamReader sr = new StreamReader(new FileStream(path, FileMode.Open, FileAccess.Read)))
            {
                string line = await sr.ReadLineAsync();
                if (line.StartsWith(data))
                {
                    items.AddLast(line);
                }
            }
            return items;
        }
    }
}