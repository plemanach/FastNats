using FastNats.Client;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace FastNats.ClientTests
{
    class UnitTestUtilities
    {
        static UnitTestUtilities()
        {
            CleanupExistingServers();
        }

        internal static string GetConfigDir()
        {
#if NET45
            var codeBaseUrl = new Uri(Assembly.GetExecutingAssembly().CodeBase);
            var codeBasePath = Uri.UnescapeDataString(codeBaseUrl.AbsolutePath);
            var runningDirectory = Path.GetDirectoryName(codeBasePath);
#else
            var runningDirectory = AppContext.BaseDirectory +
                string.Format("{0}..{0}..{0}..{0}",
                Path.DirectorySeparatorChar);
#endif
            return Path.Combine(runningDirectory, "config");
        }

        public Options DefaultTestOptions
        {
            get
            {
                var opts = ConnectionFactory.GetDefaultOptions();
                opts.Timeout = 10000;
                return opts;
            }
        }

        public IConnection DefaultTestConnection
        {
            get
            {
                return new ConnectionFactory().CreateConnection(DefaultTestOptions);
            }
        }

        internal NATSServer CreateServerOnPort(int p)
        {
            return new NATSServer(p);
        }

        internal NATSServer CreateServerWithConfig(string configFile)
        {
            return new NATSServer(" -config " + configFile);
        }

        internal NATSServer CreateServerWithArgs(string args)
        {
            return new NATSServer(" " + args);
        }

        internal static String GetFullCertificatePath(string certificateName)
        {
            return GetConfigDir() + "\\certs\\" + certificateName;
        }

        internal static void CleanupExistingServers()
        {
            Process[] procs = Process.GetProcessesByName("gnatsd");
            if (procs == null)
                return;

            foreach (Process proc in procs)
            {
                try
                {
                    proc.Kill();
                }
                catch (Exception) { } // ignore
            }

            // Let the OS cleanup.
            for (int i = 0; i < 10; i++)
            {
                procs = Process.GetProcessesByName("gnatsd");
                if (procs == null || procs.Length == 0)
                    break;

                Thread.Sleep(i * 250);
            }

            Thread.Sleep(250);
        }
    }
}
