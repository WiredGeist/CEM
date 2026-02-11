using System;
using System.Drawing;
using System.Windows.Forms;
using PicoGK; 
using MyFirstApp.Core;
using MyFirstApp.Projects; // Access SystemLoop
using MyFirstApp.Algorithms.Playground;
using MyFirstApp.Products.Propulsion;
using MyFirstApp.Products.Propulsion.Components; // <--- FIX 1: Fixes ExhaustNozzle error

namespace MyFirstApp.UI
{
    public class MainForm : Form
    {
        // GLOBAL STATE
        public static EngineeringComponent? RootComponent;
        public bool NeedsUpdate = false;
        public string? ExportPath = null; 

        // UI ELEMENTS
        public Panel ViewportPanel = null!;
        FlowLayoutPanel tabObjectBar = null!; 
        Panel pParams = null!;
        Panel pCalcs = null!;
        
        // THEME COLORS
        Color cBack = Color.FromArgb(45, 45, 48);
        Color cDark = Color.FromArgb(30, 30, 30);
        Color cText = Color.FromArgb(240, 240, 240);
        Color cAcc = Color.FromArgb(0, 122, 204);
        Color cControl = Color.FromArgb(60, 60, 60); // <--- FIX 2: Added missing cControl

        public MainForm()
        {
            this.Text = "WiredGeist Architect";
            this.Size = new Size(1800, 900);
            this.WindowState = FormWindowState.Maximized;
            this.BackColor = cDark;
            this.ForeColor = cText;
            this.Font = new Font("Segoe UI", 9F);

            InitializeMenu();
            InitializeLayout();
        }

