// FILE: WarpedInjector.cs

using System;
using System.Collections.Generic;
using System.Numerics;
using PicoGK;
using Leap71.ShapeKernel;
using MyFirstApp.Core;
using MyFirstApp.Core.Engine;

namespace MyFirstApp.Algorithms.Playground
{
    // ALGORITHM: Phyllotactic Warp Field Injector
    // CONCEPT: This advanced algorithm combines a regular hexagonal grid with the organic
    // spiral of a Phyllotaxis pattern. It first generates a perfect honeycomb grid, then
    // "warps" it using a mathematical function that maps the regular grid points onto a
    // Phyllotaxis spiral. This preserves the gap-free nature of the honeycomb while
    // creating the beautiful, visible spirals of a sunflower.
    public class WarpedInjector : EngineeringComponent
    {
        protected int   m_nNozzles      = 200;
        protected float m_fPlateRadius  = 200f;
        protected float m_fNozzleDiam   = 10f;
        protected float m_fPlateThick   = 15f;

        public WarpedInjector() { Name = "ALGORITHM: Warped Injector"; }

        protected override List<Parameter> GetComponentParameters() => new List<Parameter>
        {
            new Parameter { Name = "Approx. Nozzles", Value = m_nNozzles, Min = 50, Max = 500, OnChange = v => m_nNozzles = (int)v },
            new Parameter { Name = "Plate Radius (mm)", Value = m_fPlateRadius, Min = 100, Max = 500, OnChange = v => m_fPlateRadius = v },
            new Parameter { Name = "Nozzle Diameter (mm)", Value = m_fNozzleDiam, Min = 5, Max = 30, OnChange = v => m_fNozzleDiam = v },
            new Parameter { Name = "Plate Thickness (mm)", Value = m_fPlateThick, Min = 5, Max = 50, OnChange = v => m_fPlateThick = v },
        };
        
        // This is the "Warp Field" function.
        // It takes a simple Cartesian coordinate and maps it to a Phyllotaxis spiral coordinate.
        private Vector2 vecWarp(Vector2 vecIn)
        {
            float fGoldenAngle = MathF.PI * (3f - MathF.Sqrt(5f));
            float fRadius = vecIn.Length();
            float fAngle = MathF.Atan2(vecIn.Y, vecIn.X);

            // This is the core of the warp: we modulate the angle based on the radius.
            float fNewAngle = fAngle + fRadius * fGoldenAngle / (m_fPlateRadius*0.1f);
            
            return new Vector2(
                MathF.Cos(fNewAngle) * fRadius,
                MathF.Sin(fNewAngle) * fRadius
            );
        }

        protected override void OnConstruct(EngineeringContext ctx)
        {
            Library.Log("\n--- Starting Warped Injector Construction ---");
            
            var aNozzleVoxels   = new List<Voxels>();
            var aHexMeshes      = new List<Mesh>();

            // 1. GENERATE A REGULAR HEXAGONAL GRID
            float fHexRadius = m_fPlateRadius / MathF.Sqrt(m_nNozzles) * 1.1f;
            float fHexWidth = MathF.Sqrt(3) * fHexRadius;
            float fHexHeight = 2f * fHexRadius;

            int nGridSize = (int)(m_fPlateRadius / fHexWidth) + 5;
            
            for (int x = -nGridSize; x <= nGridSize; x++)
            {
                for (int y = -nGridSize; y <= nGridSize; y++)
                {
                    // Calculate the center of the hexagon in the regular grid
                    float fHexX = x * fHexWidth + (y % 2 == 1 ? fHexWidth/2f : 0);
                    float fHexY = y * fHexHeight * 0.75f;
                    Vector2 vecCenter = new Vector2(fHexX, fHexY);
                    
                    // Discard any hexagons that are too far from the center
                    if (vecCenter.Length() > m_fPlateRadius * 1.2f) continue;
                    
                    // 2. WARP THE HEXAGON
                    // Get the 6 corners of the hexagon
                    var aHexPoints = new List<Vector3>();
                    for (int j = 0; j < 6; j++)
                    {
                        float fAngle = (float)j / 6f * 2f * MathF.PI;
                        Vector2 vecCorner = vecCenter + new Vector2(
                            MathF.Cos(fAngle) * fHexRadius,
                            MathF.Sin(fAngle) * fHexRadius);
                        
                        // Apply the warp to each corner point
                        Vector2 vecWarpedCorner = vecWarp(vecCorner);
                        aHexPoints.Add(new Vector3(vecWarpedCorner.X, vecWarpedCorner.Y, 0));
                    }
                    
                    // 3. CONSTRUCT THE MESH CELL AND NOZZLE
                    Vector2 vecWarpedCenter = vecWarp(vecCenter);
                    var oNozzle = new BaseCylinder(
                        new LocalFrame(new Vector3(vecWarpedCenter.X, vecWarpedCenter.Y, -5f)),
                        m_fPlateThick + 10f,
                        m_fNozzleDiam / 2f);
                    aNozzleVoxels.Add(oNozzle.voxConstruct());

                    Mesh mHexCell = new Mesh();
                    for (int j = 0; j < 6; j++)
                    {
                        Vector3 p1 = aHexPoints[j];
                        Vector3 p2 = aHexPoints[(j + 1) % 6];
                        Vector3 p3 = p2 + Vector3.UnitZ * m_fPlateThick;
                        Vector3 p4 = p1 + Vector3.UnitZ * m_fPlateThick;
                        mHexCell.AddQuad(p1, p2, p3, p4, false);
                    }
                    aHexMeshes.Add(mHexCell);
                }
            }

            // 4. FINAL ASSEMBLY
            Mesh mCombinedHexes = new Mesh();
            foreach(var mHex in aHexMeshes) { mCombinedHexes.Append(mHex); }
            
            Voxels vHexPlate = new Voxels(mCombinedHexes);
            Voxels vNozzles = Sh.voxUnion(aNozzleVoxels);
            vHexPlate.BoolSubtract(vNozzles);
            
            var oCutter = new BaseCylinder(new LocalFrame(), m_fPlateThick + 2f, m_fPlateRadius);
            oCutter.SetTransformation((vec) => vec - new Vector3(0,0,1f));
            vHexPlate.BoolIntersect(oCutter.voxConstruct());
            
            ctx.Assembly.BoolAdd(vHexPlate);
            Library.Log("--- Injector Construction Complete ---");
        }
    }
}