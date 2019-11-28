using System;
using System.Text;
using System.Threading;
using System.Net;
using System.Net.Sockets;
using System.Globalization;
using System.IO;

namespace JaguarMover
{
    public class JaguarComm
    {
        private StreamReader readerReply = null;
        private StreamWriter writer = null;
        private NetworkStream replyStream = null;

        private Timer pingTimer;

//        private static byte[] recBuffer;

        private Thread receiveThread = null;


        private static Socket clientSocket;
        private static int localPort = 0;

        private static string lastRecvErrorMsg = String.Empty;
        private static int lastRecvError = 0;
        private static string lastSendErrorMsg = String.Empty;
        private static int socketErrorCount = 0;


        private string processStr = "";
        private string recStr = "";

        //        private string comID = "MOT1";    TODO remove This?
        private JagController _controller = null;

        /// <summary>
        /// </summary>
        /// <param name="control"> used to parse received data from Jaguar</param>
        public JaguarComm(JagController control)
        {
            this._controller= control;
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
                catch (Exception e)
                {
                    lastSendErrorMsg = e.Message;
                    return false;
                }

                IPEndPoint remoteEP = new IPEndPoint(ipAddress, remotePort);

                // Create a TCP socket

                clientSocket = new Socket(AddressFamily.InterNetwork,
                    SocketType.Stream, ProtocolType.Tcp);


                // Connect to the remote endpoint.
                // We will wait for this to complete rather than do it
                // asynchronously

                receiveThread = new Thread((DataReceive));
                //                receiveThread.CurrentCulture = new CultureInfo("en-US"); TODO REMOVE?

                clientSocket.Connect(remoteEP);
                localPort = ((IPEndPoint)clientSocket.LocalEndPoint).Port;


                //Start pinging(so that robot responds to all commands
                pingTimer = new Timer(Ping, null, TimeSpan.FromSeconds(0.2), TimeSpan.FromSeconds(0.3));

                receiveThread.Start();

                return true;
            }
            catch (SocketException e)
            {
                socketErrorCount++;
                lastRecvError = e.ErrorCode;
                lastRecvErrorMsg = e.ToString();
                return false;
            }
        }


        /// <summary>
        /// to decode receive package here
        /// </summary>
        public void DataReceive()
        {
            if (readerReply != null)
                readerReply.Close();
            if (replyStream != null)
                readerReply.Close();

            replyStream = new NetworkStream(clientSocket);
            readerReply = new StreamReader(replyStream);


            //receive data here

            byte[] recs = new byte[4095];
            int scount = 0;

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

                        if(recStr.Length > 0)
                        {
                            _controller.Parse(recStr);

                        }
                    }
                }
                catch
                {
                    //need to handle some error here
                }
            }
        }



        /// </summary>
        /// <param name="cmd"></param>
        /// <returns></returns>
        internal bool SendCommand(string Cmd)
        {
            /// <summary>
            Cmd += "\r\n";
            byte[] cmdData = ASCIIEncoding.UTF8.GetBytes(Cmd);
            {
                try
                {
                    if (clientSocket.Send(cmdData) != cmdData.Length)
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
                writer?.Close();
            }
            catch
            {
            }

            clientSocket.Close();


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