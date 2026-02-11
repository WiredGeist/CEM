using System;
using System.Collections.Generic;
using System.Numerics;
using PicoGK;
using Leap71.ShapeKernel;
using MyFirstApp.Core;
using MyFirstApp.Core.Engine;

namespace MyFirstApp.Products.Propulsion.Components
{
    public class CoolingSystem : EngineeringComponent
    {
        public CoolingSystem() { Name = "Regenerative Cooling"; }

        protected override List<Parameter> GetComponentParameters() => new List<Parameter>();

        public override void OnSetup(EngineeringContext ctx) { } 

        public override void OnPreview(EngineeringContext ctx)
        {
            var wallFunc = ctx.GetMath("CompressorWall");
            if (wallFunc == null) return;

            float zStart = 0f;
            float zEnd = 600f; 
            int samples = 50;

            Vector3 prevPt = Vector3.Zero;
            for(int i=0; i<=samples; i++)
            {
                float t = i / (float)samples;
                float z = zStart + (zEnd - zStart) * t;
                
                float r = wallFunc(z);
                if (r < 0) continue;

                float angle = z * 0.05f; 
                Vector3 pt = new Vector3(MathF.Cos(angle)*r, MathF.Sin(angle)*r, z);
                
                if (i > 0) Vis.Line(prevPt, pt, Pal.Warning);
                prevPt = pt;
            }
        }

        protected override void OnConstruct(EngineeringContext ctx)
        {
            // 1. GET DATA
            var wallFunc = ctx.GetMath("CompressorWall");
            if (wallFunc == null) return;
            
            float zStart = 0f;
            float zEnd = 600f; 
            
            // 2. GENERATE POINTS
            List<Vector3> points = new List<Vector3>();
            
            int steps = 200; // Reduced slightly for speed since we are doing segments
            float spiralTightness = 0.05f; 

            for (int i = 0; i <= steps; i++)
            {
                float t = i / (float)steps;
                float z = zStart + (zEnd - zStart) * t;

                float r = wallFunc(z);
                if (r > 0)
                {
                    float angle = z * spiralTightness;
                    Vector3 pos = new Vector3(
                        MathF.Cos(angle) * r, 
                        MathF.Sin(angle) * r, 
                        z
                    );
                    points.Add(pos);
                }
            }

            if (points.Count < 2) return;

            // 3. GENERATE GEOMETRY (SEGMENTED APPROACH)
            // Instead of one big Frames object, we build small pipes between points
            Voxels coolingNetwork = new Voxels();

            for (int i = 0; i < points.Count - 1; i++)
            {
                Vector3 p1 = points[i];
                Vector3 p2 = points[i+1];
                Vector3 delta = p2 - p1;
                float len = delta.Length();

                if (len < 0.001f) continue;

                // Create a frame at P1 looking at P2
                LocalFrame frame = new LocalFrame(p1, Vector3.Normalize(delta));

                // Create a straight pipe segment using the constructor we KNOW works
                // Frames(length, startFrame)
                Frames segmentFrame = new Frames(len, frame);
                
                // Pipe Radius 3mm
                BasePipe segmentPipe = new BasePipe(segmentFrame, 0f, 3f);
                
                // Add to our network
                coolingNetwork.BoolAdd(segmentPipe.voxConstruct());
            }
            
            // 4. SUBTRACT FROM ENGINE
            // We subtract the entire network at once
            ctx.Assembly.BoolSubtract(coolingNetwork);
        }
    }
}