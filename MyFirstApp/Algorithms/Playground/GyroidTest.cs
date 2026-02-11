using System;
using System.Collections.Generic;
using System.Numerics;
using PicoGK;
using Leap71.ShapeKernel;
using MyFirstApp.Core;
using MyFirstApp.Core.Engine;

namespace MyFirstApp.Algorithms.Playground
{
    public class GyroidTest : EngineeringComponent
    {
        // Parameters
        float m_Size = 100f;       // Size of the box
        float m_CellSize = 15f;    // Pore size
        float m_WallThick = 2f;    // Wall thickness

        public GyroidTest() { Name = "Test Lab: Gyroid"; }

        protected override List<Parameter> GetComponentParameters() => new List<Parameter> {
            new Parameter { Name = "Box Size", Value=m_Size, Min=50, Max=200, OnChange=v=>m_Size=v },
            new Parameter { Name = "Cell Size", Value=m_CellSize, Min=5, Max=50, OnChange=v=>m_CellSize=v },
            new Parameter { Name = "Wall Thickness", Value=m_WallThick, Min=0.5f, Max=10f, OnChange=v=>m_WallThick=v }
        };

        public override void OnSetup(EngineeringContext ctx) { } 

        public override void OnPreview(EngineeringContext ctx)
        {
            // Just draw the bounding box so we know where it will appear
            Vector3 center = new Vector3(0,0, m_Size/2);
            float h = m_Size/2;
            
            // Bottom Square
            Vis.Line(new Vector3(-h, -h, 0), new Vector3(h, -h, 0), Pal.Steel);
            Vis.Line(new Vector3(h, -h, 0), new Vector3(h, h, 0), Pal.Steel);
            Vis.Line(new Vector3(h, h, 0), new Vector3(-h, h, 0), Pal.Steel);
            Vis.Line(new Vector3(-h, h, 0), new Vector3(-h, -h, 0), Pal.Steel);
            
            // Top Square
            Vis.Line(new Vector3(-h, -h, m_Size), new Vector3(h, -h, m_Size), Pal.Steel);
            Vis.Line(new Vector3(h, -h, m_Size), new Vector3(h, h, m_Size), Pal.Steel);
            Vis.Line(new Vector3(h, h, m_Size), new Vector3(-h, h, m_Size), Pal.Steel);
            Vis.Line(new Vector3(-h, h, m_Size), new Vector3(-h, -h, m_Size), Pal.Steel);
        }

        protected override void OnConstruct(EngineeringContext ctx)
        {
            // 1. DEFINE THE BOUNDING BOX
            // Implicits are infinite. We must tell PicoGK where to stop calculating.
            BBox3 box = new BBox3(
                new Vector3(-m_Size/2, -m_Size/2, 0), // Min Corner
                new Vector3(m_Size/2, m_Size/2, m_Size) // Max Corner
            );

            // 2. INSTANTIATE THE MATH
            // This is just a definition, it costs 0 CPU time.
            IImplicit mathGyroid = new ImplicitGyroid(m_CellSize, m_WallThick);

            // 3. GENERATE VOXELS
            // This is where the magic happens. 
            // The engine scans the BBox and asks the Math class for distances.
            Voxels vGyroid = new Voxels(mathGyroid, box);

            // 4. ADD TO SCENE
            ctx.Assembly.BoolAdd(vGyroid);
        }
    }
}