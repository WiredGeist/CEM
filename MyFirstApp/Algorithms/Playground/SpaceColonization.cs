// FILE: SpaceColonization.cs

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
    // ALGORITHM: Space Colonization
    // CONCEPT: Simulates the growth of natural branching systems, like trees or veins.
    // The algorithm starts with a "root" and grows towards a cloud of "attraction" points.
    // New branches are created in the average direction of nearby attractors.
    // IMPLEMENTATION: A 'Lattice' is used to store the final structure. The core is a C# loop
    // that iteratively finds attractors, calculates growth vectors, and adds new beams to
    // the lattice, simulating growth over time.
    public class SpaceColonization : EngineeringComponent
    {
        protected int   m_nAttractors       = 300;
        protected float m_fBounds           = 400f;
        protected float m_fStepSize         = 15f;
        protected float m_fAttractionDist   = 80f;
        protected float m_fKillDist         = 20f;

        public SpaceColonization() { Name = "ALGORITHM: Space Colonization"; }

        protected override List<Parameter> GetComponentParameters() => new List<Parameter>
        {
            new Parameter { Name = "Attractors", Value = m_nAttractors, Min = 50, Max = 1000, OnChange = v => m_nAttractors = (int)v },
            new Parameter { Name = "Bounds", Value = m_fBounds, Min = 100, Max = 1000, OnChange = v => m_fBounds = v },
            new Parameter { Name = "Step Size", Value = m_fStepSize, Min = 5, Max = 50, OnChange = v => m_fStepSize = v },
            new Parameter { Name = "Attraction Dist", Value = m_fAttractionDist, Min = 20, Max = 200, OnChange = v => m_fAttractionDist = v },
            new Parameter { Name = "Kill Dist", Value = m_fKillDist, Min = 5, Max = 50, OnChange = v => m_fKillDist = v },
        };
        
        protected override void OnConstruct(EngineeringContext ctx)
        {
            Library.Log("\n--- Starting Space Colonization ---");
            
            var oLattice = new Lattice();
            var oRand = new Random();

            // 1. Create a cloud of attractor points ("food" for the tree)
            var aAttractors = new List<Vector3>();
            for (int i = 0; i < m_nAttractors; i++)
            {
                aAttractors.Add(new Vector3(
                    (float)(oRand.NextDouble() - 0.5f) * m_fBounds,
                    (float)(oRand.NextDouble())       * m_fBounds, // Only in upper half
                    (float)(oRand.NextDouble() - 0.5f) * m_fBounds
                ));
            }

            // 2. Create the initial tree with a root
            var aTree = new List<Vector3>() { Vector3.Zero };
            oLattice.AddSphere(Vector3.Zero, m_fStepSize / 2f);

            // 3. Grow the tree iteratively
            for (int i=0; i < 25; i++) // Limit iterations to prevent infinite loops
            {
                if (aAttractors.Count == 0) break;

                // For each attractor, find the closest tree node
                var aGrowthVectors = new Dictionary<int, List<Vector3>>();
                foreach (var vecAttractor in aAttractors)
                {
                    float fMinDist = float.MaxValue;
                    int nClosestNode = -1;
                    for (int j = 0; j < aTree.Count; j++)
                    {
                        float fDist = Vector3.Distance(vecAttractor, aTree[j]);
                        if (fDist < fMinDist && fDist < m_fAttractionDist)
                        {
                            fMinDist = fDist;
                            nClosestNode = j;
                        }
                    }
                    
                    if (nClosestNode != -1)
                    {
                        if (!aGrowthVectors.ContainsKey(nClosestNode))
                            aGrowthVectors[nClosestNode] = new List<Vector3>();
                        aGrowthVectors[nClosestNode].Add(Vector3.Normalize(vecAttractor - aTree[nClosestNode]));
                    }
                }

                // Add new branches based on the average growth direction
                var aNewNodes = new List<Vector3>();
                foreach (var oPair in aGrowthVectors)
                {
                    Vector3 vecAvgDir = Vector3.Zero;
                    foreach(var vecDir in oPair.Value) { vecAvgDir += vecDir; }
                    vecAvgDir = Vector3.Normalize(vecAvgDir);

                    Vector3 vecNewNode = aTree[oPair.Key] + vecAvgDir * m_fStepSize;
                    aNewNodes.Add(vecNewNode);
                    oLattice.AddBeam(aTree[oPair.Key], vecNewNode, m_fStepSize / 4f, m_fStepSize / 5f, true);
                }
                aTree.AddRange(aNewNodes);

                // Remove attractors that have been reached
                aAttractors.RemoveAll(vecAttractor => {
                    return aTree.Any(vecNode => Vector3.Distance(vecAttractor, vecNode) < m_fKillDist);
                });
                Library.Log($"Iteration {i}: {aAttractors.Count} attractors remaining.");
            }

            Voxels vTree = new Voxels(oLattice);
            ctx.Assembly.BoolAdd(vTree);
            Library.Log("--- Space Colonization Complete ---");
        }
    }
}