/*
 * Copyright Paul Haley, phaley@mail.com, August 2003
 */

using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Drawing.Text;
using System.IO;
using Haley.RotateText.Definition;

namespace Haley.RotateText
{
    /// <summary>
    ///     Generates inmage from a definition
    ///     Manages the caching and retrieval of the images from the permanent application cache
    ///     (Note this is not the cache used for dynamic images that are access via a guid on the return request,
    ///     only defintions that have a CacheName set are cached here)
    /// </summary>
    public sealed class ImageGenerator
    {
        private const int MinHeight = 2;
        private const int MinWidth = 2;

        /// <summary>Physical web root path. Set from Program.cs via IWebHostEnvironment.WebRootPath.</summary>
        public static string WebRootPath { get; set; } = string.Empty;

        public static Bitmap GenerateImage(RotateTextDefn definition)
        {
            int iHOffset;
            int iVOffset;
            int iRottextHeight;
            int iRottextWidth;
            int iBmpHeight;
            int iBmpWidth;
            var xAfterOffset = 0;
            var yAfterOffset = 0;

            // create a bitmap we can use to work out the size of the text, we will then create a new bitmap that is the right size
            // we also use this to record the default resolution
            var bmp = new Bitmap(1, 1, PixelFormat.Format32bppArgb);
            var fHRes = bmp.HorizontalResolution;
            var fVRes = bmp.VerticalResolution;
            var g = Graphics.FromImage(bmp);
            var format = new StringFormat {Alignment = definition.Align};
            var sf = g.MeasureString(definition.Text, definition.Font, Int32.MaxValue, format);
            g.Dispose();
            bmp.Dispose();

            // get the width and height of the text
            var textWidth = Max(MinWidth, (int) Math.Ceiling(sf.Width));
            var textHeight = Max(MinHeight, (int) Math.Ceiling(sf.Height));

            CalculateRotation(textWidth, textHeight, definition.Angle, out iHOffset, out iVOffset, out iRottextWidth,
                out iRottextHeight);

            // work out the size of the bitmap needed to fit the rotated text rectangle on
            // if we have hard defined width or height then use those instead
            if (definition.Height != 0)
                iBmpHeight = definition.Height;
            else
                iBmpHeight = iRottextHeight + definition.TextPadding.Top + definition.TextPadding.Bottom;
            if (definition.Width != 0)
                iBmpWidth = definition.Width;
            else
                iBmpWidth = iRottextWidth + definition.TextPadding.Left + definition.TextPadding.Right;


            // create new bitmap the right size and prepare the background from the file sclaing definitionition etc.
            LoadBackground(ref bmp, iBmpWidth, iBmpHeight, fHRes, fVRes, definition);
            g = Graphics.FromImage(bmp);

            // determine the offset needed to position the text in the right place 
            // in the background if it is not the same size as we calculate was needed for the text
            // adding in the padding where necessary and
            // remembering to adjust for differences in padding between left/right top/bottom etc.
            switch (definition.VerticalAlignment)
            {
                case VAlignment.Top:
                    yAfterOffset = definition.TextPadding.Top;
                    break;
                case VAlignment.Middle:
                    yAfterOffset = (bmp.Height - iRottextHeight)/2 + definition.TextPadding.Top -
                                   definition.TextPadding.Bottom;
                    break;
                case VAlignment.Bottom:
                    yAfterOffset = (bmp.Height - iRottextHeight) - definition.TextPadding.Bottom;
                    break;
            }
            switch (definition.HorizontalAlignment)
            {
                case HAlignment.Left:
                    xAfterOffset = definition.TextPadding.Left;
                    break;
                case HAlignment.Center:
                    xAfterOffset = (bmp.Width - iRottextWidth)/2 + definition.TextPadding.Left -
                                   definition.TextPadding.Right;
                    break;
                case HAlignment.Right:
                    xAfterOffset = (bmp.Width - iRottextWidth) - definition.TextPadding.Right;
                    break;
            }

            // create a new transformation matrix to do the rotation and corresponding translation
            var matrix = new Matrix();
            matrix.Translate(xAfterOffset, yAfterOffset);
                // translation to position the text as required by HAlignment and VAlignment
            matrix.Translate(iHOffset, iVOffset); // translation to bring the rotation back to view
            matrix.Rotate(definition.Angle); // transformation to rotate the text
            if (definition.Align == StringAlignment.Center) // transformation to cope with non left aligned text
                matrix.Translate(textWidth/2, 0);
            else if (definition.Align == StringAlignment.Far)
                matrix.Translate(textWidth, 0);

            // apply the transformation to the graphics object and write out the text
            g.Transform = matrix;
            g.SmoothingMode = SmoothingMode.HighQuality;
            g.TextRenderingHint = TextRenderingHint.AntiAlias;
            g.DrawString(definition.Text, definition.Font, new SolidBrush(definition.TextColor), 0, 0, format);
            g.Transform = matrix;
            g.Dispose();
            return bmp;
        }


