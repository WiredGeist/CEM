// FILE: VoronoiInjector.cs

using System;
using System.Collections.Generic;
using System.Numerics;
using PicoGK;
using Leap71.ShapeKernel;
using MyFirstApp.Core;
using MyFirstApp.Core.Engine;

namespace MyFirstApp.Algorithms.Playground
{
    // ALGORITHM: True Phyllotactic Voronoi Injector
    // CONCEPT: This is the definitive version. It uses the Phyllotaxis algorithm to generate
    // an optimal set of seed points. It then generates a true Voronoi diagram from these
    // points by creating a custom implicit function. This function calculates the cell walls
    // by finding where the distance to the two closest seed points is equal, resulting in a
    // perfectly packed, gap-free, organic cellular structure.
    public class VoronoiInjector : EngineeringComponent
    {
        protected int   m_nNozzles      = 100;
        protected float m_fPlateRadius  = 200f;
        protected float m_fNozzleDiam   = 10f;
        protected float m_fPlateThick   = 15f;
        protected float m_fWallThick    = 3f;

        public VoronoiInjector() { Name = "ALGORITHM: Voronoi Injector"; }

        protected override List<Parameter> GetComponentParameters() => new List<Parameter>
        {
            new Parameter { Name = "Num Nozzles", Value = m_nNozzles, Min = 30, Max = 300, OnChange = v => m_nNozzles = (int)v },
            new Parameter { Name = "Plate Radius (mm)", Value = m_fPlateRadius, Min = 100, Max = 500, OnChange = v => m_fPlateRadius = v },
            new Parameter { Name = "Nozzle Diameter (mm)", Value = m_fNozzleDiam, Min = 5, Max = 30, OnChange = v => m_fNozzleDiam = v },
            new Parameter { Name = "Plate Thickness (mm)", Value = m_fPlateThick, Min = 5, Max = 50, OnChange = v => m_fPlateThick = v },
            new Parameter { Name = "Wall Thickness (mm)", Value = m_fWallThick, Min = 1, Max = 15, OnChange = v => m_fWallThick = v },
        };
        
        // Custom implicit object that defines a 2D Voronoi diagram.
        class ImplicitVoronoi2D : IImplicit
        {
            private List<Vector2> m_aSeedPoints;
            private float m_fWallThickness;

            public ImplicitVoronoi2D(List<Vector2> aPoints, float fThickness)
            {
                m_aSeedPoints = aPoints;
                m_fWallThickness = fThickness;
            }

            public float fSignedDistance(in Vector3 vecPt)
            {
                Vector2 vecCurrent = new Vector2(vecPt.X, vecPt.Y);

                // Find the distances to the two closest seed points
                float fMinDist1 = float.MaxValue;
                float fMinDist2 = float.MaxValue;

                foreach (var vecSeed in m_aSeedPoints)
                {
                    float fDist = Vector2.Distance(vecCurrent, vecSeed);
                    if (fDist < fMinDist1)
                    {
                        fMinDist2 = fMinDist1;
                        fMinDist1 = fDist;
                    }
                    else if (fDist < fMinDist2)
                    {
                        fMinDist2 = fDist;
                    }
                }
                
                // The magic of Voronoi: the surface is halfway between the two closest points.
                // The value of this function is the distance from that surface.
                return (fMinDist2 - fMinDist1) / 2f - (m_fWallThickness / 2f);
            }
        }

        protected override void OnConstruct(EngineeringContext ctx)
        {
            Library.Log("\n--- Starting True Voronoi Injector ---");
            
            // 1. GENERATE PHYLLOTAXIS POINTS
            var aPoints = new List<Vector2>();
            float fGoldenAngle = MathF.PI * (3f - MathF.Sqrt(5f));
            for (int i = 0; i < m_nNozzles; i++)
            {
                float r = MathF.Sqrt((float)i / m_nNozzles) * m_fPlateRadius;
                float theta = i * fGoldenAngle;
                aPoints.Add(new Vector2(MathF.Cos(theta) * r, MathF.Sin(theta) * r));
            }
            Library.Log($"{aPoints.Count} seed points generated.");

            // 2. CREATE THE VORONOI IMPLICIT OBJECT
            IImplicit sdfVoronoi = new ImplicitVoronoi2D(aPoints, m_fWallThick);
            
            // 3. RENDER THE IMPLICIT FIELD
            // Define the bounding box for rendering
            BBox3 oBounds = new BBox3(
                new Vector3(-m_fPlateRadius, -m_fPlateRadius, 0),
                new Vector3( m_fPlateRadius,  m_fPlateRadius, m_fPlateThick));

            Library.Log("Rendering Voronoi field...");
            Voxels vVoronoiPlate = new Voxels(sdfVoronoi, oBounds);
            Library.Log("Voronoi plate constructed.");

            // 4. ADD NOZZLES
            var aNozzleVoxels = new List<Voxels>();
            foreach (var vecCenter in aPoints)
            {
                var oNozzle = new BaseCylinder(
                    new LocalFrame(new Vector3(vecCenter.X, vecCenter.Y, -5f)),
                    m_fPlateThick + 10f,
                    m_fNozzleDiam / 2f);
                aNozzleVoxels.Add(oNozzle.voxConstruct());
            }
            Voxels vNozzles = Sh.voxUnion(aNozzleVoxels);

            // 5. FINAL ASSEMBLY
            vVoronoiPlate.BoolSubtract(vNozzles);
            
            // Cut the result into a final circle shape
            var oCutter = new BaseCylinder(
                new LocalFrame(new Vector3(0,0,-1f)), 
                m_fPlateThick + 2f, 
                m_fPlateRadius);
            vVoronoiPlate.BoolIntersect(oCutter.voxConstruct());
            
            ctx.Assembly.BoolAdd(vVoronoiPlate);
            Library.Log("--- Injector Construction Complete ---");
        }
    }
}