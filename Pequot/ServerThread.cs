using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Net.Sockets;
using System.IO;
using System.Text.RegularExpressions;
using System.Diagnostics;
using System.Net;

namespace Pequot
{
    class ServerThread : IDisposable
    {
        private Thread internalThread;

        private TcpClient client = null;
        private NetworkStream stream = null;
        private DirectoryInfo fileDirectory;	//the directory in which the files are located
        private string fileString = "";
        private string[,] argsArray;
        private FileInfo requestedFile;
        private string requestType = "";
        private string host = "";
        private string userAgent = "";
        private HttpStatusCode statusCode = HttpStatusCode.Unused;
        private StreamReader reader = null;
        private StreamWriter writer = null;
        private bool usingPHP = false;
        private bool isPHP = false;
        private bool isDirectory = false;

        /*private String fileDirectory = "";	//the directory in which the files are located
        private String fileString = "";		//the requested file (String form)
        private String[,] argsArray;		//a 2-dimensional array containing the arguments and their values
        private File requestedFile = null;	//the requested file (File form)
        private String requestType = "";	//the type of request (e.g. GET or HEAD or POST)
        private String host = "";			//the host according to the client
        private String userAgent = "";		//the client user agent
        private int statusCode = 0;			//the status code - 0 means it has not yet been set
        private Socket clientSocket = null;	//the socket through which we communicate with the client
        private DataOutputStream dout = null;//the output to the client
        private BufferedReader rin = null;	//the input from the client*/

        public ServerThread(TcpClient client)
        {
            this.client = client;
            fileDirectory = PequotServer.Dir;

            if (internalThread == null)
                internalThread = new Thread(new ThreadStart(Work));
        }

        public void Run()
        {
            if (internalThread.IsAlive == false)
                internalThread.Start();
        }

        public void Work()
        {
            stream = client.GetStream();
            reader = new StreamReader(stream);
            writer = new StreamWriter(stream);
            writer.AutoFlush = true;

            Trace.WriteLineIf(PequotServer.verbosity.TraceInfo, "New Client Connected: " + client.Client.RemoteEndPoint.ToString());

            string inputLine = reader.ReadLine();
            ParseRequest(inputLine);

            Trace.WriteLineIf(PequotServer.verbosity.TraceVerbose, "Client headers:");
            //read the client headers
            while (!((inputLine = reader.ReadLine()).Equals(null)))
            {
                //store the host header
                if (inputLine.StartsWith("Host:"))
                    host = Regex.Replace(inputLine, @"Host:\s", "");
                //store the user-agent header
                if (inputLine.StartsWith("User-Agent:"))
                    userAgent = Regex.Replace(inputLine, @"User-Agent:\s", "");
                //if there's a blank line, the client is done telling us its headers
                if (inputLine.Equals(""))
                    break;
                Trace.WriteLineIf(PequotServer.verbosity.TraceVerbose, inputLine);
            }

            //output our response line
            OutputResponseLine();

            //output headers
            OutputHeaders();

            //output file contents
            OutputFileContents();

            stream.Close();
            client.Close();
            writer.Dispose();
            reader.Dispose();
            Quit();
        }

        private void OutputFileContents()
        {
            //if the status is 200 OK, give the file to the client
            if (statusCode == HttpStatusCode.OK)
            {
                if (!isDirectory)
                {
                    if (usingPHP)
                    {
                        Process php = new Process();
                        php.StartInfo = new ProcessStartInfo(PequotServer.phpLocation, "\"" + requestedFile.FullName + "\"");
                        php.StartInfo.UseShellExecute = false;
                        php.StartInfo.RedirectStandardOutput = true;
                        php.Start();

                        //gah, waiting for php to complete takes so long! ~1.5sec on my machine

                        //we have to do binary because php isn't always text data
                        byte[] buffer = new byte[131072];
                        int read;
                        while ((read = php.StandardOutput.BaseStream.Read(buffer, 0, buffer.Length)) > 0)
                            stream.Write(buffer, 0, read);
                    }
                    else
                    {
                        //write the data
                        byte[] barray = File.ReadAllBytes(requestedFile.FullName);
                        stream.Write(barray, 0, barray.Length);
                    }
                }
                else
                {

                    //directory listing
                    //TODO: make this work better!
                    writer.WriteLine("<html><head><title>Files in " + fileString + "</title></head><body>");
                    writer.WriteLine("<h1>Files in " + fileString + "</h1>");

                    writer.WriteLine("<ul>");
                    if (new DirectoryInfo(requestedFile.FullName).FullName != PequotServer.Dir.FullName)
                        writer.WriteLine("<li><a href=\"../\">Parent Directory</a></li>");
                    string[] dirs = Directory.GetDirectories(requestedFile.FullName);
                    foreach (string dir in dirs)
                    {
                        string name = new DirectoryInfo(dir).Name;
                        writer.WriteLine("<li><a href=\"" + name + "/\">" + name + "/</a></li>");
                    }
                    string[] files = Directory.GetFiles(requestedFile.FullName);
                    foreach (string file in files)
                    {
                        string name = new FileInfo(file).Name;
                        writer.WriteLine("<li><a href=\"" + name + "\">" + name + "</a></li>");
                    }
                    writer.WriteLine("</ul>");

                    writer.WriteLine("<hr>");
                    writer.WriteLine("<address>Pequot/" + this.GetType().Assembly.GetName().Version.ToString(3) + " Port " + PequotServer.Port + "</address>");

                    writer.WriteLine("</body></html>");
                }
            }
            else
            {
                //the status is not 200 OK, so create a dummy page informing the user about the error
                writer.WriteLine("<html><head><title>" + StatusCode(statusCode) + "</title></head><body>");
                writer.WriteLine("<h1>" + StatusCode(statusCode) + "</h1>");

                //if it's a 404 error, tell the client what we could not find.
                if (statusCode == HttpStatusCode.NotFound)
                    writer.WriteLine("<p>Could not find file " + fileString + "</p>");

                writer.WriteLine("<hr>");
                writer.WriteLine("<address>Pequot/" + this.GetType().Assembly.GetName().Version.ToString(3) + " Port " + PequotServer.Port + "</address>");

                writer.WriteLine("</body></html>");
            }
        }