        public static byte[] GenerateImageBytes(RotateTextDefn definition)
        {
            var bmp = GenerateImage(definition);

            // save the image to the output stream
            var ms = new MemoryStream();
            bmp.Save(ms, ImageFormat.Png);
            var bytes = ms.ToArray();
            ms.Close();

            bmp.Dispose();
            return bytes;
        }

        private static void CalculateRotation(int textWidth, int textHeight, int angle, out int horizontalOffset,
            out int verticalOffset, out int rotatedTextWidth, out int rotatedTextHeight)
        {
            // convert the rotation angle to radians
            var radians = ((double) angle/180)*Math.PI;

            // work out what the transalation offsets will need to be once the text is rotated to make sure the containning rectangle fits on completely on the image
            if (angle <= 90)
            {
                verticalOffset = 0;
                horizontalOffset = (int) Math.Abs(Math.Ceiling(textHeight*Math.Sin(radians)));
            }
            else if (angle <= 180)
            {
                horizontalOffset =
                    (int)
                        Math.Ceiling(Math.Abs(textWidth*Math.Sin(radians - (Math.PI/2))) +
                                     Math.Abs(textHeight*Math.Cos(radians - (Math.PI/2))));
                verticalOffset = (int) Math.Ceiling(Math.Abs(textHeight*Math.Sin(radians - (Math.PI/2))));
            }
            else if (angle <= 270)
            {
                verticalOffset =
                    (int)
                        Math.Ceiling(Math.Abs(textWidth*Math.Sin(radians - (Math.PI))) +
                                     Math.Abs(textHeight*Math.Cos(radians - (Math.PI))));
                horizontalOffset = (int) Math.Ceiling(Math.Abs(textWidth*Math.Cos(radians - (Math.PI))));
            }
            else
            {
                verticalOffset = (int) Math.Ceiling(Math.Abs(textWidth*Math.Cos(radians - (Math.PI*1.5))));
                horizontalOffset = 0;
            }

            // work out the size of the containing rectangle
            rotatedTextHeight =
                (int) Math.Ceiling(Math.Abs(textHeight*Math.Cos(radians)) + (Math.Abs(textWidth*Math.Sin(radians))));
            rotatedTextWidth =
                (int) Math.Ceiling(Math.Abs(textWidth*Math.Cos(radians)) + (Math.Abs(textHeight*Math.Sin(radians))));
        }

