//********************************************************************************************
//Author: Sergey Stoyan, CliverSoft.com
//        http://cliversoft.com
//        stoyan@cliversoft.com
//        sergey.stoyan@gmail.com
//********************************************************************************************

using System;
using System.Linq;
using System.Net;
using System.Text;
using System.IO;
using System.Threading;
using System.Text.RegularExpressions;
using System.Collections.Generic;
using Cliver;
using System.Configuration;
using System.Net.Sockets;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;
using System.Security.Cryptography;
using System.Security.Authentication;

namespace Cliver.CisteraScreenCaptureService
{
    public class TcpServerConnection : IDisposable
    {
        public TcpServerConnection(Socket socket)
        {
            this.socket = socket ?? throw new Exception("socket is null");
            stream = new NetworkStream(socket);

            Log.Main.Inform("Starting connection from " + RemoteIp + ":" + RemotePort);

            receiving_thread = ThreadRoutines.StartTry(
                run,
                (Exception e) =>
                {
                    lock (this)
                    {
                        if (this.socket == null)//disposed
                            return;
                        if (!IsAlive)
                            Log.Main.Inform("Connection from " + RemoteIp + ":" + RemotePort + " has been terminated.");
                        else
                            Log.Main.Error(e);
                    }
                },
                () =>
                {
                    MpegStream.Stop();
                    ThreadRoutines.StartTry(Dispose);
                }
                );

            //sending_thread = ThreadRoutines.StartTry(
            //    run,
            //    (Exception e) =>
            //    {
            //        lock (this)
            //        {
            //            if (this.socket == null)//disposed
            //                return;
            //            if (!IsAlive)
            //                Log.Main.Inform("Connection from " + RemoteIp + ":" + RemotePort + " has been terminated.");
            //            else
            //                Log.Main.Error(e);
            //        }
            //    },
            //    () =>
            //    {
            //        ThreadRoutines.StartTry(Dispose);
            //    }
            //    );
        }
        Socket socket = null;
        Stream stream = null;
        Thread receiving_thread = null;
        //Thread sending_thread = null;

        ~TcpServerConnection()
        {
            Dispose();
        }

        public void Dispose()
        {
            lock (this)
            {
                if (disposed)
                    return;

                Log.Main.Inform("Closing connection from " + RemoteIp + ":" + RemotePort);

                if (socket != null)
                {
                    try
                    {
                        Log.Main.Trace("Shutdown...");
                        socket.Shutdown(SocketShutdown.Both);
                    }
                    catch (System.Net.Sockets.SocketException e)
                    {
                        if(e.SocketErrorCode != SocketError.ConnectionReset)
                            Log.Main.Error(e);
                    }
                    catch (Exception e)
                    {
                        Log.Main.Error(e);
                    }
                    //Log.Main.Trace("Disconnect...");
                    //socket.Disconnect(false);//!!!blocks for timeout when the other side is disconnected improperly
                    Log.Main.Trace("Close...");
                    socket.Close();
                    Log.Main.Trace("Dispose...");
                    socket.Dispose();//!!!disposed socket does not allow to get RemoteIp etc in a error handler that might be invoked 
                    socket = null;
                }
                if (stream != null)
                {
                    stream.Dispose();
                    stream = null;
                }
                if (receiving_thread != null)
                {
                    receiving_thread.Abort();
                    receiving_thread = null;
                }
                disposed = true;
            }
        }
        bool disposed = false;

        public bool IsAlive
        {
            get
            {
                lock (this)
                {
                    if (socket == null)
                        return false;

                    return socket.IsConnectionAlive();
                }
            }
        }

        public IPAddress LocalIp
        {
            get
            {
                lock (this)
                {
                    try
                    {
                        return ((IPEndPoint)socket.LocalEndPoint).Address;
                    }
                    catch(Exception e)
                    {
                        Log.Main.Warning(e);
                        return null;
                    }
                }
            }
        }

        public ushort LocalPort
        {
            get
            {
                lock (this)
                {
                    try
                    {
                        return (ushort)((IPEndPoint)socket.LocalEndPoint).Port;
                    }
                    catch (Exception e)
                    {
                        Log.Main.Warning(e);
                        return 0;
                    }
                }
            }
        }

        public IPAddress RemoteIp
        {
            get
            {
                lock (this)
                {
                    try
                    {
                        return ((IPEndPoint)socket.RemoteEndPoint).Address;
                    }
                    catch (Exception e)
                    {
                        Log.Main.Warning(e);
                        return null;
                    }
                }
            }
        }

        public ushort RemotePort
        {
            get
            {
                lock (this)
                {
                    try
                    {
                        return (ushort)((IPEndPoint)socket.RemoteEndPoint).Port;
                    }
                    catch (Exception e)
                    {
                        Log.Main.Warning(e);
                        return 0;
                    }
                }
            }
        }

        void run()
        {
            while (receiving_thread != null)
            {
                TcpMessage m = TcpMessage.Receive(stream);

                Log.Main.Inform("Tcp message received: " + m.Name + "\r\n" + m.BodyAsText);
                UiApi.Message(MessageType.INFORM, "Tcp message received: " + m.Name + "\r\n" + m.BodyAsText);

                string reply = TcpMessage.Success;
                try
                {
                    switch (m.Name)
                    {
                        case TcpMessage.FfmpegStart:
                            MpegStream.Start(Service.UserSessionId, m.BodyAsText);
                            break;
                        case TcpMessage.FfmpegStop:
                            MpegStream.Stop();
                            break;
                        case TcpMessage.SslStart:
                            if (stream is SslStream)
                                throw new Exception("SSL is already started.");
                            break;
                        case TcpMessage.Poll:
                            if (errors.Count > 0)
                            {
                                reply = string.Join("\r\n", errors);
                                errors.Clear();
                            }                            
                            break;
                        default:
                            throw new Exception("Unknown message: " + m.Name);
                    }
                }
                catch (Exception e)
                {
                    reply = e.Message;
                    Log.Main.Error("Tcp message processing: ", e);
                }
                Log.Main.Inform("Tcp message sending: " + m.Name + "\r\n" + reply);
                m.Reply(stream, reply);
                if (m.Name == TcpMessage.SslStart && reply == TcpMessage.Success)
                    startSsl();
            }
        }

        void startSsl()
        {
            SslStream sstream = new SslStream(stream, false, remoteCertificateValidationCallback);
            stream = sstream;
            //sstream.AuthenticateAsClient("", null, SslProtocols.Tls12, false);
            //sstream.AuthenticateAsClient(server, null, SslProtocols.Default, false);
            sstream.AuthenticateAsClient("");
        }

        bool remoteCertificateValidationCallback(object sender, X509Certificate certificate, X509Chain chain, SslPolicyErrors sslPolicyErrors)
        {
            return true;
        }

        public void AddError(string error)
        {
            errors.Add(error);
        }
        readonly List<string> errors = new List<string>();
    }
}