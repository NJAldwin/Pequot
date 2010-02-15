using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Diagnostics;   

namespace Pequot
{
    class PequotServer
    {
        //off, error, warning, info, verbose
        public static TraceSwitch verbosity = new TraceSwitch("Verbosity", "Verbosity of program");
        //default port = 80
        public static int Port = 80;
        public static DirectoryInfo Dir;
        private DirectoryInfo serverDirectory;
        public static string phpLocation;
        public static string[] phpFiles;
        public static string ipToListenAt = "";

        public const string kDefaultSettings = "PequotConfig.xml";

        static void Main(string[] args)
        {
            SetupTrace();
            //verbosity level detection code here
            verbosity.Level = TraceLevel.Info;   //default is Info
            //todo: eventually put log file detection code in too

            PequotServer server = new PequotServer();
            Trace.WriteLine("Pequot Server " + server.GetType().Assembly.GetName().Version.ToString(3));
            server.LoadProperties();

            TcpListener listener = null;
            bool listening = true;
            //is this the best way to get the address?
            //TODO:fix this
            IPAddress host = IPAddress.None;
            if (ipToListenAt.Length <= 0 || !IPAddress.TryParse(ipToListenAt, out host))
            {
                foreach (IPAddress ip in Dns.GetHostAddresses(Dns.GetHostName()))
                {
                    host = ip;
                    if (ip.AddressFamily == AddressFamily.InterNetwork)
                        break;
                }
            }
            listener = new TcpListener(host, Port);
            listener.Start();

            if (listening)
                Trace.WriteLine("Server listening at IP " + host.ToString() + " on port " + Port);

            while (listening)
            {
                //spawn new thread when we get a connection
                new ServerThread(listener.AcceptTcpClient()).Run();
            }
            listener.Stop();
        }

        private static void SetupTrace()
        {
            Trace.AutoFlush = true;
            TextWriterTraceListener consoleListener = new TextWriterTraceListener(Console.Out);
            Trace.Listeners.Add(consoleListener);
            //TODO: implement log file:
            //TextWriterTraceListener fileListener = new TextWriterTraceListener(fileName);
            //Trace.Listeners.Add(fileListener);
        }

        private void LoadProperties()
        {
            //defaults
            serverDirectory = new DirectoryInfo(Directory.GetCurrentDirectory());
            phpLocation = "";//@"C:\Program Files\PHP\php-cgi.exe";   //empty string = no php
            //default location from install = "C:\Program Files\PHP\php-cgi.exe"

            AppSettings.Load(kDefaultSettings);
            Dir = new DirectoryInfo(AppSettings.Get("Directory", Path.Combine(serverDirectory.FullName, "files")));
            Port = int.Parse(AppSettings.Get("Port", "80"));
            phpLocation = AppSettings.Get("PHP Location", "");
            phpFiles = AppSettings.Get("PHP File Extensions", ".php").Split(',');
            ipToListenAt = AppSettings.Get("IP To Listen At", "");
            SaveProperties();

            if (!Dir.Exists)
                Dir.Create();

        }

        private void SaveProperties()
        {
            AppSettings.Set("Directory", Dir.FullName);
            AppSettings.Set("Port", Port.ToString());
            AppSettings.Set("PHP Location", phpLocation);
            AppSettings.Set("PHP File Extensions", String.Join(",", phpFiles));
            if(ipToListenAt.Length>0)
                AppSettings.Set("IP To Listen At", ipToListenAt);
            AppSettings.Save();
        }
    }
}
