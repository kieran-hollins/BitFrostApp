using System.Text;

namespace BitFrost
{
    public class ArtPacket
    {
        public readonly byte[] ID = Encoding.ASCII.GetBytes("Art-Net\0");
        private byte opCodeLo = 0;
        private byte opCodeHi = 0x50;
        private readonly byte protVerHi = 0;
        private readonly byte protVerLo = 0x0e;
        private byte sequence = 0;
        private byte physical = 0;
        private byte subUni = 0;
        private byte net = 0;
        private byte lengthHi = 0;
        private byte lengthLo = 0;
        private byte[] data = new byte[512];

        public byte Sequence { get { return sequence; } set { sequence = value; } }
        public byte[] Data { get { return data; } set { SetData(value, subUni); } }

        private int packetCount = 0;

        public byte[] GetPacket()
        {           
            Sequence = (byte)((packetCount++ % 256) & 0xFF); // Wraps values back to 0 after reaching 255

            byte[] packet = new byte[18 + data.Length];
            Array.Copy(ID, packet, ID.Length);
            packet[8] = opCodeLo;
            packet[9] = opCodeHi;
            packet[10] = protVerHi;
            packet[11] = protVerLo;
            packet[12] = Sequence;
            packet[13] = physical;
            packet[14] = subUni;
            packet[15] = net;
            packet[16] = lengthHi;
            packet[17] = lengthLo;
            Array.Copy(data, 0, packet, 18, data.Length);

            return packet;
        }

        public void SetData(byte[] data, int universe)
        {
            if (data.Length > 512)
                throw new ArgumentException("Data exceeds 512 bytes which is not allowed in Art-Net DMX data.");
            this.data = data;
            int dataLength = data.Length;
            lengthHi = (byte)((dataLength >> 8) & 0xFF);
            lengthLo = (byte)(dataLength & 0xFF);
            subUni = (byte)universe;
        }
    }
}
