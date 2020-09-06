using System;

namespace RZScreenSaver.Graphics.ColorSpaces{
    public class Rgba32 : ColorSpace<byte>{
        /// <summary>
        /// Data per pixel
        /// </summary>
        public const int Dpp = 4;
        public const int Red = 2, Green = 1, Blue = 0, Alpha = 3;
        #region ctors
        public Rgba32(int width, int height) : base(width, height, Dpp){}
        static public Rgba32 FromHsl(Hsla32 source){
            var result = new Rgba32(source.Width, source.Height);
            var rgb = result.Data;
            var hsl = source.Data;

            for (int i = 0; i < hsl.Length; i += 4){
                rgb[i + Alpha] = (byte) (hsl[i + Hsla32.Alpha] * byte.MaxValue);

                var hue = hsl[i + Hsla32.Hue];
                var saturation = hsl[i + Hsla32.Saturation];
                var lightness = hsl[i + Hsla32.Lightness];
                if (Math.Abs(saturation) < 1e-4F){
                    // gray scale
                    rgb[i + Red] = rgb[i + Green] = rgb[i + Blue] = (byte)(lightness * byte.MaxValue);
                }else{
                    var var2 = lightness < 0.5
                               ? lightness*(1.0F + saturation)
                               : (lightness + saturation) - (saturation*lightness);
                    var var1 = 2 * lightness - var2;

                    rgb[i + Red] = (byte)(byte.MaxValue * hue2Rgb(var1, var2, hue + 1F / 3));
                    rgb[i + Green] = (byte)(byte.MaxValue * hue2Rgb(var1, var2, hue));
                    rgb[i + Blue] = (byte)(byte.MaxValue * hue2Rgb(var1, var2, hue - 1F / 3));
                }
            }
            return result;
        }
        static float hue2Rgb(float v1, float v2, float vH){
            if (vH < 0) vH += 1.0F;
            if (vH > 1) vH -= 1.0F;
            if ((6 * vH) < 1) return (v1 + (v2 - v1) * 6 * vH);
            if ((2 * vH) < 1) return (v2);
            if ((3 * vH) < 2) return (v1 + (v2 - v1) * ((float)(2.0 / 3.0) - vH) * 6);
            return (v1);
        }
        #endregion
    }
}