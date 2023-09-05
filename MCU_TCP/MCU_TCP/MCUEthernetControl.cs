using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows.Forms;

namespace MCU_TCP
{
    public enum CommandCode : int
    {
        TransferData = 0, ReadData = 1, TransferDataWithoutSave = 2, Start = 3,Stop = 4
    }
    public class MCUEthernetControl
    {
        public const int TCP_PackageSize = 512;

        private TcpClient m_client;

        public string IP = "192.168.11.1";

        public int nPort = 80;
        public const string VersionInfo = "目前版本V1.3:\n1.包含基本傳輸資料與接收資料的確認與MCU存檔。\n2.根據檔案去對DAC做輸出。";
        #region 介面繪圖
        public Action<string, Color, bool> _DebugInfo = null;

        public void Debug(string msg, Color c)
        {
            _DebugInfo(msg, c, true);
        }
        #endregion

        [DllImport("msvcrt.dll", CallingConvention = CallingConvention.Cdecl)]
        static extern int memcmp(byte[] b1, byte[] b2, long count);

        static bool ByteArrayCompare(byte[] b1, byte[] b2)
        {
            // Validate buffers are the same length.
            // This also ensures that the count does not exceed the length of either buffer.  
            return b1.Length == b2.Length && memcmp(b1, b2, b1.Length) == 0;
        }
        public bool Start()
        {
            Action<string> DebugInfo = (s) => {
                _DebugInfo(s, Color.White, true);
            };
            CommandCode Command = CommandCode.Start;
            try
            {
                byte[] receive = new byte[TCP_PackageSize];
                double TransTime = CalcRunTime(() =>
                {
                    m_client = new TcpClient(IP, nPort);
                    m_client.ReceiveTimeout = 1000;
                    m_client.SendTimeout = 1000;

                    NetworkStream write = m_client.GetStream();

                    double WriteModeTime = CalcRunTime(() =>
                    {
                        int Mode = (int)Command;
                        RequestValue(write, Mode, out Mode);

                        Debug($"Mode={(CommandCode)Mode}\n", Color.Gold);
                    });
                    Debug($"{VersionInfo}\n", Color.Yellow);
                    DebugInfo($"______________________________________________________________________________________\n");
                 
                    DebugInfo($"Write Time: {WriteModeTime} ms");
                    DebugInfo($"\n______________________________________________________________________________________\n");
                    write.Close();
                    m_client.Close();
                });

            }
            catch (ArgumentNullException a)
            {
                Console.WriteLine("ArgumentNullException:{0}", a);
                return false;
            }
            catch (SocketException ex)
            {
                Console.WriteLine("SocketException:{0}", ex);
                return false;
            }
            return true;
        }
        public bool Stop()
        {
            Action<string> DebugInfo = (s) => {
                _DebugInfo(s, Color.White, true);
            };
            CommandCode Command = CommandCode.Stop;
            try
            {
                byte[] receive = new byte[TCP_PackageSize];
                double TransTime = CalcRunTime(() =>
                {
                    m_client = new TcpClient(IP, nPort);
                    m_client.ReceiveTimeout = 1000;
                    m_client.SendTimeout = 1000;

                    NetworkStream write = m_client.GetStream();

                    double WriteModeTime = CalcRunTime(() =>
                    {
                        int Mode = (int)Command;
                        RequestValue(write, Mode, out Mode);

                        Debug($"Mode={(CommandCode)Mode}\n", Color.Gold);
                    });
                    Debug($"{VersionInfo}\n", Color.Yellow);
                    DebugInfo($"______________________________________________________________________________________\n");

                    DebugInfo($"Write Time: {WriteModeTime} ms");
                    DebugInfo($"\n______________________________________________________________________________________\n");
                    write.Close();
                    m_client.Close();
                });

            }
            catch (ArgumentNullException a)
            {
                Console.WriteLine("ArgumentNullException:{0}", a);
                return false;
            }
            catch (SocketException ex)
            {
                Console.WriteLine("SocketException:{0}", ex);
                return false;
            }
            return true;
        }
        public bool ReadData()
        {
            Action<string> DebugInfo = (s) => {
                _DebugInfo(s, Color.White,true);
            };
            CommandCode Command = CommandCode.ReadData;
            try
            {
                byte[] receive = new byte[TCP_PackageSize];
                double TransTime = CalcRunTime(() =>
                {
                    m_client = new TcpClient(IP, nPort);
                    m_client.ReceiveTimeout = 1000;
                    m_client.SendTimeout = 1000;

                    NetworkStream write = m_client.GetStream();

                    double WriteModeTime = CalcRunTime(() =>
                    {
                        int Mode = (int)Command;
                        RequestValue(write, Mode, out Mode);

                        Debug($"Mode={(CommandCode)Mode}\n", Color.Gold);
                    });
                    Debug($"{VersionInfo}\n", Color.Yellow);
                    DebugInfo($"______________________________________________________________________________________\n");
                    DebugInfo($"<Data Content>\n");
                    byte[] RecData = new byte[4];
                    string Info = "";
                    double ReadingTime = CalcRunTime(() =>
                    {
                        while (true)
                        {
                            try
                            {
                                int count = write.Read(RecData, 0, 4);
                                if (count > 0)
                                    Info += Encoding.ASCII.GetString(RecData);
                                else
                                    break;
                            }
                            catch
                            {
                                break;
                            }
                        }
                    });

                    if (Info.Length <= 2560)
                    {
                        DebugInfo(Info + "\n");
                    }
                    else
                        Debug("Data Too Large!Copy to Clipboard!\n", Color.Red);
                    //Clipboard.SetData(DataFormats.Text, Info);
                    DebugInfo($"\n______________________________________________________________________________________\n");
                    DebugInfo($"Read Time: {ReadingTime} ms Read Data:{Info.Length:#,0} bytes");
                    DebugInfo($"\n______________________________________________________________________________________\n");
                    write.Close();
                    m_client.Close();
                });

            }
            catch (ArgumentNullException a)
            {
                Console.WriteLine("ArgumentNullException:{0}", a);
                return false;
            }
            catch (SocketException ex)
            {
                Console.WriteLine("SocketException:{0}", ex);
                return false;
            }
            return true;
        }
        public bool RequestValue(NetworkStream write, int InValue, out int OutValue)
        {
            OutValue = -1;
            try
            {
                write.Write(Encoding.ASCII.GetBytes("" + InValue), 0, Encoding.ASCII.GetBytes("" + InValue).Length);
                byte[] temp = new byte[16];
                write.Read(temp, 0, 16);
                int.TryParse(Encoding.ASCII.GetString(temp).Replace("\0", ""), out OutValue);
                return InValue == OutValue;
            }
            catch (ArgumentNullException a)
            {
                Console.WriteLine("ArgumentNullException:{0}", a);
                return false;
            }
            catch (SocketException ex)
            {
                Console.WriteLine("SocketException:{0}", ex);
                return false;
            }
            catch (Exception ex)
            {
                Console.WriteLine("Exception:{0}", ex);
                return false;
            }
        }
        public bool SendData(CommandCode SelectMode, List<byte> PrepareData, int SendTotalNumber, bool IsDebug, out int ReceiveTotalNumber, out double TransTime, out int ConnectionPort)
        {
            Action<string> DebugInfo = (s) => {
                _DebugInfo(s, Color.White, IsDebug);
            };
            ReceiveTotalNumber = 0;
            TransTime = 0;
            ConnectionPort = 0;
            int PSize = sizeof(int) * 2;
            CommandCode Command = SelectMode;

            byte[] receive = new byte[TCP_PackageSize];
            NetworkStream write = null;
            bool IsHappenError = false;

            TransTime = CalcRunTime(() =>
            {
                try
                {
                    m_client = new TcpClient(IP, nPort);
                    write = m_client.GetStream();
                }
                catch (ArgumentNullException a)
                {
                    IsHappenError = true;
                    Console.WriteLine("ArgumentNullException:{0}", a);
                }
                catch (SocketException ex)
                {
                    IsHappenError = true;
                    Console.WriteLine("SocketException:{0}", ex);
                }
                catch (Exception ex)
                {
                    IsHappenError = true;
                    Console.WriteLine("Exception:{0}", ex);
                }
                double WriteModeTime = CalcRunTime(() =>
                {
                    int Mode = (int)Command;
                    RequestValue(write, Mode, out Mode);
                    if (IsDebug)
                        Debug($"Mode={(CommandCode)Mode}\n", Color.Gold);
                });
                if (Command == SelectMode)
                {
                    if (IsDebug)
                        Debug($"{VersionInfo}\n", Color.Yellow);
                    DebugInfo($"______________________________________________________________________________________\n");
                    DebugInfo($"1.Send Data Size: \t \t {SendTotalNumber >> 10}KB  ({SendTotalNumber:#,0} bytes) \n");
                    DebugInfo($" \t \t  \t \t {(SendTotalNumber / PSize):#,0} points (1 point = {PSize:#,0}bytes)\n");
                    double WriteTime = CalcRunTime(() =>
                    {
                        //Send Total DataNumber
                        bool Result = RequestValue(write, SendTotalNumber, out SendTotalNumber);
                        if (!Result)
                            IsHappenError = true;
                    });
                    DebugInfo("2.Write Header Time: \t " + WriteTime + "ms\n");
                    //準備測試資料
                    double WriteDataTime = CalcRunTime(() =>
                    {
                        int DataSize = TCP_PackageSize;
                        //MCU會多傳兩個字元
                        const int ExtraSize = 2;
                        byte[] SendData = new byte[DataSize + ExtraSize];
                        byte[] RecData = new byte[DataSize + ExtraSize];

                        int OutputCounter = 0;
                        //任意大小參數
                        int RealDataSize = DataSize;
                        while (OutputCounter < SendTotalNumber)
                        {
                            for (int i = 0; i < DataSize; i++)
                            {
                                SendData[i] = PrepareData[OutputCounter % PrepareData.Count];
                                if (OutputCounter < SendTotalNumber)
                                {
                                    OutputCounter++;
                                }
                                else
                                {
                                    RealDataSize = i;
                                    break;
                                }
                            }
                            if (false)
                            {
                                //Debug Data
                                DebugInfo(Encoding.ASCII.GetString(SendData));
                                DebugInfo("\n");
                            }
                            try
                            {
                                write.Write(SendData, 0, RealDataSize + ExtraSize);
                                write.Read(RecData, 0, DataSize + ExtraSize);
                            }
                            catch (ArgumentNullException a)
                            {
                                IsHappenError = true;
                                Console.WriteLine("ArgumentNullException:{0}", a);

                            }
                            catch (SocketException ex)
                            {
                                IsHappenError = true;
                                Console.WriteLine("SocketException:{0}", ex);
                            }
                            catch (Exception ex)
                            {
                                IsHappenError = true;
                                Console.WriteLine("Exception:{0}", ex);
                            }
                            //確認接收發送一致
                            if (!ByteArrayCompare(RecData, SendData))
                            {
                                DebugInfo(Encoding.ASCII.GetString(RecData).Substring(0, 10) + "\n");
                            }
                        }
                    });
                    DebugInfo("3.Write Data Time: \t \t " + WriteDataTime + "ms\n");
                    double ReceiveTime = CalcRunTime(() =>
                    {
                        try
                        {
                            write.Read(receive, 0, receive.Length);
                        }
                        catch (ArgumentNullException a)
                        {
                            IsHappenError = true;
                            Console.WriteLine("ArgumentNullException:{0}", a);

                        }
                        catch (SocketException ex)
                        {
                            IsHappenError = true;
                            Console.WriteLine("SocketException:{0}", ex);
                        }
                        catch (Exception ex)
                        {
                            IsHappenError = true;
                            Console.WriteLine("Exception:{0}", ex);
                        }
                    });
                    DebugInfo("4.Receive Msg Time:  \t " + ReceiveTime + "ms\n");
                }
                try
                {
                    write.Close();
                    m_client.Close();
                }
                catch (ArgumentNullException a)
                {
                    IsHappenError = true;
                    Console.WriteLine("ArgumentNullException:{0}", a);

                }
                catch (SocketException ex)
                {
                    IsHappenError = true;
                    Console.WriteLine("SocketException:{0}", ex);
                }
                catch (Exception ex)
                {
                    IsHappenError = true;
                    Console.WriteLine("Exception:{0}", ex);
                }
            });
            string[] ReceiveData = Encoding.ASCII.GetString(receive).Trim('\0').Split(',');

            if (ReceiveData.Length == 2)
            {
                int.TryParse(ReceiveData[0], out ReceiveTotalNumber);
                int.TryParse(ReceiveData[1], out ConnectionPort);
                DebugInfo($"5.Total Trans Time: \t \t {TransTime:0.0} ms   ({(ReceiveTotalNumber >> 10) * 1000.0 / TransTime:0.0}KB / s)\n");
                DebugInfo($"6.Receive Data Size:  \t {ReceiveTotalNumber >> 10}KB  ({ReceiveTotalNumber:#,0}bytes)\n");
                DebugInfo($"7.Trans Result: \t  \t {(ReceiveTotalNumber == SendTotalNumber ? "Successful" : "Failure")}!\n");
                DebugInfo($"8.Connection Port: \t \t {ConnectionPort}\n");
            }


            DebugInfo($"______________________________________________________________________________________\n");
            return true;
        }
        public double CalcRunTime(Action act)
        {
            Stopwatch sw = new Stopwatch();
            sw.Start();
            act();
            sw.Stop();
            return Math.Round(sw.Elapsed.TotalMilliseconds, 2);
        }
    }
}
