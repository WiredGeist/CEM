using System;
using System.Numerics;
using PicoGK;

namespace MyFirstApp.Core
{
    // Pal = Palette (Renamed from Cp to avoid conflict with Leap71.ShapeKernel.Cp)
    public static class Pal
    {
        public static ColorFloat Red   = new ColorFloat(1f, 0.2f, 0.2f);
        public static ColorFloat Green = new ColorFloat(0.2f, 1f, 0.2f);
        public static ColorFloat Blue  = new ColorFloat(0.2f, 0.2f, 1f);
        public static ColorFloat Steel = new ColorFloat(0.8f, 0.8f, 0.9f);
        public static ColorFloat Warning = new ColorFloat(1f, 0.6f, 0f);
    }

    // Vis = Visuals (Renamed from Sh to avoid conflict with Leap71.ShapeKernel.Sh)
    public static class Vis
    {
        public static void Circle(Vector3 center, float radius, ColorFloat color, int resolution = 32)
        {
            PolyLine poly = new PolyLine(color);
            for (int i = 0; i <= resolution; i++)
            {
                float angle = (i / (float)resolution) * MathF.PI * 2f;
                poly.nAddVertex(new Vector3(
                    center.X + MathF.Cos(angle) * radius,
                    center.Y + MathF.Sin(angle) * radius,
                    center.Z
                ));
            }
            // Close the loop
            poly.nAddVertex(new Vector3(center.X + radius, center.Y, center.Z));
            Library.oViewer().Add(poly);
        }

        public static void Line(Vector3 start, Vector3 end, ColorFloat color)
        {
            PolyLine poly = new PolyLine(color);
            poly.nAddVertex(start);
            poly.nAddVertex(end);
            Library.oViewer().Add(poly);
        }
    }
}