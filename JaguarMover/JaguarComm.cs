using System;
using System.Text;
using System.Threading;
using System.Net;
using System.Net.Sockets;
using System.IO;

namespace JaguarMover
{
    public class JaguarComm
    {
        private StreamReader readerReply;
        private NetworkStream replyStream ;
        private Timer pingTimer;
        private Thread receiveThread;
        private static Socket _clientSocket;
        private readonly JagController controller;

        private string recStr = "";


        /// <summary>
        /// Handles sending and receiving
        /// </summary>
        /// <param name="control"> used to parse received data from Jaguar</param>
        public JaguarComm(JagController control)
        {
            this.controller = control;
        }


        //here is the WiFi connecting start
        internal bool StartClient(string addr, int portNum)
        {
            // Connect to remote server

            try
            {
                // Establish the remote endpoint for the socket
                // GetHostEntry was an attempt to do a hostname lookup
                // so that you did not have to type an IP address.
                // However, it takes a LONG time for an IP address
                // (around 20 seconds).
                //                IPHostEntry ipHostInfo = Dns.GetHostEntry(addr);
                //                IPAddress ipAddress = ipHostInfo.AddressList[0];
                IPAddress ipAddress;
                int remotePort = portNum;
                try
                {
                    ipAddress = IPAddress.Parse(addr);
                }
                catch (Exception)
                {
                    return false;
                }

                IPEndPoint remoteEp = new IPEndPoint(ipAddress, remotePort);

                // Create a TCP socket

                _clientSocket = new Socket(AddressFamily.InterNetwork,
                    SocketType.Stream, ProtocolType.Tcp);


                // Connect to the remote endpoint.
                // We will wait for this to complete rather than do it
                // asynchronously

                receiveThread = new Thread((DataReceive));
                //                receiveThread.CurrentCulture = new CultureInfo("en-US"); TODO REMOVE?

                _clientSocket.Connect(remoteEp);

                receiveThread.Start();

                //Start pinging(so that robot responds to all commands
                pingTimer = new Timer(Ping, "Some state", TimeSpan.FromSeconds(0.2), TimeSpan.FromSeconds(0.2));

                return true;
            }
            catch (SocketException)
            {
                return false;
            }
        }


        /// <summary>
        /// to decode receive package here
        /// </summary>
        public void DataReceive()
        {
            readerReply?.Close();
            if (replyStream != null)
                readerReply?.Close();

            replyStream = new NetworkStream(_clientSocket);
            readerReply = new StreamReader(replyStream);


            //receive data here

            while (true) //keep running
            {
                try
                {
                    if (!replyStream.DataAvailable)
                    {
                        Thread.Sleep(5);
                    }
                    else
                    {
                        recStr = readerReply.ReadLine();

                        if (recStr?.Length > 0)
                        {
                            controller.Parse(recStr);
                        }
                    }
                }
                catch
                {
                    //need to handle some error here
                }
            }
        }


        /// <summary>Sends a command to Jaguar</summary>
        /// <param name="cmd"></param>
        /// <returns>success or fail</returns>
        internal bool SendCommand(string cmd)
        {
            cmd += "\r\n";
            byte[] cmdData = Encoding.UTF8.GetBytes(cmd);  //if sending fails this was:  ASCIIEncoding.UTF8.GetBytes(Cmd);
            {
                try
                {
                    if (_clientSocket.Send(cmdData) != cmdData.Length)
                    {
                        return false;
                    }
                }
                catch
                {
                    return false;
                }

                return true;
            }
        }

        public void Ping(Object state)
        {
            SendCommand("PING");
        }

        /// <summary>
        /// Close the connection to a serial port.
        /// </summary>
        public void Close()
        {
            pingTimer.Dispose();

            try
            {
                readerReply?.Close(); // same as if !=null
            }
            catch
            {
                // ignored
            }

            _clientSocket.Close();

            //TODO find alternative
            if (receiveThread != null)
            {
                if (receiveThread.IsAlive)
                {
                    receiveThread.Abort(); //terminate the receive thread
                }
            }
        }
    }
}