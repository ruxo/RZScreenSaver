using System;
using System.Diagnostics;

namespace RZScreenSaver.Graphics.ColorSpaces{
    /// <summary>
    /// HSL color space with alpha channel.
    /// </summary>
    public class Hsla32 : ColorSpace<float>{
        public const int Dpp = 4;
        public const int Hue = 0, Saturation = 1, Lightness = 2, Alpha = 3;
        #region ctors
        public Hsla32(int width, int height) : base(width, height, Dpp){}
        public static Hsla32 FromPbra32(byte[] data, int width){
            Debug.Assert(data.Length % width == 0);

            const int red = Rgba32.Red, green = Rgba32.Green, blue = Rgba32.Blue;

            var height = data.Length / width / Rgba32.Dpp;
            var result = new Hsla32(width, height);
            var hsl = result.Data;
            Debug.Assert(hsl.Length == data.Length);

            for (int pixel = 0; pixel < data.Length; pixel += Dpp)
            {
                var realRed = (float)data[pixel + red] / byte.MaxValue;
                var realGreen = (float)data[pixel + green] / byte.MaxValue;
                var realBlue = (float)data[pixel + blue] / byte.MaxValue;

                var minColorness = Math.Min(Math.Min(realRed, realGreen), realBlue);
                var maxColorness = Math.Max(Math.Max(realRed, realGreen), realBlue);
                var diffColorness = maxColorness - minColorness;

                hsl[pixel + Alpha] = (float) data[pixel + Rgba32.Alpha]/byte.MaxValue;

                var light = (maxColorness + minColorness) / 2;
                hsl[pixel + Lightness] = light;

                if (Math.Abs(diffColorness) < 1e-4F){
                    //This is a gray, no chroma...
                    hsl[pixel + Hue] = hsl[pixel + Saturation] = 0;
                }else{
                    //Chromatic data...
                    hsl[pixel + Saturation] = light < 0.5? diffColorness/(maxColorness + minColorness) : diffColorness/(2 - maxColorness - minColorness);

                    var del_R = ((maxColorness - realRed) / 6 + diffColorness / 2) / diffColorness;
                    var del_G = ((maxColorness - realGreen) / 6 + diffColorness / 2) / diffColorness;
                    var del_B = ((maxColorness - realBlue) / 6 + diffColorness / 2) / diffColorness;

                    if (realRed == maxColorness)
                        hsl[pixel + Hue] = del_B - del_G;
                    else if (realGreen == maxColorness)
                        hsl[pixel + Hue] = 1.0F / 3 + del_R - del_B;
                    else if (realBlue == maxColorness)
                        hsl[pixel + Hue] = 2.0F / 3 + del_G - del_R;

                    if (hsl[pixel + Hue] < 0)
                        hsl[pixel + Hue] += 1.0F;
                    if (hsl[pixel + Hue] > 1)
                        hsl[pixel + Hue] -= 1.0F;
                }
            }
            return result;
        }
        #endregion
        public Hsla32 Saturate(Func<float,float> sfunc){
#if DEBUG
            var watch = Stopwatch.StartNew();
#endif
            for(int pixel=0; pixel < Data.Length; pixel += Dpp){
                var value = sfunc(Data[pixel + Saturation]);
                if (value > 1F)
                    value = 1F;
                else if (value < 0)
                    value = 0;
                Data[pixel + Saturation] = value;
            }
#if DEBUG
            Debug.WriteLine("Saturate took " + watch.Elapsed.TotalMilliseconds + " ms.");
#endif
            return this;
        }
        const float Zero = 1e-4F;
        public Hsla32 Desaturate(float percent, float cutoff){
            Debug.Assert(percent > 0);
            for(int pixel=0; pixel < Data.Length; pixel += Dpp){
                var value = Data[pixel + Saturation] * percent;
                if (value < cutoff)
                    value = 0;
                else if (value > 1F)
                    value = 1F;
                Data[pixel + Saturation] = value;
            }
            return this;
        }
    }
}