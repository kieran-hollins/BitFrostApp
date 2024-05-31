using System.Net.Sockets;
using System.Net;
using System.Diagnostics;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace BitFrost
{
    public class ArtNetController
    {
        private UdpClient UDPClient;
        private IPEndPoint DestinationEndPoint;
        private int Universe;
        private bool Enabled;
        private int RefreshRate;
        private Timer? SendTimer;
        private readonly object _bufferLock = new();
        private byte[] FrontBuffer;
        private byte[] BackBuffer;
        private LightingPatch Patch;
        private bool IsBufferReady = false;

        public ArtNetController(string destinationIP, int universe, LightingPatch patch, int port = 6454)
        {
            UDPClient = new();
            DestinationEndPoint = new IPEndPoint(IPAddress.Parse(destinationIP), port);
            Enabled = false;
            RefreshRate = (int)1000 / 30; // Frames Per Second (30 by default)
            FrontBuffer = new byte[512];
            BackBuffer = new byte[512];
            Patch = patch;
            Patch.OnLEDUpdate += SetData; // Subscribe to LED update event
            Universe = universe;
        }

        public void Enable()
        {
            Enabled = true;
            SendTimer = new Timer(
                callback: new TimerCallback(async _ => await SendDMXAsync()),
                state: this,
                dueTime: RefreshRate,
                period: RefreshRate);
        }

        public void Disable()
        {
            Enabled = false;
            SendTimer?.Change(Timeout.Infinite, Timeout.Infinite);
            SendTimer?.Dispose();
        }

        public void UpdateUniverse(int universe)
        {
            if (universe < 0 || universe > 254)
            {
                throw new ArgumentException("Universe must be within range 0 -> 254");
            }
            Universe = universe;
        }

        public void SetData(byte[] data)
        {
            if (data.Length > 512)
            {
                throw new ArgumentException("DMX data length exceeds 512 bytes.");
            }

            lock (_bufferLock)
            {
                Array.Copy(data, BackBuffer, data.Length); // Copy new data into the back buffer
                Debug.WriteLine($"Copying {data[0]} {data[1]} {data[2]}... to back buffer");
                if (!IsBufferReady)
                {
                    Debug.WriteLine("Swapping buffers now.");
                    SwapBuffers(); // Swap the front and back buffers
                    IsBufferReady = true;
                }
                
            }
        }

        private async Task SendDMXAsync()
        {
            if (!IsBufferReady)
            {
                return;
            }

            byte[] dataToSend;
            lock (_bufferLock)
            {
                dataToSend = (byte[])FrontBuffer.Clone(); // Cloning front buffer ensures thread safety during send
                IsBufferReady = false;
            }
            
            ArtPacket packet = new();
            packet.SetData(dataToSend, Universe);

            try
            {
                await UDPClient.SendAsync(packet.GetPacket(), packet.GetPacket().Length, DestinationEndPoint);
                Debug.WriteLine($"Sent DMX packet at {DateTime.Now}. Buffer starts with: {dataToSend[0]} {dataToSend[1]} {dataToSend[2]}");
            }
            catch (Exception e)
            {
                Debug.WriteLine($"Failed to send DMX: {e.Message}");
            }

            
        }

        private void SwapBuffers()
        {
            var temp = FrontBuffer;
            FrontBuffer = BackBuffer;
            BackBuffer = temp;
        }

        public void Close()
        {
            Disable();
            UDPClient.Close();
        }

    }

    public class ArtNetControllerJsonConverter : JsonConverter<ArtNetController>
    {
        public override ArtNetController? Read(ref Utf8JsonReader reader,
                                                Type typeToConvert,
                                                JsonSerializerOptions options)
        {
            throw new NotImplementedException();
        }
        public override void Write(Utf8JsonWriter writer,
                                               ArtNetController value,
                                               JsonSerializerOptions options)
        {
            throw new NotImplementedException();
        }
    }
}
