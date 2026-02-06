using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SqlToDataverseSync.Services
{
    public class SyncStateService
    {
        private const string FileName = "syncversion.txt";

        public long GetLastVersion()
        {
            if (!File.Exists(FileName))
                return 0;

            return long.Parse(File.ReadAllText(FileName));
        }

        public void SaveVersion(long version)
        {
            File.WriteAllText(FileName, version.ToString());
        }
    }
}
