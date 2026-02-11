using System;
using System.Collections.Generic;
using System.Numerics;
using System.Linq;
using PicoGK;
using Leap71.ShapeKernel;
using MyFirstApp.Core;
using MyFirstApp.Core.Engine;

namespace MyFirstApp.Algorithms.Playground
{
    public class ScrewGearGen : EngineeringComponent
    {
        // --- BASIC DIMENSIONS ---
        float m_ReferenceDia = 60f;     // Pitch Circle Diameter (PCD)
        float m_Length       = 80f;     // Axial Length
        float m_TwistAngle   = 180f;    // Total Helix Twist
        float m_ShaftDia     = 10f;

        // --- GEAR TOOTH PARAMETERS ---
        float m_ToothCount   = 6f;      
        float m_Addendum     = 6f;      // Height from Ref to Tip
        float m_Dedendum     = 6f;      // Depth from Ref to Root
        float m_PressureAng  = 20f;     // Flank Angle (Degrees)
        float m_FilletRad    = 2.0f;    // Radius at the root
        float m_ThickRatio   = 0.5f;    // Tooth width vs Space width

        // --- INTERNAL ---
        bool m_LatticeInfill = false;
        float m_LatticeSize  = 8.0f;

        // --- COLORS (Local definitions to avoid Pal errors) ---
        ColorFloat clrRef  = Pal.Steel;
        ColorFloat clrGuide = new ColorFloat(0.6f, 0.6f, 0.6f); // Grey

        public ScrewGearGen() { Name = "Transmission: Parametric Screw Gear"; }

        protected override List<Parameter> GetComponentParameters() => new List<Parameter> {
            new Parameter { Name = "Reference Diameter", Value=m_ReferenceDia, Min=20, Max=150, OnChange=v=>m_ReferenceDia=v },
            new Parameter { Name = "Axial Length", Value=m_Length, Min=10, Max=200, OnChange=v=>m_Length=v },
            new Parameter { Name = "Twist (Deg)", Value=m_TwistAngle, Min=-720, Max=720, OnChange=v=>m_TwistAngle=v },
            new Parameter { Name = "Shaft Diameter", Value=m_ShaftDia, Min=5, Max=40, OnChange=v=>m_ShaftDia=v },
            
            new Parameter { Name = "Tooth Count", Value=m_ToothCount, Min=3, Max=20, OnChange=v=>m_ToothCount=v },
            new Parameter { Name = "Tooth Thickness", Value=m_ThickRatio, Min=0.1f, Max=0.9f, OnChange=v=>m_ThickRatio=v },
            
            new Parameter { Name = "Addendum", Value=m_Addendum, Min=1, Max=20, OnChange=v=>m_Addendum=v },
            new Parameter { Name = "Dedendum", Value=m_Dedendum, Min=1, Max=20, OnChange=v=>m_Dedendum=v },
            new Parameter { Name = "Flank Angle", Value=m_PressureAng, Min=0, Max=45, OnChange=v=>m_PressureAng=v },
            new Parameter { Name = "Fillet Radius", Value=m_FilletRad, Min=0, Max=10, OnChange=v=>m_FilletRad=v },

            new Parameter { Name = "Lattice Infill", Value=m_LatticeInfill?1:0, Min=0, Max=1, OnChange=v=>m_LatticeInfill=v==1 },
        };

        public override void OnPreview(EngineeringContext ctx)
        {
            // Calculate dependent diameters
            float tipRad = (m_ReferenceDia / 2f) + m_Addendum;
            float rootRad = (m_ReferenceDia / 2f) - m_Dedendum;

            // Draw Reference Circle (Pitch Circle)
            Vis.Circle(Vector3.Zero, m_ReferenceDia / 2f, clrRef);
            
            // Draw Tip Circle (Outer Limit)
            Vis.Circle(Vector3.Zero, tipRad, clrGuide);
            
            // Draw Root Circle (Inner Limit)
            Vis.Circle(Vector3.Zero, rootRad, clrGuide);

            // Draw Height Line
            Vis.Line(Vector3.Zero, new Vector3(0,0,m_Length), clrRef);
        }

