using System.Collections.Generic;
using System.Numerics;
using PicoGK;
using Leap71.ShapeKernel;
using MyFirstApp.Core;
using MyFirstApp.Core.Engine;
using MyFirstApp.Products.Propulsion.Components;

namespace MyFirstApp.Products.Propulsion
{
    public class TurbojetAssembly : EngineeringComponent
    {
        float m_Diameter = 1000f;
        float m_Thrust = 80f;
        float m_Cutaway = 0f; // 0 = Solid, 1 = Cutaway

        public TurbojetAssembly() 
        { 
            Name = "Turbojet Assembly";
            // Ensure these match your existing class names exactly
            AddChild(new InletSystem());
            AddChild(new CompressorSection()); 
            AddChild(new CombustorSection());
            AddChild(new ExhaustNozzle());
            // AddChild(new CoolingSystem()); // Uncomment if you have this
        }

        protected override List<Parameter> GetComponentParameters() => new List<Parameter> {
            new Parameter { Name = "Global Diameter", Value=m_Diameter, Min=500, Max=2000, OnChange=v=>m_Diameter=v },
            new Parameter { Name = "Req. Thrust (kN)", Value=m_Thrust, Min=10, Max=200, OnChange=v=>m_Thrust=v },
            
            // --- NEW: THE CUTAWAY BUTTON (Implemented as a toggle slider) ---
            new Parameter { Name = "Section View (0=Off, 1=On)", Value=m_Cutaway, Min=0, Max=1, OnChange=v=>m_Cutaway=v }
        };

        public override void CalculatePhysics(EngineeringContext ctx)
        {
            ctx.SetData("MainDia", m_Diameter);
            ctx.SetData("ReqThrust", m_Thrust * 1000f);
        }

        public override void OnSetup(EngineeringContext ctx)
        {
            base.OnSetup(ctx);
            // Handshake for first component
            ctx.LastExitRadius = m_Diameter / 2f;
        }

        protected override void OnConstruct(EngineeringContext ctx)
        {
            // 1. Setup Data
            float totalLen = ctx.CurrentZ; 
            if (totalLen < 100) totalLen = 1500;
            float r = m_Diameter / 2f;

            // 2. Skin Logic (Simple Cylinder for now)
            LocalFrame frame = new LocalFrame(Vector3.Zero);
            BasePipe skin = new BasePipe(new Frames(totalLen, frame), 0f, 0f);
            
            float SkinFunc(float z)
            {
                if (z < totalLen * 0.2f) return r; 
                if (z > totalLen * 0.8f) return r * 0.8f; 
                return r + 20f; 
            }

            skin.SetRadius(
                new SurfaceModulation(new LineModulation(SkinFunc)),       
                new SurfaceModulation(new LineModulation(z => SkinFunc(z) + 10f)) 
            );
            
            ctx.Assembly.BoolAdd(skin.voxConstruct());

            // 3. CUTAWAY LOGIC (The Fix)
            if (m_Cutaway > 0.5f)
            {
                // Position: X=0 (Center), Y=-5000 (To cover width), Z=-100 (To cover start)
                // This box covers the +X side of the world
                Vector3 cutPos = new Vector3(0, -5000, -100); 
                LocalFrame cutFrame = new LocalFrame(cutPos);
                
                // Width(X)=5000, Depth(Y)=10000, Height(Z)=TotalLen+200
                BaseBox cutter = new BaseBox(cutFrame, 5000f, 10000f, totalLen + 500f);
                
                // --- FIX: Add to the PostProcess queue, do not subtract yet! ---
                ctx.PostProcessCuts.BoolAdd(cutter.voxConstruct());
            }
        }
    }
}