using System.Numerics;
using PicoGK;

namespace MyFirstApp.Core
{
    public static class SceneHelpers
    {
        // Blender-style Colors: X=Red, Y=Green, Z=Blue
        static ColorFloat clrX = new ColorFloat(1.0f, 0.2f, 0.2f); // Red
        static ColorFloat clrY = new ColorFloat(0.2f, 1.0f, 0.2f); // Green
        static ColorFloat clrZ = new ColorFloat(0.2f, 0.2f, 1.0f); // Blue
        static ColorFloat clrGrid = new ColorFloat(0.35f, 0.35f, 0.35f); // Dark Grey

        /// <summary>
        /// Draws XYZ axes and a floor grid
        /// </summary>
        /// <param name="fGridSize">Total size of the grid (e.g. 200mm)</param>
        /// <param name="fStep">Size of one square (e.g. 20mm)</param>
        public static void AddAxesAndGrid(float fGridSize = 200f, float fStep = 20f)
        {
            var viewer = Library.oViewer();

            // 1. DRAW AXES (Thick lines originating from 0,0,0)
            float fAxisLen = fGridSize / 2f;

            // X Axis (Red)
            PolyLine polyX = new PolyLine(clrX);
            polyX.nAddVertex(new Vector3(0, 0, 0));
            polyX.nAddVertex(new Vector3(fAxisLen, 0, 0));
            viewer.Add(polyX);

            // Y Axis (Green)
            PolyLine polyY = new PolyLine(clrY);
            polyY.nAddVertex(new Vector3(0, 0, 0));
            polyY.nAddVertex(new Vector3(0, fAxisLen, 0));
            viewer.Add(polyY);

            // Z Axis (Blue)
            PolyLine polyZ = new PolyLine(clrZ);
            polyZ.nAddVertex(new Vector3(0, 0, 0));
            polyZ.nAddVertex(new Vector3(0, 0, fAxisLen));
            viewer.Add(polyZ);

            // 2. DRAW GRID (The Floor at Z=0)
            int iSteps = (int)(fGridSize / fStep);

            for (int i = -iSteps; i <= iSteps; i++)
            {
                float pos = i * fStep;

                // Skip the center lines (0) because we drew colored axes there already
                if (pos == 0) continue; 

                // Line parallel to X
                PolyLine lineX = new PolyLine(clrGrid);
                lineX.nAddVertex(new Vector3(-fGridSize, pos, 0));
                lineX.nAddVertex(new Vector3(fGridSize, pos, 0));
                viewer.Add(lineX);

                // Line parallel to Y
                PolyLine lineY = new PolyLine(clrGrid);
                lineY.nAddVertex(new Vector3(pos, -fGridSize, 0));
                lineY.nAddVertex(new Vector3(pos, fGridSize, 0));
                viewer.Add(lineY);
            }
        }
    }
}