        void InitializeMenu()
        {
            MenuStrip ms = new MenuStrip { BackColor = cBack, ForeColor = cText };
            
            // --- 1. FILE MENU (New/Save/Load/Export) ---
            var file = new ToolStripMenuItem("File");
            
            file.DropDownItems.Add("New Project", null, (s,e) => { 
                RootComponent = null; 
                RefreshObjectTabs(); 
                try { Library.oViewer().RemoveAllObjects(); Library.oViewer().RequestUpdate(); } catch { } 
                pParams.Controls.Clear();
                pCalcs.Controls.Clear();
                NeedsUpdate = true;
            });

            file.DropDownItems.Add("Save Project As...", null, (s,e) => {
                if (RootComponent == null) return;
                SaveFileDialog sfd = new SaveFileDialog { Filter = "WiredGeist Project (*.wgp)|*.wgp" };
                if (sfd.ShowDialog() == DialogResult.OK) ProjectIO.SaveProject(RootComponent, sfd.FileName);
            });

            file.DropDownItems.Add("Open Project...", null, (s,e) => {
                OpenFileDialog ofd = new OpenFileDialog { Filter = "WiredGeist Project (*.wgp)|*.wgp" };
                if (ofd.ShowDialog() == DialogResult.OK) {
                    var loaded = ProjectIO.LoadProject(ofd.FileName);
                    if (loaded != null) SetRoot(loaded);
                }
            });

            file.DropDownItems.Add(new ToolStripSeparator());

            file.DropDownItems.Add("Export as STL...", null, (s,e) => {
                if (RootComponent == null) { MessageBox.Show("No object to export."); return; }
                SaveFileDialog sfd = new SaveFileDialog();
                sfd.Filter = "Stereolithography (*.stl)|*.stl";
                sfd.FileName = RootComponent.Name + ".stl";
                if (sfd.ShowDialog() == DialogResult.OK) {
                    ExportPath = sfd.FileName; 
                    NeedsUpdate = true; 
                    MessageBox.Show("Export queued! Check console.");
                }
            });

            file.DropDownItems.Add(new ToolStripSeparator());
            file.DropDownItems.Add("Exit", null, (s,e) => Application.Exit());

            // --- 2. SETTINGS MENU ---
            var settings = new ToolStripMenuItem("Resolution");
            void AddRes(string l, float v) {
                var i = new ToolStripMenuItem($"{l} ({v}mm)");
                if (Math.Abs(AppConfig.VoxelSize - v) < 0.01f) i.Checked = true;
                i.Click += (s,e) => { 
                    if (MessageBox.Show("Restart required to change resolution. Proceed?", "Confirm", MessageBoxButtons.YesNo) == DialogResult.Yes) {
                        AppConfig.Save(v); Application.Restart(); Environment.Exit(0); 
                    }
                };
                settings.DropDownItems.Add(i);
            }
            AddRes("High", 0.2f); AddRes("Standard", 0.5f); AddRes("Draft", 1.0f); AddRes("Raw", 3.0f);

            // --- 3. ADD OBJECT MENU ---
            var add = new ToolStripMenuItem("Add Object");

            // Propulsion
            var prop = new ToolStripMenuItem("Propulsion");
            prop.DropDownItems.Add("Turbojet Assembly", null, (s,e) => SetRoot(new TurbojetAssembly()));
            
            // Algorithms 
            var algo = new ToolStripMenuItem("Algorithms");
            algo.DropDownItems.Add("Test Lab: Compressor", null, (s,e) => SetRoot(new CompressorGen()));
            algo.DropDownItems.Add("Test Lab: Gyroid Box", null, (s,e) => SetRoot(new GyroidTest()));
            algo.DropDownItems.Add("Test Lab: Nautilus", null, (s,e) => SetRoot(new NautilusGen()));
            algo.DropDownItems.Add("Test Lab: Snowflake", null, (s,e) => SetRoot(new SnowflakeGen()));
            algo.DropDownItems.Add("Test Lab: TwistedFinTest", null, (s,e) => SetRoot(new TwistedFinTest()));
            algo.DropDownItems.Add("Test Lab: GyroidInfillTest", null, (s,e) => SetRoot(new GyroidInfillTest()));
            algo.DropDownItems.Add("Test Lab: Plane", null, (s,e) => SetRoot(new Plane()));
            algo.DropDownItems.Add("Test Lab: SupershapeFormula", null, (s,e) => SetRoot(new SupershapeFormula()));
            algo.DropDownItems.Add("Test Lab: LSystem", null, (s,e) => SetRoot(new LSystem())); 
            algo.DropDownItems.Add("Test Lab: TopologyOptimization", null, (s,e) => SetRoot(new TopologyOptimization()));
            algo.DropDownItems.Add("Test Lab: Phyllotaxis", null, (s,e) => SetRoot(new Phyllotaxis()));
            algo.DropDownItems.Add("Test Lab: SpaceColonization", null, (s,e) => SetRoot(new SpaceColonization())); 
            algo.DropDownItems.Add("Test Lab: CellularAutomata", null, (s,e) => SetRoot(new CellularAutomata()));
            algo.DropDownItems.Add("Test Lab: VoronoiInjector", null, (s,e) => SetRoot(new VoronoiInjector()));
            algo.DropDownItems.Add("Test Lab: WarpedInjector", null, (s,e) => SetRoot(new WarpedInjector()));
            algo.DropDownItems.Add("Test Lab: PolygonInjector", null, (s,e) => SetRoot(new PolygonInjector()));
            algo.DropDownItems.Add("Test Lab: ImplicitGenusGyroid", null, (s,e) => SetRoot(new ImplicitGenusGyroid()));
            algo.DropDownItems.Add("Test Lab: ScrewGearGen", null, (s,e) => SetRoot(new ScrewGearGen()));
            algo.DropDownItems.Add("Test Lab: EMotor", null, (s,e) => SetRoot(new EMotor()));
            algo.DropDownItems.Add("Test Lab: CopperCoil", null, (s,e) => SetRoot(new SmartCopperCoil()));

            add.DropDownItems.Add(prop);
            add.DropDownItems.Add(algo);

            ms.Items.Add(file);
            ms.Items.Add(add);
            ms.Items.Add(settings);
            
            this.Controls.Add(ms);
        }

