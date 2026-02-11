using System;
using System.Collections.Generic;
using System.Numerics;
using PicoGK;
using Leap71.ShapeKernel;
using MyFirstApp.Core;
using MyFirstApp.Core.Engine;

namespace MyFirstApp.Algorithms.Playground
{
    public class TwistedFinTest : EngineeringComponent
    {
        // --- PARAMETERS ---
        float m_CylRadius = 68f;
        float m_CylLength = 150f;
        
        float m_FinCount = 24f;      
        float m_Twist = 180f;        
        float m_FinHeight = 15f;     
        float m_HoleSize = 7f;      

        public TwistedFinTest() { Name = "Test Lab: Twisted Fins"; }

        protected override List<Parameter> GetComponentParameters() => new List<Parameter> {
            new Parameter { Name = "Base Radius", Value=m_CylRadius, Min=20, Max=100, OnChange=v=>m_CylRadius=v },
            new Parameter { Name = "Length", Value=m_CylLength, Min=50, Max=300, OnChange=v=>m_CylLength=v },
            new Parameter { Name = "Fin Count", Value=m_FinCount, Min=2, Max=50, OnChange=v=>m_FinCount=v },
            new Parameter { Name = "Twist (Deg)", Value=m_Twist, Min=0, Max=720, OnChange=v=>m_Twist=v },
            new Parameter { Name = "Fin Height", Value=m_FinHeight, Min=5, Max=30, OnChange=v=>m_FinHeight=v },
            new Parameter { Name = "Hole Size", Value=m_HoleSize, Min=0, Max=20, OnChange=v=>m_HoleSize=v }
        };

        public override void OnSetup(EngineeringContext ctx) { }

        // --- PREVIEW: Show the Spiral Paths ---
        public override void OnPreview(EngineeringContext ctx)
        {
            Vis.Circle(new Vector3(0,0,0), m_CylRadius, Pal.Steel);
            Vis.Circle(new Vector3(0,0,m_CylLength), m_CylRadius, Pal.Steel);

            int steps = 30; // Low res for preview speed
            float twistRad = (m_Twist * MathF.PI) / 180f;

            for (int i = 0; i < (int)m_FinCount; i++)
            {
                float angleOffset = (i / m_FinCount) * MathF.PI * 2f;
                Vector3 prev = Vector3.Zero;

                for (int s = 0; s <= steps; s++)
                {
                    float t = s / (float)steps;
                    float z = t * m_CylLength;
                    float angle = angleOffset + (t * twistRad);

                    Vector3 pos = new Vector3(
                        MathF.Cos(angle) * (m_CylRadius + m_FinHeight * 0.5f),
                        MathF.Sin(angle) * (m_CylRadius + m_FinHeight * 0.5f),
                        z
                    );

                    if (s > 0) Vis.Line(prev, pos, Pal.Warning);
                    prev = pos;
                }
            }
        }

        // --- CONSTRUCT: Optimized Lattice Approach ---
        protected override void OnConstruct(EngineeringContext ctx)
        {
            // 1. Base Cylinder
            LocalFrame frame = new LocalFrame(Vector3.Zero);
            BaseCylinder baseCyl = new BaseCylinder(frame, m_CylLength, m_CylRadius);
            Voxels vAssembly = baseCyl.voxConstruct();

            // Shell it
            Voxels vVoid = new Voxels(vAssembly);
            vVoid.Offset(-3f);
            vAssembly.BoolSubtract(vVoid);

            // 2. PREPARE LATTICES (The Optimization)
            // We use Lattices because adding beams to a lattice is near-instant math.
            // Converting a Lattice to Voxels is extremely optimized in PicoGK.
            Lattice latSolid = new Lattice();
            Lattice latVoid = new Lattice();

            float twistRad = (m_Twist * MathF.PI) / 180f;
            int steps = 100; // High res for smoothness

            for (int i = 0; i < (int)m_FinCount; i++)
            {
                float angleOffset = (i / m_FinCount) * MathF.PI * 2f;
                Vector3? prevPos = null;

                for (int s = 0; s <= steps; s++)
                {
                    float t = s / (float)steps;
                    float z = t * m_CylLength;
                    float angle = angleOffset + (t * twistRad);

                    // Center of the fin tube
                    Vector3 currentPos = new Vector3(
                        MathF.Cos(angle) * m_CylRadius, 
                        MathF.Sin(angle) * m_CylRadius,
                        z
                    );

                    if (prevPos.HasValue)
                    {
                        // Add segments to the Lattices
                        // 1. The Fin Body (Solid)
                        latSolid.AddBeam(
                            prevPos.Value, 
                            currentPos, 
                            m_FinHeight / 2f, // Start Radius
                            m_FinHeight / 2f, // End Radius
                            true // Round Cap
                        );

                        // 2. The Cooling Channel (Hole)
                        if (m_HoleSize > 0.5f)
                        {
                            latVoid.AddBeam(
                                prevPos.Value,
                                currentPos,
                                m_HoleSize / 2f,
                                m_HoleSize / 2f,
                                true
                            );
                        }
                    }
                    prevPos = currentPos;
                }
            }

            // 3. BATCH VOXELIZATION (The Speed Boost)
            // Instead of doing boolean ops inside the loop, we do them ONCE at the end.
            
            // Create the solid fins
            Voxels vFins = new Voxels(latSolid);

            // Subtract the holes (if any)
            if (m_HoleSize > 0.5f)
            {
                Voxels vHoles = new Voxels(latVoid);
                vFins.BoolSubtract(vHoles);
            }

            // 4. Merge Fins onto Skin
            vAssembly.BoolAdd(vFins);

            ctx.Assembly.BoolAdd(vAssembly);
        }
    }
}