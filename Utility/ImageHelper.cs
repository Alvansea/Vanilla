using System;
using System.Collections.Generic;

using System.Drawing;
using System.Drawing.Imaging;

namespace Vanilla.Utility
{
    public class ImageHelper
    {
        public static Bitmap CreateThumbnail(Bitmap source, int maxWidth, int maxHeight, bool maintainAspect)
        {
            Bitmap thumbnail;
            Rectangle destRect = new Rectangle(0, 0, 0, 0);

            if (maintainAspect)
            {
                if (source.Width <= maxWidth && source.Height <= maxHeight)
                {
                    return source;
                }
                if (source.Width * maxHeight > source.Height * maxWidth)
                {
                    destRect.Width = maxWidth;
                    destRect.Height = (int)(source.Height * maxWidth / source.Width);
                }
                else
                {
                    destRect.Width = (int)(source.Width * maxHeight / source.Height);
                    destRect.Height = maxHeight;
                }
                thumbnail = new Bitmap(destRect.Width, destRect.Height);
            }
            else
            {
                if (source.Width > maxWidth && source.Height > maxHeight)
                {
                    if (source.Width * maxHeight > source.Height * maxWidth)
                    {
                        destRect.Width = (int)(maxHeight * source.Width / source.Height);
                        destRect.Height = maxHeight;
                        destRect.X = (int)((maxWidth - destRect.Width) / 2);
                    }
                    else
                    {
                        destRect.Width = maxWidth;
                        destRect.Height = (int)(maxWidth * source.Height / source.Width);
                        destRect.Y = (int)((maxHeight - destRect.Height) / 2);
                    }
                }
                else
                {
                    destRect.X = (int)((maxWidth - source.Width) / 2);
                    destRect.Y = (int)((maxHeight - source.Height) / 2);
                    destRect.Width = source.Width;
                    destRect.Height = source.Height;
                }
                thumbnail = new Bitmap(maxWidth, maxHeight);
            }
            
            try
            {
                using (Graphics g = Graphics.FromImage(thumbnail))
                {
                    g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
                    g.FillRectangle(Brushes.White, 0, 0, thumbnail.Width, thumbnail.Height);
                    g.DrawImage(source, destRect);
                }
            }
            catch
            {
                return null;
            }

            return thumbnail;
        }

        public static byte[] SaveAsJpeg(Bitmap bmp, int quality)
        {
            EncoderParameters encoderParams = new EncoderParameters();
            long[] qualities = { quality };
            EncoderParameter encoderParam = new EncoderParameter(Encoder.Quality, qualities);
            encoderParams.Param[0] = encoderParam;

            ImageCodecInfo[] arrayICI = ImageCodecInfo.GetImageEncoders();
            ImageCodecInfo jpegICI = null;
            for (int i = 0; i < arrayICI.Length; i++)
            {
                if (arrayICI[i].FormatDescription.Equals("JPEG"))
                {
                    jpegICI = arrayICI[i];
                    break;
                }
            }

            byte[] buffer;
            using (System.IO.MemoryStream s = new System.IO.MemoryStream())
            {
                bmp.Save(s, jpegICI, encoderParams);
                buffer = new byte[s.Length];
                s.Position = 0;
                s.Read(buffer, 0, buffer.Length);
            }
            return buffer;
        }
    }
}