        void InitializeLayout()
        {
            // --- TOP BAR ---
            tabObjectBar = new FlowLayoutPanel { Dock = DockStyle.Top, Height = 45, BackColor = cBack, Padding = new Padding(5) };
            this.Controls.Add(tabObjectBar);

            AddSlicerControls(); // Slicer Toggle

            // --- 3-COLUMN GRID ---
            TableLayoutPanel grid = new TableLayoutPanel();
            grid.Dock = DockStyle.Fill;
            grid.ColumnCount = 3;
            grid.RowCount = 1;
            grid.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 350f)); // Params
            grid.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 300f)); // Calcs
            grid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));  // Viewport
            
            this.Controls.Add(grid);
            grid.BringToFront();

            // Col 1: Params
            Panel c1 = new Panel { Dock = DockStyle.Fill, Margin = new Padding(1) };
            c1.Controls.Add(CreateHeader("PARAMETERS"));
            pParams = new Panel { Dock = DockStyle.Fill, AutoScroll = true, BackColor = cBack };
            c1.Controls.Add(pParams); 
            grid.Controls.Add(c1, 0, 0);

            // Col 2: Calculations
            Panel c2 = new Panel { Dock = DockStyle.Fill, Margin = new Padding(1) };
            c2.Controls.Add(CreateHeader("CALCULATIONS"));
            pCalcs = new Panel { Dock = DockStyle.Fill, AutoScroll = true, BackColor = cBack };
            c2.Controls.Add(pCalcs);
            grid.Controls.Add(c2, 1, 0);

            // Col 3: Viewport
            Panel c3 = new Panel { Dock = DockStyle.Fill, Margin = new Padding(0) };
            Label lView = CreateHeader("3D VIEWPORT"); lView.ForeColor = cAcc;
            c3.Controls.Add(lView);
            ViewportPanel = new Panel { Dock = DockStyle.Fill, BackColor = Color.Black };
            c3.Controls.Add(ViewportPanel);
            grid.Controls.Add(c3, 2, 0);

            ViewportPanel.Resize += (s, e) => { if(this.Visible) WindowManager.EmbedPicoGK(ViewportPanel); };
        }

        void AddSlicerControls()
        {
            Label lbl = new Label { Text = "SLICER:", AutoSize = true, ForeColor = Color.Gray, Padding = new Padding(10,8,0,0) };
            tabObjectBar.Controls.Add(lbl);
            
            CheckBox chk = new CheckBox { Text = "On", ForeColor = Color.White, AutoSize = true, Padding = new Padding(0,5,0,0) };
            
            // UI State
            int currentAxis = 0;
            float currentOffset = 0f;

            chk.CheckedChanged += (s, e) => { 
                SystemLoop.UpdateSlicer(chk.Checked, currentAxis, currentOffset); 
            };
            tabObjectBar.Controls.Add(chk);

            ComboBox cmb = new ComboBox { Width = 50, BackColor = cControl, ForeColor = Color.White, FlatStyle = FlatStyle.Flat };
            cmb.Items.AddRange(new string[] { "X", "Y", "Z" });
            cmb.SelectedIndex = 0;
            
            cmb.SelectedIndexChanged += (s, e) => { 
                currentAxis = cmb.SelectedIndex;
                SystemLoop.UpdateSlicer(chk.Checked, currentAxis, currentOffset); 
            };
            tabObjectBar.Controls.Add(cmb);

            TrackBar tb = new TrackBar { Width = 150, Minimum = -500, Maximum = 500, TickStyle = TickStyle.None, Value = 0 };
            
            tb.Scroll += (s, e) => { 
                currentOffset = tb.Value;
                // This will trigger the Debouncer in SystemLoop, preventing crash
                SystemLoop.UpdateSlicer(chk.Checked, currentAxis, currentOffset); 
            };
            tabObjectBar.Controls.Add(tb);
        }

        Label CreateHeader(string text) => new Label { 
            Text = text, Dock = DockStyle.Top, Height = 30, 
            Font = new Font("Segoe UI", 10, FontStyle.Bold), 
            BackColor = Color.FromArgb(60,60,60), ForeColor = Color.White, 
            TextAlign = ContentAlignment.MiddleCenter 
        };

        void SetRoot(EngineeringComponent comp) { RootComponent = comp; RefreshObjectTabs(); NeedsUpdate = true; }

        void AddChildToSelected(EngineeringComponent child) {
            if (RootComponent != null) { RootComponent.AddChild(child); RefreshObjectTabs(); NeedsUpdate = true; }
        }

        void RefreshObjectTabs()
        {
            tabObjectBar.Controls.Clear();
            AddSlicerControls(); // Always keep slicer

            if (RootComponent != null)
            {
                AddTabButton(RootComponent);
                foreach (var child in RootComponent.Children) AddTabButton(child);

                Button btnPlus = new Button { Text = "+", Width = 30, Height = 30, BackColor = cAcc, FlatStyle = FlatStyle.Flat };
                btnPlus.Click += (s, e) => {
                    ContextMenuStrip cms = new ContextMenuStrip();
                    // FIX 1: ExhaustNozzle is now recognized because we added the namespace
                    cms.Items.Add("Add Exhaust Nozzle", null, (xx, yy) => AddChildToSelected(new ExhaustNozzle()));
                    cms.Show(Cursor.Position);
                };
                tabObjectBar.Controls.Add(btnPlus);
            }

            if (RootComponent != null && pParams.Controls.Count == 0) BuildInspector(RootComponent);
        }

        void AddTabButton(EngineeringComponent comp)
        {
            Button btn = new Button { Text = comp.Name, AutoSize = true, Height = 30, BackColor = cControl, ForeColor = Color.White, FlatStyle = FlatStyle.Flat };
            btn.Click += (s, e) => BuildInspector(comp);
            tabObjectBar.Controls.Add(btn);
        }

        void BuildInspector(EngineeringComponent? comp)
    {
        pParams.Controls.Clear();
        pCalcs.Controls.Clear();
        if (comp == null) return;

        // --- 1. PARAMETERS (Inputs) ---
        int y = 5;
        
        // Header for the Object Name
        Label lHeader = new Label { 
            Text = comp.Name.ToUpper(), 
            Top = y, Left = 5, 
            AutoSize = true, 
            Font = new Font("Segoe UI", 10, FontStyle.Bold), 
            ForeColor = cAcc 
        };
        pParams.Controls.Add(lHeader);
        y += 30;

        foreach(var p in comp.GetParameters())
        {
            // =========================================================
            // FIX: ACTION BUTTON LOGIC
            // If name starts with ">>>", render a big Action Button
            // =========================================================
            if (p.Name.StartsWith(">>>"))
            {
                Button btnAction = new Button {
                    Text = p.Name.Replace(">>>", "").Replace("<<<", "").Trim(),
                    Top = y, Left = 5,
                    Width = pParams.Width - 30, // Full Width
                    Height = 40,
                    BackColor = cAcc, // Accent Color (Blue)
                    ForeColor = Color.White,
                    FlatStyle = FlatStyle.Flat,
                    Font = new Font("Segoe UI", 9F, FontStyle.Bold),
                    Cursor = Cursors.Hand,
                    Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right
                };

                // Remove border for cleaner look
                btnAction.FlatAppearance.BorderSize = 0;

                btnAction.Click += (s, e) => {
                    // 1. Set value to 1 (True)
                    p.OnChange(1.0f); 
                    // 2. Trigger System Update
                    NeedsUpdate = true; 
                };

                pParams.Controls.Add(btnAction);
                y += 50; // Spacing after button
                continue; // Skip the rest of the loop for this parameter
            }

            // =========================================================
            // CHECKBOX LOGIC
            // =========================================================
            if(p.Name.StartsWith("[ ")) {
                CheckBox chk = new CheckBox { 
                    Text = p.Name, 
                    Top = y, Left = 5, 
                    AutoSize = true, 
                    Checked = p.Value > 0.5f, 
                    ForeColor = Color.Gray 
                };
                chk.CheckedChanged += (s,e) => { p.OnChange(chk.Checked?1:0); NeedsUpdate=true; };
                pParams.Controls.Add(chk); 
                y += 35; 
                continue;
            }

            // =========================================================
            // STANDARD SLIDER LOGIC
            // =========================================================
            Panel pControl = new Panel { 
                Top = y, Left = 0, 
                Width = pParams.Width - 25, 
                Height = 65,
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right 
            };

            Label l = new Label { 
                Text = p.Name, 
                Top = 0, Left = 5, 
                AutoSize = true, 
                ForeColor = cText,
                Font = new Font("Segoe UI", 9F)
            };
            
            int txtWidth = 60;
            TextBox txt = new TextBox { 
                Top = 25, 
                Left = pControl.Width - txtWidth - 5, 
                Width = txtWidth, 
                BackColor = cControl, 
                ForeColor = Color.White, 
                BorderStyle = BorderStyle.FixedSingle, 
                Text = p.Value.ToString("0.##"),
                Anchor = AnchorStyles.Top | AnchorStyles.Right 
            };

            TrackBar tb = new TrackBar { 
                Top = 25, 
                Left = 0, 
                Width = pControl.Width - txtWidth - 10, 
                Maximum = 1000, 
                TickStyle = TickStyle.None,
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right 
            };

            float range = p.Max - p.Min; if(range <= 0) range = 1;
            tb.Value = (int)(((Math.Clamp(p.Value, p.Min, p.Max) - p.Min) / range) * 1000);

            tb.Scroll += (s, e) => {
                float val = p.Min + (tb.Value / 1000f) * range;
                p.OnChange(val); p.Value = val; 
                txt.Text = val.ToString("0.##");
            };
            tb.MouseUp += (s, e) => { NeedsUpdate = true; BuildInspector(comp); };

            Action commit = () => {
                if(float.TryParse(txt.Text, out float val)) {
                    if(val < p.Min) val = p.Min; if(val > p.Max) val = p.Max;
                    p.OnChange(val); p.Value = val;
                    tb.Value = (int)(((val - p.Min) / range) * 1000);
                    txt.Text = val.ToString("0.##");
                    NeedsUpdate = true;
                } else { txt.Text = p.Value.ToString("0.##"); }
            };
            txt.KeyDown += (s, e) => { if(e.KeyCode == Keys.Enter) { commit(); e.SuppressKeyPress = true; this.ActiveControl = null; } };
            txt.Leave += (s, e) => commit();

            pControl.Controls.Add(l); 
            pControl.Controls.Add(tb); 
            pControl.Controls.Add(txt);
            pParams.Controls.Add(pControl);
            y += 70;
        }

        // --- 2. CALCULATIONS (Outputs) ---
        var results = comp.GetResults();
        if (results.Count == 0) {
            pCalcs.Controls.Add(new Label { Text = "No simulation data.", Top = 5, ForeColor = Color.Gray, AutoSize = true });
        } else {
            int cy = 5;
            foreach(var r in results) {
                Panel row = new Panel { Top = cy, Width = pCalcs.Width - 10, Height = 25 };
                
                Label lKey = new Label { 
                    Text = r.Label, Dock = DockStyle.Left, Width = 150, 
                    ForeColor = Color.Gray, TextAlign = ContentAlignment.MiddleLeft 
                };
                
                Label lVal = new Label { 
                    Text = r.Value, Dock = DockStyle.Fill, 
                    ForeColor = r.IsWarning ? Color.Red : cAcc, 
                    Font = new Font("Consolas", 10, FontStyle.Bold),
                    TextAlign = ContentAlignment.MiddleLeft
                };
                
                row.Controls.Add(lVal);
                row.Controls.Add(lKey);
                pCalcs.Controls.Add(row);
                cy += 30;
            }
        }
    }
    }
}