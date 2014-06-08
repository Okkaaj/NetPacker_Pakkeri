/*
 * Simple .NET executable packer. Compression ratio isn't very good, but it's just a proof of concept.
 * Uses codedom library for building/compiling the output file. 
 * It's coded pretty quickly(1.5 hours) to keep a small break from coding a PE packer in pure C.
 * 
 * Yksinkertainen .NET EXE päkkeri. Ratio ei mikään päätä huimaava, mutta toimii hyvin esimerkkinä. 
 * Käyttää codedom kirjastoa kompressoidun tiedoston 'kääntämiseen'.
 * Tunnin - puolentoista tunnin ajantappo projekti, sillävälin kun C:llä koodattu päkkeri on työn alla :)
 * - Okkaaj
*/


using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using System.Runtime.InteropServices;
using System.CodeDom.Compiler;
using Microsoft.CSharp;

namespace dotNET_Packer
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            OpenFileDialog OFD = new OpenFileDialog();

            OFD.Filter = "Managed Executables (*.exe)|*.exe";

            if (OFD.ShowDialog() == DialogResult.OK)
            {
                textBox1.Text = OFD.FileName;
            }
            else
            {
                MessageBox.Show("Did you select a file?");
            }

        }

        private void button2_Click(object sender, EventArgs e)
        {
            if (String.IsNullOrEmpty(textBox1.Text))
            {
                MessageBox.Show("Please select a file first...", "Error!");
                return;
            }

            System.IO.FileInfo FileInfo = new System.IO.FileInfo(textBox1.Text);

            byte[] FileData = System.IO.File.ReadAllBytes(textBox1.Text);

            if (FileData == null || FileData.Length != FileInfo.Length)
            {
                MessageBox.Show("Something went wrong file reading the file...", "Error!");
                return;
            }

            if(!isDotNet(FileData))
            {
                MessageBox.Show("Only managed executables are supported!", "Error!");
                return;
            }

            string OutputName;

            SaveFileDialog SFD = new SaveFileDialog();
            SFD.Filter = "Executables (*.exe)|*.exe";
           
            if (SFD.ShowDialog() == DialogResult.OK)
            {
                OutputName = SFD.FileName;
            }
            else
            {
                MessageBox.Show("Did you choose the output location?");
                return;
            }


            FileData = NativeMethods.Compress(FileData);

            string errors = String.Empty;
            string o_source = dotNET_Packer.Properties.Resources.SourceTemplate;
            string source = string.Copy(o_source);
            string OrigSrc = source;

            source = source.Replace("[COMPRESSED_DATA]", Formatter(FileData));

            CompilerParameters cp = new CompilerParameters();
            
            Dictionary<string, string> ProviderOptions = new Dictionary<string, string>();

            cp.GenerateExecutable = true;
            cp.ReferencedAssemblies.AddRange(new String[] { "System.dll" });
            cp.OutputAssembly = OutputName;
            cp.CompilerOptions = "/optimize+ /target:winexe /platform:x86";
            cp.IncludeDebugInformation = false;

            ProviderOptions.Add("CompilerVersion", "v2.0");

            CompilerResults results = new CSharpCodeProvider(ProviderOptions).CompileAssemblyFromSource(cp, source);

            if (results.Errors.Count > 0)
            {
                foreach (CompilerError err in results.Errors)
                {
                    errors += "Error: " + err.ToString() + "\r\n\r\n";
                }
            }
            else
            {
                errors = "Compression succeed! File saved to: \n" + OutputName;
            }

            MessageBox.Show(errors);
            
        }

        static string Formatter(byte[] ba)
        {
            StringBuilder hex = new StringBuilder(ba.Length * 2);
           
            foreach (byte b in ba)
            {
                hex.AppendFormat("0x{0:x2},", b);
            }

            string str = hex.ToString();
            str = str.Remove((str.Length - 1), 1);
            return str;
        }

       bool isDotNet(byte[] data)
        {
            try
            {
                System.Reflection.Assembly.Load(data);
                return true;
            }
            catch
            {
                return false;
            }
        }

    }

    internal static class NativeMethods
    {
        const ushort COMPRESSION_FORMAT_LZNT1 = 2;
        const ushort COMPRESSION_ENGINE_MAXIMUM = 0x100;
        
        [DllImport("ntdll.dll")]
        private static extern uint RtlGetCompressionWorkSpaceSize(ushort CompressionFormat, ref uint pNeededBufferSize, ref uint Unknown);

        [DllImport("ntdll.dll")]
        private static extern uint RtlCompressBuffer(ushort CompressionFormat, byte[] SourceBuffer, int SourceBufferLength, byte[] DestinationBuffer, int DestinationBufferLength, uint Unknown, ref int pDestinationSize, IntPtr WorkspaceBuffer);

        [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        static internal extern IntPtr LocalAlloc(int uFlags, IntPtr sizetdwBytes);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern IntPtr LocalFree(IntPtr hMem);
        
        [DllImport("ntdll.dll")]
        private static extern uint RtlDecompressBuffer(ushort CompressionFormat, byte[] DestinationBuffer, int DestinationBufferLength, byte[] SourceBuffer, int SourceBufferLength, ref int pDestinationSize);

        public static byte[] Compress(byte[] buffer)
        {
            byte[] outBuf = new byte[buffer.Length * 6];
            uint dwSize = 0;
            uint dwRet = 0;
            uint ret = RtlGetCompressionWorkSpaceSize(COMPRESSION_FORMAT_LZNT1 | COMPRESSION_ENGINE_MAXIMUM, ref dwSize, ref dwRet);
            
            if (ret != 0)
            {
                return null;
            }

            int dstSize = 0;
           
            IntPtr hWork = LocalAlloc(0, new IntPtr(dwSize));
            
            ret = RtlCompressBuffer(COMPRESSION_FORMAT_LZNT1 | COMPRESSION_ENGINE_MAXIMUM, buffer, buffer.Length, outBuf, outBuf.Length, 0, ref dstSize, hWork);
           
            if (ret != 0)
            {
                return null;
            }

            LocalFree(hWork);

            Array.Resize(ref outBuf, dstSize);

            return outBuf;
        }
        public static byte[] Decompress(byte[] buffer)
        {
            byte[] outBuf = new byte[buffer.Length * 6];
           
            uint dwSize = 0;
            
            uint dwRet = 0;
            
            uint ret = RtlGetCompressionWorkSpaceSize(COMPRESSION_FORMAT_LZNT1, ref dwSize, ref dwRet);
            
            if (ret != 0)
            {
                return null;
            }
            int dstSize = 0;
            
            ret = RtlDecompressBuffer(COMPRESSION_FORMAT_LZNT1, outBuf, outBuf.Length, buffer, buffer.Length, ref dstSize);
           
            if (ret != 0)
            {
                return null;
            }

            Array.Resize(ref outBuf, dstSize);
            return outBuf;
        }
    }
}
