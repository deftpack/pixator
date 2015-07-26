using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Web.Http;

namespace Pixator.Api.Controllers
{
    public class DynamicController : ApiController
    {
        public HttpResponseMessage Get(int width, int height, int? seed)
        {
            var randomGenerator = seed == null ? new Random() : new Random((int)seed);
            var image = RoygbivStripes(width, height, randomGenerator);
            var imageStream = new MemoryStream();
            var result = new HttpResponseMessage(HttpStatusCode.OK);
            image.Save(imageStream, ImageFormat.Png);
            result.Content = new ByteArrayContent(imageStream.ToArray());
            result.Content.Headers.ContentType = new MediaTypeHeaderValue("image/png");
            return result;
        }

        private static Bitmap Stripes(int width, int height, Random randomGenerator)
        {
            var minColumnNumber = width / 5;
            var maxColumnNumber = width / 2;
            var numberOfColumns = randomGenerator.Next(minColumnNumber, maxColumnNumber);
            var colors = GetRandomColors(randomGenerator, numberOfColumns);
            var colorStops = CreateColorStops(colors, width, randomGenerator);

            return CreateGradientStripes(width, height, colorStops);
        }

        private static Bitmap RoygbivStripes(int width, int height, Random randomGenerator)
        {
            var intermediateSteps = randomGenerator.Next(1, width / 21);
            var shift = randomGenerator.Next(0, ROYGBIV.Length);
            var colors = FillWithIntermediateColors(ShiftColors(ROYGBIV, shift), intermediateSteps);
            var colorStops = CreateColorStops(colors, width, randomGenerator);

            return CreateGradientStripes(width, height, colorStops);
        }

        private static Bitmap CreateGradientStripes(int width, int height, IList<ColorStop> colorStops)
        {
            var image = new Bitmap(width, height);
            var graphics = Graphics.FromImage(image);
            var rectangle = new Rectangle(0, 0, width, height);
            var graphicsPath = new GraphicsPath();
            var brush = new LinearGradientBrush(rectangle, Color.Black, Color.Black, 0, false);
            var colorBlend = new ColorBlend(colorStops.Count);
            graphicsPath.AddRectangle(rectangle);
            colorBlend.Colors = colorStops.Select(cs => cs.Color).ToArray();
            colorBlend.Positions = colorStops.Select(cs => cs.Position).ToArray();
            brush.InterpolationColors = colorBlend;
            graphics.FillPath(brush, graphicsPath);
            return image;
        }

        private static Color[] ROYGBIV = new[] { Color.Red, Color.Orange, Color.Yellow, Color.Green, Color.Blue, Color.Indigo, Color.Violet };
        private const float accuracy = 1000;
        private const float minWidthMultiplier = 0.5f;
        private const float maxWidthMultiplier = 2.0f;

        private static IList<Column> CreateColumns(IList<Color> originalColors, int width, Random randomGenerator)
        {

            var columns = new List<Column>();
            var numberOfColumns = originalColors.Count;
            var columnWidths = GetColumnWidths(randomGenerator, numberOfColumns, width);

            for (int i = 0; i < numberOfColumns; i++)
            {
                columns.Add(new Column
                {
                    Color = originalColors[i],
                    Width = columnWidths[i]
                });
            }

            return columns;
        }

        private static IList<float> CreateMultipliers(Random randomGenerator, int numberOfMultipliers)
        {
            var randomMultipliers = Enumerable.Range(1, numberOfMultipliers)
                .Select(x => randomGenerator.Next((int)(minWidthMultiplier * accuracy), (int)(maxWidthMultiplier * accuracy)) / accuracy).ToList();
            var adjustment = randomMultipliers.Average();
            return randomMultipliers.Select(x => x / adjustment).ToList();
        }

        private static IList<Color> GetRandomColors(Random randomGenerator, int numberOfColors)
        {
            return Enumerable.Range(1, numberOfColors).Select(cw =>
                Color.FromArgb(randomGenerator.Next(255), randomGenerator.Next(255), randomGenerator.Next(255))).ToList();
        }

        private static IList<int> GetColumnWidths(Random randomGenerator, int numberOfColumns, int width)
        {
            var defaultWidth = (int)Math.Round((double)width / numberOfColumns);
            var widths = Enumerable.Range(1, numberOfColumns).Select(x => defaultWidth).ToList();
            var adjustments = Enumerable.Range(1, numberOfColumns / 2).Select(x =>
                randomGenerator.Next((int)(minWidthMultiplier * defaultWidth), (int)(maxWidthMultiplier * defaultWidth)) - defaultWidth).ToList();
            var reverseAdjustments = adjustments.Select(a => -a).ToList();
            adjustments.AddRange(reverseAdjustments);

            var difference = widths.Sum() - width;

            for (int i = 0; i < Math.Abs(difference); i++)
            {
                widths[i] += difference > 0 ? -1 : +1; 
            }

            for (int i = 0; i < adjustments.Count; i++)
            {
                widths[i] += adjustments[i];
            }

            return widths.OrderBy(x => randomGenerator.Next()).ToList();
        }

        private static IList<ColorStop> CreateColorStops(IList<Color> originalColors, int width, Random randomGenerator)
        {
            var colorStops = new List<ColorStop> { new ColorStop { Color = originalColors.First(), Position = 0 } };
            var numberOfStops = originalColors.Count - 1;
            var columnWidths = GetColumnWidths(randomGenerator, numberOfStops, width);

            var horizontalCursor = 0;
            for (int i = 0; i < numberOfStops; i++)
            {
                horizontalCursor += columnWidths[i];

                colorStops.Add(new ColorStop
                {
                    Color = originalColors[i + 1],
                    Position = (float)horizontalCursor / width
                });
            }
            return colorStops;
        }

        private static IList<Color> ShiftColors(IList<Color> originalColors, int shift)
        {
            var colors = originalColors.Skip(shift).ToList();
            colors.AddRange(originalColors.Take(shift));
            return colors;
        }

        private static IList<Color> FillWithIntermediateColors(IList<Color> originalColors, int stepCount)
        {
            var colors = new List<Color>();
            for (int i = 0; i < originalColors.Count - 1; i++)
            {
                var temporaryColors = CreateColorSteps(originalColors[i], originalColors[i + 1], stepCount);
                colors.AddRange(temporaryColors.Take(temporaryColors.Count - 1));
            }
            colors.Add(originalColors.Last());
            return colors;
        }

        private static IList<Color> CreateColorSteps(Color start, Color end, int steps)
        {
            var colors = new List<Color> { start };
            colors.AddRange(Enumerable.Range(1, steps).Select(i => Color.FromArgb(255,
                MixColor(() => start.R, () => end.R, steps, i),
                MixColor(() => start.G, () => end.G, steps, i),
                MixColor(() => start.B, () => end.B, steps, i))));
            colors.Add(end);
            return colors;
        }

        private static int MixColor(Func<byte> start, Func<byte> end, int steps, int index)
        {
            return start() - (((start() - end()) / (steps + 1)) * index);
        }
    }

    public struct Column
    {
        public Color Color;
        public int Width;
    }

    public struct ColorStop
    {
        public Color Color;
        public float Position;
    }
}
