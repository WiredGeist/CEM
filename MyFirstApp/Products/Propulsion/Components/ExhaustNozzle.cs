using System.Collections.Generic;
using System.Numerics;
using PicoGK;
using Leap71.ShapeKernel;
using MyFirstApp.Core; // <--- This line is important for Pal and Vis
using MyFirstApp.Core.Engine;
using MyFirstApp.Algorithms.Physics;

namespace MyFirstApp.Products.Propulsion.Components
{
    public class ExhaustNozzle : EngineeringComponent
    {
        float m_Press = 60f;
        float m_Exp = 14f;
        
        // State
        float m_rIn, m_rThroat, m_rExit, m_Height;

        public ExhaustNozzle() { Name = "Exhaust Nozzle"; }

        protected override List<Parameter> GetComponentParameters() => new List<Parameter> {
            new Parameter { Name = "Pressure (Bar)", Value=m_Press, Min=10, Max=100, OnChange=v=>m_Press=v },
            new Parameter { Name = "Exp. Ratio", Value=m_Exp, Min=2, Max=30, OnChange=v=>m_Exp=v }
        };

        public override void OnSetup(EngineeringContext ctx)
        {
            base.OnSetup(ctx);
            
            float thrust = ctx.GetData<float>("ReqThrust");
            if (thrust <= 0) thrust = 50000f; // Default if missing
            
            float pressurePa = m_Press * 100000f;

            // Physics Calcs
            float At = GasDynamics.CalculateThroatArea(thrust, pressurePa);
            float Ae = GasDynamics.CalculateExitArea(At, m_Exp);
            
            m_rThroat = GasDynamics.AreaToRadiusMM(At);
            m_rExit = GasDynamics.AreaToRadiusMM(Ae);
            m_rIn = ctx.LastExitRadius; 
            if (m_rIn <= 0) m_rIn = m_rThroat * 2f;

            m_Height = m_rExit * 4.0f;

            // Advance Cursor
            ctx.CurrentZ += m_Height;
        }

        public override void OnPreview(EngineeringContext ctx)
        {
            Vis.Circle(new Vector3(0,0, m_MyStartZ), m_rIn, Pal.Blue);  // FIX: Used Pal
            Vis.Circle(new Vector3(0,0, m_MyStartZ + m_Height), m_rExit, Pal.Red); // FIX: Used Pal
        }

        protected override void OnConstruct(EngineeringContext ctx)
        {
            Frames spine = new Frames(m_Height, new LocalFrame(new Vector3(0,0, m_MyStartZ)), 1f);
            BasePipe noz = new BasePipe(spine, 0f, 0f);
            
            float RFunc(float z) => FluidDynamics.fGetDeLavalRadius(z, m_Height*0.3f, m_Height*0.6f, m_rIn, m_rThroat, m_rExit);
            
            noz.SetRadius(
                new SurfaceModulation(new LineModulation(z => 0f)), 
                new SurfaceModulation(new LineModulation(RFunc))
            );
            
            Voxels v = noz.voxConstruct();
            Voxels vVoid = new Voxels(v);
            vVoid.Offset(-5f);
            v.BoolSubtract(vVoid);

            ctx.Assembly.BoolAdd(v);
        }
    }
}