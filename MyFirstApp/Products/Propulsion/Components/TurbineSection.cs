using System;
using System.Collections.Generic;
using System.Numerics;
using PicoGK;
using Leap71.ShapeKernel;
using MyFirstApp.Core; // For Pal and Vis
using MyFirstApp.Core.Engine;

namespace MyFirstApp.Products.Propulsion.Components
{
    public class TurbineSection : EngineeringComponent
    {
        float m_Len = 300f;
        float m_rIn, m_rOut;

        public TurbineSection() { Name = "Turbine"; }
        
        protected override List<Parameter> GetComponentParameters() => new List<Parameter> {
            new Parameter { Name = "Length", Value=m_Len, Min=100, Max=600, OnChange=v=>m_Len=v }
        };

        // 1. LOGIC PHASE
        public override void OnSetup(EngineeringContext ctx)
        {
            base.OnSetup(ctx);
            
            m_rIn = ctx.LastExitRadius;
            if (m_rIn <= 0) m_rIn = 400f; // Default

            // Turbine expands slightly
            m_rOut = m_rIn * 1.1f;

            // Advance Cursor
            ctx.LastExitRadius = m_rOut;
            ctx.CurrentZ += m_Len;
        }

        // 2. PREVIEW PHASE
        public override void OnPreview(EngineeringContext ctx)
        {
            Vector3 start = new Vector3(0,0, m_MyStartZ);
            Vector3 end   = new Vector3(0,0, m_MyStartZ + m_Len);

            Vis.Circle(start, m_rIn, Pal.Blue);
            Vis.Circle(end, m_rOut, Pal.Red);
            Vis.Line(start + new Vector3(m_rIn,0,0), end + new Vector3(m_rOut,0,0), Pal.Steel);
            Vis.Line(start + new Vector3(-m_rIn,0,0), end + new Vector3(-m_rOut,0,0), Pal.Steel);
        }

        // 3. GEOMETRY PHASE
        // FIX: Changed 'Construct' to 'OnConstruct'
        protected override void OnConstruct(EngineeringContext ctx)
        {
            LocalFrame f = new LocalFrame(new Vector3(0,0, m_MyStartZ));
            
            // Simple tapered housing
            BaseCone housing = new BaseCone(f, m_Len, m_rIn, m_rOut);
            
            Voxels v = housing.voxConstruct();
            Voxels vVoid = new Voxels(v);
            vVoid.Offset(-10f);
            v.BoolSubtract(vVoid);

            // Add simple internal discs for turbine blades
            int stages = 3;
            for(int i=0; i<stages; i++)
            {
                float z = m_MyStartZ + (m_Len/stages)*i + 20;
                float r = m_rIn + (m_rOut - m_rIn) * (i/(float)stages);
                
                // Add a "disc" (simulated by a small cylinder)
                LocalFrame bladeFrame = new LocalFrame(new Vector3(0,0,z));
                BaseCylinder blades = new BaseCylinder(bladeFrame, 20f, r-5f);
                v.BoolAdd(blades.voxConstruct());
            }

            ctx.Assembly.BoolAdd(v);
        }
    }
}