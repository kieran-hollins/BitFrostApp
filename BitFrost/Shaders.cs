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

        [AutoConstructor]
        [EmbeddedBytecode(DispatchAxis.X)]
        public readonly partial struct WaveShader : IComputeShader
        {
            public readonly ReadWriteBuffer<float> ledColors;
            public readonly float time;
            public readonly int width;
            public readonly int height;
            public readonly float frequency;
            public readonly float amplitude;

            public void Execute()
            {
                int x = ThreadIds.X % width;
                int y = ThreadIds.X / width;
                float u = x / (float)width;
                float v = y / (float)height;

                float wave = 0.5f + 0.5f * Hlsl.Sin(time * frequency + amplitude * (u * u + v * v));
                float r = wave;
                float g = 0.5f * wave;
                float b = 1.0f - wave;

                int index = (y * width + x) * 3;
                ledColors[index + 0] = r;
                ledColors[index + 1] = g;
                ledColors[index + 2] = b;
            }
        }
    }
}
