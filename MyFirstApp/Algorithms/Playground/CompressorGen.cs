using System;
using System.Collections.Generic;
using System.Numerics;
using System.Linq;
using System.Threading.Tasks;
using PicoGK;
using Leap71.ShapeKernel;
using MyFirstApp.Core;
using MyFirstApp.Core.Engine;

namespace MyFirstApp.Algorithms.Playground
{
    public class CompressorGen : EngineeringComponent
    {
        // --- DIMENSIONS ---
        float m_OutletRadius = 80f;     
        float m_InletRadius = 25f;      
        float m_AxialHeight = 45f;      
        float m_ShaftDiameter = 8f;     
        
        // --- AERODYNAMICS ---
        float m_Beta1_Hub = -30f;       
        float m_Beta1_Shroud = -60f;    
        float m_Beta2 = -40f;           
        
        // --- MECHANICAL ---
        float m_BladeCount = 7f;        
        bool  m_UseSplitters = true;    
        float m_SplitterStart = 0.4f;   
        float m_HubInletRatio = 0.45f;
        float m_Ellipticity = 0.5f;     

        // THICKNESS
        float m_ThickRoot = 2.5f;       
        float m_ThickTip = 0.8f;        

        // --- INTERNAL STRUCTURE ---
        bool m_LatticeInfill = true;    
        float m_SkinThickness = 1.5f;   
        float m_ShaftWall = 3.0f;       
        float m_LatticeBeam = 1.2f;     
        float m_LatticeSize = 7.0f;     

        public CompressorGen() { Name = "Propulsion: Aero Impeller"; }

        protected override List<Parameter> GetComponentParameters() => new List<Parameter> {
            new Parameter { Name = "Outlet Radius", Value=m_OutletRadius, Min=40, Max=150, OnChange=v=>m_OutletRadius=v },
            new Parameter { Name = "Inlet Radius", Value=m_InletRadius, Min=10, Max=60, OnChange=v=>m_InletRadius=v },
            new Parameter { Name = "Axial Height", Value=m_AxialHeight, Min=20, Max=80, OnChange=v=>m_AxialHeight=v },
            new Parameter { Name = "Shaft Diameter", Value=m_ShaftDiameter, Min=4, Max=20, OnChange=v=>m_ShaftDiameter=v },
            
            new Parameter { Name = "Main Blade Count", Value=m_BladeCount, Min=3, Max=12, OnChange=v=>m_BladeCount=v },
            new Parameter { Name = "Use Splitters", Value=m_UseSplitters?1:0, Min=0, Max=1, OnChange=v=>m_UseSplitters=v==1 },
            
            new Parameter { Name = "Lattice Infill", Value=m_LatticeInfill?1:0, Min=0, Max=1, OnChange=v=>m_LatticeInfill=v==1 },
            new Parameter { Name = "Lattice Cell Size", Value=m_LatticeSize, Min=3f, Max=15f, OnChange=v=>m_LatticeSize=v },

            new Parameter { Name = "Root Thickness", Value=m_ThickRoot, Min=1f, Max=5f, OnChange=v=>m_ThickRoot=v },
            new Parameter { Name = "Tip Thickness", Value=m_ThickTip, Min=0.2f, Max=2f, OnChange=v=>m_ThickTip=v },
            
            new Parameter { Name = "Inducer Hub", Value=m_Beta1_Hub, Min=-70f, Max=0f, OnChange=v=>m_Beta1_Hub=v },
            new Parameter { Name = "Inducer Shroud", Value=m_Beta1_Shroud, Min=-80f, Max=-20f, OnChange=v=>m_Beta1_Shroud=v },
            new Parameter { Name = "Backsweep", Value=m_Beta2, Min=-60f, Max=0f, OnChange=v=>m_Beta2=v },
            new Parameter { Name = "Curve Shape", Value=m_Ellipticity, Min=0.2f, Max=0.9f, OnChange=v=>m_Ellipticity=v }
        };

        public override void OnPreview(EngineeringContext ctx)
        {
            Vector3 center = new Vector3(0,0, m_AxialHeight * (1f - m_SplitterStart));
            Vis.Circle(center, m_InletRadius, Pal.Steel);
        }

