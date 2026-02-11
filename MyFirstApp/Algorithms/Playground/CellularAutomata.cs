// FILE: CellularAutomata.cs

using System;
using System.Collections.Generic;
using System.Numerics;
using PicoGK;
using Leap71.ShapeKernel;
using MyFirstApp.Core;
using MyFirstApp.Core.Engine;

namespace MyFirstApp.Algorithms.Playground
{
    // ALGORITHM: 3D Cellular Automata
    // CONCEPT: A shape is "grown" iteratively based on a simple set of rules. A grid of
    // cells (voxels) is created, and in each step (generation), a cell becomes "alive" or
    // "dies" based on how many living neighbors it has. This results in complex, organic,
    // and often surprising structures emerging from a simple seed.
    // IMPLEMENTATION: This is a pure voxel algorithm. We use two Voxel objects, 'voxCurrent'
    // and 'voxNext'. We loop through a 3D grid, count the neighbors for each cell in 'voxCurrent',
    // apply the rules, and write the new state into 'voxNext'. Then we swap them and repeat.
    public class CellularAutomata : EngineeringComponent
    {
        // --- Automata Parameters ---
        // Note: Performance is highly dependent on GridSize (O(n^3)). Keep it reasonable.
        protected int   m_nGridSize     = 50;   // The size of the cubic simulation space.
        protected int   m_nIterations   = 10;   // Number of generations to simulate.
        protected float m_fSeedDensity  = 0.2f; // Initial random seed density.

        public CellularAutomata() { Name = "ALGORITHM: Cellular Automata"; }

        protected override List<Parameter> GetComponentParameters() => new List<Parameter>
        {
            new Parameter { Name = "Grid Size", Value = m_nGridSize, Min = 20, Max = 80, OnChange = v => m_nGridSize = (int)v },
            new Parameter { Name = "Iterations", Value = m_nIterations, Min = 1, Max = 25, OnChange = v => m_nIterations = (int)v },
            new Parameter { Name = "Seed Density (%)", Value = m_fSeedDensity * 100, Min = 5, Max = 50, OnChange = v => m_fSeedDensity = v/100f },
        };
        
        protected override void OnConstruct(EngineeringContext ctx)
        {
            Library.Log("\n--- Starting Cellular Automata Construction ---");
            
            // The simulation runs on a boolean grid first, then we build the voxels at the end.
            // This is much faster than manipulating voxels directly in the loop.
            bool[,,] abGrid = new bool[m_nGridSize, m_nGridSize, m_nGridSize];
            var oRand = new Random();

            // 1. SEED THE INITIAL GRID
            // We create a random cluster of "living" cells in the center.
            for (int x = 0; x < m_nGridSize; x++)
            for (int y = 0; y < m_nGridSize; y++)
            for (int z = 0; z < m_nGridSize; z++)
            {
                // Is this cell in the central seed area?
                if (Math.Abs(x - m_nGridSize/2) < m_nGridSize/4 &&
                    Math.Abs(y - m_nGridSize/2) < m_nGridSize/4 &&
                    Math.Abs(z - m_nGridSize/2) < m_nGridSize/4)
                {
                    if (oRand.NextDouble() < m_fSeedDensity)
                    {
                        abGrid[x, y, z] = true;
                    }
                }
            }

            // 2. RUN THE SIMULATION
            for (int i = 0; i < m_nIterations; i++)
            {
                bool[,,] abNextGrid = new bool[m_nGridSize, m_nGridSize, m_nGridSize];
                for (int x = 1; x < m_nGridSize - 1; x++)
                for (int y = 1; y < m_nGridSize - 1; y++)
                for (int z = 1; z < m_nGridSize - 1; z++)
                {
                    // Count living neighbors
                    int nNeighbors = 0;
                    for (int dx = -1; dx <= 1; dx++)
                    for (int dy = -1; dy <= 1; dy++)
                    for (int dz = -1; dz <= 1; dz++)
                    {
                        if (dx == 0 && dy == 0 && dz == 0) continue;
                        if (abGrid[x + dx, y + dy, z + dz]) nNeighbors++;
                    }

                    // THE RULES OF LIFE (these can be changed for different results)
                    // Rule 1: A living cell with 4 to 6 neighbors survives.
                    if (abGrid[x, y, z] && nNeighbors >= 4 && nNeighbors <= 6)
                    {
                        abNextGrid[x, y, z] = true;
                    }
                    // Rule 2: A dead cell with exactly 6 neighbors becomes alive.
                    else if (!abGrid[x, y, z] && nNeighbors == 6)
                    {
                        abNextGrid[x, y, z] = true;
                    }
                }
                abGrid = abNextGrid; // The next generation becomes the current one.
                Library.Log($"Iteration {i+1} complete.");
            }

            // 3. CONSTRUCT THE VOXELS FROM THE FINAL GRID
            Library.Log("Constructing voxels from final grid...");
            var oLattice = new Lattice();
            float fVoxelSize = 5f; // The size of each cell in mm
            for (int x = 0; x < m_nGridSize; x++)
            for (int y = 0; y < m_nGridSize; y++)
            for (int z = 0; z < m_nGridSize; z++)
            {
                if (abGrid[x, y, z])
                {
                    oLattice.AddSphere(new Vector3(x, y, z) * fVoxelSize, fVoxelSize * 0.7f);
                }
            }

            Voxels vAutomata = new Voxels(oLattice);
            ctx.Assembly.BoolAdd(vAutomata);
            Library.Log("--- Cellular Automata Construction Complete ---");
        }
    }
}