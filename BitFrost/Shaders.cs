using ComputeSharp;
using Microsoft.AspNetCore.Routing.Constraints;
using Microsoft.Extensions.Hosting;
using System.Numerics;
using System.Security;
using System.Security.Cryptography.X509Certificates;
using TerraFX.Interop.DirectX;
using TerraFX.Interop.Windows;

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
        public readonly partial struct Robocop : IComputeShader
        {
            public readonly ReadWriteBuffer<float> LEDColours;
            public readonly ReadOnlyBuffer<float> magnitudeBuffer;
            public readonly ReadOnlyBuffer<float> frequencyBuffer;
            public readonly int width;
            public readonly int time;
            
            public void Execute()
            {
                int x = ThreadIds.X;

                // Aggregate the magnitude values and count the non-zero entries
                float magnitude = 0.0f;
                int magCounter = 0;

                for (int i = 0; i < magnitudeBuffer.Length; i++)
                {
                    if (magnitudeBuffer[i] > 0)
                    {
                        magnitude += magnitudeBuffer[i];
                        magCounter++;
                    }
                }

                // Calculate the average magnitude and use it to determine the color ratio
                float ratio = (magCounter > 0) ? Hlsl.Lerp(0.0f, 1.0f, magnitude / magCounter) * 180.0f : 0.0f;

                // Calculate the light position based on time
                float lightPosition = Hlsl.Fmod(time / 1000.0f, 2.0f * width) - width;

                // Calculate the distance of the current pixel from the light position
                float distance = Hlsl.Abs(x - lightPosition);

                // Set the LED color with intensity decreasing with distance
                float intensity = Hlsl.Exp(-distance / 10.0f) * ratio;

                LEDColours[x * 3 + 0] = intensity;
                LEDColours[x * 3 + 1] = intensity;
                LEDColours[x * 3 + 2] = intensity;

            }
        }



        [AutoConstructor]
        [EmbeddedBytecode(DispatchAxis.X)]
        public readonly partial struct BeatFlashRed : IComputeShader
        {
            public readonly ReadWriteBuffer<float> LEDColours;
            public readonly ReadOnlyBuffer<float> magnitudeBuffer;
            public readonly ReadOnlyBuffer<float> frequencyBuffer;
            public readonly int width;

            public void Execute()
            {
                int x = ThreadIds.X;

                // Determine the bar height for each magnitude index
                int index = x * (width / (magnitudeBuffer.Length / 2));
                float magnitude = magnitudeBuffer[index];

                // Set the bar length based on the magnitude
                float barLength = Hlsl.Lerp(0.0f, (float)width, magnitude);

                // Set the LED color based on the bar length
                LEDColours[x * 3 + 0] = (x < barLength) ? 1.0f : 0.0f; // Red
                LEDColours[x * 3 + 1] = 0.0f; // Green
                LEDColours[x * 3 + 2] = 0.0f; // Blue

            }
        }



        [AutoConstructor]
        [EmbeddedBytecode(DispatchAxis.XY)]
        public readonly partial struct Truchet : IComputeShader
        {
            public readonly ReadWriteBuffer<float> LEDColours;
            public readonly float time;
            public readonly int width;
            public readonly int height;

            float HeightMap(float2 p)
            {
                p *= 3;

                // Hexagonal coordinates
                float2 h = new float2(p.X + p.Y * 0.57735f, p.Y * 1.1547f);

                // Closest hexagon center
                float2 fh = Hlsl.Floor(h);
                float2 f = h - fh; h = fh;
                float c = Hlsl.Frac((h.X + h.Y) / 3);
                h = c<0.666f ? c < 0.333f ? h : h + 1 : h + Hlsl.Step(f.YX, f);

                p -= new float2(h.X = h.Y * 0.5f, h.Y * 0.8660254f);

                // Rotate (flip, in this case) random hexagons. Otherwise, you'd have a bunch of circles only.
                // Note that "h" is unique to each hexagon, so we can use it as the random ID.
                c = Hlsl.Frac(Hlsl.Cos(Hlsl.Dot(h, new float2(41f, 289f))) * 43758.5453f);
                p -= p * Hlsl.Step(c, 0.5f) * 2f;

                // Minimum squared distance to neighbors. Taking the square root after comparing, for speed.
                // Three partitions need to be checked due to the flipping process.
                p -= new float2(-1f, 0f);
                c = Hlsl.Dot(p, p); // Reusing "c" again.
                p -= new float2(1.5f, 0.8660254f);
                c = Hlsl.Min(c, Hlsl.Dot(p, p));
                p -= new float2(0f, -1.73205f);
                c = Hlsl.Min(c, Hlsl.Dot(p, p));

                return Hlsl.Sqrt(c);
            }

            float Map(float3 p)
            {
                float c = HeightMap(p.XY); // Height map.
                // Wrapping, or folding the height map values over, to produce the nicely lined-up, wavy patterns.
                c = Hlsl.Cos(c * 6.2831589f) + Hlsl.Cos(c * 6.2831589f * 2.0f);
                c = (Hlsl.Clamp(c * 0.6f + 0.5f, 0.0f, 1.0f));


                // Back plane, placed at vec3(0., 0., 1.), with plane normal vec3(0., 0., -1).
                // Adding some height to the plane from the heightmap.
                return 1.0f - p.Z - c * .025f;
            }

            // The normal function with some edge detection and curvature rolled into it. Sometimes, it's possible to 
            // get away with six taps, but we need a bit of epsilon value variance here, so there's an extra six.
            float3 GetNormal(float3 p, ref float edge, ref float crv)
            {

                float2 e = new float2(0.01f, 0f); // Larger epsilon for greater sample spread, thus thicker edges.

                // Take some distance function measurements from either side of the hit point on all three axes.
                float d1 = Map(p + e.XYY), d2 = Map(p - e.XYY);
                float d3 = Map(p + e.YXY), d4 = Map(p - e.YXY);
                float d5 = Map(p + e.YYX), d6 = Map(p - e.YYX);
                float d = Map(p) * 2.0f;  // The hit point itself - Doubled to cut down on calculations. See below.

                // Edges - Take a geometry measurement from either side of the hit point. Average them, then see how
                // much the value differs from the hit point itself. Do this for X, Y and Z directions. Here, the sum
                // is used for the overall difference, but there are other ways. Note that it's mainly sharp surface 
                // curves that register a discernible difference.
                edge = Hlsl.Abs(d1 + d2 - d) + Hlsl.Abs(d3 + d4 - d) + Hlsl.Abs(d5 + d6 - d);
                //edge = max(max(abs(d1 + d2 - d), abs(d3 + d4 - d)), abs(d5 + d6 - d)); // Etc.

                // Once you have an edge value, it needs to normalized, and smoothed if possible. How you 
                // do that is up to you. This is what I came up with for now, but I might tweak it later.
                edge = Hlsl.SmoothStep(0.0f, 1.0f, Hlsl.Sqrt(edge / e.X * 2.0f));

                // We may as well use the six measurements to obtain a rough curvature value while we're at it.
                crv = Hlsl.Clamp((d1 + d2 + d3 + d4 + d5 + d6 - d * 3.0f) * 32.0f + 0.6f, 0.0f, 1.0f);

                // Redoing the calculations for the normal with a more precise epsilon value.
                e = new float2(0.0025f, 0.0f);
                d1 = Map(p + e.XYY); d2 = Map(p - e.XYY);
                d3 = Map(p + e.YXY); d4 = Map(p - e.YXY);
                d5 = Map(p + e.YYX); d6 = Map(p - e.YYX);


                // Return the normal.
                // Standard, normalized gradient mearsurement.
                return Hlsl.Normalize(new float3(d1 - d2, d3 - d4, d5 - d6));
            }

            // I keep a collection of occlusion routines... OK, that sounded really nerdy. :)
            // Anyway, I like this one. I'm assuming it's based on IQ's original.
            float CalculateAO(float3 p, float3 n)
            {
                float sca = 2.0f, occ = 0.0f;
                for (float i = 0.0f; i < 5.0f; i++)
                {
                    float hr = 0.01f + i * 0.5f / 4.0f;
                    float dd = Map(n * hr + p);
                    occ += (hr - dd) * sca;
                    sca *= 0.7f;
                }
                return Hlsl.Clamp(1.0f - occ, 0.0f, 1.0f);
            }

            // Compact, self-contained version of IQ's 3D value noise function.
            float N3D(float3 p)
            {
                float3 s = new float3(7f, 157f, 113f);
                float3 ip = Hlsl.Floor(p); p -= ip;
                float4 h = new float4(0.0f, s.Y, s.Z, s.Y + s.Z + Hlsl.Dot(ip, s));
                p = p * p * (3.0f - 2.0f * p); //p *= p*p*(p*(p * 6. - 15.) + 10.);
                h = Hlsl.Lerp(Hlsl.Frac(Hlsl.Sin(Hlsl.Fmod(h, 6.2831589f)) * 43758.5453f),
                        Hlsl.Frac(Hlsl.Sin(Hlsl.Fmod(h + s.X, 6.2831589f)) * 43758.5453f), p.X);
                h.XY = Hlsl.Lerp(h.XZ, h.YW, p.Y);
                return Hlsl.Lerp(h.X, h.Y, p.Z); // Range: [0, 1].
            }

            // Simple environment mapping. Pass the reflected vector in and create some
            // colored noise with it. The normal is redundant here, but it can be used
            // to pass into a 3D texture mapping function to produce some interesting
            // environmental reflections.
            float3 EnvMap(float3 rd, float3 sn)
            {

                float3 sRd = rd; // Save rd, just for some mixing at the end.

                // Add a time component, scale, then pass into the noise function.
                rd.XY -= time * 0.25f;
                rd *= 3.0f;

                float c = N3D(rd) * 0.57f + N3D(rd * 2.0f) * 0.28f + N3D(rd * 4.0f) * 0.15f; // Noise value.
                c = Hlsl.SmoothStep(0.4f, 1.0f, c); // Darken and add contast for more of a spotlight look.

                float3 col = new float3(c, c * c, c * c * c * c); // Simple, warm coloring.
                                                          //vec3 col = vec3(min(c*1.5, 1.), pow(c, 2.5), pow(c, 12.)); // More color.

                // Mix in some more red to tone it down and return.
                return Hlsl.Lerp(col, col.YZX, sRd * 0.25f + 0.25f);

            }

            // vec2 to vec2 hash.
            float2 hash22(float2 p)
            {

                // Faster, but doesn't disperse things quite as nicely as other combinations. :)
                float n = Hlsl.Sin(Hlsl.Fmod(Hlsl.Dot(p, new float2(41, 289)), 6.2831589f));
                return Hlsl.Frac(new float2(262144, 32768) * n) * 0.75f + 0.25f;

                // Animated.
                //p = fract(vec2(262144, 32768)*n); 
                //return sin( p*6.2831853 + iTime )*.35 + .65; 

            }

            // 2D 2nd-order Voronoi: Obviously, this is just a rehash of IQ's original. I've tidied
            // up those if-statements. Since there's less writing, it should go faster. That's how 
            // it works, right? :)
            //
            float Voronoi(float2 p)
            {

                float2 g = Hlsl.Floor(p), o; p -= g;

                float3 d = new float3(1f, 1f, 1f); // 1.4, etc. "d.z" holds the distance comparison value.

                for (int y = -1; y <= 1; y++)
                {
                    for (int x = -1; x <= 1; x++)
                    {

                        o = new float2(x, y);
                        o += hash22(g + o) - p;

                        d.Z = Hlsl.Dot(o, o);
                        // More distance metrics.
                        //o = abs(o);
                        //d.z = max(o.x*.8666 + o.y*.5, o.y);// 
                        //d.z = max(o.x, o.y);
                        //d.z = (o.x*.7 + o.y*.7);

                        d.Y = Hlsl.Max(d.X, Hlsl.Min(d.Y, d.Z));
                        d.X = Hlsl.Min(d.X, d.Z);

                    }
                }

                return Hlsl.Max(d.Y / 1.2f - d.X * 1.0f, 0.0f) / 1.2f;
                //return d.y - d.x; // return 1.-d.x; // etc.

            }


            public void Execute()
            {
                // Unit directional ray - Coyote's observation.
                float3 rd = Hlsl.Normalize(new float3(new float2(2.0f, 2.0f) * ThreadIds.XY - new float2(width, height), height));

                float tm = time / 2.0f;

                // Rotate the XY-plane back and forth. Note that sine and cosine are kind of rolled into one.
                float2 a = Hlsl.Sin(new float2(1.570796f, 0) + Hlsl.Sin(tm / 4.0f) * 0.3f); // Fabrice's observation.
                rd.XY = new float2x2(a.X, a.Y, -a.Y, a.X) * rd.XY;


                // Ray origin. Moving in the X-direction to the right.
                float3 ro = new float3(tm, Hlsl.Cos(tm / 4.0f), 0.0f) ;


                // Light position, hovering around behind the camera.
                float3 lp = ro + new float3(Hlsl.Cos(tm / 2.0f) * 0.5f, Hlsl.Sin(tm / 2.0f) * 0.5f, -0.5f);

                // Standard raymarching segment. Because of the straight forward setup, not many iterations are necessary.
                float d, t = 0.0f;
                for (int j = 0; j < 32; j++)
                {

                    d = Map(ro + rd * t); // distance to the function.
                    t += d * 0.7f; // Total distance from the camera to the surface.

                    // The plane "is" the far plane, so no "far = plane" break is needed.
                    if (d < 0.001) break;

                }

                // Edge and curve value. Passed into, and set, during the normal calculation.
                float edge = 0.0f, crv = 0.0f;

                // Surface postion, surface normal and light direction.
                float3 sp = ro + rd * t;
                float3 sn = GetNormal(sp, ref edge, ref crv);
                float3 ld = lp - sp;



                // Coloring and texturing the surface.
                //
                // Height map.
                float c = HeightMap(sp.XY);

                // Folding, or wrapping, the values above to produce the snake-like pattern that lines up with the randomly
                // flipped hex cells produced by the height map.
                float3 fold = Hlsl.Cos(new float3(1, 2, 4) * c * 6.2831589f);

                // Using the height map value, then wrapping it, to produce a finer grain Truchet pattern for the overlay.
                float c2 = HeightMap((sp.XY + sp.Z * .025f) * 6.0f);
                c2 = Hlsl.Cos(c2 * 6.2831589f * 3.0f);
                c2 = Hlsl.Clamp(c2 + 0.5f, 0.0f, 1.0f);


                // Function based bump mapping. I prefer none in this example, but it's there if you want it.   
                //if(temp.x>0. || temp.y>0.) sn = dbF(sp, sn, .001);

                // Surface color value.
                float3 oC = new float3(1, 1, 1);

                if (fold.X > 0.0f) oC = new float3(1, 0.05f, 0.1f) * c2; // Reddish pink with finer grained Truchet overlay.

                if (fold.X < 0.05 && (fold.Y) < 0.0f) oC = new float3(1, 0.7f, 0.45f) * (c2 * 0.25f + 0.75f); // Lighter lined borders.
                else if (fold.X < 0.0f) oC = new float3(1, 0.8f, 0.4f) * c2; // Gold, with overlay.

                //oC *= n3D(sp*128.)*.35 + .65; // Extra fine grained noisy texturing.


                // Sending some greenish particle pulses through the snake-like patterns. With all the shininess going 
                // on, this effect is a little on the subtle side.
                float p1 = 1.0f - Hlsl.SmoothStep(0.0f, 0.1f, fold.X * 0.5f + 0.5f); // Restrict to the snake-like path.
                // Other path.
                //float p2 = 1.0 - smoothstep(0., .1, cos(heightMap(sp.xy + 1. + iTime/4.)*6.283)*.5+.5);
                float p2 = 1.0f - Hlsl.SmoothStep(0.0f, 0.1f, Voronoi(sp.XY * 4.0f + new float2(tm, Hlsl.Cos(tm / 4.0f))));
                p1 = (p2 + 0.25f) * p1; // Overlap the paths.
                oC += oC.YXZ * p1 * p1; // Gives a kind of electron effect. Works better with just Voronoi, but it'll do.




                float lDist = Hlsl.Max(Hlsl.Length(ld), 0.001f); // Light distance.
                float atten = 1.0f / (1.0f + lDist * 0.125f); // Light attenuation.

                ld /= lDist; // Normalizing the light direction vector.

                float diff = Hlsl.Max(Hlsl.Dot(ld, sn), 0.0f); // Diffuse.
                float spec = Hlsl.Pow(Hlsl.Max(Hlsl.Dot(Hlsl.Reflect(-ld, sn), -rd), 0.0f), 16.0f); // Specular.                
                float fre = Hlsl.Pow(Hlsl.Clamp(Hlsl.Dot(sn, rd) + 1.0f, 0.0f, 1.0f), 3.0f); // Fresnel, for some mild glow.

                // Shading. Note, there are no actual shadows. The camera is front on, so the following
                // two functions are enough to give a shadowy appearance.
                crv = crv * 0.9f + 0.1f; // Curvature value, to darken the crevices.
                float ao = CalculateAO(sp, sn); // Ambient occlusion, for self shadowing.



                // Combining the terms above to light the texel.
                float3 col = oC * (diff + 0.5f) + new float3(1.0f, 0.7f, 0.4f) * spec * 2.0f + new float3(0.4f, 0.7f, 1) * fre;

                col += (oC * 0.5f + 0.5f) * EnvMap(Hlsl.Reflect(rd, sn), sn) * 6.0f; // Fake environment mapping.


                // Edges.
                col *= 1.0f - edge * 0.85f; // Darker edges.   

                // Applying the shades.
                col *= (atten * crv * ao);


                // Rough gamma correction, then present to the screen.
                float4 Colour = new float4(Hlsl.Sqrt(Hlsl.Clamp(col, 0.0f, 1.0f)), 1.0f);

                LEDColours[(ThreadIds.Y * width + ThreadIds.X) * 3 + 0] = Colour.X; // Red
                LEDColours[(ThreadIds.Y * width + ThreadIds.X) * 3 + 0] = Colour.Y; // Green
                LEDColours[(ThreadIds.Y * width + ThreadIds.X) * 3 + 0] = Colour.Z; // Blue
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
