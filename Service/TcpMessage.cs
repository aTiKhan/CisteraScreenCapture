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

namespace Cliver.CisteraScreenCaptureService
{
    public class TcpMessage
    {
        public const string FfmpegStart = "FfmpegStart";
        public const string FfmpegStop = "FfmpegStop";
        public const string SslStart = "SslStart";
        public const string Success = "OK";
        public const string Poll = "Poll";

        public readonly UInt16 Size;
        public string Name
        {
            get
            {
                for (int i = 0; i < NameBodyAsBytes.Length; i++)
                    if (NameBodyAsBytes[i] == '\0')
                        return System.Text.Encoding.ASCII.GetString(NameBodyAsBytes, 0, i);
                return null;
            }
        }
        public string BodyAsText
        {
            get
            {
                for (int i = 0; i < NameBodyAsBytes.Length; i++)
                    if (NameBodyAsBytes[i] == '\0')
                    {
                        i++;
                        int c = 0;
                        for (; i + c < NameBodyAsBytes.Length; c++)
                            if (NameBodyAsBytes[i + c] == '\0')
                                return System.Text.Encoding.ASCII.GetString(NameBodyAsBytes, i, c);
                        return System.Text.Encoding.ASCII.GetString(NameBodyAsBytes, i, c);
                    }
                return null;
            }
        }
        public byte[] BodyAsBytes
        {
            get
            {
                for (int i = 2; i < NameBodyAsBytes.Length; i++)
                    if (NameBodyAsBytes[i] == '\0')
                    {
                        i++;
                        byte[] body = new byte[NameBodyAsBytes.Length - i];
                        if (i < NameBodyAsBytes.Length)
                            NameBodyAsBytes.CopyTo(body, i);
                        return body;
                    }
                return null;
            }
        }
        public readonly byte[] NameBodyAsBytes;

        public TcpMessage(byte[] name_body_as_bytes)
        {
            if (name_body_as_bytes.Length > UInt16.MaxValue)
                throw new Exception("NameBodyAsBytes.Length > UInt16.MaxValue :\r\n\r\n" + Encoding.ASCII.GetString(name_body_as_bytes));
            Size = (UInt16)name_body_as_bytes.Length;
            NameBodyAsBytes = name_body_as_bytes;
        }

        public TcpMessage(string name, string body)
        {
            if (body == null)
                body = "";
            NameBodyAsBytes = new byte[name.Length + 1 + body.Length + 1];
            if (NameBodyAsBytes.Length > UInt16.MaxValue)
                throw new Exception("NameBodyAsBytes.Length > UInt16.MaxValue :\r\n\r\n" + name + "\r\n\r\n" + body);
            Size = (UInt16)(NameBodyAsBytes.Length);
            int i = 0;
            Encoding.ASCII.GetBytes(name).CopyTo(NameBodyAsBytes, i);
            i += name.Length + 1;
            Encoding.ASCII.GetBytes(body).CopyTo(NameBodyAsBytes, i);
        }        
    }
}