        //    Loads and processes the background image
        private static void LoadBackground(ref Bitmap bmp, int width, int height, float horizontalResolution,
            float verticalResolution, RotateTextDefn definition)
        {
            // map relative paths
            if (definition.BackgroundImageFile != null)
            {
                var imgpath = definition.BackgroundImageFile;
                if (!definition.BackgroundImageFile.Substring(1, 1).Equals(":"))
                {
                    string imgfile;
                    var pos = Max(definition.BackgroundImageFile.LastIndexOf("\\", StringComparison.Ordinal),
                        definition.BackgroundImageFile.LastIndexOf("/", StringComparison.Ordinal));
                    if (pos == -1)
                    {
                        imgfile = definition.BackgroundImageFile;
                        imgpath = ".";
                    }
                    else
                    {
                        imgfile = definition.BackgroundImageFile.Substring(pos + 1,
                            definition.BackgroundImageFile.Length - pos - 1);
                        imgpath = definition.BackgroundImageFile.Substring(0, pos);
                    }

                    imgpath = System.IO.Path.Combine(WebRootPath, imgpath.TrimStart('~', '/').Replace('/', System.IO.Path.DirectorySeparatorChar));
                    if (!imgpath.EndsWith("\\"))
                        imgpath += "\\";

                    definition.BackgroundImageFile = imgpath + imgfile;
                }
            }
            // create new bitmap the right size and prepare the background from the file sclaing definitionition etc.
            if (((definition.BackgroundImageFile != null) && (File.Exists(definition.BackgroundImageFile))) ||
                (definition.BackgroundImageBytes != null) || (definition.BackgroundImageBitmap != null))
            {
                Bitmap bmpImage;
                var bNoDispose = false;
                if (definition.BackgroundImageBitmap != null)
                {
                    bmpImage = definition.BackgroundImageBitmap;
                    bNoDispose = true;
                }
                else if (definition.BackgroundImageBytes != null)
                {
                    var ms = new MemoryStream(definition.BackgroundImageBytes);
                    bmpImage = new Bitmap(ms);
                }
                else
                {
                    bmpImage = new Bitmap(definition.BackgroundImageFile);
                }

                // reset the resolution of loaded images to match what we need
                bmpImage.SetResolution(horizontalResolution, verticalResolution);
                if (definition.UseScalingDefinition || definition.ResizeBackground || (definition.Height > 0) ||
                    (definition.Width > 0))
                {
                    ContructBasicBackground(ref bmp, width, height, definition.BackgroundColor, horizontalResolution,
                        verticalResolution);
                    if (definition.UseScalingDefinition)
                    {
                        FillBackground(bmp, bmpImage, definition.ScalingDefinition);
                    }
                    else
                    {
                        FillBackground(bmp, bmpImage, definition.ResizeBackground, definition.MaintainAspect);
                    }
                }
                else
                {
                    bmp = bmpImage;
                }
                if (!bNoDispose)
                    bmpImage.Dispose();
            }
            else
            {
                ContructBasicBackground(ref bmp, width, height, definition.BackgroundColor, horizontalResolution,
                    verticalResolution);
            }
        }

        private static void ContructBasicBackground(ref Bitmap bmp, int width, int height, Color backgroundColor,
            float horizontalResolution, float verticalResolution)
        {
            bmp = new Bitmap(width, height, PixelFormat.Format32bppArgb);
            var g = Graphics.FromImage(bmp);
            g.Clear(backgroundColor);
            bmp.SetResolution(horizontalResolution, verticalResolution);
            g.InterpolationMode = InterpolationMode.HighQualityBicubic;
        }