        protected override void OnConstruct(EngineeringContext ctx)
        {
            // DRAFT MODE CHECK
            float currentVoxelSize = AppConfig.VoxelSize;
            float activeThickTip = m_ThickTip;
            bool highQuality = currentVoxelSize <= 0.2f;

            if (!highQuality)
            {
                // Force tip to be robust in low res
                float minThick = currentVoxelSize * 2.5f;
                if (activeThickTip < minThick) activeThickTip = minThick;
            }

            // 1. BEZIER PROFILES
            float rHubInlet = m_InletRadius * m_HubInletRatio; 
            float b2 = m_AxialHeight * 0.15f;       
            
            BezierCurve hubProfile = new BezierCurve(
                new Vector2(rHubInlet, m_AxialHeight),      
                new Vector2(rHubInlet, m_AxialHeight * (1-m_Ellipticity)), 
                new Vector2(m_OutletRadius * (1-m_Ellipticity), 0),        
                new Vector2(m_OutletRadius, 0)              
            );

            BezierCurve shroudProfile = new BezierCurve(
                new Vector2(m_InletRadius, m_AxialHeight),  
                new Vector2(m_InletRadius, m_AxialHeight * (1-m_Ellipticity)),
                new Vector2(m_OutletRadius * (1-m_Ellipticity), b2),
                new Vector2(m_OutletRadius, b2)             
            );

            // 2. INTEGRATE STREAMLINES
            int steps = highQuality ? 150 : 80;
            List<Vector3> ptsHub = IntegrateStreamline(hubProfile, m_Beta1_Hub, m_Beta2, steps);
            List<Vector3> ptsShroud = IntegrateStreamline(shroudProfile, m_Beta1_Shroud, m_Beta2, steps);

            // 3. BUILD BLADE MESHES
            Mesh mMainBlade = ConstructBladeSolid(ptsHub, ptsShroud, 0, steps, m_ThickRoot, activeThickTip);
            
            // Fix CS8600: Nullable Mesh
            Mesh? mSplitterBlade = null;
            if (m_UseSplitters)
            {
                int startIndex = (int)(steps * m_SplitterStart);
                mSplitterBlade = ConstructBladeSolid(ptsHub, ptsShroud, startIndex, steps, m_ThickRoot, activeThickTip);
            }

            // 4. ARRAY BLADES
            Mesh mImpeller = new Mesh();
            float angleStep = 360f / m_BladeCount;
            
            for(int i=0; i < (int)m_BladeCount; i++)
            {
                float rot = i * angleStep * (MathF.PI / 180f);
                Quaternion q = Quaternion.CreateFromAxisAngle(Vector3.UnitZ, rot);
                Matrix4x4 mat = Matrix4x4.CreateFromQuaternion(q);
                mImpeller.Append(mMainBlade.mshCreateTransformed(mat));
                
                if (m_UseSplitters && mSplitterBlade != null)
                {
                    float rotSplit = (i + 0.5f) * angleStep * (MathF.PI / 180f);
                    Quaternion qSplit = Quaternion.CreateFromAxisAngle(Vector3.UnitZ, rotSplit);
                    mImpeller.Append(mSplitterBlade.mshCreateTransformed(Matrix4x4.CreateFromQuaternion(qSplit)));
                }
            }

            Voxels vImpeller = new Voxels(mImpeller);

            // 5. HUB GEOMETRY PREP
            float zMin = -2f;
            float zMax = m_AxialHeight + 1f;
            int lookupRes = 500;
            float[] radiusCache = new float[lookupRes];
            
            var rawTable = ptsHub.Select(p => new Vector2(p.Z, new Vector2(p.X, p.Y).Length())).OrderBy(v => v.X).ToList();
            for(int i=0; i<lookupRes; i++)
            {
                float t = i / (float)(lookupRes - 1);
                float z = zMin + t * (zMax - zMin);
                radiusCache[i] = InterpolateRadius(rawTable, z, rHubInlet, m_OutletRadius);
            }

            // 6. GENERATE HUB
            List<Vector3> spinePts = new List<Vector3>();
            for(int i=0; i<=steps; i++) spinePts.Add(new Vector3(0,0, zMin + i * (zMax - zMin)/steps));
            
            Frames hubFrame = new Frames(spinePts, Vector3.UnitX, 0.5f);
            BasePipe hubBody = new BasePipe(hubFrame, 0f, 0f);
            
            SurfaceModulation.RatioFunc hubRadFunc = (phi, t) => {
                int idx = (int)(t * (lookupRes - 1));
                if(idx < 0) idx = 0; if(idx >= lookupRes) idx = lookupRes-1;
                return radiusCache[idx];
            };
            
            hubBody.SetRadius(new SurfaceModulation(0f), new SurfaceModulation(hubRadFunc));
            
            // Fix CS1503: Cast int to uint
            hubBody.SetLengthSteps((uint)(highQuality ? 150 : 80)); 
            hubBody.SetRadialSteps((uint)(highQuality ? 128 : 64)); 
            
            Voxels vHub = hubBody.voxConstruct();

            // --- LATTICE INFILL ---
            if (m_LatticeInfill)
            {
                float sleeveRad = (m_ShaftDiameter/2f) + m_ShaftWall;
                BaseCylinder sleeve = new BaseCylinder(new LocalFrame(), m_AxialHeight + 1f, sleeveRad); 
                sleeve.SetTransformation(v => v + new Vector3(0,0,-1f));
                Voxels vSleeve = sleeve.voxConstruct();
                
                Voxels vCavity = vHub.voxDuplicate();
                vCavity.Offset(-m_SkinThickness);
                vCavity.BoolSubtract(vSleeve);
                
                Lattice lat = new Lattice();
                BBox3 bounds = vHub.oCalculateBoundingBox();
                
                int nx = (int)(bounds.vecSize().X / m_LatticeSize);
                int ny = (int)(bounds.vecSize().Y / m_LatticeSize);
                int nz = (int)(bounds.vecSize().Z / m_LatticeSize);
                
                for(int z=0; z<=nz; z++)
                {
                    float posZ = bounds.vecMin.Z + z * m_LatticeSize;
                    
                    float t_z = (posZ - zMin) / (zMax - zMin);
                    int z_idx = (int)(t_z * (lookupRes - 1));
                    if (z_idx < 0) z_idx = 0; if (z_idx >= lookupRes) z_idx = lookupRes-1;
                    
                    float maxR_at_Z = radiusCache[z_idx] - (m_SkinThickness * 0.5f);
                    float maxR_sq = maxR_at_Z * maxR_at_Z;

                    for(int y=0; y<=ny; y++)
                    for(int x=0; x<=nx; x++)
                    {
                        Vector3 pos = bounds.vecMin + new Vector3(x*m_LatticeSize, y*m_LatticeSize, z*m_LatticeSize);
                        
                        if ((pos.X*pos.X + pos.Y*pos.Y) > maxR_sq) continue;

                        if(x<nx) lat.AddBeam(pos, pos + new Vector3(m_LatticeSize,0,0), m_LatticeBeam, m_LatticeBeam, true);
                        if(y<ny) lat.AddBeam(pos, pos + new Vector3(0,m_LatticeSize,0), m_LatticeBeam, m_LatticeBeam, true);
                        if(z<nz) lat.AddBeam(pos, pos + new Vector3(0,0,m_LatticeSize), m_LatticeBeam, m_LatticeBeam, true);
                        
                        if(x<nx && y<ny && z<nz)
                             lat.AddBeam(pos, pos + new Vector3(m_LatticeSize, m_LatticeSize, m_LatticeSize), m_LatticeBeam, m_LatticeBeam, true);
                    }
                }
                
                Voxels vLat = new Voxels(lat);
                
                if (highQuality) 
                {
                    vLat.Smoothen(2.0f); 
                }
                
                vLat.BoolIntersect(vCavity);
                vHub.BoolSubtract(vCavity);
                vHub.BoolAdd(vLat);
                vHub.BoolAdd(vSleeve);
            }
            
            vImpeller.BoolAdd(vHub);
            
            BaseCylinder bore = new BaseCylinder(new LocalFrame(), m_AxialHeight + 10f, m_ShaftDiameter/2f);
            bore.SetTransformation(v => v + new Vector3(0,0,-5f));
            vImpeller.BoolSubtract(bore.voxConstruct());

            ctx.Assembly.BoolAdd(vImpeller);
        }

