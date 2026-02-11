using System;
using System.Collections.Generic;
using System.Numerics;
using System.Diagnostics;
using PicoGK;
using Leap71.ShapeKernel;
using MyFirstApp.Core;
using MyFirstApp.Core.Engine;

namespace MyFirstApp.Algorithms.Playground
{
    public class StepTimer : IDisposable
    {
        string m_strTask;
        Stopwatch m_oStopwatch;
        public StepTimer(string strTask) { m_strTask = strTask; m_oStopwatch = Stopwatch.StartNew(); Library.Log($"[..] START: {m_strTask}"); }
        public void Dispose() { m_oStopwatch.Stop(); Library.Log($"[OK] DONE : {m_strTask} (Took {m_oStopwatch.Elapsed.TotalSeconds:F1}s)"); }
    }

    public class Plane : EngineeringComponent
    {
        // TRIGGER
        protected bool m_bGenerate              = false; 

        // WING PARAMETERS
        protected float m_fSpan                 = 1500f;
        protected float m_fFuselageWidth        = 400f;
        protected float m_fSweepDegrees         = 30f;
        protected float m_fTwistDegrees         = 5f;
        protected float m_fDihedralDeg          = 3.0f;  
        protected float m_fRootChord            = 1000f;
        protected float m_fShoulderChord        = 450f;
        protected float m_fTipChord             = 100f;
        protected float m_fRootThickness        = 0.15f;
        protected float m_fTipThickness         = 0.12f;
        protected float m_fCamberPercent        = 5.0f;

        // MORPHING
        protected bool  m_bEnableMorphing       = true;  
        protected float m_fMorphSkinThickness   = 2.0f;  
        protected float m_fSlatSpacingRoot      = 12f;   
        protected float m_fSlatSpacingTip       = 20f;   
        protected float m_fSlatThickRoot        = 1.5f;  
        protected float m_fSlatThickTip         = 0.8f;  
        protected float m_fMorphAmplitude       = 6.0f;  
        protected float m_fMorphFrequency       = 0.08f; 
        protected float m_fLatticeDensityY      = 1.0f;

        // SPINE & CARGO
        protected float m_fSpineRibSpacing      = 50f;   
        protected float m_fSpineRibThickness    = 6.0f;  
        protected float m_fCargoFloorSkin       = 5.0f;  
        protected float m_fCargoRoofSkin        = 20.0f; 
        protected float m_fCargoCornerRadius    = 40.0f; 
        
        // ENGINE
        protected int   m_nEngineCount          = 2;     
        protected float m_fWingPositionPct      = 0.15f; 
        protected float m_fArraySpacing         = 150f;  
        protected bool  m_bMergeExhausts        = false;
        protected float m_fEngineGlobalX        = 400f;  
        protected float m_fEngineGlobalZ        = 0f; 
        protected float m_fEngineLength         = 150f;
        protected float m_fEngineDiameter       = 50f;   
        protected float m_fThrust               = 0.02f; 
        
        protected float m_fInletSquareness      = 0.1f; // Now used as "Shape Factor" (0=Round, 1=Sharp Eye)
        protected float m_fOutletSquareness     = 0.1f;  
        protected float m_fInletWidthRatio      = 1f;   // Aspect Ratio (Width vs Height)
        protected float m_fOutletWidthRatio     = 1f;
        protected float m_fInletOffsetZ         = 30f;   
        protected float m_fOutletOffsetZ        = 10f;   
        protected float m_fSkinThickness        = 5f; 
        protected float m_fFilletRadius         = 10f; 
        protected float m_fInletTilt            = 0f;    

        public Plane() { Name = "DESIGN: Bio-Spine Morphing Drone"; }

