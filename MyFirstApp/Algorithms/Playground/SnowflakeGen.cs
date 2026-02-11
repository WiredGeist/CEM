using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Numerics;
using PicoGK;
using Leap71.ShapeKernel;
using MyFirstApp.Core;
using MyFirstApp.Core.Engine;

namespace MyFirstApp.Algorithms.Playground
{
    public class SnowflakeGen : EngineeringComponent
    {
        // --- GRAVNER-GRIFFEATH PARAMETERS ---
        float m_Size = 100f;            
        float m_Steps = 3000;           
        
        // RHO (Vapor Density): 0.40 is the sweet spot for Stars
        float m_Rho = 0.40f;            
        
        // KAPPA (Diffusion Rate)
        float m_Kappa = 0.05f;          
        
        // MU (Freezing Rate)
        float m_Mu = 0.15f;             

        // Visuals
        float m_Thickness = 1.0f;       
        float m_Noise = 0.05f; 

        public SnowflakeGen() { Name = "Test Lab: Snowflake Molecular"; }

        protected override List<Parameter> GetComponentParameters() => new List<Parameter> {
            new Parameter { Name = "Physical Size (mm)", Value=m_Size, Min=10, Max=200, OnChange=v=>m_Size=v },
            new Parameter { Name = "Growth Steps", Value=m_Steps, Min=500, Max=10000, OnChange=v=>m_Steps=v },
            
            // Physics
            new Parameter { Name = "Vapor Density (Rho)", Value=m_Rho, Min=0.3f, Max=0.8f, OnChange=v=>m_Rho=v },
            new Parameter { Name = "Diffusion (Kappa)", Value=m_Kappa, Min=0.01f, Max=0.5f, OnChange=v=>m_Kappa=v },
            new Parameter { Name = "Freezing Rate (Mu)", Value=m_Mu, Min=0.01f, Max=1.0f, OnChange=v=>m_Mu=v },
            new Parameter { Name = "Random Noise", Value=m_Noise, Min=0.0f, Max=0.2f, OnChange=v=>m_Noise=v },
            
            new Parameter { Name = "Crystal Thickness", Value=m_Thickness, Min=0.1f, Max=5.0f, OnChange=v=>m_Thickness=v }
        };

        // --- GRID ---
        const int GRID_SIZE = 500; 
        const int CENTER = GRID_SIZE / 2;
        
        // Fixed: Initialized with null! to satisfy CS8618
        // These are re-allocated in OnConstruct every time.
        float[] m_DiffusiveMass = null!;
        float[] m_BoundaryMass = null!;
        bool[]  m_Ice = null!;
        
        float[] m_NextDiffusive = null!; 
        float[] m_NextBoundary = null!;
        bool[]  m_NextIce = null!;

        // Neighbors (Odd-R Hexagonal)
        static readonly int[,] Neighbors = { {1,0}, {0,-1}, {1,-1}, {-1,0}, {0,1}, {1,1} };
        static readonly int[,] EvenNeighbors = { {1,0}, {-1,-1}, {0,-1}, {-1,0}, {-1,1}, {0,1} };

        public override void OnPreview(EngineeringContext ctx)
        {
            float s = m_Size / 2f;
            ColorFloat clr = new ColorFloat(0.5f, 0.8f, 1.0f); 
            Vis.Line(new Vector3(-s, s, 0), new Vector3(s, s, 0), clr);
            Vis.Line(new Vector3(-s, -s, 0), new Vector3(s, -s, 0), clr);
            Vis.Line(new Vector3(-s, -s, 0), new Vector3(-s, s, 0), clr);
            Vis.Line(new Vector3(s, -s, 0), new Vector3(s, s, 0), clr);
        }

