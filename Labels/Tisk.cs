using Microsoft.Win32.SafeHandles;//musi byt na zacatku
// using Form;
using System.Text;
using System.Runtime.InteropServices;
using System.Net.Sockets;

namespace LabelsTG.Labels
{
    public static class Printer
    {
        public static void PrintLabel(string telo)
        {
            if (Configuration.PrinterType == 0)
                SharedPrinter.Print(Configuration.PrinterAddress, telo);
            if (Configuration.PrinterType == 1)
                LocalPrinter.SendStringToPrinter(Configuration.PrinterAddress, telo);
            if (Configuration.PrinterType == 2)
                IpPrinter.Print(Configuration.PrinterAddress, telo);
            // if (Configuration.PrinterType == 3)
            // {
            //     NotificationForm notification = new NotificationForm("", telo);
            //     notification.Display();
            //     Console.ReadKey();
            // }
        }
        private static class SharedPrinter
        {
            [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
            static extern SafeFileHandle CreateFile(string lpFileName, FileAccess dwDesiredAccess, uint dwShareMode, IntPtr lpSecurityAttributes, FileMode dwCreationDisposition, uint dwFlagsAndAttributes, IntPtr hTemplateFile);

            public static bool Print(string printerAddress, string eplText)
            {
                bool IsConnected = true;
                try
                {
                    byte[] buffer = new byte[eplText.Length];
                    buffer = Encoding.GetEncoding(Configuration.EplEncoding).GetBytes(eplText);
                    SafeFileHandle fh = CreateFile(printerAddress, FileAccess.Write, 0, IntPtr.Zero, FileMode.OpenOrCreate, 0, IntPtr.Zero);
                    FileStream lpt1 = new FileStream(fh, FileAccess.ReadWrite);
                    lpt1.Write(buffer, 0, buffer.Length);
                    lpt1.Close();
                }
                catch (Exception ex)
                {
                    ErrorHandler.HandleError("SharedPrinter", ex);
                    IsConnected = false;
                }
                return IsConnected;
            }
        }
        private class LocalPrinter
        {
            // Structure and API declarions:
            [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi)]
            public class DOCINFOA
            {
                [MarshalAs(UnmanagedType.LPStr)] public string pDocName;
                [MarshalAs(UnmanagedType.LPStr)] public string pOutputFile;
                [MarshalAs(UnmanagedType.LPStr)] public string pDataType;
            }
            [DllImport("winspool.Drv", EntryPoint = "OpenPrinterA", SetLastError = true, CharSet = CharSet.Ansi, ExactSpelling = true, CallingConvention = CallingConvention.StdCall)]
            public static extern bool OpenPrinter([MarshalAs(UnmanagedType.LPStr)] string szPrinter, out IntPtr hPrinter, IntPtr pd);

            [DllImport("winspool.Drv", EntryPoint = "ClosePrinter", SetLastError = true, ExactSpelling = true, CallingConvention = CallingConvention.StdCall)]
            public static extern bool ClosePrinter(IntPtr hPrinter);

            [DllImport("winspool.Drv", EntryPoint = "StartDocPrinterA", SetLastError = true, CharSet = CharSet.Ansi, ExactSpelling = true, CallingConvention = CallingConvention.StdCall)]
            public static extern bool StartDocPrinter(IntPtr hPrinter, Int32 level, [In, MarshalAs(UnmanagedType.LPStruct)] DOCINFOA di);

            [DllImport("winspool.Drv", EntryPoint = "EndDocPrinter", SetLastError = true, ExactSpelling = true, CallingConvention = CallingConvention.StdCall)]
            public static extern bool EndDocPrinter(IntPtr hPrinter);

            [DllImport("winspool.Drv", EntryPoint = "StartPagePrinter", SetLastError = true, ExactSpelling = true, CallingConvention = CallingConvention.StdCall)]
            public static extern bool StartPagePrinter(IntPtr hPrinter);

            [DllImport("winspool.Drv", EntryPoint = "EndPagePrinter", SetLastError = true, ExactSpelling = true, CallingConvention = CallingConvention.StdCall)]
            public static extern bool EndPagePrinter(IntPtr hPrinter);

            [DllImport("winspool.Drv", EntryPoint = "WritePrinter", SetLastError = true, ExactSpelling = true, CallingConvention = CallingConvention.StdCall)]
            public static extern bool WritePrinter(IntPtr hPrinter, IntPtr pBytes, Int32 dwCount, out Int32 dwWritten);