        protected override List<Parameter> GetComponentParameters() => new List<Parameter>
        {   
            new Parameter { Name = ">>> GENERATE GEOMETRY <<<", Value = m_bGenerate ? 1 : 0, Min = 0, Max = 1, OnChange = v => m_bGenerate = v > 0.5f },
            new Parameter { Name = "Total Span", Value = m_fSpan, Min = 500, Max = 3000, OnChange = v => m_fSpan = v },
            new Parameter { Name = "Fuselage Width", Value = m_fFuselageWidth, Min = 100, Max = 1000, OnChange = v => m_fFuselageWidth = v },
            new Parameter { Name = "Wing Sweep", Value = m_fSweepDegrees, Min = 0, Max = 60, OnChange = v => m_fSweepDegrees = v },
            new Parameter { Name = "Twist", Value = m_fTwistDegrees, Min = 0, Max = 15, OnChange = v => m_fTwistDegrees = v },
            new Parameter { Name = "Dihedral", Value = m_fDihedralDeg, Min = -5, Max = 15, OnChange = v => m_fDihedralDeg = v },
            new Parameter { Name = "Root Chord", Value = m_fRootChord, Min = 200, Max = 1500, OnChange = v => m_fRootChord = v },
            new Parameter { Name = "Shoulder Chord", Value = m_fShoulderChord, Min = 100, Max = 1000, OnChange = v => m_fShoulderChord = v },
            new Parameter { Name = "Tip Chord", Value = m_fTipChord, Min = 20, Max = 500, OnChange = v => m_fTipChord = v },
            new Parameter { Name = "Root Thickness %", Value = m_fRootThickness * 100, Min = 5, Max = 30, OnChange = v => m_fRootThickness = v / 100f },
            new Parameter { Name = "Tip Thickness %", Value = m_fTipThickness * 100, Min = 2, Max = 30, OnChange = v => m_fTipThickness = v / 100f },
            new Parameter { Name = "Camber %", Value = m_fCamberPercent, Min = 0, Max = 10, OnChange = v => m_fCamberPercent = v },
            
            // Engine
            new Parameter { Name = "Engine Count", Value = m_nEngineCount, Min = 0, Max = 4, OnChange = v => m_nEngineCount = (int)Math.Round(v) },
            new Parameter { Name = "Engine Position %", Value = m_fWingPositionPct * 100, Min = 0, Max = 80, OnChange = v => m_fWingPositionPct = v / 100f },
            new Parameter { Name = "Merge Exhausts", Value = m_bMergeExhausts ? 1 : 0, Min = 0, Max = 1, OnChange = v => m_bMergeExhausts = v > 0.5f },
            new Parameter { Name = "Array Spacing", Value = m_fArraySpacing, Min = 50, Max = 400, OnChange = v => m_fArraySpacing = v },
            new Parameter { Name = "Longitudinal X", Value = m_fEngineGlobalX, Min = 0, Max = 1000, OnChange = v => m_fEngineGlobalX = v },
            new Parameter { Name = "Vertical Z", Value = m_fEngineGlobalZ, Min = -100, Max = 100, OnChange = v => m_fEngineGlobalZ = v },
            new Parameter { Name = "Engine Length", Value = m_fEngineLength, Min = 50, Max = 500, OnChange = v => m_fEngineLength = v },
            new Parameter { Name = "Core Diameter", Value = m_fEngineDiameter, Min = 10, Max = 150, OnChange = v => m_fEngineDiameter = v },
            
            // Engine Shape (Key Changes Here)
            new Parameter { Name = "Inlet Aspect Ratio", Value = m_fInletWidthRatio, Min = 1, Max = 8, OnChange = v => m_fInletWidthRatio = v },
            new Parameter { Name = "Inlet Shape (0=O, 1=Eye)", Value = m_fInletSquareness, Min = 0, Max = 1, OnChange = v => m_fInletSquareness = v },
            new Parameter { Name = "Inlet Offset Z", Value = m_fInletOffsetZ, Min = 0, Max = 100, OnChange = v => m_fInletOffsetZ = v }, 
            
            new Parameter { Name = "Outlet Aspect Ratio", Value = m_fOutletWidthRatio, Min = 1, Max = 12, OnChange = v => m_fOutletWidthRatio = v },
            new Parameter { Name = "Outlet Shape (0=O, 1=Eye)", Value = m_fOutletSquareness, Min = 0, Max = 1, OnChange = v => m_fOutletSquareness = v },
            new Parameter { Name = "Outlet Offset Z", Value = m_fOutletOffsetZ, Min = 0, Max = 100, OnChange = v => m_fOutletOffsetZ = v },

            new Parameter { Name = "Engine Skin", Value = m_fSkinThickness, Min = 2, Max = 20, OnChange = v => m_fSkinThickness = v },
            new Parameter { Name = "Engine Fillet", Value = m_fFilletRadius, Min = 2, Max = 50, OnChange = v => m_fFilletRadius = v },

            // Morphing
            new Parameter { Name = "[M] Enable Morphing", Value = m_bEnableMorphing ? 1 : 0, Min = 0, Max = 1, OnChange = v => m_bEnableMorphing = v > 0.5f },
            new Parameter { Name = "[M] Skin Thickness", Value = m_fMorphSkinThickness, Min = 0.5f, Max = 5f, OnChange = v => m_fMorphSkinThickness = v },
            new Parameter { Name = "[Spine] Rib Spacing", Value = m_fSpineRibSpacing, Min = 20, Max = 100, OnChange = v => m_fSpineRibSpacing = v },
            new Parameter { Name = "[Spine] Rib Thick", Value = m_fSpineRibThickness, Min = 2, Max = 20, OnChange = v => m_fSpineRibThickness = v },
            new Parameter { Name = "[Cargo] Floor Skin", Value = m_fCargoFloorSkin, Min = 2, Max = 50, OnChange = v => m_fCargoFloorSkin = v },
            new Parameter { Name = "[Cargo] Roof Skin", Value = m_fCargoRoofSkin, Min = 2, Max = 50, OnChange = v => m_fCargoRoofSkin = v },
            new Parameter { Name = "[Cargo] Roundness", Value = m_fCargoCornerRadius, Min = 5, Max = 100, OnChange = v => m_fCargoCornerRadius = v },
            new Parameter { Name = "[Infill] Y-Density", Value = m_fLatticeDensityY, Min = 0.2f, Max = 3.0f, OnChange = v => m_fLatticeDensityY = v },
            new Parameter { Name = "[Infill] Wave Amp", Value = m_fMorphAmplitude, Min = 0, Max = 20, OnChange = v => m_fMorphAmplitude = v },
            new Parameter { Name = "[Infill] Wave Freq", Value = m_fMorphFrequency, Min = 0.01f, Max = 0.2f, OnChange = v => m_fMorphFrequency = v },
            new Parameter { Name = "[Infill] Space Root", Value = m_fSlatSpacingRoot, Min = 5, Max = 50, OnChange = v => m_fSlatSpacingRoot = v },
            new Parameter { Name = "[Infill] Space Tip", Value = m_fSlatSpacingTip, Min = 5, Max = 50, OnChange = v => m_fSlatSpacingTip = v },
            new Parameter { Name = "[Infill] Wall Root", Value = m_fSlatThickRoot, Min = 0.5f, Max = 5f, OnChange = v => m_fSlatThickRoot = v },
            new Parameter { Name = "[Infill] Wall Tip", Value = m_fSlatThickTip, Min = 0.5f, Max = 5f, OnChange = v => m_fSlatThickTip = v },
        };

