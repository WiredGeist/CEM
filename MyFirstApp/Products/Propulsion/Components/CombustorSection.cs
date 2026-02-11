using System;
using System.Collections.Generic;
using System.Numerics;
using PicoGK;
using Leap71.ShapeKernel;
using MyFirstApp.Core;
using MyFirstApp.Core.Engine;
using MyFirstApp.Algorithms.Physics; 

namespace MyFirstApp.Products.Propulsion.Components
{
    public class CombustorSection : EngineeringComponent
    {
        // --- INPUT PARAMETERS (Physics from the Paper) ---
        float m_MassFlow = 35f;    // kg/s
        float m_InletTemp = 750f;  // Kelvin
        float m_InletPress = 25f;  // Bar

        // --- CALCULATED STATE (Geometry) ---
        float m_MeanDia;
        float m_CasingHeight;
        float m_LinerHeight;
        float m_LengthPrimary;
        float m_LengthSecondary;
        float m_LengthDilution;
        float m_TotalLen;

        public CombustorSection() { Name = "Annular Combustor"; }

        protected override List<Parameter> GetComponentParameters() => new List<Parameter> {
            new Parameter { Name = "Mass Flow (kg/s)", Value=m_MassFlow, Min=10, Max=100, OnChange=v=>m_MassFlow=v },
            new Parameter { Name = "Inlet Temp (K)", Value=m_InletTemp, Min=400, Max=900, OnChange=v=>m_InletTemp=v },
            new Parameter { Name = "Inlet Pressure (Bar)", Value=m_InletPress, Min=5, Max=40, OnChange=v=>m_InletPress=v }
        };

        // 1. LOGIC PHASE: Turn Physics into Dimensions
        public override void OnSetup(EngineeringContext ctx)
        {
            base.OnSetup(ctx); 

            // A. Get Environment
            float inputRadius = ctx.LastExitRadius;
            if (inputRadius <= 0) inputRadius = 400f;
            m_MeanDia = inputRadius * 2f;

            // B. Run The Paper's Math (Equations)
            float refArea = CombustorMath.CalculateReferenceArea(m_MassFlow, m_InletTemp, m_InletPress * 100000f);
            m_CasingHeight = CombustorMath.CalculateRefHeight(refArea, m_MeanDia);

            float ftArea = CombustorMath.CalculateFlameTubeArea(refArea);
            m_LinerHeight = CombustorMath.CalculateFlameTubeHeight(ftArea, m_MeanDia);

            var lengths = CombustorMath.CalculateZoneLengths(m_CasingHeight, m_LinerHeight);
            m_LengthPrimary = lengths.Primary;
            m_LengthSecondary = lengths.Secondary;
            m_LengthDilution = lengths.Dilution;
            
            m_TotalLen = m_LengthPrimary + m_LengthSecondary + m_LengthDilution;

            // Handshake for next component
            ctx.LastExitRadius = inputRadius; 
            ctx.CurrentZ += m_TotalLen;
        }

        // 2. PREVIEW PHASE
        public override void OnPreview(EngineeringContext ctx)
        {
            Vector3 centerStart = new Vector3(0,0, m_MyStartZ);
            Vector3 centerEnd = new Vector3(0,0, m_MyStartZ + m_TotalLen);
            
            // Draw Casing Outline
            float casingR = (m_MeanDia/2f) + (m_CasingHeight/2f);
            Vis.Circle(centerStart, casingR, Pal.Steel);
            Vis.Circle(centerEnd, casingR, Pal.Steel);
            
            // Draw Liner Outline (Combustion Zone)
            float linerR = (m_MeanDia/2f);
            Vis.Circle(centerStart + new Vector3(0,0,10), linerR, Pal.Red);
            Vis.Circle(centerEnd - new Vector3(0,0,10), linerR, Pal.Red);
        }

        // 3. CONSTRUCT PHASE
        protected override void OnConstruct(EngineeringContext ctx)
        {
            LocalFrame frame = new LocalFrame(new Vector3(0,0, m_MyStartZ));
            
            // --- A. Build The Liner (The Flame Tube) ---
            float linerInnerR = (m_MeanDia / 2f) - (m_LinerHeight / 2f);
            float linerOuterR = (m_MeanDia / 2f) + (m_LinerHeight / 2f);

            // FIX: Create with dummy 0,0 first
            BasePipe liner = new BasePipe(new Frames(m_TotalLen, frame), 0f, 0f);
            
            // FIX: Apply modulation
            liner.SetRadius(
                new SurfaceModulation(new LineModulation(z => linerInnerR)), // Inner
                new SurfaceModulation(new LineModulation(z => linerOuterR))  // Outer
            );

            Voxels vLiner = liner.voxConstruct();
            Voxels vVoid = new Voxels(vLiner);
            vVoid.Offset(-2f); // 2mm Wall
            vLiner.BoolSubtract(vVoid);

            // --- B. Cut The Dilution Holes ---
            Voxels vHoles = new Voxels();
            // Primary Holes
            AddHoleRow(vHoles, m_MyStartZ + m_LengthPrimary * 0.5f, 12, 15f);
            // Dilution Holes
            AddHoleRow(vHoles, m_MyStartZ + m_LengthPrimary + m_LengthSecondary * 0.5f, 16, 10f);

            vLiner.BoolSubtract(vHoles);

            // --- C. Build The Outer Casing ---
            float casInnerR = (m_MeanDia / 2f) - (m_CasingHeight / 2f);
            float casOuterR = (m_MeanDia / 2f) + (m_CasingHeight / 2f);
            
            // FIX: Create with dummy 0,0 first
            BasePipe casing = new BasePipe(new Frames(m_TotalLen, frame), 0f, 0f);
            
            // FIX: Apply modulation
            casing.SetRadius(
                new SurfaceModulation(new LineModulation(z => casInnerR)), 
                new SurfaceModulation(new LineModulation(z => casOuterR))
            );
            
            Voxels vCasing = casing.voxConstruct();
            Voxels vCasingVoid = new Voxels(vCasing);
            vCasingVoid.Offset(-4f);
            vCasing.BoolSubtract(vCasingVoid);

            // Combine
            ctx.Assembly.BoolAdd(vLiner);
            ctx.Assembly.BoolAdd(vCasing);
        }

        void AddHoleRow(Voxels target, float zPos, int count, float radius)
        {
            for(int i=0; i<count; i++)
            {
                float angle = (i/(float)count) * MathF.PI * 2f;
                Vector3 pos = new Vector3(
                    MathF.Cos(angle) * (m_MeanDia/2f),
                    MathF.Sin(angle) * (m_MeanDia/2f),
                    zPos
                );
                
                Vector3 dir = Vector3.Normalize(new Vector3(pos.X, pos.Y, 0));
                
                // Start hole punch slightly inside and go outward
                LocalFrame holeFrame = new LocalFrame(pos - (dir * 50f), dir);
                BaseCylinder punch = new BaseCylinder(holeFrame, 150f, radius);
                
                target.BoolAdd(punch.voxConstruct());
            }
        }
    }
}