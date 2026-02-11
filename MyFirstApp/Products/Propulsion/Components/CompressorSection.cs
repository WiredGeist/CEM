using System;
using System.Collections.Generic;
using System.Numerics;
using PicoGK;
using Leap71.ShapeKernel;
using MyFirstApp.Core; 
using MyFirstApp.Core.Engine;

namespace MyFirstApp.Products.Propulsion.Components
{
    public class CompressorSection : EngineeringComponent
    {
        float m_Len = 600f;
        float m_Ratio = 0.6f;
        float m_rIn, m_rOut; 

        public CompressorSection() { Name = "Axial Compressor"; }

        protected override List<Parameter> GetComponentParameters() => new List<Parameter> {
            new Parameter { Name = "Length", Value=m_Len, Min=200, Max=1000, OnChange=v=>m_Len=v },
            new Parameter { Name = "Comp. Ratio", Value=m_Ratio*100, Min=30, Max=90, OnChange=v=>m_Ratio=v/100f }
        };

        public override void OnSetup(EngineeringContext ctx)
        {
            base.OnSetup(ctx); 
            m_rIn = ctx.LastExitRadius;
            if (m_rIn <= 0) m_rIn = ctx.GetData<float>("MainDia") / 2f;
            m_rOut = m_rIn * m_Ratio;

            ctx.CurrentZ += m_Len; 
            ctx.LastExitRadius = m_rOut;
        }

        public override void OnPreview(EngineeringContext ctx)
        {
            Vector3 start = new Vector3(0, 0, m_MyStartZ);
            Vector3 end = new Vector3(0, 0, m_MyStartZ + m_Len);
            
            Vis.Circle(start, m_rIn, Pal.Blue);
            Vis.Circle(end, m_rOut, Pal.Red);
            Vis.Line(start + new Vector3(m_rIn, 0, 0), end + new Vector3(m_rOut, 0, 0), Pal.Steel);
            Vis.Line(start + new Vector3(-m_rIn, 0, 0), end + new Vector3(-m_rOut, 0, 0), Pal.Steel);

            int stages = 5;
            for(int i=0; i<stages; i++)
            {
                float z = m_MyStartZ + (m_Len/stages)*i;
                Vis.Circle(new Vector3(0,0,z), m_rIn * 0.5f, Pal.Warning);
            }
        }

        protected override void OnConstruct(EngineeringContext ctx)
        {
            float zStart = m_MyStartZ; 
            LocalFrame frame = new LocalFrame(new Vector3(0, 0, zStart));
            
            float RadiusFunc(float zLocal)
            {
                float t = zLocal / m_Len;
                float tSmooth = t * t * (3f - 2f * t); 
                return m_rIn + (m_rOut - m_rIn) * tSmooth;
            }

            BasePipe casing = new BasePipe(new Frames(m_Len, frame), 0f, 0f);
            casing.SetRadius(
                new SurfaceModulation(new LineModulation(z => 0f)), 
                new SurfaceModulation(new LineModulation(RadiusFunc))
            );
            
            Voxels vCasing = casing.voxConstruct();
            Voxels vVoid = new Voxels(vCasing);
            vVoid.Offset(-8f); 
            vCasing.BoolSubtract(vVoid);

            Lattice bladeLattice = new Lattice();
            int stages = 5;
            for (int s = 0; s < stages; s++)
            {
                float zStage = zStart + (m_Len / stages) * s + 50f;
                float currentR = RadiusFunc((m_Len / stages) * s);
                
                bladeLattice.AddBeam(new Vector3(0, 0, zStage - 20), new Vector3(0, 0, zStage + 20), currentR * 0.3f, currentR * 0.3f, true);

                int bladeCount = 12;
                for (int b = 0; b < bladeCount; b++)
                {
                    float angle = (b / (float)bladeCount) * MathF.PI * 2f;
                    Vector3 pHub = new Vector3(MathF.Cos(angle) * currentR * 0.3f, MathF.Sin(angle) * currentR * 0.3f, zStage);
                    Vector3 pTip = new Vector3(MathF.Cos(angle + 0.2f) * (currentR - 5f), MathF.Sin(angle + 0.2f) * (currentR - 5f), zStage);
                    bladeLattice.AddBeam(pHub, pTip, 5f, 2f, true); 
                }
            }
            vCasing.BoolAdd(new Voxels(bladeLattice));
            ctx.Assembly.BoolAdd(vCasing);
        }
    }
}