        // =====================================================================================
        // --- 1. NEW GEOMETRY LOGIC (MORPHING PIPE) ---
        // =====================================================================================
        
        // This calculates an ELLIPSE radius based on angle (Phi)
        // AspectRatio: Width / Height.  (>1 is Wide, <1 is Tall)
        float GetEllipticalRadius(float phi, float avgRadius, float aspectRatio)
        {
            // Ellipse Math in Polar Coordinates
            // a = semi-major (width), b = semi-minor (height)
            
            // Convert avgRadius + AspectRatio into a/b
            // Area ~ avgRadius^2.  Area = a*b.
            float width = avgRadius * MathF.Sqrt(aspectRatio);
            float height = avgRadius / MathF.Sqrt(aspectRatio);
            
            float a = width;
            float b = height;

            // r(theta) = (ab) / Sqrt( (b cos)^2 + (a sin)^2 )
            float den = MathF.Sqrt(MathF.Pow(b * MathF.Cos(phi), 2) + MathF.Pow(a * MathF.Sin(phi), 2));
            return (a * b) / den;
        }

        private BasePipe GetDuctDefinition(float fYPos, bool bIsSkin)
        {
            CalculateAeroDimensions(out float wIn, out float hIn, out float wOut, out float hOut, out float diamCore);
            float fThick = bIsSkin ? m_fSkinThickness : 0f;
            
            // Adjust dimensions for skin
            // We convert "Box Dimensions" into "Average Radii" for the pipe
            float coreRad = (diamCore / 2f) + fThick;
            float inletAvgRad = (MathF.Sqrt(wIn * hIn) / 2f) + fThick;
            float outletAvgRad = (MathF.Sqrt(wOut * hOut) / 2f) + fThick;

            // Aspect Ratios (Width / Height)
            // Adding thickness changes AR slightly, so recalculate
            float arIn = (wIn + fThick*2) / (hIn + fThick*2);
            float arOut = (wOut + fThick*2) / (hOut + fThick*2);

            List<Vector3> ctrlPoints = CalculateConformalPath(fYPos, m_fEngineDiameter);
            if (!bIsSkin) { // Cutter extension
                ctrlPoints[0] -= Vector3.UnitX * 20f;
                ctrlPoints[4] += Vector3.UnitX * 50f;
            }

            List<Vector3> smoothPath = SplineOperations.aGetNURBSpline(ctrlPoints, 100);
            
            // Use MIN_ROTATION for pipes to keep the "Eye" horizontal relative to the path
            // (BasePipe usually defines 0 degrees as "Right/X" in local frame)
            Frames frames = new Frames(smoothPath, Frames.EFrameType.MIN_ROTATION);
            
            // Create Pipe (Inner Radius 0 = Solid)
            BasePipe sDuct = new BasePipe(frames, 0f, 10f); 

            float t_Center = 0.5f;
            for (int i = 0; i < smoothPath.Count; i++) { if (smoothPath[i].X >= m_fEngineGlobalX) { t_Center = (float)i / smoothPath.Count; break; } }
            
            // --- 3D SURFACE MODULATION ---
            // This defines the shape for every single point (angle, length)
            sDuct.SetRadius(new SurfaceModulation((phi, t) => 
            {
                float angle = phi * 2f * MathF.PI; // Convert 0-1 to Radians
                t = Math.Clamp(t, 0f, 1f);
                
                float currentAvgRad, currentAR;

                if (t <= t_Center) 
                {
                    // Morph: Inlet (Ellipse) -> Core (Circle)
                    float lerp = t / t_Center;
                    // Non-linear easing looks better for inlets
                    float ease = 1f - MathF.Pow(1f - lerp, 2); 
                    
                    currentAvgRad = float_Lerp(inletAvgRad, coreRad, ease);
                    currentAR = float_Lerp(arIn, 1.0f, ease); // Morph AR to 1.0 (Circle)
                } 
                else 
                {
                    // Morph: Core (Circle) -> Outlet (Slit)
                    float lerp = (t - t_Center) / (1f - t_Center);
                    currentAvgRad = float_Lerp(coreRad, outletAvgRad, lerp);
                    currentAR = float_Lerp(1.0f, arOut, lerp); // Morph AR to Slit
                }

                // Calculate the exact radius for this angle to form the Ellipse/Eye
                return GetEllipticalRadius(angle, currentAvgRad, currentAR);

            }), new SurfaceModulation(0f)); // Inner radius 0 (solid)

            return sDuct;
        }

