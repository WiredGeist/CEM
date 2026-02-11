using System;
using System.Collections.Generic;
using System.Numerics;
using MyFirstApp.Core; // For Pal/Vis

namespace MyFirstApp.Core
{
    public class VeinNode
    {
        public Vector3 Position;
        public VeinNode? Parent;
        public List<VeinNode> Children = new List<VeinNode>();
        public float Thickness;
    }

    public static class FractalMath
    {
        // Random Seed for consistent results
        private static Random _rng = new Random(12345);

        /// <summary>
        /// Grows a recursive tree structure constrained to a specific radius profile.
        /// </summary>
        public static List<List<Vector3>> GenerateConstrainedVeins(
            Func<float, float> boundaryFunc, // The Wall Radius Logic
            float zStart, 
            float zEnd, 
            int initialBranches = 6)
        {
            var allPaths = new List<List<Vector3>>();
            var activeTips = new List<VeinNode>();

            // 1. Initialize Roots (Spaced around the circle)
            for (int i = 0; i < initialBranches; i++)
            {
                float angle = (i / (float)initialBranches) * MathF.PI * 2f;
                float r = boundaryFunc(zStart);
                
                VeinNode root = new VeinNode 
                { 
                    Position = new Vector3(MathF.Cos(angle) * r, MathF.Sin(angle) * r, zStart),
                    Thickness = 4f
                };
                activeTips.Add(root);
            }

            // 2. Grow Step-by-Step
            // Instead of pure recursion (which can stack overflow), we use a loop (Iterative Growth)
            float stepSize = 10f;
            float branchChance = 0.05f; // 5% chance to split
            
            while (activeTips.Count > 0)
            {
                var newTips = new List<VeinNode>();

                foreach (var tip in activeTips)
                {
                    // A. Calculate new position (Move Up + Random Wiggle)
                    // Wiggle logic: Brownian motion
                    float wiggleX = ((float)_rng.NextDouble() - 0.5f) * 5f;
                    float wiggleY = ((float)_rng.NextDouble() - 0.5f) * 5f;
                    
                    Vector3 idealMove = new Vector3(wiggleX, wiggleY, stepSize);
                    Vector3 candidatePos = tip.Position + idealMove;

                    // B. Stop if we reached the top
                    if (candidatePos.Z >= zEnd) 
                    {
                        // Save this path
                        allPaths.Add(TraceBack(tip));
                        continue; 
                    }

                    // C. CONSTRAINT (The Engineering Part)
                    // Force the point to snap to the wall radius
                    float requiredRadius = boundaryFunc(candidatePos.Z);
                    if (requiredRadius <= 0) continue; // Wall ended

                    // Flatten Z to calculate current radius
                    Vector2 flatPos = new Vector2(candidatePos.X, candidatePos.Y);
                    Vector2 normalizedDir = Vector2.Normalize(flatPos);
                    
                    // Project back onto the wall cylinder
                    Vector2 constrainedPos2D = normalizedDir * requiredRadius;
                    
                    candidatePos = new Vector3(constrainedPos2D.X, constrainedPos2D.Y, candidatePos.Z);

                    // D. Create Node
                    VeinNode newNode = new VeinNode { Position = candidatePos, Parent = tip, Thickness = tip.Thickness * 0.98f };
                    tip.Children.Add(newNode);

                    // E. Branching Logic (Fractal)
                    bool split = _rng.NextDouble() < branchChance;
                    if (split)
                    {
                        // Create a second branch slightly offset
                        // Twist the angle slightly
                        float branchAngle = 0.3f; // Radians
                        float ca = MathF.Cos(branchAngle);
                        float sa = MathF.Sin(branchAngle);
                        
                        // Rotate position around Z for the clone
                        float newX = candidatePos.X * ca - candidatePos.Y * sa;
                        float newY = candidatePos.X * sa + candidatePos.Y * ca;
                        
                        VeinNode branchNode = new VeinNode { 
                            Position = new Vector3(newX, newY, candidatePos.Z), 
                            Parent = tip, 
                            Thickness = tip.Thickness * 0.8f 
                        };
                        newTips.Add(branchNode);
                    }
                    
                    newTips.Add(newNode);
                }
                
                activeTips = newTips;
            }

            return allPaths;
        }

        private static List<Vector3> TraceBack(VeinNode endNode)
        {
            var path = new List<Vector3>();
            var curr = endNode;
            while(curr != null)
            {
                path.Add(curr.Position);
                curr = curr.Parent;
            }
            path.Reverse();
            return path;
        }
    }
}