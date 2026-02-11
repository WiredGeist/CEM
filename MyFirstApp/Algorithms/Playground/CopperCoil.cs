using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;
using PicoGK;
using Leap71.ShapeKernel;
using MyFirstApp.Core;       // <--- Required for SimulationData and EngineeringComponent
using MyFirstApp.Core.Engine;

namespace MyFirstApp.Algorithms.Playground
{
    public class SmartCopperCoil : EngineeringComponent
    {
        // Toggles
        protected bool m_bGenerate = false;
        protected bool m_bShowField = false;

        // GEOMETRY INPUTS
        protected float m_fInnerRadius  = 30f;
        protected float m_fWireDiameter = 5.0f;
        protected int   m_nTurns        = 10;
        protected float m_fSquareness   = 0.0f; // 0 = Round, 1 = Square
        protected float m_fPitchGap     = 2.0f;
        protected float m_fLeadLength   = 40f;

        // PHYSICS INPUTS
        protected float m_fVoltage      = 12.0f; 

        // CONSTANTS (Copper)
        const double CONST_DENSITY_CU       = 8960;         // kg/m^3
        const double CONST_RESISTIVITY_CU   = 1.68e-8;      // Ohm*m
        const double CONST_MU_0             = 1.256637e-6;  // T*m/A (Permeability)

        // CACHED RESULTS
        protected CoilStats m_CachedStats; 

        public SmartCopperCoil() { Name = "PHYSICS: Smart Coil"; }

        // -------------------------------------------------------------------------------
        // 1. UI PARAMETERS
        // -------------------------------------------------------------------------------
        protected override List<Parameter> GetComponentParameters() => new List<Parameter>
        {
            // Action Buttons
            new Parameter { Name = ">>> GENERATE MESH <<<", Value = m_bGenerate ? 1 : 0, Min = 0, Max = 1, OnChange = v => m_bGenerate = v > 0.5f },
            new Parameter { Name = "[ Show B-Field ]", Value = m_bShowField ? 1 : 0, Min = 0, Max = 1, OnChange = v => m_bShowField = v > 0.5f },
            
            new Parameter { Name = "Input Voltage (V)", Value = m_fVoltage, Min = 0.1f, Max = 240, OnChange = v => m_fVoltage = v },
            
            new Parameter { Name = "Inner Diameter", Value = m_fInnerRadius * 2, Min = 10, Max = 200, OnChange = v => m_fInnerRadius = v / 2f },
            new Parameter { Name = "Wire Thickness", Value = m_fWireDiameter, Min = 1, Max = 20, OnChange = v => m_fWireDiameter = v },
            new Parameter { Name = "Winding Turns", Value = m_nTurns, Min = 1, Max = 50, OnChange = v => m_nTurns = (int)v },
            new Parameter { Name = "Shape (Round-Square)", Value = m_fSquareness, Min = 0, Max = 1.0f, OnChange = v => m_fSquareness = v },
            new Parameter { Name = "Pitch Gap", Value = m_fPitchGap, Min = 0, Max = 10.0f, OnChange = v => m_fPitchGap = v },
        };

        // -------------------------------------------------------------------------------
        // 2. RESULTS OUTPUT (To UI Panel)
        // -------------------------------------------------------------------------------
        public override List<SimulationData> GetResults()
        {
            // Return placeholder if no calculation has run yet
            if (m_CachedStats.MassKg == 0) 
                return new List<SimulationData> { new SimulationData { Label = "Status", Value = "Pending..." } };

            return new List<SimulationData>
            {
                new SimulationData { Label = "Wire Length",   Value = $"{m_CachedStats.WireLengthM:F3} m" },
                new SimulationData { Label = "Copper Mass",   Value = $"{m_CachedStats.MassKg:F3} kg" },
                new SimulationData { Label = "Resistance",    Value = $"{m_CachedStats.Resistance:F4} Ω" },
                
                // Add warnings if values get dangerous (>100A or >1000W)
                new SimulationData { 
                    Label = "Current (I)",   
                    Value = $"{m_CachedStats.CurrentAmps:F1} A", 
                    IsWarning = m_CachedStats.CurrentAmps > 100 
                },
                new SimulationData { 
                    Label = "Heat Loss (P)", 
                    Value = $"{m_CachedStats.PowerWatts:F1} W", 
                    IsWarning = m_CachedStats.PowerWatts > 1000 
                } 
            };
        }

        // -------------------------------------------------------------------------------
        // 3. LIFECYCLE (Preview & Construct)
        // -------------------------------------------------------------------------------
        public override void OnPreview(EngineeringContext ctx)
        {
            // Fast visualization of the spine
            List<Vector3> spine = GenerateCoilSpine();
            Sh.PreviewLine(spine, Cp.clrGray);
            
            // Run physics math immediately so the UI panel updates while dragging sliders
            CalculatePhysics(spine);
        }