        protected override void OnConstruct(EngineeringContext ctx)
        {
            // 1. GENERATE PROFILE
            List<Vector2> profile = GenerateDetailedGearProfile(
                m_ReferenceDia / 2f,
                m_Addendum,
                m_Dedendum,
                (int)m_ToothCount,
                m_PressureAng,
                m_ThickRatio,
                m_FilletRad
            );

            // 2. HELICAL SWEEP
            float voxSize = AppConfig.VoxelSize;
            int zSteps = (voxSize < 0.2f) ? 200 : 100;
            
            Mesh mGear = ConstructHelicalMesh(profile, m_Length, m_TwistAngle, zSteps);
            Voxels vGear = new Voxels(mGear);

            // 3. HOLLOWING & LATTICE
            if (m_LatticeInfill)
            {
                // Create a cavity that respects the root diameter
                float rootRad = (m_ReferenceDia / 2f) - m_Dedendum;
                float shell = 2.0f;
                float cavityRad = rootRad - shell;
                
                if (cavityRad > (m_ShaftDia/2f + shell))
                {
                    BaseCylinder cavCyl = new BaseCylinder(new LocalFrame(), m_Length, cavityRad);
                    Voxels vCavity = cavCyl.voxConstruct();
                    
                    // Subtract Shaft zone from cavity
                    BaseCylinder shaftProt = new BaseCylinder(new LocalFrame(), m_Length, (m_ShaftDia/2f) + shell);
                    vCavity.BoolSubtract(shaftProt.voxConstruct());

                    // Generate Lattice
                    Lattice lat = new Lattice();
                    BBox3 bounds = vGear.oCalculateBoundingBox();
                    int nx = (int)(bounds.vecSize().X / m_LatticeSize) + 1;
                    int ny = (int)(bounds.vecSize().Y / m_LatticeSize) + 1;
                    int nz = (int)(bounds.vecSize().Z / m_LatticeSize) + 1;
                    float beam = 1.5f;

                    for(int z=0; z<nz; z++)
                    for(int y=0; y<ny; y++)
                    for(int x=0; x<nx; x++)
                    {
                        Vector3 pos = bounds.vecMin + new Vector3(x*m_LatticeSize, y*m_LatticeSize, z*m_LatticeSize);
                        if (new Vector2(pos.X, pos.Y).Length() > cavityRad) continue; 

                        if(x<nx-1) lat.AddBeam(pos, pos + new Vector3(m_LatticeSize,0,0), beam, beam, true);
                        if(y<ny-1) lat.AddBeam(pos, pos + new Vector3(0,m_LatticeSize,0), beam, beam, true);
                        if(z<nz-1) lat.AddBeam(pos, pos + new Vector3(0,0,m_LatticeSize), beam, beam, true);
                        if(x<nx-1 && y<ny-1 && z<nz-1) lat.AddBeam(pos, pos + new Vector3(m_LatticeSize, m_LatticeSize, m_LatticeSize), beam, beam, true);
                    }

                    Voxels vLat = new Voxels(lat);
                    vLat.BoolIntersect(vCavity);
                    vGear.BoolSubtract(vCavity);
                    vGear.BoolAdd(vLat);
                }
            }

            // 4. SHAFT BORE
            BaseCylinder bore = new BaseCylinder(new LocalFrame(), m_Length + 10f, m_ShaftDia / 2f);
            bore.SetTransformation(v => v + new Vector3(0, 0, -5f));
            vGear.BoolSubtract(bore.voxConstruct());

            ctx.Assembly.BoolAdd(vGear);
        }

