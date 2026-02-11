using System;
using System.Collections.Generic;
using System.Numerics;
using PicoGK;
using Leap71.ShapeKernel;
using MyFirstApp.Core;
using MyFirstApp.Core.Engine;

namespace MyFirstApp.Algorithms.Playground
{
    public class GyroidInfillTest : EngineeringComponent
    {
        // Parameters
        float m_Length = 100f;
        float m_Radius = 30f;
        float m_ShellThickness = 3f;   // Thickness of the outer skin
        float m_GyroidSize = 15f;      // Size of the gyroid cells
        float m_GyroidWall = 1f;       // Thickness of the gyroid internal walls

        public GyroidInfillTest() { Name = "Test Lab: Shell & Infill"; }

        protected override List<Parameter> GetComponentParameters() => new List<Parameter> {
            new Parameter { Name = "Beam Length", Value=m_Length, Min=50, Max=200, OnChange=v=>m_Length=v },
            new Parameter { Name = "Beam Radius", Value=m_Radius, Min=10, Max=50, OnChange=v=>m_Radius=v },
            new Parameter { Name = "Shell Thickness", Value=m_ShellThickness, Min=1, Max=10, OnChange=v=>m_ShellThickness=v },
            new Parameter { Name = "Infill Size", Value=m_GyroidSize, Min=5, Max=40, OnChange=v=>m_GyroidSize=v },
            new Parameter { Name = "Infill Wall", Value=m_GyroidWall, Min=0.5f, Max=5f, OnChange=v=>m_GyroidWall=v }
        };

        public override void OnSetup(EngineeringContext ctx) { }

        public override void OnPreview(EngineeringContext ctx)
        {
            // Draw wireframe of the beam
            Vector3 start = new Vector3(0,0,0);
            Vector3 end = new Vector3(0,0, m_Length);
            
            Vis.Circle(start, m_Radius, Pal.Blue);
            Vis.Circle(end, m_Radius, Pal.Blue);
            Vis.Line(start + new Vector3(m_Radius,0,0), end + new Vector3(m_Radius,0,0), Pal.Steel);
            Vis.Line(start - new Vector3(m_Radius,0,0), end - new Vector3(m_Radius,0,0), Pal.Steel);
        }

        protected override void OnConstruct(EngineeringContext ctx)
        {
            // 1. CREATE BASE SHAPE (The Solid Beam)
            Lattice lat = new Lattice();
            lat.AddBeam(
                new Vector3(0,0,0),
                new Vector3(0,0, m_Length),
                m_Radius,
                m_Radius,
                true // Round caps
            );
            
            // Render the solid shape
            Voxels vMain = new Voxels(lat);

            // 2. CREATE THE INNER CORE (The Void)
            // We make a copy of the solid shape
            Voxels vCore = new Voxels(vMain);
            
            // Shrink it by the shell thickness (Negative Offset)
            // This creates a shape that fits perfectly inside the main one
            vCore.Offset(-m_ShellThickness);

            // 3. CREATE THE SHELL
            // Subtract the core from the main. 
            // vMain is now a hollow pipe/shell.
            vMain.BoolSubtract(vCore);

            // 4. GENERATE GYROID INFILL
            // We take the Core (which represents the empty air inside)
            // and intersect it with the infinite Gyroid math.
            IImplicit mathGyroid = new ImplicitGyroid(m_GyroidSize, m_GyroidWall);
            
            // This cuts the Gyroid so it only exists inside the core
            vCore.IntersectImplicit(mathGyroid);

            // 5. MERGE BACK TOGETHER
            // Add the gyroid core back into the shell
            vMain.BoolAdd(vCore);

            // 6. OUTPUT
            ctx.Assembly.BoolAdd(vMain);
        }
    }
}