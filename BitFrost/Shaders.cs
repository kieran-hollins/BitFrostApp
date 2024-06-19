using ComputeSharp;
using Microsoft.AspNetCore.Routing.Constraints;
using System.Numerics;
using System.Security;
using System.Security.Cryptography.X509Certificates;
using TerraFX.Interop.DirectX;

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
        public readonly partial struct HalfRedHalfBlue : IComputeShader
        {
            public readonly ReadWriteBuffer<float> LEDColours;
            public readonly int Width;

            public void Execute()
            {
                int i = ThreadIds.X;

                float val = Hlsl.Sin(i);

                LEDColours[i * 3 + 0] = Hlsl.Lerp(0.0f, 1.0f, val); // Red
                LEDColours[i * 3 + 1] = Hlsl.Lerp(0.0f, 1.0f, val); // Green
                LEDColours[i * 3 + 2] = Hlsl.Lerp(0.0f, 1.0f, val); ; // Blue

            }
        }

        [AutoConstructor]
        [EmbeddedBytecode(DispatchAxis.X)]
        public readonly partial struct BeatFlashRed : IComputeShader
        {
            public readonly ReadWriteBuffer<float> LEDColours;
            public readonly ReadOnlyBuffer<float> magnitudeBuffer;
            public readonly ReadOnlyBuffer<float> frequencyBuffer;
            public readonly float threshold; // Threshold to detect beat
            public readonly float fadeOutSpeed; // Speed at which the color fades out

            public void Execute()
            {
                // Get the current index
                int index = ThreadIds.X;
                float magnitude = 0.0f;

                for (int i = 0; i < magnitudeBuffer.Length; i++)
                {
                    if (magnitudeBuffer[i] > magnitude)
                    {
                        magnitude = magnitudeBuffer[i];
                    }
                }

                // Check if the magnitude exceeds the threshold (indicating a beat)
                if (magnitude > threshold)
                {
                    // Set the LED color to red
                    LEDColours[index * 3] = 1.0f; // Red
                    LEDColours[index * 3 + 1] = 0.0f; // Green
                    LEDColours[index * 3 + 2] = 0.0f; // Blue
                }
                else
                {
                    // Fade out effect
                    LEDColours[index * 3] = Hlsl.Max(0.0f, LEDColours[index * 3] - fadeOutSpeed);
                    LEDColours[index * 3 + 1] = Hlsl.Max(0.0f, LEDColours[index * 3 + 1] - fadeOutSpeed);
                    LEDColours[index * 3 + 2] = Hlsl.Max(0.0f, LEDColours[index * 3 + 2] - fadeOutSpeed);
                }
            }
        }



        [AutoConstructor]
        [EmbeddedBytecode(DispatchAxis.X)]
        public readonly partial struct HelloShader : IComputeShader
        {
            public readonly ReadWriteBuffer<float> LEDColours;
            public readonly float time;

            public void Execute()
            {
                int x = ThreadIds.X;

                float r = 0.5f + 0.5f * Hlsl.Cos(time + (float)Math.PI * 0.25f);
                float g = 0.5f + 0.5f * Hlsl.Cos(time + 2 * (float)Math.PI * 0.25f);
                float b = 0.5f + 0.5f * Hlsl.Cos(time + 4 * (float)Math.PI * 0.25f);

                LEDColours[x * 3 + 0] = r; // Red
                LEDColours[x * 3 + 1] = g; // Green
                LEDColours[x * 3 + 2] = b; // Blue

            }
        }



        [AutoConstructor]
        [EmbeddedBytecode(DispatchAxis.X)]
        public readonly partial struct AverageColourShader : IComputeShader
        {
            public readonly ReadWriteBuffer<float> ledColours;
            public readonly ReadOnlyBuffer<float> magnitudeBuffer;
            public readonly int width;
            public readonly int numBins;

            public void Execute()
            {
                int x = ThreadIds.X;
                int totalElements = magnitudeBuffer.Length / 2;

                // Calculate the current bin index based on the thread ID and width
                int binIndex = (x * numBins) / totalElements;

                // Fetch the magnitude for the current bin
                float magnitude = magnitudeBuffer[binIndex];

                // Normalize the magnitude
                float normalizedMagnitude = Hlsl.Lerp(0.0f, 1.0f, magnitude);

                // Calculate the color based on the position and normalized magnitude
                float3 colour = PositionToColour(x, width);
                float3 finalColour = colour * normalizedMagnitude;

                // Assign the final color to the LED buffer
                ledColours[x * 3 + 0] = finalColour.X;
                ledColours[x * 3 + 1] = finalColour.Y;
                ledColours[x * 3 + 2] = finalColour.Z;
            }

            private float3 PositionToColour(int x, int width)
            {
                float ratio = (float)x / width;
                float3 colour;

                if (ratio < 0.5f)
                {
                    float subRatio = ratio * 2.0f; // Scale to [0, 1]
                    colour = new float3(1.0f - ratio, subRatio, 0.0f); // Red to Green
                }
                else
                {
                    float subRatio = (ratio - 0.5f) * 2.0f; // Scale to [0, 1]
                    colour = new float3(0.0f, 1.0f - subRatio, ratio); // Green to Blue
                }

                return colour;
            }
        }

        [AutoConstructor]
        [EmbeddedBytecode(DispatchAxis.X)]
        public readonly partial struct WaveShader : IComputeShader
        {
            public readonly ReadWriteBuffer<float> ledColors;
            public readonly ReadOnlyBuffer<float> magnitudeBuffer;
            public readonly ReadOnlyBuffer<float> frequencyBuffer;

            public void Execute()
            {
                int x = ThreadIds.X;

                //float2 fragcoord = new float2(x, y);
                //float2 resolution = new float2(width, height);
                //float2 uv = fragcoord / resolution;

                // Improve index calculation and reduce redundant operations
                int magFreqIndex = (int)(x * magnitudeBuffer.Length);
                float magnitude = magnitudeBuffer[magFreqIndex];
                float frequency = frequencyBuffer[magFreqIndex];

                // Smoother and more visually appealing wave function
                float wave = 0.5f + 0.5f * Hlsl.Sin(frequency + magnitude * magFreqIndex);

                // Enhance color calculation for more vibrant output
                float r = Hlsl.SmoothStep(0.0f, 1.0f, wave);
                float g = Hlsl.SmoothStep(0.3f, 1.0f, wave);
                float b = Hlsl.SmoothStep(0.6f, 1.0f, wave);

                ledColors[x + 0] = r;
                ledColors[x + 1] = g;
                ledColors[x + 2] = b;
            }
        }

        [AutoConstructor]
        [EmbeddedBytecode(DispatchAxis.XY)]
        public readonly partial struct FFTGlow : IComputeShader
        {
            public readonly ReadWriteBuffer<float> LEDColours;
            public readonly ReadOnlyBuffer<float> magnitudeBuffer;
            public readonly ReadOnlyBuffer<float> frequencyBuffer;
            public readonly int width;
            public readonly int height;
            public readonly float force;


            public void Execute()
            {
                int x = ThreadIds.X;
                int y = ThreadIds.Y;
                float2 fragcoord = new float2(x, y);
                float2 resolution = new float2(width, height);

                // Normalise pixel coordinates from 0 to 1
                float2 uv = fragcoord / resolution;

                // Get the corresponding frequency and magnitude
                int index = (int)(uv.X * magnitudeBuffer.Length);
                float magnitude = magnitudeBuffer[index];
                float frequency = frequencyBuffer[index];

                float fft = Hlsl.Pow(magnitude, 4.0f);
                fft *= 9.0f * Hlsl.Pow(force / 5.5f, 3.0f);

                float3 col = new float3(1.0f, 1.0f, 1.0f) * Hlsl.Abs(fft);
                col *= hat(2.0f * uv.Y, 1.0f);

                float3 low = new float3(1.0f, 0.0f, 0.0f);
                float3 mid = new float3(0.0f, 1.0f, 0.0f);
                float3 high = new float3(0.0f, 0.0f, 1.0f);

                col *= mix_colors(low, mid, high, uv.X * uv.X);

                int outputIndex = (y * width + x) * 3;
                LEDColours[outputIndex + 0] = col.X; //r
                LEDColours[outputIndex + 1] = col.Y; //g
                LEDColours[outputIndex + 2] = col.Z; //b

            }

            private float hat(float x, float c)
            {
                return Hlsl.Max(1.0f - Hlsl.Abs(x - c), 0.0f);
            }

            private float3 mix_colors(float3 c0, float3 c1, float3 c2, float f)
            {
                return c0 * hat(f, 0.0f) + c1 * hat(2.0f * f, 1.0f) + c2 * hat(f, 1.0f);
            }
        }

        [AutoConstructor]
        [EmbeddedBytecode(DispatchAxis.X)]
        public readonly partial struct PhantomShader : IComputeShader
        {
            public readonly ReadWriteBuffer<float> LEDColours;
            public readonly int TotalLeds;

            public void Execute()
            {



            }

            // 2 x 2 Rotation Matrix
            private float2x2 Rotate(float a)
            {
                float c = Hlsl.Cos(a);
                float s = Hlsl.Sin(a);
                return new float2x2(c, s, -s, c);
            }

            private float2 Pmod(float2 p, float r)
            {
                float a = Hlsl.Atan2(p.X, p.Y) + float.Pi / r;
                float n = float.Pi * 2.0f / r;
                a = (float)(Math.Floor(a / n) * n);

                return Hlsl.Mul(p, Rotate(-a));
            }

            private float Box(float3 p)
            {
                float3 d = Hlsl.Abs(p) - 1.0f;
                return Hlsl.Min(Hlsl.Max(d.X, Hlsl.Max(d.Y, d.Z)), 0.0f) + Hlsl.Length(Hlsl.Max(d, 0.0f));
            }

            private float IfsBox(float3 p, float time)
            {
                for (int i = 0; i < 5; i++)
                {
                    p = Hlsl.Abs(p) - 1.0f;
                    p.XY = Hlsl.Mul(p.XY, Rotate(time * 0.3f));
                    p.XZ = Hlsl.Mul(p.XZ, Rotate(time * 0.1f));
                }
                p.XZ = Hlsl.Mul(p.XZ, Rotate(time));
                return Box(p);
            }

            private float Map(float3 p, float time)
            {
                float3 p1 = p;
                p1.X = Mod(p1.X - 5.0f, 10.0f) - 5.0f;
                p1.Y = Mod(p1.Y - 5.0f, 10.0f) - 5.0f;
                p1.Z = Mod(p1.Z, 16.0f) - 8.0f;
                p1.XY = Pmod(p1.XY, 5.0f);
                return IfsBox(p1, time);
            }

            private static float Mod(float a, float b)
            {
                return a - b * Hlsl.Floor(a / b);
            }

        }

        
    }
}