        protected override void OnConstruct(EngineeringContext ctx)
        {
            if (!m_bGenerate) return;

            // A. Generate High-Res Spine
            List<Vector3> spine = GenerateCoilSpine();
            
            // B. Create Pipe Surface
            // MIN_ROTATION is critical for non-circular coils to prevent twisting
            Frames coilFrames = new Frames(spine, Frames.EFrameType.MIN_ROTATION);
            BasePipe coilPipe = new BasePipe(coilFrames, 0f, m_fWireDiameter / 2f);
            
            // Adaptive Resolution: Higher for squares to capture corners
            uint lenSteps = (m_fSquareness > 0.5f) ? (uint)(m_nTurns * 100) : (uint)(m_nTurns * 50);
            coilPipe.SetLengthSteps(lenSteps);
            coilPipe.SetRadialSteps(32);

            // C. Voxelize
            Voxels voxCoil = coilPipe.voxConstruct();
            Sh.PreviewVoxels(voxCoil, Cp.clrWarning, 0.1f, 0.95f);
            ctx.Assembly.BoolAdd(voxCoil);

            // D. Physics & Field
            CalculatePhysics(spine);
            
            if (m_bShowField)
            {
                // Visualize magnetic field using the calculated Current
                VisualizeMagneticField(spine, (float)m_CachedStats.CurrentAmps);
            }

            m_bGenerate = false;
        }

        // -------------------------------------------------------------------------------
        // 4. PHYSICS ENGINE
        // -------------------------------------------------------------------------------
        public struct CoilStats 
        { 
            public double Resistance; 
            public double CurrentAmps; 
            public double PowerWatts; 
            public double MassKg; 
            public double WireLengthM; 
        }

        private void CalculatePhysics(List<Vector3> spine)
        {
            // 1. Geometry properties
            float fLengthMM = SplineOperations.fGetTotalLength(spine);
            double dLengthM = fLengthMM / 1000.0;

            double dRadiusM = (m_fWireDiameter / 2.0) / 1000.0;
            double dAreaM2  = Math.PI * dRadiusM * dRadiusM;

            // 2. Physical properties
            double dVolumeM3 = dAreaM2 * dLengthM;
            double dMassKg   = dVolumeM3 * CONST_DENSITY_CU;

            // 3. Electrical properties (Ohm's Law)
            double dRes = CONST_RESISTIVITY_CU * (dLengthM / dAreaM2);
            double dCurrent = m_fVoltage / dRes;
            double dPower   = dCurrent * dCurrent * dRes;

            // 4. Store results
            m_CachedStats = new CoilStats { 
                Resistance = dRes, 
                CurrentAmps = dCurrent, 
                PowerWatts = dPower, 
                MassKg = dMassKg, 
                WireLengthM = dLengthM 
            };
            
            // Console logging for debug
            Library.Log($"Calc: I={dCurrent:F2}A, P={dPower:F2}W");
        }

        // -------------------------------------------------------------------------------
        // 5. MAGNETIC FIELD VISUALIZATION (Biot-Savart Law)
        // -------------------------------------------------------------------------------
        private void VisualizeMagneticField(List<Vector3> spine, float currentAmps)
        {
            // Optimization: Downsample the spine for field calculation 
            // (Integral over 2000 points is slow, 400 is plenty for viz)
            List<Vector3> simSpine = SplineOperations.aSubSampleList(spine, 5);

            int nLines = 12;
            float fFieldLineLength = m_nTurns * m_fPitchGap * 4f; 
            int nSteps = 100;
            float fStepSize = fFieldLineLength / nSteps;

            for (int i = 0; i < nLines; i++)
            {
                // Start streamlines inside the coil
                float angle = (float)i / nLines * 2f * MathF.PI;
                Vector3 startPos = new Vector3(
                    m_fInnerRadius * 0.5f * MathF.Cos(angle), 
                    m_fInnerRadius * 0.5f * MathF.Sin(angle), 
                    0
                );

                List<Vector3> fieldLine = TraceBFieldLine(startPos, simSpine, (float)CONST_MU_0 * currentAmps, nSteps, fStepSize);
                Sh.PreviewLine(fieldLine, Cp.clrCrystal);
            }
        }

