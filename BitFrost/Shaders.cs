using ComputeSharp;
using System.Security;

namespace BitFrost
{
    public partial class Shaders
    {
        [AutoConstructor]
        [EmbeddedBytecode(DispatchAxis.X)]
        public readonly partial struct StaticRed : IComputeShader
        {
            public readonly ReadWriteBuffer<float> LEDColours;
            public readonly int TotalLeds;

            public void Execute()
            {
                int i = ThreadIds.X;

                LEDColours[i * 3 + 0] = 1.0f; // Red
                LEDColours[i * 3 + 1] = 0.0f; // Green
                LEDColours[i * 3 + 2] = 0.0f; // Blue
            }
        }
    }
}