        // =====================================================================================
        // --- 2. PREVIEW LOGIC (Instant Mesh) ---
        // =====================================================================================
        public override void OnPreview(EngineeringContext ctx)
        {
            Mesh mshRight = ConstructHalfPlane(true);
            Mesh mshLeft = ConstructHalfPlane(false);
            Sh.PreviewMesh(mshRight, new ColorFloat(1f,1f,1f), 0.2f);
            Sh.PreviewMesh(mshLeft, new ColorFloat(1f,1f,1f), 0.2f);

            if (m_nEngineCount > 0)
            {
                List<float> yPositions = CalculateEngineYPositions();
                foreach(float y in yPositions)
                {
                    BasePipe ductSkin = GetDuctDefinition(y, true);
                    
                    // Reduce resolution for fast preview
                    ductSkin.SetRadialSteps(32); 
                    ductSkin.SetLengthSteps(20);
                    
                    Mesh mshSkin = ductSkin.mshConstruct();
                    Sh.PreviewMesh(mshSkin, Cp.clrGreen, 0.4f);
                }
            }

            if (m_bEnableMorphing)
            {
                PreviewInfillLines(true);
                PreviewInfillLines(false);
            }
        }

        // =====================================================================================
        // --- 3. CONSTRUCT LOGIC (High Res Voxels) ---
        // =====================================================================================
        protected override void OnConstruct(EngineeringContext ctx)
        {
            if (!m_bGenerate) return; 

            using (new StepTimer("Full Generation"))
            {
                // 1. GENERATE BASE HULL
                Voxels voxBaseHull = null;
                using (new StepTimer("Base Hull Mesh"))
                {
                    Mesh mshRight = ConstructHalfPlane(true); 
                    Mesh mshLeft  = ConstructHalfPlane(false);
                    voxBaseHull = new Voxels();
                    voxBaseHull.RenderMesh(mshRight);
                    voxBaseHull.RenderMesh(mshLeft);
                    voxBaseHull.Smoothen(2f); 
                }

                // 2. GENERATE ENGINES (Using new Pipe Logic)
                Voxels voxEngines = new Voxels();
                Voxels voxCutters = new Voxels();
                
                if (m_nEngineCount > 0)
                {
                    using (new StepTimer("Engines"))
                    {
                        List<float> yPositions = CalculateEngineYPositions();
                        foreach(float y in yPositions) 
                        { 
                            // Skin (High Res)
                            BasePipe pipeSkin = GetDuctDefinition(y, true);
                            pipeSkin.SetRadialSteps(128); 
                            pipeSkin.SetLengthSteps(100);
                            
                            Voxels vSkin = pipeSkin.voxConstruct(); 
                            voxEngines.BoolAdd(vSkin);

                            // Cutter
                            BasePipe pipeCutter = GetDuctDefinition(y, false);
                            pipeCutter.SetRadialSteps(128); 
                            pipeCutter.SetLengthSteps(100);
                            
                            Voxels vCutter = pipeCutter.voxConstruct(); 
                            voxCutters.BoolAdd(vCutter);
                        }
                        
                        // We don't need heavy smoothing anymore because the shape is naturally round!
                        // Just a tiny blend
                        Voxels voxFillet = new Voxels(voxEngines);
                        voxFillet.Offset(m_fFilletRadius);       
                        voxFillet.Offset(-m_fFilletRadius);     
                        voxEngines.BoolAdd(voxFillet);
                    }
                }
                
                // 3. MORPHING LOGIC
                if (!m_bEnableMorphing)
                {
                    ctx.Assembly.BoolAdd(voxBaseHull);
                    if (m_nEngineCount > 0)
                    {
                        ctx.Assembly.BoolAdd(voxEngines);
                        ctx.Assembly.BoolSubtract(voxCutters);
                    }
                }
                else
                {
                    Voxels voxStructureVolume = new Voxels(voxBaseHull);
                    voxStructureVolume.Offset(-m_fMorphSkinThickness); 
                    Voxels voxSkin = new Voxels(voxBaseHull);
                    Voxels voxInner = new Voxels(voxBaseHull);
                    voxInner.Offset(-m_fMorphSkinThickness);
                    voxSkin.BoolSubtract(voxInner);

                    float fCargoCenterZ = 0; float fCargoWidth = 0;
                    Vector2 envStart = GetWingEnvelope(m_fEngineGlobalX - m_fRootChord*0.3f, 0);
                    Vector2 envEnd   = GetWingEnvelope(m_fEngineGlobalX + m_fRootChord*0.3f, 0);
                    float fFloorZ = Math.Max(envStart.Y, envEnd.Y) + m_fCargoFloorSkin;
                    float fCeilingZ = Math.Min(envStart.X, envEnd.X) - m_fCargoRoofSkin;
                    float fCargoHeight = Math.Max(10f, fCeilingZ - fFloorZ);
                    fCargoCenterZ = fFloorZ + (fCargoHeight / 2.0f);
                    float fCargoLength = m_fRootChord * 0.55f; 
                    fCargoWidth  = m_fFuselageWidth * 0.75f; 
                    LocalFrame lfCenter = new LocalFrame(new Vector3(m_fEngineGlobalX, 0, fCargoCenterZ));
                    BaseBox boxCargoRaw = new BaseBox(lfCenter, fCargoLength, fCargoWidth, fCargoHeight);
                    Voxels voxCargo = boxCargoRaw.voxConstruct();
                    voxCargo.Fillet(m_fCargoCornerRadius); 

                    using (new StepTimer("Smart Flow Lattice"))
                    {
                        IImplicit smartLattice = new ImplicitSmartFlowLattice(
                            m_fSlatSpacingRoot, m_fSlatSpacingTip,
                            m_fSlatThickRoot, m_fSlatThickTip,
                            m_fMorphAmplitude, m_fMorphFrequency,
                            m_fSpan,
                            m_fFuselageWidth, 
                            m_fSpineRibThickness, m_fSpineRibSpacing, fCargoWidth, fCargoCenterZ, m_fLatticeDensityY 
                        );
                        // FIX: Generate lattice in FULL HULL, then subtract hollow
                        Voxels voxLatticeFull = new Voxels(voxBaseHull);
                        voxLatticeFull.IntersectImplicit(smartLattice);
                        voxLatticeFull.BoolSubtract(voxCargo);
                        
                        // Trim to fit inside hollow area
                        // (Alternatively, just intersect with structure volume if that's easier)
                        voxLatticeFull.BoolIntersect(voxStructureVolume);
                        
                        ctx.Assembly.BoolAdd(voxLatticeFull);
                    }

                    ctx.Assembly.BoolAdd(voxSkin);            
                    if (m_nEngineCount > 0)
                    {
                        ctx.Assembly.BoolAdd(voxEngines);
                        ctx.Assembly.BoolSubtract(voxCutters);
                    }
                }
            }
            m_bGenerate = false;
        }