        private List<Vector3> TraceBFieldLine(Vector3 start, List<Vector3> coilPath, float magnitudeFactor, int steps, float stepSize)
        {
            List<Vector3> line = new List<Vector3>();
            Vector3 currentPos = start;
            line.Add(currentPos);

            // Euler integration along the B-Field vector
            for(int i=0; i<steps; i++)
            {
                Vector3 B = CalculateBVector(currentPos, coilPath);
                
                // Stop if field is effectively zero
                if (B.LengthSquared() < 1e-12) break; 

                // Move in the direction of the field
                Vector3 dir = B.Normalize();
                currentPos += dir * stepSize;
                line.Add(currentPos);
            }
            return line;
        }

        // Calculates B-Vector at point P using discretized Biot-Savart
        private Vector3 CalculateBVector(Vector3 pointP, List<Vector3> coilPath)
        {
            Vector3 B_total = Vector3.Zero;

            for (int i = 0; i < coilPath.Count - 1; i++)
            {
                Vector3 p1 = coilPath[i];
                Vector3 p2 = coilPath[i+1];
                
                // Wire segment vector
                Vector3 dl = p2 - p1;
                
                // Vector from wire segment to Point P
                Vector3 wireCenter = (p1 + p2) * 0.5f;
                Vector3 rVec = pointP - wireCenter;
                float rMagSq = rVec.LengthSquared();
                
                // Cross product accumulation
                if (rMagSq > 0.001f) // Avoid division by zero
                {
                    // B ~ (dl x r) / |r|^3
                    B_total += Vector3.Cross(dl, rVec) / (float)Math.Pow(rMagSq, 1.5);
                }
            }
            return B_total;
        }

        // -------------------------------------------------------------------------------
        // 6. GEOMETRY GENERATION
        // -------------------------------------------------------------------------------
        private List<Vector3> GenerateCoilSpine()
        {
            List<Vector3> rawPoints = new List<Vector3>();
            float fPitch = m_fWireDiameter + m_fPitchGap;
            float fTotalHeight = m_nTurns * fPitch;
            float zStart = -fTotalHeight / 2f;
            float rBase = m_fInnerRadius + (m_fWireDiameter / 2f);
            
            // "Squareness" slider controls the exponent of the SuperShape
            // 2.0 = Circle, 20.0 = Rounded Square
            float fExponent = float_Lerp(2f, 20f, m_fSquareness);

            int nPointsPerTurn = 100; 
            int nTotalPoints = m_nTurns * nPointsPerTurn;

            for (int i = 0; i <= nTotalPoints; i++)
            {
                float t = (float)i / nTotalPoints; 
                float t_turn = t * m_nTurns;
                float angleRad = t_turn * 2f * MathF.PI;

                // SuperShape math for organic transition from Circle to Square
                float rMod = fGetSuperShapeRadius(angleRad, fExponent);
                float rCurrent = rBase * rMod;

                float x = rCurrent * MathF.Cos(angleRad);
                float y = rCurrent * MathF.Sin(angleRad);
                float z = zStart + (t_turn * fPitch);

                rawPoints.Add(new Vector3(x, y, z));
            }

            // Tangential Leads (Connections)
            if (m_fLeadLength > 0)
            {
                // Start
                Vector3 pFirst = rawPoints[0];
                Vector3 pSecond = rawPoints[1];
                Vector3 tanStart = (pFirst - pSecond).Normalize();
                Vector3 leadDirStart = Vector3.Lerp(tanStart, new Vector3(tanStart.X, tanStart.Y, 0), 0.5f).Normalize();
                rawPoints.Insert(0, pFirst + (leadDirStart * m_fLeadLength));

                // End
                Vector3 pLast = rawPoints[^1];
                Vector3 pPrev = rawPoints[^2];
                Vector3 tanEnd = (pLast - pPrev).Normalize();
                Vector3 leadDirEnd = Vector3.Lerp(tanEnd, new Vector3(tanEnd.X, tanEnd.Y, 0), 0.5f).Normalize();
                rawPoints.Add(pLast + (leadDirEnd * m_fLeadLength));
            }

            // Global Smoothing to remove kinks at lead transitions
            uint nSamples = (uint)(rawPoints.Count);
            return SplineOperations.aGetNURBSpline(rawPoints, nSamples);
        }

        private float fGetSuperShapeRadius(float phi, float n)
        {
            if (n <= 2.01f) return 1.0f; // Performance optimization for perfect circle
            float m = 4f; 
            float part1 = MathF.Pow(MathF.Abs(MathF.Cos(m * phi / 4f)), n);
            float part2 = MathF.Pow(MathF.Abs(MathF.Sin(m * phi / 4f)), n);
            return MathF.Pow(part1 + part2, -1f / n);
        }

        private float float_Lerp(float a, float b, float t) => a + (b - a) * t;
    }
}