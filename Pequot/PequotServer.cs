using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Net;
using System.Net.Sockets;

namespace Pequot
{
    class PequotServer
    {
        static int verbosityLevel = 1;
        private int port;
        private string directory;
        private string serverDirectory;
	
        static void Main(string[] args)
        {
            //verbosity level detection code here
            PequotServer server = new PequotServer();
            verbosityLevel = 2;//detection
            WriteLineVerbose("Pequot Server", 1);
            server.LoadProperties();

            TcpListener listener = null;
            bool listening = true;
            //check for existence of directory here
            listener = new TcpListener(server.port);
            listener.Start();

            if (listening)
                WriteLineVerbose("Server listening on port " + server.port, 1);

            while (listening)
            {
                //spawning of threads goes here
                //(if that's how it's going to work)
            }
            listener.Stop();
        }

        static void WriteLineVerbose(string s, int vLevel)
        {
            if (vLevel >= verbosityLevel)
                Console.WriteLine(s);
        }

        private void LoadProperties()
        {
            serverDirectory = Directory.GetCurrentDirectory();
            directory = Path.Combine(serverDirectory, "files");

            port = 89;
                //todo: implement
            
        }
    }
}