        Mesh ConstructBladeSolid(List<Vector3> hubPts, List<Vector3> shroudPts, int startIdx, int totalSteps, float rootThick, float tipThick)
        {
            Mesh m = new Mesh();
            int count = hubPts.Count;
            
            for (int i = startIdx; i < count - 1; i++)
            {
                Vector3 h1 = hubPts[i]; Vector3 s1 = shroudPts[i];
                Vector3 h2 = hubPts[i+1]; Vector3 s2 = shroudPts[i+1];
                
                float t_progress = (float)(i - startIdx) / (count - 1 - startIdx);
                float curHubThick = rootThick; 
                float curShroudThick = tipThick;
                
                if (t_progress > 0.8f)
                {
                    float taper = (1f - t_progress) / 0.2f; 
                    curHubThick = 0.4f + (rootThick - 0.4f) * taper;
                    curShroudThick = 0.4f + (tipThick - 0.4f) * taper;
                }

                Vector3 span = s1 - h1;
                Vector3 stream = h2 - h1;
                Vector3 normal = Vector3.Normalize(Vector3.Cross(stream, span));
                
                Vector3 offHub = normal * (curHubThick / 2f);
                Vector3 offShroud = normal * (curShroudThick / 2f);
                
                Vector3 p1 = h1 + offHub; Vector3 p2 = h1 - offHub; 
                Vector3 p3 = s1 + offShroud; Vector3 p4 = s1 - offShroud; 
                Vector3 p5 = h2 + offHub; Vector3 p6 = h2 - offHub; 
                Vector3 p7 = s2 + offShroud; Vector3 p8 = s2 - offShroud; 
                
                MeshUtility.AddQuad(ref m, p1, p5, p7, p3); 
                MeshUtility.AddQuad(ref m, p4, p8, p6, p2); 
                MeshUtility.AddQuad(ref m, p3, p7, p8, p4); 
                MeshUtility.AddQuad(ref m, p2, p6, p5, p1); 
                
                if (i == startIdx) MeshUtility.AddQuad(ref m, p1, p3, p4, p2);
                if (i == count - 2) MeshUtility.AddQuad(ref m, p5, p6, p8, p7);
            }
            return m;
        }

