// FILE: TopologyOptimization.cs

using System;
using System.Collections.Generic;
using System.Numerics;
using PicoGK;
using Leap71.ShapeKernel;
using MyFirstApp.Core;
using MyFirstApp.Core.Engine;

namespace MyFirstApp.Algorithms.Playground
{
    public class TopologyOptimization : EngineeringComponent
    {
        protected float m_fSize             = 500f;
        protected float m_fLoadRadius       = 50f;
        protected float m_fGyroidCellSize   = 20f;

        public TopologyOptimization() { Name = "ALGORITHM: Topology Opt."; }

        protected override List<Parameter> GetComponentParameters() => new List<Parameter>
        {
            new Parameter { Name = "Size", Value = m_fSize, Min = 200, Max = 1000, OnChange = v => m_fSize = v },
            new Parameter { Name = "Load Radius", Value = m_fLoadRadius, Min = 20, Max = 150, OnChange = v => m_fLoadRadius = v },
            new Parameter { Name = "Gyroid Cell Size", Value = m_fGyroidCellSize, Min = 10, Max = 50, OnChange = v => m_fGyroidCellSize = v },
        };
        
        // This is a custom implicit object that simulates a stress field.
        class ImplicitStressField : IImplicit
        {
            public List<IImplicit> m_aStressZones = new List<IImplicit>();

            public float fSignedDistance(in Vector3 vecPt)
            {
                float fMinDist = float.MaxValue;
                foreach (var oZone in m_aStressZones)
                {
                    fMinDist = MathF.Min(fMinDist, oZone.fSignedDistance(in vecPt));
                }
                return fMinDist;
            }
        }

        protected override void OnConstruct(EngineeringContext ctx)
        {
            Library.Log("\n--- Starting Topology Optimization ---");

            // 1. DEFINE THE PROBLEM: Design Space, Anchors, and Loads
            BBox3 oBounds = new BBox3(new Vector3(-m_fSize/2f), new Vector3(m_fSize/2f));
            Vector3 vecAnchor1 = new Vector3(-m_fSize * 0.4f, -m_fSize * 0.4f, 0);
            Vector3 vecAnchor2 = new Vector3( m_fSize * 0.4f, -m_fSize * 0.4f, 0);
            Vector3 vecLoad = new Vector3(0, m_fSize * 0.4f, 0);

            // 2. CREATE THE SIMULATED STRESS FIELD
            var oStressField = new ImplicitStressField();
            oStressField.m_aStressZones.Add(new ImplicitSphere(vecAnchor1, m_fLoadRadius));
            oStressField.m_aStressZones.Add(new ImplicitSphere(vecAnchor2, m_fLoadRadius));
            oStressField.m_aStressZones.Add(new ImplicitSphere(vecLoad,    m_fLoadRadius * 1.5f));

            for (float i = 0; i < 1f; i += 0.05f)
            {
                oStressField.m_aStressZones.Add(new ImplicitSphere(Vector3.Lerp(vecAnchor1, vecLoad, i), m_fLoadRadius * (1f-i)));
                oStressField.m_aStressZones.Add(new ImplicitSphere(Vector3.Lerp(vecAnchor2, vecLoad, i), m_fLoadRadius * (1f-i)));
            }
            Library.Log("Simulated stress field created.");

            // 3. RENDER THE STRESS FIELD INTO A SOLID VOXEL OBJECT
            Voxels voxStressShape = new Voxels(oStressField, oBounds);
            Library.Log("Stress field rendered to voxels.");

            // 4. CREATE A LATTICE MATERIAL
            IImplicit sdfGyroid = new ImplicitGyroid(m_fGyroidCellSize, 0.4f);

            // 5. CARVE THE MATERIAL
            voxStressShape.IntersectImplicit(sdfGyroid);
            Library.Log("Gyroid intersected with stress field.");

            // ** THE CRITICAL FIX IS HERE **
            // To create voxels from an implicit sphere, you must pass BOTH the
            // implicit object AND the bounding box to the Voxels constructor.
            // Sh.voxUnion is a helper that can take a list of voxel objects.
            Voxels voxAnchors = Sh.voxUnion(new List<Voxels>()
            {
                new Voxels(new ImplicitSphere(vecAnchor1, m_fLoadRadius), oBounds),
                new Voxels(new ImplicitSphere(vecAnchor2, m_fLoadRadius), oBounds),
                new Voxels(new ImplicitSphere(vecLoad, m_fLoadRadius * 1.5f), oBounds)
            });

            voxStressShape.BoolAdd(voxAnchors);
            
            ctx.Assembly.BoolAdd(voxStressShape);
            Library.Log("--- Topology Optimization Complete ---");
        }
    }
}