using System;
using System.Collections.Generic;
using System.Numerics;
using PicoGK;
using Leap71.ShapeKernel;
using MyFirstApp.Core;
using MyFirstApp.Core.Engine;

namespace MyFirstApp.Algorithms.Playground
{
    public class NautilusGen : EngineeringComponent
    {
        // --- RAUP'S COILING PARAMETERS ---
        float m_Scale = 2f;           
        float m_ExpansionW = 2.93f;   
        float m_Rotations = 2.7f;     
        
        // --- SHAPE PARAMETERS ---
        float m_ApertureScale = 0.60f;
        float m_ShellWidth = 0.82f;    
        float m_WallThick = 1.0f;     
        
        // --- EXTERNAL RIBS ---
        float m_RibAmp = 0.4f;         
        float m_RibFreq = 60f;         
        
        // --- INTERNAL STRUCTURE ---
        float m_ChamberCount = 32f;   
        float m_SeptaThick = 0.6f;
        
        // Curvature: 1.2 = Deep Umbrella, 3.0 = Flat Plate
        float m_SeptaCurvature = 1.5f; 
        
        bool  m_Cutaway = true;       

        public NautilusGen() { Name = "Test Lab: Nautilus"; }

        protected override List<Parameter> GetComponentParameters() => new List<Parameter> {
            new Parameter { Name = "Initial Size", Value=m_Scale, Min=1, Max=10, OnChange=v=>m_Scale=v },
            new Parameter { Name = "Expansion (W)", Value=m_ExpansionW, Min=1.5f, Max=4.0f, OnChange=v=>m_ExpansionW=v },
            new Parameter { Name = "Rotations", Value=m_Rotations, Min=1.5f, Max=4.0f, OnChange=v=>m_Rotations=v },
            
            new Parameter { Name = "Shell Width", Value=m_ShellWidth, Min=0.4f, Max=1.5f, OnChange=v=>m_ShellWidth=v },
            new Parameter { Name = "Aperture Size", Value=m_ApertureScale, Min=0.3f, Max=0.8f, OnChange=v=>m_ApertureScale=v },
            
            new Parameter { Name = "Rib Height", Value=m_RibAmp, Min=0f, Max=2f, OnChange=v=>m_RibAmp=v },
            new Parameter { Name = "Rib Count", Value=m_RibFreq, Min=0f, Max=150f, OnChange=v=>m_RibFreq=v },

            new Parameter { Name = "Chambers", Value=m_ChamberCount, Min=0, Max=50, OnChange=v=>m_ChamberCount=v },
            new Parameter { Name = "Septa Thickness", Value=m_SeptaThick, Min=0.2f, Max=2f, OnChange=v=>m_SeptaThick=v },
            new Parameter { Name = "Septa Curvature", Value=m_SeptaCurvature, Min=1.1f, Max=4.0f, OnChange=v=>m_SeptaCurvature=v },

            new Parameter { Name = "Show Cutaway", Value=m_Cutaway?1:0, Min=0, Max=1, OnChange=v=>m_Cutaway=v==1 }
        };

        public override void OnPreview(EngineeringContext ctx)
        {
            float maxTheta = m_Rotations * 2f * MathF.PI;
            float k = MathF.Log(m_ExpansionW) / (2f * MathF.PI);
            Vector3 prev = GetLogSpiralPos(0, k);

            for(int i=1; i<=100; i++)
            {
                float theta = (i / 100f) * maxTheta;
                Vector3 pos = GetLogSpiralPos(theta, k);
                Vis.Line(prev, pos, Pal.Warning);
                prev = pos;
            }
        }

        Vector3 GetLogSpiralPos(float theta, float k)
        {
            float r = m_Scale * MathF.Exp(k * theta);
            return new Vector3(MathF.Cos(theta) * r, MathF.Sin(theta) * r, 0);
        }