        private string StatusCode(HttpStatusCode statCode)
        {
            switch (statCode)
            {
                case HttpStatusCode.OK:
                    return "200 OK";
                case HttpStatusCode.NotFound:
                    return "404 Not Found";
                case HttpStatusCode.MovedPermanently:
                    return "301 Moved Permanently";
                case HttpStatusCode.Redirect:
                    return "302 Moved Temporarily";
                /* HTTP/1.1 only and we're currently using HTTP/1.0
                case 303:
                    return "303 See Other";
                */
                case HttpStatusCode.NotImplemented:
                    return "501 Not Implemented";
                case HttpStatusCode.InternalServerError:
                default:
                    //if it's some other value not yet added here, our server has an error
                    return "500 Server Error";
            }
        }

        private void OutputHeaders()
        {
            //identify ourselves
            writer.WriteLine("Server: Pequot/" + this.GetType().Assembly.GetName().Version.ToString(3));
            //the connection will be closed, not keep-alive
            writer.WriteLine("Connection: close");

            if (statusCode == HttpStatusCode.OK && !isDirectory)
            {
                //if the status is OK, then we are planning on outputting our file
                //so we have to tell the client what type of content it is.

                //php outputs its own headers
                if (!usingPHP)
                {
                    string cType = GetMimeType(requestedFile.FullName);
                    writer.WriteLine("Content-Type: " + cType);
                    Trace.WriteLineIf(PequotServer.verbosity.TraceVerbose, "Content-Type: " + cType);
                    writer.WriteLine("Content-Length: " + requestedFile.Length);
                }
                else
                {
                    Trace.WriteLineIf(PequotServer.verbosity.TraceVerbose, "PHP Requested");
                }

            }
            else if (statusCode == HttpStatusCode.MovedPermanently)
            {
                //if the status is Permanently Moved, we have to tell the client
                //where to go

                //the dummy page itself will be html
                writer.WriteLine("Content-Type: text/html");
                Trace.WriteLineIf(PequotServer.verbosity.TraceVerbose, "Content-Type: text/html");

                //tell the client where to go
                writer.WriteLine("Location: http://" + host + fileString);
                Trace.WriteLineIf(PequotServer.verbosity.TraceVerbose, "Location: http://" + host + fileString);
            }
            else
            {
                //if the status is some other error, the dummy page
                //will be in html
                writer.WriteLine("Content-Type: text/html");
                Trace.WriteLineIf(PequotServer.verbosity.TraceVerbose, "Content-Type: text/html");
            }
            //end of headers signified by blank line
            //PHP can write its own headers, though.
            if(!usingPHP)
                writer.WriteLine();
        }

        private string GetMimeType(string fileName)
        {
            string mimeType = "application/unknown";
            string ext = System.IO.Path.GetExtension(fileName).ToLower();
            Microsoft.Win32.RegistryKey regKey = Microsoft.Win32.Registry.ClassesRoot.OpenSubKey(ext);
            if (regKey != null && regKey.GetValue("Content Type") != null)
                mimeType = regKey.GetValue("Content Type").ToString();
            return mimeType;
        }

