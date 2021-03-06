﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace SpeedTest
{
    class Program
    {
        static void Main(string[] args)
        {
            int kb = 1024;
            int mb = 1024 * 1024;

            int fileSize = mb * 40;

            int[] packetSizes = new int[]
            {
                4096,
                4096,
                2048,
                2048,
                1024,
                1024,
                512,
                512,
                
            };

            foreach (int packetSize in packetSizes)
            {
                test(packetSize * kb, fileSize);
            }

            Console.WriteLine("!");
            Console.ReadLine();

        }

        static void test(int packetSize, int fileSize)
        {
            byte[] buffer = new byte[packetSize];
            Socket socket = new Socket(SocketType.Stream, ProtocolType.Tcp);

            socket.ReceiveBufferSize = packetSize;
            socket.ReceiveTimeout = 60000;

            // 15sec did not time out direct to modem
            //socket.ReceiveTimeout = 15000;
            socket.Connect("vps.sonyar.info", 1337);

            int totalBytes = 0;
            var w = Stopwatch.StartNew();
            int count = 0;

            double mb;
            double throughput;
            string text;

            try
            {
                while (true)
                {

                    int bytes = socket.Receive(buffer);
                    totalBytes += bytes;

                    //string recvText = (bytes / 1024.0).ToString("0.00") + " KB Received: " + (totalBytes / 1024.0 / 1024.0).ToString("0.00") + " MB Total, Packet: " + (packetSize / 1024.0) + "\r\n";
                    //Console.Write(recvText);
                    //File.AppendAllText("receive-buffer-debug.txt", recvText);

                    ++count;

                    if (totalBytes >= fileSize)
                    {
                        Console.WriteLine("File size reached, closing...");
                        socket.Close();
                        break;
                    }

                    if (bytes == 0)
                    {
                        Console.WriteLine("0 bytes received, closing...");
                        socket.Close();
                        break;
                    }

                    if (count % 1000 == 0)
                    {
                        mb = totalBytes / 1024.0 / 1024.0;
                        throughput = mb / w.Elapsed.TotalSeconds * 8;
                        text = w.Elapsed + ": " + mb + " MB: " + throughput + " Mb/s \r\n";
                        Console.Write(text);
                    }
                }

                WriteResult("COMPLETED", totalBytes, packetSize, w);
            }
            catch (Exception e)
            {
                if (e.Message.Contains("did not properly respond after a period of time"))
                {
                    WriteResult("TIMEOUT", totalBytes, packetSize, w);
                }
                else
                {
                    WriteResult("ERROR", totalBytes, packetSize, w);
                    Console.WriteLine(e.Message);
                }
                
                socket.Close();
                
            }
         
        }

        static void WriteResult(string result, int totalBytes, int packetSize, Stopwatch w)
        {
            double mb = totalBytes / 1024.0 / 1024.0;
            double throughput = mb / w.Elapsed.TotalSeconds * 8;
            string text = DateTime.Now + ", Result: " + result + ", File: " + mb.ToString("0") + " MB, Packet: " + 
                packetSize / 1024.0 + " KB, Speed: " + throughput.ToString("0.0") + " Mb/s \r\n";

            File.AppendAllText("results.txt", text);
            Console.WriteLine(text);
        }
    }
}
