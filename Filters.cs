using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KGlab1
{
    abstract class Filters
    {
        protected abstract Color calculateNewPixelColor(Bitmap sourceImage, int x, int y);
        public Bitmap processImage(Bitmap sourceImage, BackgroundWorker worker)
        {
            Bitmap resultImage = new Bitmap(sourceImage.Width, sourceImage.Height);
            for (int i = 0; i < sourceImage.Width; i++)
            {
                worker.ReportProgress((int)((float)i / resultImage.Width * 100));
                if (worker.CancellationPending)
                    return null;
                for (int j = 0; j < sourceImage.Height; j++)
                {
                    resultImage.SetPixel(i, j, calculateNewPixelColor(sourceImage, i, j));
                }
            }

            return resultImage;
        }
        public int Clamp(int value, int min, int max)
        {
            if (value < min)
                return min;
            if (value > max)
                return max;
            return value;
        }
        public class InvertFilter : Filters
        {
            protected override Color calculateNewPixelColor(Bitmap sourceImage, int x, int y)
            {
                Color sourceColor = sourceImage.GetPixel(x, y);
                Color resultColor = Color.FromArgb(255 - sourceColor.R,
                                                   255 - sourceColor.G,
                                                   255 - sourceColor.B);
                return resultColor;
            }
        }

        public class GrayScale : Filters
        {
            protected override Color calculateNewPixelColor(Bitmap sourceImage, int x, int y)
            {
                Color sourceColor = sourceImage.GetPixel(x, y);
                double Intensity = 0.299 * sourceColor.R + 0.587 * sourceColor.G + 0.114 * sourceColor.B;
                int IntIntensity = (int)Intensity;
                Color resultColor = Color.FromArgb(IntIntensity, IntIntensity, IntIntensity);
                return resultColor;
            }
        }

        public class Sepiy : Filters
        {
            protected override Color calculateNewPixelColor(Bitmap sourceImage, int x, int y)
            {
                Color sourceColor = sourceImage.GetPixel(x, y);
                double Intensity = 0.299 * sourceColor.R + 0.587 * sourceColor.G + 0.114 * sourceColor.B;
                int k = 20;
                int IntIntensity = (int)Intensity;
                int resultR = Clamp(IntIntensity + 2 * k, 0, 255);
                int resultG = Clamp((int)(IntIntensity + 0.5 * k), 0, 255);
                int resultB = Clamp(IntIntensity - 1 * k, 0, 255);
                Color resultColor = Color.FromArgb(resultR, resultG, resultB);
                return resultColor;
            }
        }
        public class Yrkost : Filters
        {
            protected override Color calculateNewPixelColor(Bitmap sourceImage, int x, int y)
            {
                Color sourceColor = sourceImage.GetPixel(x, y);
                int k = 30;
                int resultR = Clamp(sourceColor.R + k, 0, 255);
                int resultG = Clamp(sourceColor.G + k, 0, 255);
                int resultB = Clamp(sourceColor.B + k, 0, 255);
                Color resultColor = Color.FromArgb(resultR, resultG, resultB);
                return resultColor;
            }
        }
        public class MatrixFilter : Filters
        {
            protected float[,] kernel = null;
            protected MatrixFilter() { }
            public MatrixFilter(float[,] kernel)
            {
                this.kernel = kernel;
            }
            protected override Color calculateNewPixelColor(Bitmap sourceImage, int x, int y)
            {
                int radiusX = kernel.GetLength(0) / 2;
                int radiusY = kernel.GetLength(1) / 2;
                float resultR = 0;
                float resultG = 0;
                float resultB = 0;
                for (int l = -radiusY; l <= radiusY; l++)
                    for(int k = -radiusX; k <= radiusX; k++)
                    {
                        int idX = Clamp(x + k, 0, sourceImage.Width - 1);
                        int idY = Clamp(y + l, 0, sourceImage.Height - 1);
                        Color neighborColor = sourceImage.GetPixel(idX, idY);
                        resultR += neighborColor.R * kernel[k + radiusX, l + radiusY];
                        resultG += neighborColor.G * kernel[k + radiusX, l + radiusY];
                        resultB += neighborColor.B * kernel[k + radiusX, l + radiusY];
                    }
                return Color.FromArgb(
                    Clamp((int)resultR, 0, 255),
                    Clamp((int)resultG, 0, 255),
                    Clamp((int)resultB, 0, 255));
            }
            public class BlurFilter : MatrixFilter
            {
                public BlurFilter()
                {
                    int sizeX = 3;
                    int sizeY = 3;
                    kernel = new float[sizeX, sizeY];
                    for (int i = 0; i < sizeX; i++)
                        for (int j = 0; j < sizeY; j++)
                            kernel[i, j] = 1.0f / (float)(sizeX * sizeY);
                  
                }
            }
            public class GaussianFilter : MatrixFilter
            {
                public void createGaussianKernel(int radius, float sigma)
                {
                    int size = 2 * radius + 1;
                    kernel = new float[size, size];
                    float norm = 0;
                    for (int i = -radius; i <= radius; i++)
                        for(int j = -radius; j <= radius; j++)
                        {
                            kernel[i + radius, j + radius] = (float)(Math.Exp(-(i * i + j * j) / (2 * sigma * sigma)));
                            norm += kernel[i + radius, j + radius];
                        }
                    for (int i = 0; i < size; i++)
                        for (int j = 0; j < size; j++)
                            kernel[i, j] /= norm;
                }
            public GaussianFilter()
                {
                    createGaussianKernel(3, 2);
                }
            }

            public class SobelFilter : MatrixFilter
            {
                public SobelFilter()
                {
                    int sizeX = 3;
                    int sizeY = 3;
                    float[,] Gx = new float[3,3];
                    float[,] Gy = new float[3,3];
                    Gx[0,0] = -1; Gx[1,0] = 0; Gx[2,0] = 1;
                    Gx[0,1] = -2; Gx[1,1] = 0; Gx[2,1] = 2;
                    Gx[0,2] = -1; Gx[1,2] = 0; Gx[2,2] = 1;
                    Gy[0,0] = -1; Gy[1,0] = -2; Gy[2,0] = -1;
                    Gy[0,1] =  0; Gy[1,1] =  0; Gy[2,1] =  0;
                    Gy[0,2] =  1; Gy[1,2] =  2; Gy[2,2] =  1;                   

                    kernel = new float[sizeX, sizeY];
                    for (int i = 0; i < sizeX; i++)
                        for (int j = 0; j < sizeY; j++)
                            kernel[i, j] = (Gx[i,j] + Gy[i,j]);
                                
                            
                }
            }
            public class RezkostFilter : MatrixFilter
            {
                public RezkostFilter()
                {
                    int sizeX = 3;
                    int sizeY = 3;
                    float[,] Gx = new float[3, 3];
                    Gx[0, 0] = -1; Gx[1, 0] = -1; Gx[2, 0] = -1;
                    Gx[0, 1] = -1; Gx[1, 1] = 9; Gx[2, 1] = -1;
                    Gx[0, 2] = -1; Gx[1, 2] = -1; Gx[2, 2] = -1;
                    kernel = new float[sizeX, sizeY];
                    for (int i = 0; i < sizeX; i++)
                        for (int j = 0; j < sizeY; j++)
                            kernel[i, j] = Gx[i, j];


                }
            }
            public class TesnenieFilter : MatrixFilter
            {
                public TesnenieFilter()
                {
                    int sizeX = 3;
                    int sizeY = 3;
                    float[,] Gx = new float[3, 3];
                    Gx[0, 0] = 0; Gx[1, 0] = 1; Gx[2, 0] = 0;
                    Gx[0, 1] = 1; Gx[1, 1] = 0; Gx[2, 1] = -1;
                    Gx[0, 2] = 0; Gx[1, 2] = -1; Gx[2, 2] = 0;
                    kernel = new float[sizeX, sizeY];
                    for (int i = 0; i < sizeX; i++)
                        for (int j = 0; j < sizeY; j++)
                            kernel[i, j] = Gx[i, j];


                }
            }

        }
    }
}
