using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Windows.Forms;

namespace MCU_TCP
{

    public partial class Form1 : Form
    {

        public delegate void AppendText(RichTextBox Box, string text, Color c);
        public void Print(RichTextBox Box, string text, Color c)
        {
            if (Box.InvokeRequired)
            {
                AppendText helper = new AppendText(Print);
                Box.Invoke(helper, Box, text, c);
            }
            else
            {
                Box.SelectionColor = c;
                Box.AppendText(text);
                Box.SelectionStart = richTextBox1.TextLength;

                // Scrolls the contents of the control to the current caret position.
                Box.ScrollToCaret();
            }
        }
        public void _DebugInfo(string msg, Color c, bool IsDebug = true)
        {
            if (IsDebug)
            {
                Print(richTextBox1, msg, c);
            }
        }

        MCUEthernetControl Control = new MCUEthernetControl();

        public Form1()
        {
            InitializeComponent();

        }
        private void Form1_Load(object sender, EventArgs e)
        {
            Control._DebugInfo = _DebugInfo;
        }
        public List<byte> CreateTestData()
        {
            List<byte> SineData16Bit = new List<byte>();
            for (double i = 0.0; i < 360.0; i += 0.1)
            {
                double n = 0.5 * (Math.Sin(i * 3.14159265 / 180.0) + 1);

                string hexValue = ((int)(n * 65535)).ToString("X").PadLeft(4, '0');
                string _hexValue = ((int)((1 - n) * 65535)).ToString("X").PadLeft(4, '0');

                SineData16Bit.AddRange(Encoding.ASCII.GetBytes(hexValue));
                SineData16Bit.AddRange(Encoding.ASCII.GetBytes(_hexValue));
            }
            return SineData16Bit;
        }
        private void btnSend_Click(object sender, EventArgs e)
        {
            CommandCode SelectMode = checkSaveFile.Checked ? CommandCode.TransferData : CommandCode.TransferDataWithoutSave;
            new Thread(() =>
            {
                int TotalNum = (int)numericUpDownTotal.Value;
                double TransTime = 0;
                int RecNum = 0;
                int ID = 0;
                //產生測試資料
                List<byte> SineData16Bit = CreateTestData();
                if (Control.SendData(SelectMode, SineData16Bit, TotalNum, true, out RecNum, out TransTime, out ID))
                {

                }
            }).Start();
        }

        private void button2_Click_1(object sender, EventArgs e)
        {
            CommandCode SelectMode = checkSaveFile.Checked ? CommandCode.TransferData : CommandCode.TransferDataWithoutSave;
            new Thread(() =>
            {
                int ErrorCount = 0;
                int TotalCount = (int)numericUpDown2.Value;
                double TotalTime = 0;
                //產生測試資料
                List<byte> SineData16Bit = CreateTestData();

                for (int i = 0; i < TotalCount; i++)
                {
                    int TotalNum = (int)numericUpDownTotal.Value;
                    double TransTime = 0;
                    int RecNum = 0;
                    int ID = 0;
                    if (Control.SendData(SelectMode, SineData16Bit, TotalNum, false, out RecNum, out TransTime, out ID))
                    {
                        if (TotalNum != RecNum)
                        {
                            ErrorCount++;
                        }
                        TotalTime += TransTime;
                        _DebugInfo($"{ID},{i},{TotalNum},{RecNum},{TransTime:0},{ErrorCount}\n", Color.White);
                    }
                }
                _DebugInfo($"Test Round:{TotalCount} Error Rate:{ErrorCount * 100.0 / TotalCount:0.0}% Avg Trans-Time:{TotalTime / TotalCount:0}ms\n", Color.Yellow);

            }).Start();
        }


        private void btnClear_Click(object sender, EventArgs e)
        {
            richTextBox1.Clear();
        }

        private void btnStart_Click(object sender, EventArgs e)
        {
            new Thread(() =>
            {
                Control.Start();
            }).Start();
        }

        private void btnRead_Click(object sender, EventArgs e)
        {
            new Thread(() =>
            {
                Control.ReadData();
            }).Start();
        }

        private void btnStop_Click(object sender, EventArgs e)
        {
            new Thread(() =>
            {
                Control.Stop();
            }).Start();
        }
    }
}
