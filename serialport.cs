using System.IO.Ports;
using System.Text;
using System.Threading;

static class SerialPortApp
{
    static SerialPort? serialPort;
    static bool running = true;

    public static void Main()
    {
        Console.OutputEncoding = Encoding.UTF8;
        Console.WriteLine("SerialPort Console App\n");

        var portName = ChoosePort();
        if (portName == null)
        {
            Console.WriteLine("No serial ports were selected. Exiting.");
            return;
        }

        var baudRate = ChooseBaudRate();
        if (baudRate == null)
        {
            Console.WriteLine("No baud rate was selected. Exiting.");
            return;
        }

        OpenPort(portName, baudRate.Value);

        var receiveThread = new Thread(ReadFromPort) { IsBackground = true };
        receiveThread.Start();

        Console.WriteLine("Type text and press Enter to send data. Type '/quit' to exit.");
        while (running)
        {
            var line = Console.ReadLine();
            if (line == null)
                break;

            if (line.Trim().Equals("/quit", StringComparison.OrdinalIgnoreCase))
            {
                running = false;
                break;
            }

            WriteToPort(line + "\r\n");
        }

        ClosePort();
        Console.WriteLine("Serial port closed. Goodbye.");
    }

    static string? ChoosePort()
    {
        var ports = SerialPort.GetPortNames();
        if (ports.Length == 0)
        {
            Console.WriteLine("No serial ports were found.");
            return null;
        }

        Console.WriteLine("Available serial ports:");
        for (var i = 0; i < ports.Length; i++)
        {
            Console.WriteLine($"  {i + 1}. {ports[i]}");
        }

        while (true)
        {
            Console.Write("Select a port by number: ");
            var input = Console.ReadLine();
            if (input == null)
                return null;

            if (int.TryParse(input.Trim(), out var selection) && selection >= 1 && selection <= ports.Length)
                return ports[selection - 1];

            Console.WriteLine("Invalid selection. Please enter a valid port number.");
        }
    }

    static int? ChooseBaudRate()
    {
        var defaults = new[] { 9600, 19200, 38400, 57600, 115200 };
        Console.WriteLine("Common baud rates:");
        for (var i = 0; i < defaults.Length; i++)
        {
            Console.WriteLine($"  {i + 1}. {defaults[i]}");
        }
        Console.WriteLine("  0. Enter custom baud rate");

        while (true)
        {
            Console.Write("Select baud rate option: ");
            var input = Console.ReadLine();
            if (input == null)
                return null;

            if (int.TryParse(input.Trim(), out var selection))
            {
                if (selection >= 1 && selection <= defaults.Length)
                    return defaults[selection - 1];

                if (selection == 0)
                {
                    Console.Write("Enter custom baud rate: ");
                    var customInput = Console.ReadLine();
                    if (customInput == null)
                        return null;
                    if (int.TryParse(customInput.Trim(), out var customBaud) && customBaud > 0)
                        return customBaud;
                }
            }

            Console.WriteLine("Invalid baud rate selection. Try again.");
        }
    }

    static void OpenPort(string portName, int baudRate)
    {
        serialPort = new SerialPort(portName, baudRate)
        {
            Encoding = Encoding.UTF8,
            ReadTimeout = 500,
            WriteTimeout = 500,
        };

        try
        {
            serialPort.Open();
            Console.WriteLine($"Opened {portName} at {baudRate} baud.");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error opening port: {ex.Message}");
            running = false;
        }
    }

    static void ReadFromPort()
    {
        if (serialPort == null)
            return;

        while (running)
        {
            try
            {
                var line = serialPort.ReadLine();
                Console.WriteLine($"[RX] {line}");
            }
            catch (TimeoutException)
            {
                continue;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Read error: {ex.Message}");
                running = false;
            }
        }
    }

    static void WriteToPort(string data)
    {
        if (serialPort == null || !serialPort.IsOpen)
        {
            Console.WriteLine("Port is not open. Cannot send data.");
            return;
        }

        try
        {
            serialPort.Write(data);
            Console.WriteLine($"[TX] {data.TrimEnd()}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Write error: {ex.Message}");
        }
    }

    static void ClosePort()
    {
        if (serialPort == null)
            return;

        try
        {
            serialPort.Close();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error closing port: {ex.Message}");
        }
    }
}