        // =====================================================================================
        // --- HELPERS (Copied from previous) ---
        // =====================================================================================

        private void PreviewInfillLines(bool bRightSide)
        {
            float fHalfSpan = m_fSpan / 2.0f;
            float fStartY = 0; 
            float currentY = fStartY;
            int safetyCounter = 0;
            while(currentY < fHalfSpan && safetyCounter < 1000)
            {
                safetyCounter++;
                float t_Span = currentY / fHalfSpan;
                float rawSpacing = float_Lerp(m_fSlatSpacingRoot, m_fSlatSpacingTip, t_Span);
                float effectiveSpacing = rawSpacing / Math.Max(0.1f, m_fLatticeDensityY);
                float sign = bRightSide ? 1f : -1f;
                if (currentY == 0 && sign == -1f) { currentY += effectiveSpacing; continue; }

                PolyLine ribLine = new PolyLine(Cp.clrBillie);
                WingSlice slice = GetWingDataAtY(currentY); 
                float xStart = slice.LeadingEdgeX;
                float xEnd = slice.LeadingEdgeX + slice.Chord;
                bool anyPointValid = false;
                for(float x = xStart; x <= xEnd; x += 30f)
                {
                    float waveOffset = MathF.Sin(x * m_fMorphFrequency) * m_fMorphAmplitude;
                    float waveY = (currentY + waveOffset); 
                    WingSlice waveSlice = GetWingDataAtY(waveY);
                    if (x >= waveSlice.LeadingEdgeX && x <= (waveSlice.LeadingEdgeX + waveSlice.Chord))
                    {
                        Vector2 env = GetWingEnvelope(x, waveY);
                        float midZ = (env.X + env.Y) / 2.0f;
                        ribLine.nAddVertex(new Vector3(x, waveY * sign, midZ));
                        anyPointValid = true;
                    }
                }
                if (anyPointValid) Library.oViewer().Add(ribLine);
                currentY += effectiveSpacing;
            }
        }

        // [All other Helper Classes (ImplicitSmartFlowLattice) and Private Methods stay exactly the same]
        // [Re-pasting them here to ensure the file is complete]

