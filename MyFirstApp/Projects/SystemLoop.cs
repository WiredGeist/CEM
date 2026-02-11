// FILE: SystemLoop.cs

using System;
using System.Threading;
using System.Windows.Forms;
using System.Numerics;
using PicoGK;
using MyFirstApp.UI;
using MyFirstApp.Core;
using MyFirstApp.Core.Engine;

namespace MyFirstApp.Projects
{
    public static class SystemLoop
    {
        static MainForm? ui;

        // --- SLICER STATE ---
        public static bool SliceActive = false;
        public static int SliceAxis = 0; 
        public static float SliceOffset = 0f;
        
        // DEBOUNCER
        private static DateTime _lastSliceInteraction = DateTime.Now;
        private static bool _sliceDirty = false;

        public static void UpdateSlicer(bool active, int axis, float offset)
        {
            if (SliceActive != active || SliceAxis != axis || Math.Abs(SliceOffset - offset) > 0.1f)
            {
                SliceActive = active;
                SliceAxis = axis;
                SliceOffset = offset;
                
                _sliceDirty = true;
                _lastSliceInteraction = DateTime.Now;
                
                if(ui != null) ui.NeedsUpdate = true;
            }
        }

        public static void Run()
        {
            Console.WriteLine("--- WIREDGEIST ENGINE START ---");

            Thread uiThread = new Thread(() => 
            {
                Application.SetHighDpiMode(HighDpiMode.SystemAware);
                Application.EnableVisualStyles();
                ui = new MainForm();
                Application.Run(ui);
            });
            uiThread.SetApartmentState(ApartmentState.STA);
            uiThread.Start();

            while (ui == null || !ui.IsHandleCreated) Thread.Sleep(100);

            while (true)
            {
                try { Library.oViewer().SetBackgroundColor(new ColorFloat(0.15f, 0.15f, 0.15f)); break; }
                catch { Thread.Sleep(200); }
            }

            int attempts = 0;
            bool embedded = false;
            while (!embedded && attempts < 50) 
            {
                if (!ui.Visible) break;
                ui.Invoke(new Action(() => 
                { 
                    WindowManager.EmbedPicoGK(ui.ViewportPanel); 
                    WindowManager.UpdateSize(ui.ViewportPanel);
                }));
                Thread.Sleep(200);
                attempts++;
                if (attempts > 10) embedded = true; 
            }

            // --- MAIN RENDER LOOP ---
            while (ui.Visible)
            {
                bool slicerReady = false;
                if (_sliceDirty && (DateTime.Now - _lastSliceInteraction).TotalMilliseconds > 100)
                {
                    slicerReady = true;
                    _sliceDirty = false;
                }

                if (ui.NeedsUpdate || slicerReady)
                {
                    ui.NeedsUpdate = false;
                    try
                    {
                        EngineeringContext ctx = new EngineeringContext();

                        if (MainForm.RootComponent != null)
                        {
                            ProcessPhysics(MainForm.RootComponent, ctx);
                            ProcessConstruct(MainForm.RootComponent, ctx);

                            Voxels vSolids = new Voxels(ctx.GlobalSolids);
                            ctx.Assembly.BoolAdd(vSolids);

                            Voxels vVoids = new Voxels(ctx.GlobalVoids);
                            ctx.Assembly.BoolSubtract(vVoids);
                        }

                        if (SliceActive && ctx.Assembly != null)
                        {
                            BBox3 bounds = new BBox3();
                            ctx.Assembly.CalculateProperties(out _, out bounds); 
                            float size = bounds.vecSize().Length() * 2f; 
                            
                            Vector3 center = Vector3.Zero;
                            Vector3 boxSize = new Vector3(size,size,size);

                            if (SliceAxis == 0) center = new Vector3(SliceOffset + size/2f, bounds.vecCenter().Y, bounds.vecCenter().Z);
                            else if (SliceAxis == 1) center = new Vector3(bounds.vecCenter().X, SliceOffset + size/2f, bounds.vecCenter().Z);
                            else center = new Vector3(bounds.vecCenter().X, bounds.vecCenter().Y, SliceOffset + size/2f);

                            Mesh mCutter = Utils.mshCreateCube(boxSize, center);
                            Voxels vCutter = new Voxels();
                            vCutter.RenderMesh(mCutter);
                            ctx.Assembly.BoolSubtract(vCutter);
                        }

                        // ==================================================================
                        // --- FIX START: EXPORT LOGIC ---
                        // ==================================================================
                        if (!string.IsNullOrEmpty(ui.ExportPath) && ctx.Assembly != null)
                        {
                            try
                            {
                                Console.WriteLine($"[System] Exporting STL to: {ui.ExportPath}...");
                                
                                // Convert Voxels to Mesh
                                Mesh mshExport = ctx.Assembly.mshAsMesh();
                                
                                // Save
                                mshExport.SaveToStlFile(ui.ExportPath);
                                
                                Console.WriteLine("[System] Export Successful.");
                                MessageBox.Show($"Successfully exported to:\n{ui.ExportPath}", "Export Complete");
                            }
                            catch (Exception ex)
                            {
                                Console.WriteLine("[System] Export Error: " + ex.Message);
                                MessageBox.Show("Failed to export STL:\n" + ex.Message, "Export Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                            }
                            finally
                            {
                                // Reset the path so we don't try to save again in the next frame
                                ui.ExportPath = null;
                            }
                        }
                        // ==================================================================
                        // --- FIX END ---
                        // ==================================================================

                        // 4. RENDER
                        Library.oViewer().RemoveAllObjects();
                        Library.oViewer().SetBackgroundColor(new ColorFloat(0.15f, 0.15f, 0.15f));

                        if (ctx.Assembly != null && ctx.Assembly.mshAsMesh().nTriangleCount() > 0)
                        {
                            Library.oViewer().Add(ctx.Assembly, 0);
                            Library.oViewer().SetGroupMaterial(0, new ColorFloat(0.8f, 0.8f, 0.8f), 0.6f, 0.4f);
                        }

                        if (MainForm.RootComponent != null)
                        {
                            ProcessPreview(MainForm.RootComponent, ctx);
                        }
                        
                        Library.oViewer().RequestUpdate();
                    }
                    catch (Exception ex) 
                    { 
                        Console.WriteLine("Render Error: " + ex.Message); 
                    }
                }
                
                Thread.Sleep(50);
            }
        }

        static void ProcessPhysics(EngineeringComponent comp, EngineeringContext ctx)
        {
            if (!comp.Enabled) return;
            comp.CalculatePhysics(ctx);
            lock(comp.Children) foreach(var child in comp.Children) ProcessPhysics(child, ctx);
        }

        static void ProcessConstruct(EngineeringComponent comp, EngineeringContext ctx)
        {
            if (!comp.Enabled) return;
            comp.Build(ctx);
            lock(comp.Children) foreach(var child in comp.Children) ProcessConstruct(child, ctx);
        }

        static void ProcessPreview(EngineeringComponent comp, EngineeringContext ctx)
        {
            if (!comp.Enabled) return;
            comp.OnPreview(ctx);
            lock(comp.Children) foreach(var child in comp.Children) ProcessPreview(child, ctx);
        }
    }
}