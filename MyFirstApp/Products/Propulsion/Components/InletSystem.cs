using System;
using System.Collections.Generic;
using System.Numerics;
using PicoGK;
using Leap71.ShapeKernel;
using MyFirstApp.Core; // For Pal and Vis
using MyFirstApp.Core.Engine;

namespace MyFirstApp.Products.Propulsion.Components
{
    public class InletSystem : EngineeringComponent
    {
        float m_Len = 300f;
        float m_Dia; 

        public InletSystem() { Name = "Inlet"; }
        
        protected override List<Parameter> GetComponentParameters() => new List<Parameter> {
            new Parameter { Name = "Length", Value=m_Len, Min=100, Max=1000, OnChange=v=>m_Len=v }
        };

        // 1. LOGIC PHASE
        public override void OnSetup(EngineeringContext ctx)
        {
            base.OnSetup(ctx);
            
            m_Dia = ctx.GetData<float>("MainDia");
            if (m_Dia <= 0) m_Dia = 1000f; // Default

            // Logic: The inlet starts at m_MyStartZ
            
            // Advance Cursor for the next component
            ctx.LastExitRadius = m_Dia / 2f;
            ctx.CurrentZ += m_Len;
        }

        // 2. PREVIEW PHASE (Instant Wireframe)
        public override void OnPreview(EngineeringContext ctx)
        {
            Vector3 start = new Vector3(0,0, m_MyStartZ);
            Vector3 end   = new Vector3(0,0, m_MyStartZ + m_Len);
            float radius = m_Dia / 2f;

            Vis.Circle(start, radius, Pal.Blue);
            Vis.Circle(end, radius, Pal.Red);
            Vis.Line(start, end, Pal.Steel);
            
            // Draw Spike preview
            Vis.Line(start, start - new Vector3(0,0, 100), Pal.Warning);
        }

        // 3. GEOMETRY PHASE (Cached Voxels)
        // FIX: Changed 'Construct' to 'OnConstruct'
        protected override void OnConstruct(EngineeringContext ctx)
        {
            LocalFrame f = new LocalFrame(new Vector3(0,0, m_MyStartZ));
            
            // Outer Duct
            BasePipe pipe = new BasePipe(f, m_Len, m_Dia/2f - 20, m_Dia/2f);
            ctx.Assembly.BoolAdd(pipe.voxConstruct());
            
            // Inner Spike (Cone)
            LocalFrame spikeFrame = new LocalFrame(new Vector3(0,0, m_MyStartZ));
            BaseCone spike = new BaseCone(spikeFrame, m_Len, 0, m_Dia/4f);
            
            ctx.Assembly.BoolAdd(spike.voxConstruct());
        }
    }
}