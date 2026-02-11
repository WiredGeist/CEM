// FILE: Phyllotaxis.cs

using System;
using System.Collections.Generic;
using System.Numerics;
using PicoGK;
using Leap71.ShapeKernel;
using MyFirstApp.Core;
using MyFirstApp.Core.Engine;

namespace MyFirstApp.Algorithms.Playground
{
    // ALGORITHM: Phyllotaxis (Sunflower Spiral)
    // CONCEPT: This algorithm mimics the optimal packing strategy found in nature, such as the
    // arrangement of seeds in a sunflower head. Points are placed in a spiral, with each new
    // point rotated by the "golden angle" (137.5 degrees).
    // IMPLEMENTATION: A 'Lattice' object is used, which is extremely fast. We loop and calculate
    // the 3D position of each point according to the phyllotaxis formula, then add a small
    // sphere at that location. The result is then converted to voxels once at the end.
    public class Phyllotaxis : EngineeringComponent
    {
        protected int   m_nPoints       = 500;
        protected float m_fRadius       = 100f;
        protected float m_fSpiralPitch  = 10f;

        public Phyllotaxis() { Name = "ALGORITHM: Phyllotaxis"; }

        protected override List<Parameter> GetComponentParameters() => new List<Parameter>
        {
            new Parameter { Name = "Num Points", Value = m_nPoints, Min = 100, Max = 2000, OnChange = v => m_nPoints = (int)v },
            new Parameter { Name = "Radius", Value = m_fRadius, Min = 50, Max = 500, OnChange = v => m_fRadius = v },
            new Parameter { Name = "Spiral Pitch", Value = m_fSpiralPitch, Min = 1, Max = 50, OnChange = v => m_fSpiralPitch = v },
        };
        
        protected override void OnConstruct(EngineeringContext ctx)
        {
            Library.Log("\n--- Starting Phyllotaxis Construction ---");
            
            var oLattice = new Lattice();
            float fGoldenAngle = MathF.PI * (3f - MathF.Sqrt(5f)); // The golden angle

            for (int i = 0; i < m_nPoints; i++)
            {
                float y = 1 - (float)i / (m_nPoints - 1);       // Goes from 1 to 0
                float radius = MathF.Sqrt(1 - y * y) * m_fRadius; // Radius at this height

                float theta = fGoldenAngle * i; // The magic angle

                float x = MathF.Cos(theta) * radius;
                float z = MathF.Sin(theta) * radius;
                
                // Add a small sphere at the calculated point. This is very fast.
                oLattice.AddSphere(new Vector3(x, y * m_fSpiralPitch, z), 5f);
            }
            Library.Log($"{m_nPoints} spheres added to lattice.");

            // Convert the entire lattice to voxels in one single, fast operation.
            Voxels vPhyllotaxis = new Voxels(oLattice);

            ctx.Assembly.BoolAdd(vPhyllotaxis);
            Library.Log("--- Phyllotaxis Construction Complete ---");
        }
    }
}