        protected override void OnConstruct(EngineeringContext ctx)
        {
            int N = GRID_SIZE * GRID_SIZE;
            m_DiffusiveMass = new float[N];
            m_BoundaryMass = new float[N];
            m_Ice = new bool[N];
            m_NextDiffusive = new float[N];
            m_NextBoundary = new float[N];
            m_NextIce = new bool[N];

            Random rnd = new Random(1234);

            // 1. INITIALIZE (Gravner-Griffeath Condition)
            for(int i=0; i<N; i++) 
            {
                float noise = ((float)rnd.NextDouble() - 0.5f) * m_Noise;
                m_DiffusiveMass[i] = m_Rho + noise;
            }

            int cIdx = GetIdx(CENTER, CENTER);
            m_Ice[cIdx] = true;
            m_DiffusiveMass[cIdx] = 0; 

            // 2. SIMULATION LOOP
            for (int t = 0; t < m_Steps; t++)
            {
                Parallel.For(1, GRID_SIZE - 1, y =>
                {
                    int[,] neighbors = (y % 2 != 0) ? Neighbors : EvenNeighbors;

                    for (int x = 1; x < GRID_SIZE - 1; x++)
                    {
                        int idx = y * GRID_SIZE + x;

                        if (m_Ice[idx])
                        {
                            m_NextIce[idx] = true;
                            m_NextDiffusive[idx] = 0;
                            m_NextBoundary[idx] = m_BoundaryMass[idx];
                            continue;
                        }

                        // --- PHASE A: DIFFUSION ---
                        float u = m_DiffusiveMass[idx];
                        float sumNeighbors = 0;
                        int validNeighbors = 0;
                        bool isBoundary = false;

                        for (int n = 0; n < 6; n++)
                        {
                            int ny = y + neighbors[n, 1];
                            int nx = x + neighbors[n, 0];
                            int nIdx = ny * GRID_SIZE + nx;

                            if (m_Ice[nIdx])
                            {
                                isBoundary = true; 
                            }
                            else
                            {
                                sumNeighbors += m_DiffusiveMass[nIdx];
                                validNeighbors++;
                            }
                        }

                        float diffusionTerm = 0;
                        if (validNeighbors > 0)
                        {
                            float avg = sumNeighbors / 6.0f; 
                            diffusionTerm = m_Kappa * (avg - u);
                        }
                        
                        float u_next = u + diffusionTerm;

                        // --- PHASE B: FREEZING ---
                        float b_next = m_BoundaryMass[idx];
                        bool ice_next = false;

                        if (isBoundary)
                        {
                            float transfer = u_next * m_Mu;
                            b_next += transfer;
                            u_next -= transfer;

                            if (b_next >= 1.0f)
                            {
                                ice_next = true;
                            }
                        }

                        m_NextDiffusive[idx] = u_next;
                        m_NextBoundary[idx] = b_next;
                        m_NextIce[idx] = ice_next;
                    }
                });

                Array.Copy(m_NextDiffusive, m_DiffusiveMass, N);
                Array.Copy(m_NextBoundary, m_BoundaryMass, N);
                Array.Copy(m_NextIce, m_Ice, N);
            }

            // 3. GENERATE GEOMETRY
            GenerateSharpMesh(ctx);
        }

        void GenerateSharpMesh(EngineeringContext ctx)
        {
            Mesh mTotal = new Mesh();
            
            // Auto-scale
            float maxR = 0;
            int count = 0;
            for(int i=0; i<m_Ice.Length; i++) {
                if(m_Ice[i]) {
                    int x = i % GRID_SIZE;
                    int y = i / GRID_SIZE;
                    float d = (x-CENTER)*(x-CENTER) + (y-CENTER)*(y-CENTER);
                    if(d > maxR) maxR = d;
                    count++;
                }
            }
            if(count < 5) maxR = 5;
            maxR = MathF.Sqrt(maxR);
            
            float cellSpacing = (m_Size * 0.5f) / maxR;
            float sqrt3 = MathF.Sqrt(3f);
            
            // --- FIXED MESH GENERATION ---
            // Use ShapeKernel BaseCylinder to create a hexagonal prism
            // Radius calculation for tight packing:
            float hexRad = (cellSpacing / sqrt3) * 1.05f; 
            
            // Create a temporary Hexagon at origin
            BaseCylinder hexGen = new BaseCylinder(new LocalFrame(), m_Thickness, hexRad);
            hexGen.SetRadialSteps(6); // Make it hexagonal
            Mesh protoHex = hexGen.mshConstruct();
            
            // Iterate grid and append hexagons
            for (int y = 0; y < GRID_SIZE; y++)
            {
                for (int x = 0; x < GRID_SIZE; x++)
                {
                    int idx = y * GRID_SIZE + x;
                    
                    if (m_Ice[idx])
                    {
                        // Hex Cartesian Coords
                        float xPos = x * cellSpacing;
                        float yPos = y * cellSpacing * (sqrt3 / 2f);
                        if (y % 2 != 0) xPos += (cellSpacing / 2f);

                        xPos -= (CENTER * cellSpacing) + (cellSpacing * 0.25f);
                        yPos -= (CENTER * cellSpacing * (sqrt3 / 2f));

                        // Append to total mesh
                        Mesh hex = protoHex.mshCreateTransformed(Vector3.One, new Vector3(xPos, yPos, 0));
                        mTotal.Append(hex);
                    }
                }
            }

            Voxels vSnow = new Voxels(mTotal);
            ctx.Assembly.BoolAdd(vSnow);
        }

        int GetIdx(int x, int y) 
        {
            if(x<0 || x>=GRID_SIZE || y<0 || y>=GRID_SIZE) return -1;
            return y * GRID_SIZE + x;
        }
    }
}