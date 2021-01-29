using SharpDX;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace Grafika4a
{
    public class Texture
    {
        private byte[] internalBuffer;
        private int width;
        private int height;

        public Texture(string filename, int width, int height)
        {
            this.width = width;
            this.height = height;
            Load(filename);
        }

        /*async */void Load(string filename)
        {
            BitmapImage img = new BitmapImage(new Uri(@"D:\DATA\Programming\Grafika\" + filename));

            Console.WriteLine("BPP: " + img.Format.BitsPerPixel);
            int stride = (int)img.PixelWidth * (img.Format.BitsPerPixel / 8);
            internalBuffer = new byte[(int)img.PixelHeight * stride];
            img.CopyPixels(internalBuffer, stride, 0);
        }

        public Color4 Map(float tu, float tv)
        {
            if (internalBuffer == null)
                return Color4.White;

            int u = Math.Abs((int)(tu * width) % width);//% to cycle
            int v = Math.Abs((int)(tv * height) % height);
            int pos = (u + v * width) * 4;
            byte b = internalBuffer[pos];
            byte g = internalBuffer[pos + 1];
            byte r = internalBuffer[pos + 2];
            byte a = internalBuffer[pos + 3];

            return new Color4(r / 255.0f, g / 255.0f, b / 255.0f, a / 255.0f);
        }
    }
}
