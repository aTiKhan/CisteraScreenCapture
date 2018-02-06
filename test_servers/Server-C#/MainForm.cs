using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading;
using System.Windows.Forms;
using System.Net.Sockets;
using System.Net;
using Cliver.CisteraScreenCapture;
//using Mono.Zeroconf;
//using Bonjour;
//using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using System.IO;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;
using System.Security.Cryptography;
using System.Security.Authentication;
using Cliver.CisteraScreenCaptureService;


namespace Cliver.CisteraScreenCaptureTestServer
{
    public partial class MainForm : Form
    {
        public MainForm()
        {
            InitializeComponent();

            mpegCommandLine.Text = "-f gdigrab -framerate 10 -f rtp_mpegts -srtp_out_suite AES_CM_128_HMAC_SHA1_80 -srtp_out_params aMg7BqN047lFN72szkezmPyN1qSMilYCXbqP/sCt srtp://127.0.0.1:5920";
            state.Text = "";

            CreateHandle();
            startEnabled = false;
            stopEnabled = false;

            ThreadRoutines.StartTry(() => { run_http_service(); });
            stateText = "Wating for HTTP request...";

            //Bonjour.Start("test", "_cisterascreencapturecontroller._tcp", null, null, 123);
            
            FormClosed += delegate
              {
                  try
                  {
                      listener.Stop();
                  }
                  catch
                  {

                  }
              };
        }
        Socket socket;

        void run_http_service()
        {
            try
            {
                if (listener != null)
                {
                    listener.Stop();
                    listener.Close();
                }
                listener = new HttpListener();
                listener.Prefixes.Add("http://*:80/");
                listener.Start();
                while (listener.IsListening)
                {
                    HttpListenerContext context = listener.GetContext();
                    ThreadPool.QueueUserWorkItem((o) => { request_handler(context); });
                }
            }
            catch (ThreadAbortException)
            { }
            catch (Exception e)
            {
                Message.Error(e);
            }
            finally
            {
                if (listener != null)
                {
                    listener.Stop();
                    listener.Close();
                    listener = null;
                }
            }
        }
        static HttpListener listener = null;

        private void request_handler(object _context)
        {
            HttpListenerContext context = (HttpListenerContext)_context;
            string responseString;
            string username = null;
            HttpListenerRequest request = context.Request;
            try
            {
                Match m = Regex.Match(request.Url.Query, @"username=(.+?)(&|$)");
                if (!m.Success)
                    throw new Exception("No username in http request.");
                username = m.Groups[1].Value;

                m = Regex.Match(request.Url.Query, @"ipaddress=(.+?)(&|$)");
                if (!m.Success)
                    throw new Exception("No ipaddress in http request.");
                remoteHost = m.Groups[1].Value;

                m = Regex.Match(request.Url.Query, @"port=(.+?)(&|$)");
                if (!m.Success)
                    throw new Exception("No port in http request.");
                remotePort = m.Groups[1].Value;

                responseString = "OK";
            }
            catch (Exception e)
            {
                responseString = Message.GetExceptionDetails(e);
            }

            byte[] buffer = System.Text.Encoding.UTF8.GetBytes(responseString);
            HttpListenerResponse response = context.Response;
            response.ContentLength64 = buffer.Length;
            System.IO.Stream output = response.OutputStream;
            output.Write(buffer, 0, buffer.Length);
            output.Close();

            List<string> ss = new List<string>();
            ss.Add("Received HTTP request: " + request.Url);
            ss.Add("username: " + username);
            ss.Add("remoteHost: " + remoteHost);
            ss.Add("remotePort: " + remotePort);
            ss.Add("Sent HTTP response: " + responseString);
            //Message.Inform(string.Join("\r\n", ss));

            stateText = string.Join("\r\n", ss);
            startEnabled = true;
        }
        string remoteHost;
        string remotePort;

        void connect_socket()
        {
            if (socket != null)
            {
                if (socket.IsConnectionAlive())
                    return;
                try
                {
                    socket.Close();
                }
                finally
                {
                    socket = null;
                }
                if (stream != null)
                    stream.Close();
            }

            IPAddress ipAddress = NetworkRoutines.GetLocalIpForDestination(IPAddress.Parse("127.0.0.1"));
            //IPEndPoint localEndPoint = new IPEndPoint(ipAddress, int.Parse(localTcpPort.Text));
            socket = new Socket(ipAddress.AddressFamily, SocketType.Stream, ProtocolType.Tcp);

            //socket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.DontLinger, true);
            //socket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
            socket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.KeepAlive, true);
            //_Socket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReceiveTimeout, 500)
            //_Socket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.SendTimeout, Timeout)
            //socket.Bind(localEndPoint);

            socket.Connect(remoteHost, int.Parse(remotePort));
            stream = new NetworkStream(socket);

            ThreadRoutines.StartTry(() => {
                while(socket!=null)
                {
                    Thread.Sleep(5000);
                    poll();
                }
            });
        }