        public class ImplicitSmartFlowLattice : IImplicit
        {
            float m_fSpaceWingRoot, m_fSpaceWingTip;
            float m_fThickWingRoot, m_fThickWingTip;
            float m_fAmp, m_fFreq;
            float m_fTotalSpan;
            float m_fFuselageWidth;
            float m_fBodyRibThickness;
            float m_fBodyRibSpacing;
            float m_fYDensityScale; 
            float m_fCargoRadiusY;
            float m_fCargoCenterZ;

            public ImplicitSmartFlowLattice(
                float spaceWingRoot, float spaceWingTip, float thickWingRoot, float thickWingTip, 
                float amp, float freq, float totalSpan, float fuselageWidth, float bodyRibThick, float bodyRibSpacing,
                float cargoWidth, float cargoCenterZ, float yDensityScale)
            {
                m_fSpaceWingRoot = spaceWingRoot; m_fSpaceWingTip = spaceWingTip;
                m_fThickWingRoot = thickWingRoot; m_fThickWingTip = thickWingTip;
                m_fAmp = amp; m_fFreq = freq;
                m_fTotalSpan = totalSpan;
                m_fFuselageWidth = fuselageWidth;
                m_fBodyRibThickness = bodyRibThick;
                m_fBodyRibSpacing = bodyRibSpacing;
                m_fCargoRadiusY = cargoWidth / 2.0f;
                m_fCargoCenterZ = cargoCenterZ;
                m_fYDensityScale = yDensityScale;
            }

            public float fSignedDistance(in Vector3 vecPt)
            {
                Vector3 vecWarped = vecPt;
                float dy = vecPt.Y; 
                float dz = vecPt.Z - m_fCargoCenterZ; 
                float distFromCargoCenter = MathF.Sqrt(dy*dy + dz*dz);
                float fRepulsionRadius = m_fCargoRadiusY * 1.5f; 

                if (distFromCargoCenter < fRepulsionRadius && distFromCargoCenter > 0.1f)
                {
                    float t_Repel = 1.0f - (distFromCargoCenter / fRepulsionRadius);
                    t_Repel = Math.Clamp(t_Repel, 0f, 1f);
                    t_Repel = t_Repel * t_Repel; 
                    float fWarpMag = m_fCargoRadiusY * 0.8f * t_Repel;
                    float dirY = -dy / distFromCargoCenter;
                    float dirZ = -dz / distFromCargoCenter;
                    vecWarped.Y += dirY * fWarpMag;
                    vecWarped.Z += dirZ * fWarpMag;
                }

                float fYAbs = Math.Abs(vecPt.Y);
                float fShoulderY = m_fFuselageWidth / 2.0f;
                float t_Zone = SmoothStep(fShoulderY - 30f, fShoulderY + 30f, fYAbs);

                float t_Span = fYAbs / (m_fTotalSpan / 2.0f);
                float currentSpacing = float_Lerp(m_fBodyRibSpacing, float_Lerp(m_fSpaceWingRoot, m_fSpaceWingTip, t_Span), t_Zone);
                float currentThick = float_Lerp(m_fBodyRibThickness, float_Lerp(m_fThickWingRoot, m_fThickWingTip, t_Span), t_Zone);
                float currentAmp = m_fAmp * t_Zone; 

                float fWaveOffset = MathF.Sin(vecWarped.X * m_fFreq) * currentAmp;
                float effectiveSpacingY = currentSpacing / m_fYDensityScale;
                float fYDistorted = vecWarped.Y + fWaveOffset;
                float fSineY = MathF.Sin((fYDistorted * MathF.PI * 2.0f) / effectiveSpacingY);
                float fDistY = (MathF.Abs(fSineY) * (effectiveSpacingY / (2f * MathF.PI))) - (currentThick * 0.5f);
                float fZDistorted = vecWarped.Z + fWaveOffset; 
                float fSineZ = MathF.Sin((fZDistorted * MathF.PI * 2.0f) / currentSpacing);
                float fDistZ = (MathF.Abs(fSineZ) * (currentSpacing / (2f * MathF.PI))) - (currentThick * 0.5f);
                return Math.Min(fDistY, fDistZ);
            }

            private static float float_Lerp(float a, float b, float t) => a + (b - a) * t;
            private static float SmoothStep(float edge0, float edge1, float x) {
                float t = Math.Clamp((x - edge0) / (edge1 - edge0), 0.0f, 1.0f);
                return t * t * (3.0f - 2.0f * t);
            }
        }
        