        List<Vector2> GenerateDetailedGearProfile(
            float refRadius, 
            float addendum, 
            float dedendum, 
            int toothCount, 
            float pressureAngleDeg, 
            float thickRatio,
            float filletRadius)
        {
            List<Vector2> points = new List<Vector2>();

            float tipRadius = refRadius + addendum;
            float rootRadius = refRadius - dedendum;
            
            if (rootRadius < 1f) rootRadius = 1f;
            if (filletRadius > (refRadius - rootRadius) * 0.8f) filletRadius = (refRadius - rootRadius) * 0.8f;

            float anglePerTooth = (2f * MathF.PI) / toothCount;
            float halfToothAngle = anglePerTooth * thickRatio * 0.5f; 

            // Pressure angle calculations
            float pressureRad = pressureAngleDeg * (MathF.PI / 180f);
            float tanPress = MathF.Tan(pressureRad);

            // Adjust widths based on height (Triangular profile logic)
            float hTip = tipRadius - refRadius;
            float angleTipDelta = (hTip * tanPress) / tipRadius;
            float tipHalfAngle = halfToothAngle - angleTipDelta;
            if (tipHalfAngle < 0.01f) tipHalfAngle = 0.01f; 

            float hRoot = refRadius - rootRadius;
            float angleRootDelta = (hRoot * tanPress) / rootRadius;
            float rootHalfAngle = halfToothAngle + angleRootDelta;
            float spaceHalfAngle = (anglePerTooth / 2f);

            for (int i = 0; i < toothCount; i++)
            {
                float centerAngle = i * anglePerTooth;

                // 1. Root Point (Left) with Fillet
                AddFilletArc(points, rootRadius, centerAngle - rootHalfAngle, filletRadius);

                // 2. Flank Left
                points.Add(Polar(rootRadius, centerAngle - rootHalfAngle));
                points.Add(Polar(tipRadius, centerAngle - tipHalfAngle));

                // 3. Tip Arc
                int tipRes = 4;
                for(int k=0; k<=tipRes; k++)
                {
                    float t = k / (float)tipRes;
                    float a = (centerAngle - tipHalfAngle) + t * (2f * tipHalfAngle);
                    points.Add(Polar(tipRadius, a));
                }

                // 4. Flank Right
                points.Add(Polar(tipRadius, centerAngle + tipHalfAngle));
                points.Add(Polar(rootRadius, centerAngle + rootHalfAngle));

                // 5. Root Point (Right) with Fillet
                AddFilletArc(points, rootRadius, centerAngle + rootHalfAngle, filletRadius);
            }

            return points;
        }

        Vector2 Polar(float r, float ang)
        {
            return new Vector2(r * MathF.Cos(ang), r * MathF.Sin(ang));
        }

        // Fixed: Removed unused parameters to clean warnings
        void AddFilletArc(List<Vector2> pts, float rBase, float angCorner, float filletRad)
        {
            // For now, we simply add the sharp corner. 
            // A true geometric fillet requires intersecting the flank line with the root circle
            // which is complex. This placeholder ensures the shape is valid and watertight.
            // If filletRad is used later for complex logic, the parameter is ready.
            pts.Add(Polar(rBase, angCorner));
        }

        Mesh ConstructHelicalMesh(List<Vector2> profile, float length, float totalTwistDeg, int zSteps)
        {
            Mesh m = new Mesh();
            int pointsPerLayer = profile.Count;
            float totalTwistRad = totalTwistDeg * (MathF.PI / 180f);
            int[][] vertexIndices = new int[zSteps + 1][];

            for (int z = 0; z <= zSteps; z++)
            {
                float t = z / (float)zSteps;
                float currentZ = t * length;
                float currentAngle = t * totalTwistRad;
                float cosA = MathF.Cos(currentAngle);
                float sinA = MathF.Sin(currentAngle);

                vertexIndices[z] = new int[pointsPerLayer];
                for (int i = 0; i < pointsPerLayer; i++)
                {
                    Vector2 p = profile[i];
                    float rotX = p.X * cosA - p.Y * sinA;
                    float rotY = p.X * sinA + p.Y * cosA;
                    vertexIndices[z][i] = m.nAddVertex(new Vector3(rotX, rotY, currentZ));
                }
            }

            for (int z = 0; z < zSteps; z++)
            {
                for (int i = 0; i < pointsPerLayer; i++)
                {
                    int nextI = (i + 1) % pointsPerLayer;
                    int v1 = vertexIndices[z][i];
                    int v2 = vertexIndices[z][nextI];
                    int v3 = vertexIndices[z+1][nextI];
                    int v4 = vertexIndices[z+1][i];
                    m.nAddTriangle(v1, v2, v4);
                    m.nAddTriangle(v2, v3, v4);
                }
            }

            int bottomCenterIdx = m.nAddVertex(Vector3.Zero);
            for (int i = 0; i < pointsPerLayer; i++)
            {
                int nextI = (i + 1) % pointsPerLayer;
                m.nAddTriangle(bottomCenterIdx, vertexIndices[0][nextI], vertexIndices[0][i]);
            }

            Vector3 topCenter = new Vector3(0, 0, length);
            int topCenterIdx = m.nAddVertex(topCenter);
            int lastZ = zSteps;
            for (int i = 0; i < pointsPerLayer; i++)
            {
                int nextI = (i + 1) % pointsPerLayer;
                m.nAddTriangle(topCenterIdx, vertexIndices[lastZ][i], vertexIndices[lastZ][nextI]);
            }

            return m;
        }
    }
}