        float InterpolateRadius(List<Vector2> table, float z, float minR, float maxR)
        {
            if (z <= table[0].X) return maxR;
            if (z >= table[table.Count-1].X) return minR;
            for(int i=0; i<table.Count-1; i++) {
                if(z >= table[i].X && z < table[i+1].X) {
                    float t = (z - table[i].X) / (table[i+1].X - table[i].X);
                    return table[i].Y + t * (table[i+1].Y - table[i].Y);
                }
            }
            return minR;
        }

        List<Vector3> IntegrateStreamline(BezierCurve profile, float betaStart, float betaEnd, int steps)
        {
            List<Vector3> points = new List<Vector3>();
            float currentTheta = 0f;
            Vector2 prevPt = profile.GetPoint(0); 
            points.Add(new Vector3(prevPt.X, 0, prevPt.Y));

            for (int i = 1; i <= steps; i++)
            {
                float t = i / (float)steps;
                Vector2 pt = profile.GetPoint(t); 
                float dm = Vector2.Distance(pt, prevPt);
                
                float betaDeg = betaStart + t * (betaEnd - betaStart);
                float betaRad = betaDeg * (MathF.PI / 180f);
                float r = (pt.X + prevPt.X) / 2f; if (r < 0.1f) r = 0.1f;
                
                float dTheta = (dm * MathF.Tan(betaRad)) / r;
                currentTheta += dTheta;
                
                float x = pt.X * MathF.Cos(currentTheta);
                float y = pt.X * MathF.Sin(currentTheta);
                float z = pt.Y;
                points.Add(new Vector3(x, y, z));
                prevPt = pt;
            }
            return points;
        }
    }

    public class BezierCurve
    {
        Vector2 p0, p1, p2, p3;
        public BezierCurve(Vector2 a, Vector2 b, Vector2 c, Vector2 d) { p0=a; p1=b; p2=c; p3=d; }
        public Vector2 GetPoint(float t)
        {
            float u = 1 - t; float tt = t * t; float uu = u * u;
            return (u * uu * p0) + (3 * uu * t * p1) + (3 * u * tt * p2) + (t * tt * p3);
        }
    }
}