        struct WingSlice { public float Chord, Thickness, LeadingEdgeX, Z; }
        private WingSlice GetWingDataAtY(float y)
        {
            float fHalfSpan = m_fSpan / 2.0f;
            float fShoulderY = m_fFuselageWidth / 2.0f;
            float yAbs = MathF.Abs(y);
            WingSlice s = new WingSlice();
            if (yAbs <= fShoulderY) {
                float t = yAbs / fShoulderY;
                s.Chord = float_Lerp(m_fRootChord, m_fShoulderChord, t);
                s.Thickness = float_Lerp(m_fRootThickness, m_fRootThickness * 0.8f, t);
                s.LeadingEdgeX = yAbs * MathF.Tan(DegreesToRadians(m_fSweepDegrees * 1.5f));
            } else {
                float t = (yAbs - fShoulderY) / (fHalfSpan - fShoulderY);
                s.Chord = float_Lerp(m_fShoulderChord, m_fTipChord, t);
                s.Thickness = float_Lerp(m_fRootThickness * 0.8f, m_fTipThickness, t);
                float fXAtShoulder = fShoulderY * MathF.Tan(DegreesToRadians(m_fSweepDegrees * 1.5f));
                s.LeadingEdgeX = fXAtShoulder + (yAbs - fShoulderY) * MathF.Tan(DegreesToRadians(m_fSweepDegrees));
            }
            s.Z = (yAbs / fHalfSpan) * (yAbs / fHalfSpan) * (fHalfSpan * MathF.Tan(DegreesToRadians(m_fDihedralDeg)));
            return s;
        }

        private Vector2 GetWingEnvelope(float x, float y)
        {
            WingSlice s = GetWingDataAtY(y);
            float x_local = x - s.LeadingEdgeX;
            float x_pct = x_local / s.Chord;
            if (x_pct < 0 || x_pct > 1) return new Vector2(s.Z, s.Z);
            float m = m_fCamberPercent / 100f;
            float p = 0.4f;
            float z_camber = (x_pct < p) ? (m / (p * p)) * (2 * p * x_pct - x_pct * x_pct) : (m / ((1 - p) * (1 - p))) * (1 - 2 * p + 2 * p * x_pct - x_pct * x_pct);
            float z_thick = 5.0f * s.Thickness * (0.2969f * MathF.Sqrt(x_pct) - 0.1260f * x_pct - 0.3516f * MathF.Pow(x_pct, 2) + 0.2843f * MathF.Pow(x_pct, 3) - 0.1015f * MathF.Pow(x_pct, 4));
            return new Vector2(s.Z + (z_camber + z_thick) * s.Chord, s.Z + (z_camber - z_thick) * s.Chord);
        }

        private Mesh ConstructHalfPlane(bool bIsRightSide, float fDihedralOverride = -999f, float fTwistOverride = -999f)
        {
            int nRibsTotal = 40;
            List<List<Vector3>> aRibs = new List<List<Vector3>>();
            float fHalfSpan = m_fSpan / 2.0f;
            for (int i = 0; i < nRibsTotal; i++) {
                float t = (float)i / (nRibsTotal - 1);
                float fCurrentY_Abs = t * fHalfSpan;
                float fCurrentY = bIsRightSide ? fCurrentY_Abs : -fCurrentY_Abs;
                WingSlice s = GetWingDataAtY(fCurrentY_Abs);
                float twist = float_Lerp(0, -m_fTwistDegrees, t);
                List<Vector3> aPoints = GenerateNacaPoints(s.Thickness);
                Matrix4x4 mat = Matrix4x4.CreateScale(s.Chord) * 
                                Matrix4x4.CreateTranslation(-s.Chord*0.25f, 0, 0) * 
                                Matrix4x4.CreateRotationY(DegreesToRadians(twist)) * 
                                Matrix4x4.CreateTranslation(s.LeadingEdgeX, fCurrentY, s.Z);
                aRibs.Add(TransformPoints(aPoints, mat));
            }
            return MeshFromRibs(aRibs);
        }
        
        private List<Vector3> GenerateNacaPoints(float fThicknessRatio, int nResolution = 40)
        {
            float m = m_fCamberPercent / 100f;
            float p = 0.4f;
            float t = fThicknessRatio;
            List<Vector3> aUpper = new List<Vector3>();
            List<Vector3> aLower = new List<Vector3>();
            for (int i = 0; i <= nResolution; i++) {
                float x = (float)i / nResolution;
                float z_camber = (x < p) ? (m / (p * p)) * (2 * p * x - x * x) : (m / ((1 - p) * (1 - p))) * (1 - 2 * p + 2 * p * x - x * x);
                float z_thickness = 5.0f * t * (0.2969f * MathF.Sqrt(x) - 0.1260f * x - 0.3516f * MathF.Pow(x, 2) + 0.2843f * MathF.Pow(x, 3) - 0.1015f * MathF.Pow(x, 4));
                aUpper.Add(new Vector3(x, 0, z_camber + z_thickness));
                aLower.Add(new Vector3(x, 0, z_camber - z_thickness));
            }
            List<Vector3> aPoints = new List<Vector3>();
            aPoints.AddRange(aUpper);
            for (int i = aLower.Count - 2; i > 0; i--) aPoints.Add(aLower[i]);
            aPoints.Add(aPoints[0]);
            return aPoints;
        }

        private Mesh MeshFromRibs(List<List<Vector3>> aRibs)
        {
            Mesh msh = new Mesh();
            int nPointsPerRib = aRibs[0].Count;
            for (int i = 0; i < aRibs.Count - 1; i++) {
                for (int j = 0; j < nPointsPerRib - 1; j++) {
                    msh.nAddTriangle(aRibs[i][j], aRibs[i+1][j], aRibs[i+1][j+1]); 
                    msh.nAddTriangle(aRibs[i][j], aRibs[i+1][j+1], aRibs[i][j+1]);
                }
            }
            AddCap(msh, aRibs[0], true);
            AddCap(msh, aRibs[^1], false);
            return msh;
        }