        private void OutputResponseLine()
        {
            //here's the response line
            writer.WriteLine("HTTP/1.0 " + StatusCode(statusCode));
            //writer.Flush();

            Trace.WriteLineIf(PequotServer.verbosity.TraceVerbose, "Server response:");
            Trace.WriteLineIf(PequotServer.verbosity.TraceVerbose, "Status code: " + StatusCode(statusCode));
        }

        private void ParseRequest(string requestString)
        {
            requestType = Regex.Match(requestString, @"^(.+?)\b").Value;
            fileString = Regex.Match(requestString, @"/(.*?)(?=\s)").Value;
            string argumentsString = Regex.Match(requestString, @"(?<=\?)(.*?) HTTP/\d\.\d$").Groups[1].Value;
            fileString = Regex.Replace(fileString, @"(\?.+$)", "");

            Trace.WriteLineIf(PequotServer.verbosity.TraceInfo, "Client has requested: " + fileString);

            if (!argumentsString.Equals(""))
            {
                //split each argument/value pair
                string[] rawArgsArray = argumentsString.Split('&');
                //split the pairs and put them into the 2-dimensional array
                argsArray = new string[rawArgsArray.Length, 2];
                for (int i = 0; i < rawArgsArray.Length; i++)
                {
                    string[] temparray = rawArgsArray[i].Split('=');
                    argsArray[i, 0] = temparray[0];
                    argsArray[i, 1] = temparray[1];
                }
                Trace.WriteLineIf(PequotServer.verbosity.TraceInfo, "Arguments:");
                int len = argsArray.GetLength(0);
                for (int i = 0; i < len; i++)
                    //"the argument,the value"
                    Trace.WriteLineIf(PequotServer.verbosity.TraceInfo, argsArray[i, 0] + "," + argsArray[i, 1]);
            }
            else
            {
                Trace.WriteLineIf(PequotServer.verbosity.TraceInfo, "No arguments.");
            }

            //TODO: better detection code for directory requests
            //check to see if client has not requested a file (no .xxxx)
            if (!Regex.IsMatch(fileString, @"\.(.*?)$"))
            {
                //check if user has requested directory but left off trailing slash (e.g. /site)
                if (!Regex.IsMatch(fileString, @"/$"))
                {
                    //if so, add slash to file string, then tell browser to redirect user to new address
                    fileString += "/";
                    statusCode = HttpStatusCode.MovedPermanently;
                }
                else
                {
                    //this is temporary.
                    //if they're just requesting a directory, give them the index file
                    fileString += "index.html";
                    //todo:make this configurable
                    isDirectory = true;
                }
            }

            //put together a file from the directory and the file string 
            requestedFile = new FileInfo(Path.Combine(fileDirectory.FullName, Uri.UnescapeDataString(fileString).TrimStart('/').Replace("/", "\\")));

            // check to see if the user is attempting to access levels above the
            // directory with the files
            // e.g. if the directory is C:\files\ and the user is attempting to
            // access C:\something.bat (by requesting \..\something.bat)
            if (!requestedFile.FullName.StartsWith(fileDirectory.FullName))
            {

                //if so, return 404 and set the requested file to null (just in case)
                statusCode = HttpStatusCode.NotFound;
                requestedFile = null;
            }

            //determine whether or not we're using php
            if (requestedFile != null)
            {
                isPHP = PequotServer.phpFiles.Contains(requestedFile.Extension.ToLower());
                usingPHP = (PequotServer.phpLocation != "" && isPHP);
            }

            //if it's a php file but php is not installed
            if (isPHP && !usingPHP)
            {
                statusCode = HttpStatusCode.NotImplemented;
                requestedFile = null;
            }

            //check if the status code has not already been set
            if (statusCode == HttpStatusCode.Unused)
            {
                //the code hasn't already been set
                if (!requestedFile.Exists)
                {

                    if (!isDirectory)
                    {
                        //the file doesn't exist so 404 Not Found
                        statusCode = HttpStatusCode.NotFound;
                    }
                    else
                    {
                        fileString = fileString.Substring(0, fileString.Length - "index.html".Length);
                        requestedFile = new FileInfo(Path.Combine(fileDirectory.FullName, Uri.UnescapeDataString(fileString).TrimStart('/').Replace("/", "\\")));
                        if (!new DirectoryInfo(requestedFile.FullName).Exists)
                            statusCode = HttpStatusCode.NotFound;
                        else
                            statusCode = HttpStatusCode.OK;
                    }
                }
                else
                {
                    //if the requested file exists
                    //the code is 200 OK
                    statusCode = HttpStatusCode.OK;
                    isDirectory = false;
                }
            }
        }

        public void Quit()
        {
            Cleanup();
        }

        private void Cleanup()
        {
            internalThread.Join(0);
            internalThread = null;
        }

        #region IDisposable Members

        public void Dispose()
        {
            Cleanup();
        }

        #endregion
    }
}
