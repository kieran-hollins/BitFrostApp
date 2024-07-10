using BitFrost;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BitFrostTests
{
    [TestClass]
    public class ArtPacketTests
    {
        [TestMethod]
        public void GetPacket_ValidData_ReturnsCorrectPacket()
        {
            ArtPacket packet = new ArtPacket();
            byte[] data = new byte[512];

            for (int i = 0; i < data.Length; i++)
            {
                data[i] = (byte)(i % 256);
            }

            packet.SetData(data, 0);

            byte[] result = packet.GetPacket();

            Assert.AreEqual(530, result.Length);
            CollectionAssert.AreEqual(Encoding.ASCII.GetBytes("Art-Net\0"), result[0..8]);
            Assert.AreEqual(0x50, result[9]);
            Assert.AreEqual(0x00, result[10]);
            Assert.AreEqual(0x0e, result[11]);
            CollectionAssert.AreEqual(data, result[18..]);
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentException))]
        public void SetData_DataExceeds512Bytes_ThrowsException()
        {
            ArtPacket packet = new ArtPacket();
            byte[] data = new byte[513]; // Exceeds the allowed size
            packet.SetData(data, 0);
        }

        [TestMethod]
        public void Sequence_IncrementsCorrectly()
        {
            ArtPacket packet = new ArtPacket();
            byte[] data = new byte[512];

            packet.SetData(data, 0);

            for (int i = 0; i < 300; i++)
            {
                byte[] result = packet.GetPacket();
                Assert.AreEqual((byte)(i % 256), result[12]);
            }
        }

        [TestMethod]
        public void SetData_ValidData_SetsLengthCorrectly()
        {
            ArtPacket packet = new ArtPacket();
            byte[] data = new byte[300];

            packet.SetData(data, 0);

            Assert.AreEqual(300, (packet.GetPacket()[16] << 8) | packet.GetPacket()[17]);
        }
    }
}
