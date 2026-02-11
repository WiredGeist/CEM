using System;
using System.Numerics;
using PicoGK;

namespace MyFirstApp.Algorithms
{
    public class ImplicitGyroid : IImplicit
    {
        float m_fScale;
        float m_fThickness;

        /// <summary>
        /// Defines a Gyroid field.
        /// </summary>
        /// <param name="fUnitSize">The size of one "cell" or pore (in mm).</param>
        /// <param name="fThickness">The thickness of the wall (in mm).</param>
        public ImplicitGyroid(float fUnitSize, float fThickness)
        {
            // Convert Unit Size to Frequency (2*PI / Size)
            // We store this to avoid recalculating it millions of times.
            m_fScale = (2f * MathF.PI) / fUnitSize;
            
            // We store half thickness because the math goes outwards from the center
            m_fThickness = fThickness / 2f; 
        }

        // The Engine calls this millions of times. It must be FAST.
        public float fSignedDistance(in Vector3 vec)
        {
            // 1. Scale the coordinate to the Gyroid Frequency
            float x = vec.X * m_fScale;
            float y = vec.Y * m_fScale;
            float z = vec.Z * m_fScale;

            // 2. The Gyroid Formula
            // sin(x)cos(y) + sin(y)cos(z) + sin(z)cos(x)
            float val = MathF.Sin(x) * MathF.Cos(y) + 
                        MathF.Sin(y) * MathF.Cos(z) + 
                        MathF.Sin(z) * MathF.Cos(x);

            // 3. Convert to Wall
            // The Gyroid surface is at 0.
            // We want a wall with thickness.
            // Distance = |Value| - Thickness
            // Negative result = Inside the wall
            // Positive result = Outside the wall
            
            // Note: We divide by m_fScale to bring the distance back to "Millimeters" 
            // roughly (Gradient normalization), otherwise the wall thickness is warped.
            return (MathF.Abs(val) / m_fScale) - m_fThickness;
        }
    }
}