        TcpMessage sendAndReceiveReply(Stream stream, TcpMessage message)
        {
            lock (stream)
            {
                byte[] sizeAsBytes = BitConverter.GetBytes(message.Size);
                stream.Write(sizeAsBytes, 0, sizeAsBytes.Length);
                //throw new Exception("Could not send to stream the required count of bytes: " + sizeAsBytes.Length);
                stream.Write(message.NameBodyAsBytes, 0, message.NameBodyAsBytes.Length);
                //throw new Exception("Could not send to stream the required count of bytes: " + NameBodyAsBytes.Length);

                byte[] message_size_buffer = new byte[2];
                if (stream.Read(message_size_buffer, 0, message_size_buffer.Length) < message_size_buffer.Length)
                    throw new Exception("Could not read from stream the required count of bytes: " + message_size_buffer.Length);
                UInt16 message_size = BitConverter.ToUInt16(message_size_buffer, 0);
                byte[] message_buffer = new byte[message_size];
                if (stream.Read(message_buffer, 0, message_buffer.Length) < message_buffer.Length)
                    throw new Exception("Could not read from stream the required count of bytes: " + message_buffer.Length);
                return new TcpMessage(message_buffer);
            }
        }

        private void poll()
        {
            try
            {
                stateText = "sending poll...";

                TcpMessage m = new TcpMessage(TcpMessage.Poll, null);
                TcpMessage m2 = sendAndReceiveReply(stream, m);

                //Message.Inform("Response: " + m2.BodyAsText);
                stateText = "polled: " + m2.BodyAsText;
            }
            catch (Exception ex)
            {
                Message.Error(ex);
            }
            finally
            {
            }
        }

        //void connect_socket2()
        //{
        //    IPAddress ipAddress = NetworkRoutines.GetLocalIpForDestination(IPAddress.Parse("127.0.0.1"));
        //    IPEndPoint localEndPoint = new IPEndPoint(ipAddress, int.Parse(localTcpPort.Text) + port_i);
        //    port_i++;
        //    socket = new Socket(ipAddress.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
        //    socket.Bind(localEndPoint);
        //    socket.Connect(remoteHost, int.Parse(remotePort));
        //    if (stream != null)
        //        stream.Close();
        //    stream = new NetworkStream(socket);
        //}
        //int port_i = 0;

        //void disconnect_socket()
        //{
        //    return;
        //    try
        //    {
        //        socket.Shutdown(SocketShutdown.Both);
        //    }
        //    catch { }
        //    try
        //    {
        //        socket.Disconnect(true);
        //    }
        //    catch { }
        //    //try
        //    //{
        //    //    socket.Close();
        //    //}
        //    //finally
        //    //{
        //    //    socket = null;
        //    //}
        //    //if (stream != null)
        //    //{
        //    //    stream.Dispose();
        //    //    stream = null;
        //    //}
        //}

        private void start_Click(object sender, EventArgs e)
        {
            try
            {
                stateText = "Starting MPEG...";

                connect_socket();
                TcpMessage m = new TcpMessage(TcpMessage.FfmpegStart, mpegCommandLine.Text);
                TcpMessage m2 = sendAndReceiveReply(stream, m);

                //Message.Inform("Response: " + m2.BodyAsText);
                stateText = "MPEG started";
                startEnabled = false;
                stopEnabled = true;
            }
            catch (Exception ex)
            {
                Message.Error(ex);
            }
            finally
            {
            }
        }

        Stream stream = null;

        private void stop_Click(object sender, EventArgs e)
        {
            try
            {
                stateText = "Stopping MPEG...";

                connect_socket();
                TcpMessage m = new TcpMessage(TcpMessage.FfmpegStop, null);
                TcpMessage m2 = sendAndReceiveReply(stream, m);

                //Message.Inform("Response: " + m2.BodyAsText);
                stateText = "MPEG stopped";
                startEnabled = true;
                stopEnabled = false;
            }
            catch (Exception ex)
            {
                Message.Error(ex);
            }
            finally
            {
            }
        }

        string stateText
        {
            set
            {
                state.BeginInvoke(() =>
                {
                    state.Text += DateTime.Now.ToString("yy-MM-dd hh:mm:ss") + " " + value + "\r\n";
                });
            }
        }

        bool startEnabled
        {
            set
            {
                start.BeginInvoke(() =>
                {
                    start.Enabled = value;
                });
            }
        }

        bool stopEnabled
        {
            set
            {
                stop.BeginInvoke(() =>
                {
                    stop.Enabled = value;
                });
            }
        }

        private void bSsl_Click(object sender, EventArgs e)
        {
            try
            {
                stateText = "Starting SSL...";

                connect_socket();
                TcpMessage m = new TcpMessage(TcpMessage.SslStart, null);
                TcpMessage m2 = sendAndReceiveReply(stream, m);
                if (m2.BodyAsText.Trim() != TcpMessage.Success)
                    throw new Exception(m2.BodyAsText);

                //Message.Inform("Response: " + m2.BodyAsText);
                stateText = "Starting SSL-2...";

                SslStream sstream = new SslStream(stream, false);
                stream = sstream;

                byte[] certificateBuffer = SslRoutines.GetBytesFromPEM(File.ReadAllText("server_certificate.pem"), SslRoutines.PemStringType.Certificate);
                X509Certificate2 certificate = new X509Certificate2(certificateBuffer);
                byte[] keyBuffer = SslRoutines.GetBytesFromPEM(File.ReadAllText("server_key.pem"), SslRoutines.PemStringType.PrivateKey);
                certificate.PrivateKey = SslRoutines.CreateRsaProviderFromPrivateKey(keyBuffer);

                sstream.AuthenticateAsServer(certificate);

                bSsl.Enabled = false;
            }
            catch (Exception ex)
            {
                Message.Error(ex);
            }
            finally
            {
            }
        }
    }
}