        private static void FillBackground(Bitmap target, Bitmap source, ScalingDefn scalingDefinition)
        {
            var g = Graphics.FromImage(target);
            // copy the center over the entire image
            var left = Max(scalingDefinition.TopLeft.Width, scalingDefinition.BottomLeft.Width);
            var right = Max(scalingDefinition.TopRight.Width, scalingDefinition.BottomRight.Width);
            var top = Max(scalingDefinition.TopLeft.Height, scalingDefinition.TopRight.Height);
            var bottom = Max(scalingDefinition.BottomLeft.Height, scalingDefinition.BottomRight.Height);
            var width = source.Width - right - left;
            var height = source.Height - bottom - top;
            g.DrawImage(source, new Rectangle(0, 0, target.Width, target.Height),
                new Rectangle(left, right, width, height), GraphicsUnit.Pixel);
            // copy the corners
            //topleft
            g.DrawImage(source, new Rectangle(0, 0, scalingDefinition.TopLeft.Width, scalingDefinition.TopLeft.Height),
                new Rectangle(0, 0, scalingDefinition.TopLeft.Width, scalingDefinition.TopLeft.Height),
                GraphicsUnit.Pixel);
            // top right
            g.DrawImage(source,
                new Rectangle(target.Width - scalingDefinition.TopRight.Width, 0, scalingDefinition.TopRight.Width,
                    scalingDefinition.TopRight.Height),
                new Rectangle(source.Width - scalingDefinition.TopRight.Width, 0, scalingDefinition.TopRight.Width,
                    scalingDefinition.TopRight.Height), GraphicsUnit.Pixel);
            // bottomleft
            g.DrawImage(source,
                new Rectangle(0, target.Height - scalingDefinition.BottomLeft.Height, scalingDefinition.BottomLeft.Width,
                    scalingDefinition.BottomLeft.Height),
                new Rectangle(0, source.Height - scalingDefinition.BottomLeft.Height, scalingDefinition.BottomLeft.Width,
                    scalingDefinition.BottomLeft.Height), GraphicsUnit.Pixel);
            // bottomright
            g.DrawImage(source,
                new Rectangle(target.Width - scalingDefinition.BottomRight.Width,
                    target.Height - scalingDefinition.BottomRight.Height, scalingDefinition.BottomRight.Width,
                    scalingDefinition.BottomRight.Height),
                new Rectangle(source.Width - scalingDefinition.BottomRight.Width,
                    source.Height - scalingDefinition.BottomRight.Height, scalingDefinition.BottomRight.Width,
                    scalingDefinition.BottomRight.Height), GraphicsUnit.Pixel);
            // copy the borders
            // top
            g.DrawImage(source,
                new Rectangle(scalingDefinition.TopLeft.Width, 0,
                    target.Width - scalingDefinition.TopLeft.Width - scalingDefinition.TopRight.Width,
                    scalingDefinition.TopBorder),
                new Rectangle(scalingDefinition.TopLeft.Width, 0,
                    source.Width - scalingDefinition.TopLeft.Width - scalingDefinition.TopRight.Width,
                    scalingDefinition.TopBorder), GraphicsUnit.Pixel);
            // bottom
            g.DrawImage(source,
                new Rectangle(scalingDefinition.BottomLeft.Width, target.Height - scalingDefinition.BottomBorder,
                    target.Width - scalingDefinition.BottomLeft.Width - scalingDefinition.BottomRight.Width,
                    scalingDefinition.BottomBorder),
                new Rectangle(scalingDefinition.BottomLeft.Width, source.Height - scalingDefinition.BottomBorder,
                    source.Width - scalingDefinition.BottomLeft.Width - scalingDefinition.BottomRight.Width,
                    scalingDefinition.BottomBorder), GraphicsUnit.Pixel);
            // left
            g.DrawImage(source,
                new Rectangle(0, scalingDefinition.TopLeft.Height, scalingDefinition.LeftBorder,
                    target.Height - scalingDefinition.BottomLeft.Height - scalingDefinition.TopLeft.Height),
                new Rectangle(0, scalingDefinition.TopLeft.Height, scalingDefinition.LeftBorder,
                    source.Height - scalingDefinition.BottomLeft.Height - scalingDefinition.TopLeft.Height),
                GraphicsUnit.Pixel);
            // right
            g.DrawImage(source,
                new Rectangle(target.Width - scalingDefinition.RightBorder, scalingDefinition.TopRight.Height,
                    scalingDefinition.RightBorder,
                    target.Height - scalingDefinition.BottomRight.Height - scalingDefinition.TopRight.Height),
                new Rectangle(source.Width - scalingDefinition.RightBorder, scalingDefinition.TopRight.Height,
                    scalingDefinition.RightBorder,
                    source.Height - scalingDefinition.BottomRight.Height - scalingDefinition.TopRight.Height),
                GraphicsUnit.Pixel);
        }

        private static void FillBackground(Bitmap target, Bitmap source, bool resize, bool maintainAspect)
        {
            var g = Graphics.FromImage(target);
            if (resize)
            {
                // resize the background to fit the bitmap
                Rectangle rect;
                if (maintainAspect)
                {
                    // work out which dimension is the restricting one and calulate the right scaling factor for the image
                    double factor;
                    if ((target.Width/(double) target.Height) > (source.Width/(double) source.Height))
                    {
                        factor = target.Height/(double) source.Height;
                    }
                    else
                    {
                        factor = target.Width/(double) source.Width;
                    }
                    // work out the position of the destination rectangle on the image
                    rect = new Rectangle(
                        (int) ((target.Width - (source.Width*factor))/2),
                        (int) ((target.Height - (source.Height*factor))/2),
                        (int) (source.Width*factor),
                        (int) (source.Height*factor));
                }
                else
                {
                    rect = new Rectangle(0, 0, target.Width, target.Height);
                }
                g.DrawImage(source, rect, new Rectangle(0, 0, source.Width, source.Height), GraphicsUnit.Pixel);
            }
            else
            {
                // we need to copy part of the bitmap as the button is not allowed to grow
                // I have decided to take the top left bit - although this could be made to follow the Vertical & HorizontalAlignment properties the same as the text
                var rect = new Rectangle(0, 0, Min(target.Width, source.Width), Min(target.Height, source.Height));
                g.DrawImage(source, rect, rect, GraphicsUnit.Pixel);
            }
        }

        private static int Max(int i, int j)
        {
            return Math.Max(i, j);
        }

        private static int Min(int i, int j)
        {
            return Math.Min(i, j);
        }
    }
}