        protected override void OnConstruct(EngineeringContext ctx)
        {
            float maxTheta = m_Rotations * 2f * MathF.PI;
            float k = MathF.Log(m_ExpansionW) / (2f * MathF.PI); 

            // 1. SPINE
            List<Vector3> pathPoints = new List<Vector3>();
            int steps = 300; 
            for (int i = 0; i <= steps; i++)
            {
                float theta = (i / (float)steps) * maxTheta;
                pathPoints.Add(GetLogSpiralPos(theta, k));
            }
            Frames spine = new Frames(pathPoints, Vector3.UnitZ, 0.5f);

            // 2. RADIUS
            float rStart = m_Scale * m_ApertureScale;
            float rEnd   = (m_Scale * MathF.Exp(k * maxTheta)) * m_ApertureScale;

            SurfaceModulation.RatioFunc innerRadFunc = (phi, t) => rStart + t * (rEnd - rStart);
            SurfaceModulation.RatioFunc outerRadFunc = (phi, t) => {
                float baseR = rStart + t * (rEnd - rStart);
                float ripple = m_RibAmp * MathF.Sin(t * m_RibFreq * MathF.PI * 2f);
                if(ripple < 0) ripple = 0;
                return baseR + m_WallThick + ripple;
            };

            // 3. TRANSFORM
            BaseShape.fnVertexTransformation ellipseTrafo = (vec) => 
                new Vector3(vec.X, vec.Y, vec.Z * m_ShellWidth);

            // 4. SHELL & VOID
            BasePipe outerPipe = new BasePipe(spine, 0f, 0f);
            outerPipe.SetRadius(new SurfaceModulation(innerRadFunc), new SurfaceModulation(outerRadFunc));
            outerPipe.SetTransformation(ellipseTrafo);
            Voxels vOuter = outerPipe.voxConstruct();

            BasePipe innerPipe = new BasePipe(spine, 0f, 0f);
            innerPipe.SetRadius(new SurfaceModulation(0f), new SurfaceModulation(innerRadFunc));
            innerPipe.SetTransformation(ellipseTrafo);
            Voxels vInner = innerPipe.voxConstruct(); 

            // 5. MAKE MAIN SHELL
            Voxels vShell = vOuter.voxDuplicate();
            vShell.BoolSubtract(vInner);

            // 6. BUILD SEPTA (SAFETY CLIPPER METHOD)
            if (m_ChamberCount > 0)
            {
                Lattice latPlugs = new Lattice();
                Lattice latScoops = new Lattice();
                
                // We also create a "Safety Lattice" of cylinders to clip the leaking parts
                // Note: We can't use Lattice for cylinders, so we use a BasePipe approach later.
                // But creating 32 BasePipes is slow.
                // OPTIMIZATION: We can define the "Safety Zone" as a pipe slightly larger than the inner void.
                // Or simply trust that if we keep the sphere small enough, it works.
                // Let's use the "Exact Fit" math.

                for(int i = 1; i <= (int)m_ChamberCount; i++)
                {
                    float theta = i * (maxTheta / m_ChamberCount);
                    float num = MathF.Exp(k * theta) - 1f;
                    float den = MathF.Exp(k * maxTheta) - 1f;
                    float t = Math.Clamp(num / den, 0.01f, 0.99f);

                    Vector3 pos = spine.vecGetSpineAlongLength(t);
                    Vector3 tangent = spine.vecGetLocalZAlongLength(t); 
                    float localR = rStart + t * (rEnd - rStart);
                    
                    // --- UMBRELLA MATH ---
                    float sphereR = localR * m_SeptaCurvature;
                    
                    // Calculate Offset: d = sqrt(R^2 - r^2)
                    // This anchors the rim of the sphere to the tube wall
                    // If curvature is high (flat), R is big, d is big -> sphere is far back.
                    // If curvature is low (deep), R is small (~r), d is small -> sphere is at pos.
                    
                    // Safety check
                    if(sphereR < localR * 1.01f) sphereR = localR * 1.01f;
                    
                    float offsetD = MathF.Sqrt((sphereR * sphereR) - (localR * localR));
                    
                    // Orientation: Push CENTER forward (Tangent) -> Concave (Dish)
                    Vector3 center = pos + (tangent * offsetD);

                    // Add to Lattices
                    latPlugs.AddSphere(center, sphereR);
                    latScoops.AddSphere(center, sphereR - m_SeptaThick);
                }

                // Generate Voxels
                Voxels vPlugs = new Voxels(latPlugs);
                Mesh mPlugs = vPlugs.mshAsMesh();
                mPlugs = MeshUtility.mshApplyTransformation(mPlugs, ellipseTrafo);
                vPlugs = new Voxels(mPlugs);
                
                Voxels vScoops = new Voxels(latScoops);
                Mesh mScoops = vScoops.mshAsMesh();
                mScoops = MeshUtility.mshApplyTransformation(mScoops, ellipseTrafo);
                vScoops = new Voxels(mScoops);

                // Create Hollow Walls
                vPlugs.BoolSubtract(vScoops);

                // --- THE SAFETY CLIPPER ---
                // To stop the "Leaking", we intersect the septa with the TUBE VOID.
                // This clips off the "Side Walls" because they exist *outside* the tube 
                // (or inside the neighbor tube, which vInner technically includes).
                
                // Wait, if vInner includes neighbor tube, Intersecting keeps the leak!
                // TRICK: We intersect with a slightly SHRUNKEN version of vInner.
                // Or we create a "Guard" pipe that is only the CURRENT segment.
                // Since we can't easily isolate the segment, let's use a "Thickened Spine" clip.
                // Actually, the simplest fix is: Ensure the Septa don't touch the neighbor.
                // This is hard with tight spirals.
                
                // Let's try strictly Intersecting with vInner first.
                // If the leak persists, it means the septum is bridging the gap between whorls.
                vPlugs.BoolIntersect(vInner);

                // Drill Siphuncle
                float siphRad = 1.0f;
                BasePipe drillPipe = new BasePipe(spine, 0f, 0f);
                drillPipe.SetRadius(new SurfaceModulation(0f), new SurfaceModulation(siphRad));
                Voxels vDrill = drillPipe.voxConstruct();
                vPlugs.BoolSubtract(vDrill);

                // Add to Shell
                vShell.BoolAdd(vPlugs);

                // Add Siphuncle
                BasePipe siphPipe = new BasePipe(spine, 0f, 0f);
                siphPipe.SetRadius(new SurfaceModulation(siphRad), new SurfaceModulation(siphRad + 0.5f));
                Voxels vSiphuncle = siphPipe.voxConstruct();
                vSiphuncle.BoolIntersect(vInner);
                vShell.BoolAdd(vSiphuncle);
            }

            // 7. CUTAWAY
            if (m_Cutaway)
            {
                BBox3 bounds = vShell.oCalculateBoundingBox();
                Vector3 center = bounds.vecCenter();
                Vector3 size = bounds.vecSize();
                
                float cutHeight = center.Z + (size.Z * 0.05f); 
                float pad = 5f; 
                
                LocalFrame cutFrame = new LocalFrame(new Vector3(center.X, center.Y, cutHeight));
                BaseBox cutter = new BaseBox(cutFrame, size.X + pad, size.Y + pad, size.Z + pad);
                vShell.BoolSubtract(cutter.voxConstruct());
            }

            ctx.Assembly.BoolAdd(vShell);
        }
    }
}