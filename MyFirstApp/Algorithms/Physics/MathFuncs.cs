using System;
using System.Numerics;

namespace MyFirstApp.Algorithms
{
    public static class MathFuncs
    {
        /// <summary>
        /// The Gyroid is a Triply Periodic Minimal Surface (TPMS).
        /// It creates a "hornet nest" structure that separates two fluid domains perfectly.
        /// Formula: sin(x)cos(y) + sin(y)cos(z) + sin(z)cos(x) = 0
        /// </summary>
        public static float Gyroid(Vector3 p, float scale)
        {
            float x = p.X * scale;
            float y = p.Y * scale;
            float z = p.Z * scale;

            return MathF.Sin(x) * MathF.Cos(y) + 
                   MathF.Sin(y) * MathF.Cos(z) + 
                   MathF.Sin(z) * MathF.Cos(x);
        }
    }
}