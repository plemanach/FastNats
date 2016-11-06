using FastNats.Client;
using System;
using System.Diagnostics;
using System.Threading;

namespace FastNats.ClientTests
{
    class NATSServer : IDisposable
    {
#if NET45
        static readonly string SERVEREXE = "gnatsd.exe";
#else
        static readonly string SERVEREXE = "gnatsd";
#endif
        // Enable this for additional server debugging info.
        static bool debug = false;
        Process p;
        ProcessStartInfo psInfo;

        internal static bool Debug
        {
            set { debug = value; }
            get { return debug; }
        }

        public NATSServer() : this(true) { }

        public NATSServer(bool verify)
        {
            createProcessStartInfo();
            p = Process.Start(psInfo);
            if (verify)
            {
                for (int i = 0; i < 10; i++)
                {
                    try
                    {
                        var c = new ConnectionFactory().CreateConnection();
                        c.Close();
                        break;
                    }
                    catch
                    {
                        Thread.Sleep(i * 250);
                    }
                }
            }
        }

        private void addArgument(string arg)
        {
            if (psInfo.Arguments == null)
            {
                psInfo.Arguments = arg;
            }
            else
            {
                string args = psInfo.Arguments;
                args += arg;
                psInfo.Arguments = args;
            }
        }

        public NATSServer(int port)
        {
            createProcessStartInfo();
            addArgument("-p " + port);

            p = Process.Start(psInfo);
            Thread.Sleep(500);
        }

        public NATSServer(string args)
        {
            createProcessStartInfo();
            addArgument(args);
            p = Process.Start(psInfo);
            Thread.Sleep(500);
        }

        private void createProcessStartInfo()
        {
            psInfo = new ProcessStartInfo(SERVEREXE);

            if (debug)
            {
                psInfo.Arguments = " -DV ";
            }
            else
            {
#if NET45
                psInfo.WindowStyle = ProcessWindowStyle.Hidden;
#else
                psInfo.CreateNoWindow = true;
#endif
            }

            psInfo.WorkingDirectory = UnitTestUtilities.GetConfigDir();
        }

        public void Bounce(int millisDown)
        {
            Shutdown();
            Thread.Sleep(millisDown);
            p = Process.Start(psInfo);
            Thread.Sleep(500);
        }

        public void Shutdown()
        {
            if (p == null)
                return;

            try
            {
                p.Kill();
            }
            catch (Exception) { }

            p = null;
        }

        void IDisposable.Dispose()
        {
            Shutdown();
        }
    }
}