        private void AddCap(Mesh msh, List<Vector3> ribPoints, bool bReverseWinding)
        {
            Vector3 vecCenter = Vector3.Zero;
            foreach (var p in ribPoints) vecCenter += p;
            vecCenter /= ribPoints.Count;
            for (int i = 0; i < ribPoints.Count - 1; i++) {
                if (bReverseWinding) msh.nAddTriangle(vecCenter, ribPoints[i+1], ribPoints[i]);
                else msh.nAddTriangle(vecCenter, ribPoints[i], ribPoints[i+1]);
            }
        }

        private List<Vector3> TransformPoints(List<Vector3> aInputPoints, Matrix4x4 matTransform)
        {
            var aOutput = new List<Vector3>();
            foreach (var vec in aInputPoints) aOutput.Add(Vector3.Transform(vec, matTransform));
            return aOutput;
        }
        
        private List<float> CalculateEngineYPositions()
        {
            List<float> yPositions = new List<float>();
            float fShoulderY = m_fFuselageWidth / 2.0f;
            float fHalfSpan  = m_fSpan / 2.0f;
            float fBaseY     = fShoulderY + m_fWingPositionPct * (fHalfSpan - fShoulderY);

            if (m_nEngineCount == 1) yPositions.Add(0);
            else if (m_nEngineCount == 2) { yPositions.Add(fBaseY); yPositions.Add(-fBaseY); }
            else if (m_nEngineCount == 3) { yPositions.Add(0); yPositions.Add(fBaseY); yPositions.Add(-fBaseY); }
            else if (m_nEngineCount == 4) {
                float offset = m_fArraySpacing / 2.0f;
                yPositions.Add(fBaseY - offset); yPositions.Add(fBaseY + offset);
                yPositions.Add(-(fBaseY - offset)); yPositions.Add(-(fBaseY + offset));
            }
            return yPositions;
        }

        private void CalculateAeroDimensions(out float wIn, out float hIn, out float wOut, out float hOut, out float diamCore)
        {
            diamCore = m_fEngineDiameter;
            float fAreaInlet  = m_fThrust * 50000f; 
            float fAreaOutlet = m_fThrust * 30000f; 
            hIn = MathF.Sqrt(fAreaInlet / m_fInletWidthRatio);
            wIn = hIn * m_fInletWidthRatio;
            float fMaxHeight = m_fEngineDiameter * 1.5f; 
            if (hIn > fMaxHeight) { hIn = fMaxHeight; wIn = fAreaInlet / hIn; }
            hOut = MathF.Sqrt(fAreaOutlet / m_fOutletWidthRatio);
            wOut = hOut * m_fOutletWidthRatio;
        }

        private List<Vector3> CalculateConformalPath(float fYPos, float fCoreDiam)
        {
            float xCoreStart = m_fEngineGlobalX - (m_fEngineLength / 2f);
            float xCoreEnd   = m_fEngineGlobalX + (m_fEngineLength / 2f);
            float xInlet  = xCoreStart - (fCoreDiam * 3.0f);
            float xOutlet = xCoreEnd + (fCoreDiam * 2.0f);
            Vector2 envInlet  = GetWingEnvelope(xInlet, fYPos);
            Vector2 envCore   = GetWingEnvelope(m_fEngineGlobalX, fYPos);
            Vector2 envOutlet = GetWingEnvelope(xOutlet, fYPos);
            float zWingBot = envCore.Y;
            float zWingTop = envCore.X;
            float zIdeal   = zWingTop + m_fEngineGlobalZ;
            float zSafeBot = zWingBot + (fCoreDiam/2f) + 2f;
            float zCoreAbs = Math.Max(zIdeal, zSafeBot);
            float zInletAbs  = envInlet.X + m_fInletOffsetZ + m_fEngineGlobalZ;
            float zOutletAbs = envOutlet.X + m_fOutletOffsetZ + m_fEngineGlobalZ;
            float yOutlet = fYPos;
            float fTanLen = 50f;
            if (m_bMergeExhausts && MathF.Abs(fYPos) > 10f) { yOutlet = fYPos * 0.4f; }
            return new List<Vector3> {
                new Vector3(xInlet, fYPos, zInletAbs),
                new Vector3(xInlet + fTanLen, fYPos, zInletAbs),
                new Vector3(m_fEngineGlobalX, fYPos, zCoreAbs),
                new Vector3(xOutlet - fTanLen, yOutlet, zOutletAbs),
                new Vector3(xOutlet, yOutlet, zOutletAbs)
            };
        }

        private float DegreesToRadians(float d) => d * (MathF.PI / 180f);
        private static float float_Lerp(float a, float b, float t) => a + (b - a) * t;
    }
}