            // SendBytesToPrinter()
            // When the function is given a printer name and an unmanaged array
            // of bytes, the function sends those bytes to the print queue.
            // Returns true on success, false on failure.
            public static bool SendBytesToPrinter(string szPrinterName, IntPtr pBytes, Int32 dwCount)
            {
                int dwError = 0, dwWritten = 0;
                IntPtr hPrinter = new IntPtr(0);
                DOCINFOA di = new DOCINFOA();
                bool bSuccess = false; // Assume failure unless you specifically succeed.

                di.pDocName = "My C#.NET RAW Document";
                di.pDataType = "RAW";

                // Open the printer.
                if (OpenPrinter(szPrinterName.Normalize(), out hPrinter, IntPtr.Zero))
                {
                    // Start a document.
                    if (StartDocPrinter(hPrinter, 1, di))
                    {
                        // Start a page.
                        if (StartPagePrinter(hPrinter))
                        {
                            // Write your bytes.
                            bSuccess = WritePrinter(hPrinter, pBytes, dwCount, out dwWritten);
                            EndPagePrinter(hPrinter);
                        }
                        EndDocPrinter(hPrinter);
                    }
                    ClosePrinter(hPrinter);
                }
                // If you did not succeed, GetLastError may give more information
                // about why not.
                if (bSuccess == false)
                {
                    dwError = Marshal.GetLastWin32Error();
                    ErrorHandler.HandleError("LocalPrinter", new Exception($"Tisk na mistní tiskárně se nezdařil, kód chyby: {dwError}"));
                }
                return bSuccess;
            }

            public static bool SendFileToPrinter(string szPrinterName, string szFileName)
            {
                // Open the file.
                FileStream fs = new FileStream(szFileName, FileMode.Open);
                // Create a BinaryReader on the file.
                BinaryReader br = new BinaryReader(fs);
                // Dim an array of bytes big enough to hold the file's contents.
                Byte[] bytes = new Byte[fs.Length];
                bool bSuccess = false;
                // Your unmanaged pointer.
                IntPtr pUnmanagedBytes = new IntPtr(0);
                int nLength;
                nLength = Convert.ToInt32(fs.Length);
                // Read the contents of the file into the array.
                bytes = br.ReadBytes(nLength);
                // Allocate some unmanaged memory for those bytes.
                pUnmanagedBytes = Marshal.AllocCoTaskMem(nLength);
                // Copy the managed byte array into the unmanaged array.
                Marshal.Copy(bytes, 0, pUnmanagedBytes, nLength);
                // Send the unmanaged bytes to the printer.
                bSuccess = SendBytesToPrinter(szPrinterName, pUnmanagedBytes, nLength);
                // Free the unmanaged memory that you allocated earlier.
                Marshal.FreeCoTaskMem(pUnmanagedBytes);
                return bSuccess;
            }
            public static bool SendStringToPrinter(string szPrinterName, string szString)
            {
                IntPtr pBytes;
                Int32 dwCount;
                // How many characters are in the string?
                dwCount = szString.Length;
                // Assume that the printer is expecting ANSI text, and then convert
                // the string to ANSI text.
                pBytes = Marshal.StringToCoTaskMemAnsi(szString);
                // Send the converted ANSI string to the printer.
                bool NavratovaHodnota = SendBytesToPrinter(szPrinterName, pBytes, dwCount);
                Marshal.FreeCoTaskMem(pBytes);
                return NavratovaHodnota;
            }
        }
        private class IpPrinter
        {
            public static int Print(string adresaTiskarny, string EPLString)
            {

                // Printer IP Address and communication port
                string ipAddress = adresaTiskarny;
                int port = 9100;
                try
                {
                    // Open connection
                    TcpClient client = new TcpClient();
                    client.Connect(ipAddress, port);

                    // Write ZPL String to connection
                    StreamWriter writer = new StreamWriter(client.GetStream(), Encoding.GetEncoding("windows-1250"));
                    //pridavam "N" na zacatek kvuli vymazani predchoziho stitku
                    writer.Write("N" + Environment.NewLine);
                    writer.Write(EPLString);
                    writer.Flush();

                    // Close Connection
                    writer.Close();
                    client.Close();
                    return 0;
                }
                catch (Exception ex)
                {
                    // Catch Exception
                    ErrorHandler.HandleError("IpPrinter", ex);
                    return 1;
                }
            }
        }
    }
}

