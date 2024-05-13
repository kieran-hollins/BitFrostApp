using System.Net.Sockets;
using System.Net;
using System.Diagnostics;

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
            SendTimer = new Timer(async _ => await SendDMXAsync(), null, RefreshRate, RefreshRate);
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
                SwapBuffers(); // Swap the front and back buffers
            }
        }

        private async Task SendDMXAsync()
        {
            byte[] dataToSend;
            lock (_bufferLock)
            {
                dataToSend = (byte[])FrontBuffer.Clone(); // Cloning front buffer ensures thread safety during send
            }
            
            ArtPacket packet = new();
            packet.SetData(dataToSend, Universe);

            try
            {
                await UDPClient.SendAsync(packet.GetPacket(), packet.GetPacket().Length, DestinationEndPoint);
                Debug.WriteLine($"Sent DMX packet at {DateTime.Now}");
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
}
