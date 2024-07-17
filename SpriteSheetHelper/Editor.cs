using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Xml.Linq;

namespace SpriteSheetHelper
{
    public partial class Editor : Form
    {

        List<string> SheetPaths = new List<string>();
        public string? workingDir = null;

        Sprite? SelectedSprite = null;
        TreeNode? SelectedNode = null;
        class Sprite
        {
            public string name = "New Sprite";
            public string stack = "";
            public string sheet;
            public Rectangle bounds;
            public bool rotates = false;
            public override string ToString()
            {
                return name ?? "New Sprite";
            }
        }

        public int GRID = 8;


        public Editor()
        {
            InitializeComponent();
            typeof(Panel).InvokeMember("DoubleBuffered", BindingFlags.SetProperty | BindingFlags.Instance | BindingFlags.NonPublic, null, pDrawing, new object[] { true });
            // Add event handlers for the required drag events.
            treeView1.ItemDrag += new ItemDragEventHandler(treeView1_ItemDrag);
            treeView1.DragEnter += new DragEventHandler(treeView1_DragEnter);
            treeView1.DragOver += new DragEventHandler(treeView1_DragOver);
            treeView1.DragDrop += new DragEventHandler(treeView1_DragDrop);
            this.KeyDown += Editor_KeyDown1;
            treeView1.KeyDown += Editor_KeyDown1;
            textBox1.KeyDown += Editor_KeyDown1;
            pDrawing.KeyDown += Editor_KeyDown1;
        }

        //Handles the editor keydown event
        private void Editor_KeyDown1(object? sender, KeyEventArgs e)
        {
            if (!CanUI()) return;

            if (ModifierKeys.HasFlag(Keys.Control))
            {
                if (e.KeyCode == Keys.N)
                {
                    newSpriteToolStripMenuItem_Click(this, new EventArgs());
                }
                else if (e.KeyCode == Keys.G)
                {
                    insertNodeToolStripMenuItem_Click(this, new EventArgs());
                }
            }
        }

        private void bAddSheet_Click(object sender, EventArgs e)
        {
            using (OpenFileDialog ofd = new OpenFileDialog() { Filter = "Image Files | *.png" })
            {
                if (ofd.ShowDialog() == DialogResult.OK)
                {
                    if (SheetPaths.Contains(ofd.FileName)) return;
                    SheetPaths.Add(ofd.FileName);
                    lbSheets.Items.Clear();
                    cbSheetSelector.Items.Clear();
                    foreach (string s in SheetPaths)
                    {
                        lbSheets.Items.Add(s);
                        cbSheetSelector.Items.Add(Path.GetFileNameWithoutExtension(s));
                    }
                }
            }
        }

        private void bRemoveSheet_Click(object sender, EventArgs e)
        {

            if (lbSheets.SelectedItem != null)
            {
                SheetPaths.Remove((string)lbSheets.SelectedItem);
                lbSheets.Items.Clear();
                cbSheetSelector.Items.Clear();
                foreach (string s in SheetPaths)
                {
                    lbSheets.Items.Add(s);
                    cbSheetSelector.Items.Add(Path.GetFileNameWithoutExtension(s));
                }
            }
        }

        private void cbSheetSelector_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (!CanUI()) return;

            if (pDrawing.Tag != null)
            {
                ((IDisposable)pDrawing.Tag).Dispose();
                pDrawing.Tag = null;
            }
            pDrawing.Invalidate();
        }


        public Point ClickPlace;
        public bool Dragging = false;
        public Point MouseCurrent;
        public Rectangle Selected;

        private void pDrawing_Paint(object sender, PaintEventArgs e)
        {
            //draw it nicely
            e.Graphics.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.NearestNeighbor;
            e.Graphics.PixelOffsetMode = System.Drawing.Drawing2D.PixelOffsetMode.Half;

            //go through each sheet in the list of available sheets
            int n = cbSheetSelector.SelectedIndex;
            if (n >= 0 && n < SheetPaths.Count)
            {
                if (pDrawing.Tag == null)
                {
                    string p = (workingDir != null ? workingDir + "\\" : "") + SheetPaths[n];
                    if (File.Exists(p))
                        pDrawing.Tag = Image.FromFile(p);
                }

                //if we actuallyhave a tag for the main viewer panel then draw it
                if (pDrawing.Tag != null && pDrawing.Tag is Image i)
                {

                    //clear stuff
                    e.Graphics.Clear(pDrawing.BackColor);
                    //integral scale, we want crisp pixel graphics
                    int s = Math.Min((int)pDrawing.Width / i.Width, (int)pDrawing.Height / i.Height);
                    int w = (pDrawing.Width - s * i.Width) / 2;
                    int h = (pDrawing.Height - s * i.Height) / 2;
                    Rectangle iBounds = new Rectangle(w, h, s * i.Width, s * i.Height);

                    e.Graphics.DrawImage(i, w, h, s * i.Width, s * i.Height);

                    //now set the transform to the scale yay
                    e.Graphics.Transform = new System.Drawing.Drawing2D.Matrix(s * GRID, 0, 0, s * GRID, w, h);


                    var iR = new Rectangle(0, 0, i.Width, i.Height);
                    if (iR.Contains(MouseCurrent) && (!Dragging || iR.Contains(ClickPlace)))
                    {
                        if (Dragging)
                        {
                            var p = MouseCurrent;
                            e.Graphics.DrawRectangle(new Pen(Color.FromArgb(128, 255, 0, 0), 2 / (GRID * s)),
                                Math.Min(ClickPlace.X / GRID, MouseCurrent.X / GRID),
                                Math.Min(ClickPlace.Y / GRID, MouseCurrent.Y / GRID),
                                1 + Math.Abs((ClickPlace.X - MouseCurrent.X) / GRID),
                                1 + Math.Abs((ClickPlace.Y - MouseCurrent.Y) / GRID));
                        }
                        else e.Graphics.DrawRectangle(new Pen(Color.FromArgb(128, 255, 0, 0), 1f / (GRID * s)), MouseCurrent.X / GRID, MouseCurrent.Y / GRID, 1, 1); ;
                    }

                    if (iR.Contains(Selected))
                    {
                        e.Graphics.DrawRectangle(new Pen(Color.FromArgb(128, 50, 80, 255), 3f / (GRID * s)), Selected.X / GRID, Selected.Y / GRID, Selected.Width / GRID, Selected.Height / GRID);
                    }


                }
            }
            else
            {
                //We did have an image, but we're not selecting anything, so dispose
                if (pDrawing.Tag != null && pDrawing.Tag is Image i)
                {
                    i.Dispose();
                    pDrawing.Tag = null;
                }
            }

        }

        private void pDrawing_MouseMove(object sender, MouseEventArgs e)
        {
            if (!CanUI()) return;

            if (pDrawing.Tag != null && pDrawing.Tag is Image i)
            {
                //get the location
                int s = Math.Min((int)pDrawing.Width / i.Width, (int)pDrawing.Height / i.Height);
                int w = (pDrawing.Width - s * i.Width) / 2;
                int h = (pDrawing.Height - s * i.Height) / 2;
                Rectangle iBounds = new Rectangle(w, h, s * i.Width, s * i.Height);
                MouseCurrent = new Point((e.X - iBounds.X) / s, (e.Y - iBounds.Y) / s);



                pDrawing.Invalidate();
            }
        }

        private void pDrawing_MouseDown(object sender, MouseEventArgs e)
        {
            if (!CanUI()) return;

            if (pDrawing.Tag != null && pDrawing.Tag is Image i)
            {
                //get the location
                int s = Math.Min((int)pDrawing.Width / i.Width, (int)pDrawing.Height / i.Height);
                int w = (pDrawing.Width - s * i.Width) / 2;
                int h = (pDrawing.Height - s * i.Height) / 2;
                Rectangle iBounds = new Rectangle(w, h, s * i.Width, s * i.Height);
                MouseCurrent = new Point((e.X - iBounds.X) / s, (e.Y - iBounds.Y) / s);
                Dragging = true;
                ClickPlace = MouseCurrent;
                Selected = new Rectangle(-1, -1, 0, 0);
                pDrawing.Invalidate();
            }
        }

        private void pDrawing_MouseUp(object sender, MouseEventArgs e)
        {
            if (!CanUI()) return;

            if (pDrawing.Tag != null && pDrawing.Tag is Image i)
            {

                if (ModifierKeys.HasFlag(Keys.Control))
                {
                    newSpriteToolStripMenuItem_Click(this, new EventArgs());
                    textBox1.SelectAll();
                }


                //get the location
                int s = Math.Min((int)pDrawing.Width / i.Width, (int)pDrawing.Height / i.Height);
                int w = (pDrawing.Width - s * i.Width) / 2;
                int h = (pDrawing.Height - s * i.Height) / 2;
                Rectangle iBounds = new Rectangle(w, h, s * i.Width, s * i.Height);
                MouseCurrent = new Point((e.X - iBounds.X) / ( s), (e.Y - iBounds.Y) / ( s));
                var iR = new Rectangle(0, 0, i.Width , i.Height);
                if (iR.Contains(MouseCurrent) && (!Dragging || iR.Contains(ClickPlace)))
                    Selected = new Rectangle(
                        Math.Min(GRID * (ClickPlace.X / GRID), GRID * (MouseCurrent.X / GRID)),
                        Math.Min(GRID * (ClickPlace.Y / GRID), GRID * (MouseCurrent.Y / GRID)),
                        GRID + (Math.Abs(ClickPlace.X - MouseCurrent.X) / GRID) * GRID,
                        GRID + (Math.Abs(ClickPlace.Y - MouseCurrent.Y) / GRID) * GRID);
                else Selected = new Rectangle(-1, -1, 0, 0);

                if (SelectedSprite != null && SelectedNode != null)
                {
                    SelectedSprite.bounds = new Rectangle(Selected.X, Selected.Y, Selected.Width, Selected.Height);
                }

                if (iR.Contains(Selected))
                {
                    lbSpriteDimensions.Text = Selected.ToString();
                }

                Dragging = false;
                pDrawing.Invalidate();
                pPreview.Invalidate();
            }
        }

        private void pPreview_Paint(object sender, PaintEventArgs e)
        {

            e.Graphics.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.NearestNeighbor;
            e.Graphics.PixelOffsetMode = System.Drawing.Drawing2D.PixelOffsetMode.Half;

            if (pDrawing.Tag != null && pDrawing.Tag is Image i)
            {
                //get the location for the preview image
                float s = Math.Min((int)pDrawing.Width / i.Width, (int)pDrawing.Height / i.Height);
                int w = (int)((pDrawing.Width - s * i.Width) / 2);
                int h = (int)((pDrawing.Height - s * i.Height) / 2);
                //and the bounding rectangles
                Rectangle iBounds = new Rectangle(w, h, (int)(s * i.Width), (int)(s * i.Height));
                var iR = new Rectangle(0, 0, i.Width, i.Height);
                //Now make sure that the bounds actually contains the selection
                if (iR.Contains(Selected) && Selected.Width > 0 && Selected.Height > 0)
                {
                    var sR_ = new Rectangle(Selected.X, Selected.Y, Selected.Width, Selected.Height);
                    var sR = new Rectangle(sR_.X, sR_.Y , sR_.Width , sR_.Height );

                    //get the location
                    s = Math.Min((float)pPreview.Width / sR.Width, (float)pPreview.Height / sR.Height);
                    w = (int)((pPreview.Width - s * sR.Width) / 2);
                    h = (int)((pPreview.Height - s * sR.Height) / 2);

                    var ssr = new Rectangle(w, h, (int)(sR.Width * s), (int)(sR.Height * s));

                    e.Graphics.DrawImage(i,
                        ssr,
                        sR,
                        GraphicsUnit.Pixel);
                }


            }
        }


        private void textBox1_TextChanged(object sender, EventArgs e)
        {
            //we changed the text, so update the thing properly
            if (SelectedSprite != null && SelectedNode != null)
            {
                SelectedSprite.name = textBox1.Text;
                SelectedNode.Text = ">> " + SelectedSprite.name + "*";
            }
        }

        private void Editor_KeyDown(object sender, KeyEventArgs e)
        {
            if (!CanUI()) return;
            return;

            if (e.KeyCode == Keys.Enter)
            {
                if (SelectedSprite != null && SelectedNode != null)
                {
                    SelectedSprite.name = textBox1.Text;
                    SelectedSprite.rotates = cbRotates.Checked;
                    SelectedSprite.bounds = Selected;
                    //trick the listbox into redrawing
                    SelectedNode.Text = ">> " + SelectedSprite.name + "*";
                }

                pPreview.Invalidate();
                pDrawing.Invalidate();
            }

        }


        private void exportToolStripMenuItem_Click(object sender, EventArgs e)
        {
            using (SaveFileDialog sfd = new SaveFileDialog() { Filter = "Sprite Definitions | *.spr" })
            {
                if (sfd.ShowDialog() == DialogResult.OK)
                {
                    string dir = Path.GetDirectoryName(sfd.FileName) + "\\" + Path.GetFileNameWithoutExtension(sfd.FileName);
                    Directory.CreateDirectory(dir);
                    Dictionary<string, List<Sprite>> sprites = new Dictionary<string, List<Sprite>>();

                    //copy the sprite sheet parts into the thing here
                    Dictionary<string, string> sheet_copies = new Dictionary<string, string>();

                    //clean it out for us to be put there
                    foreach(string d in Directory.EnumerateDirectories(dir))
                        Directory.Delete(d);
                    
                    foreach (var n in treeView1.GetAllNodes())
                    {
                        if (n.Tag is Sprite s)
                        {
                            List<Sprite> spriteGroup;
                            if (sprites.ContainsKey(s.sheet)) spriteGroup = sprites[s.sheet];
                            else sprites.Add(s.sheet, spriteGroup = new List<Sprite>());
                            spriteGroup.Add(s);
                            var nn = n;
                            s.stack = "";
                            while (nn.Parent != null) { nn = nn.Parent; s.stack = nn.Text + "\\" + s.stack; }
                            if (s.stack.EndsWith('\\')) s.stack = s.stack.Remove(s.stack.Length - 1);
                            if (!sheet_copies.ContainsKey(s.sheet))
                            {
                                sheet_copies.Add(s.sheet, (s.sheet.Contains("\\") ? Path.GetFileName(s.sheet) : s.sheet));
                                if (!File.Exists(dir + "\\" + (s.sheet.Contains("\\") ? Path.GetFileName(s.sheet) : s.sheet)))
                                    File.Copy((workingDir != null ? workingDir + "\\" : "") + s.sheet, dir + "\\" + (s.sheet.Contains("\\") ? Path.GetFileName(s.sheet) : s.sheet));
                            }
                            s.sheet = Path.GetFileName("\\" + (s.sheet.Contains("\\") ? Path.GetFileName(s.sheet) : s.sheet));
                        }
                    }

                    //delete everything that isn't one of the sheets?


                    StringBuilder sb = new StringBuilder(100 * sprites.Count);
                    foreach (var k in sprites)
                    {
                        if (File.Exists((workingDir != null ? workingDir + "\\" : "") + k.Key))
                        {
                            using (Image i = Image.FromFile((workingDir != null ? workingDir + "\\" : "") + k.Key))
                            {
                                foreach (Sprite s in k.Value)
                                {
                                    Rectangle r_out = new Rectangle(0, 0, s.bounds.Width, s.bounds.Height);
                                    Rectangle r_in = new Rectangle(s.bounds.X, s.bounds.Y, s.bounds.Width, s.bounds.Height);
                                    using (Bitmap result = new Bitmap(r_out.Width, r_out.Height))
                                    {
                                        using (Graphics g = Graphics.FromImage(result))
                                        {
                                            g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.NearestNeighbor;
                                            g.DrawImage(i, r_out, r_in, GraphicsUnit.Pixel);
                                        }

                                        Directory.CreateDirectory(dir + "\\" + s.stack);
                                        result.Save(dir + "\\" + s.stack + "\\" + s.name + ".png");
                                        if (s.rotates)
                                        {
                                            using (Graphics g = Graphics.FromImage(result))
                                            {
                                                g.Clear(Color.FromArgb(0, 0, 0, 0));
                                                g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.NearestNeighbor;
                                                g.RotateTransform(90f);
                                                g.TranslateTransform(0, -1 * r_out.Width);
                                                g.DrawImage(i, r_out, r_in, GraphicsUnit.Pixel);
                                                //the 90 degree rotated can be mirrored later with maybe less rounding nuisance
                                                result.Save(dir + "\\" + s.stack + "\\" + s.name + "_rotated.png");
                                            }
                                        }
                                    }

                                    sb.Append("{name:" + s.name +"; stack:" + s.stack + "; sheet:" + s.sheet + "; bounds:" + r_in.X + ", " + r_in.Y + ", " + r_in.Width + ", " + r_in.Height + ";}\n");
                                    s.stack = "";
                                }

                            }
                        }
                    }


                    File.WriteAllText(dir + "\\index.spr", sb.ToString());
                    ResetUi();
                    var str = File.ReadAllText(dir + "\\index.spr");
                    workingDir = dir;

                    Import(str);
                }


            }
        }

        private void importToolStripMenuItem_Click(object sender, EventArgs e)
        {
            using (OpenFileDialog ofd = new OpenFileDialog() { Filter = "Sprite Definitions | *.spr" })
            {
                if (ofd.ShowDialog() == DialogResult.OK)
                {
                    var s = File.ReadAllText(ofd.FileName);
                    ResetUi();                  
                    workingDir = Path.GetDirectoryName(ofd.FileName);
                    Import(s);
                }
            }
        }


        static int LastIndex(char c, Span<char> s, int i = int.MaxValue)
        {
            for(int j = int.Min(i, s.Length-1); j >= 0; --j)
            {
                if (s[j] == c) return j;
            }
            return -1;
        }



        /// <summary>
        /// Parses a string thingimy into a list of data things, keyed in the same way as constructed in exportToolStripblahblah click.
        /// </summary>
        /// <param name="s"></param>
        /// <returns></returns>
        public Dictionary<string, string> Parse(Span<char> s)
        {
            Dictionary<string, string> output = new();
            int i = s.LastIndexOf('{');
            if (i > 0)
            {
                s = s.Slice(i, s.Length - i);

                int j = s.IndexOf('}');
                if (j >= 0)
                {
                    s = s.Slice(0, j);
                }
            }

            //now iterate the semi-colons and colons
            s[0] = ' ';
            int offset = 1;
            int pos = s.IndexOf(';');
            while(pos > 0)
            {
                int ppos = s.IndexOf(":");
                if (ppos > 0)
                {
                    string key = s.Slice(offset, ppos-offset).ToString().Trim();
                    string val = s.Slice(ppos + 1, pos - ppos - 1).ToString().Trim();
                    if (!output.TryAdd(key, val)) output[key] = val;
                }


                for(int j = offset; j <= pos; j++)
                {
                    s[j] = ' ';
                }

                offset = pos;
                pos = s.IndexOf(';');
            }

            s.Fill(' ');
            if (output.Count == 0) return null;
            return output;

        }

        /// <summary>
        /// Unspools a stringification of a rectangle
        /// </summary>
        /// <param name="s"></param>
        /// <returns></returns>
        public static Rectangle Unspool(string s)
        {
            var ss = s.Split(',').Select(x => x.Trim()).Where(x => x.Length > 0).ToArray();
            if (ss.Length != 4) return new Rectangle(0, 0, 0, 0);
            try
            {
                return new Rectangle(int.Parse(ss[0]), int.Parse(ss[1]), int.Parse(ss[2]), int.Parse(ss[3]));
            }
            catch
            {
                return new Rectangle(0, 0, 0, 0);
            }
        }

        /// <summary>
        /// Imports the data from the given String
        /// </summary>
        /// <param name="s"></param>
        public void Import(string s)
        {

            var sp = new Span<char>(s.ToCharArray());
            Dictionary<string, string> d = null;
            int counter = 0;

            while((d = Parse(sp)) != null)
            {
                ++counter;
                if (!d.ContainsKey("name")) d.Add("name", "Sprite" + counter);
                if (!d.ContainsKey("stack")) d.Add("stack", "Root");
                if (!d.ContainsKey("bounds")) d.Add("bounds", "0,0,0,0");
                if (!d.ContainsKey("sheet")) continue;

                Sprite spr = new Sprite() { name = d["name"], sheet = d["sheet"], stack = d["stack"], bounds = Unspool(d["bounds"]) };
            
                if (!SheetPaths.Contains(spr.sheet)) SheetPaths.Add(spr.sheet);

                TreeNode n = null;
                foreach (TreeNode nn in treeView1.Nodes)
                {
                    if (nn.Text.ToLower().Equals("root")) n = nn;
                    break;
                }
                if (n == null)
                {
                    n = new TreeNode("Root");
                    treeView1.Nodes.Add(n);
                }

                string[] stackparts = spr.stack.Split('\\').Where(x => x != null && x.Length > 0).ToArray();
                bool FoundOwner = false;

                //walk up the nodes
                string test_path = "";
                test_path += n;
                for (int ind = 1; ind < stackparts.Length; ind++)
                {
                    bool found = false;
                    foreach (TreeNode nn in n.Nodes)
                    {
                        if (nn.Text.ToLower().Equals(stackparts[ind].ToLower().Trim()))
                        {
                            test_path += "\\" + nn.Text;
                            n = nn;
                            found = true;
                            break;
                        }
                    }

                    if (!found)
                    {
                        TreeNode _n = new TreeNode(stackparts[ind]);
                        n.Nodes.Add(_n);
                        n = _n;
                    }

                }

                if (n != null) n.Nodes.Add(new TreeNode(spr.name) { Tag = spr });

            }


            lbSheets.Items.Clear();
            cbSheetSelector.Items.Clear();
            foreach (string path in SheetPaths)
            {
                lbSheets.Items.Add(path);
                cbSheetSelector.Items.Add(path.Contains("\\") ? Path.GetFileNameWithoutExtension(path) : path);
            }

        }

        private void treeView1_NodeMouseDoubleClick(object sender, TreeNodeMouseClickEventArgs e)
        {
            if (!CanUI()) return;

            if (e.Node.Tag is Sprite s)
            {
                try
                {
                    int n = SheetPaths.IndexOf(s.sheet);
                    if (n >= 0)
                    {
                        cbSheetSelector.SelectedIndex = n;
                        //now we will do the thing because stuff
                        Selected = s.bounds;
                        cbRotates.Checked = s.rotates;
                        lbSpriteDimensions.Text = Selected.X + ", " + Selected.Y + "\n" + Selected.Width + ", " + Selected.Height;
                        if (SelectedNode != null) SelectedNode.Text = SelectedNode.Text.Replace(">> ", "");
                        SelectedNode = e.Node;
                        SelectedNode.Text = ">> " + SelectedNode.Text.Replace(">> ", "");
                        SelectedSprite = s;
                        textBox1.Text = s.name;

                    }
                }
                catch
                {
                    if (SheetPaths.Count > 0)
                    {
                        s.sheet = SheetPaths.First();
                    }
                }
            }
            else
            {
                if (e.Node.IsExpanded)
                {
                    e.Node.Collapse(false);
                }
                else
                {
                    e.Node.Expand();
                }
            }



            pPreview.Invalidate();
            pDrawing.Invalidate();
        }

        private void treeView1_NodeMouseClick(object sender, TreeNodeMouseClickEventArgs e)
        {
            if (!CanUI()) return;

            if (e.Button == MouseButtons.Right)
            {
                var n = treeView1.GetNodeAt(e.Location);
                if (n != null) treeView1.SelectedNode = n;
                insertNodeToolStripMenuItem.Enabled = true;
                deleteNodeToolStripMenuItem.Enabled = true;
                renameNodeToolStripMenuItem.Enabled = (n?.Tag ?? null) == null;
                contextMenuStrip1.Show(Cursor.Position);
            }
        }

        private void treeView1_MouseClick(object sender, MouseEventArgs e)
        {
            if (!CanUI()) return;

            if (e.Button == MouseButtons.Right)
            {
                insertNodeToolStripMenuItem.Enabled = true;
                deleteNodeToolStripMenuItem.Enabled = false;
                renameNodeToolStripMenuItem.Enabled = false;
                contextMenuStrip1.Show(Cursor.Position);
            }
        }



        private void treeView1_ItemDrag(object sender, ItemDragEventArgs e)
        {
            // Move the dragged node when the left mouse button is used.
            if (e.Button == MouseButtons.Left)
            {
                DoDragDrop(e.Item, DragDropEffects.Move);
            }
        }

        // Set the target drop effect to the effect 
        // specified in the ItemDrag event handler.
        private void treeView1_DragEnter(object sender, DragEventArgs e)
        {
            e.Effect = e.AllowedEffect;
        }

        // Select the node under the mouse pointer to indicate the 
        // expected drop location.
        private void treeView1_DragOver(object sender, DragEventArgs e)
        {
            // Retrieve the client coordinates of the mouse position.
            Point targetPoint = treeView1.PointToClient(new Point(e.X, e.Y));

            // Select the node at the mouse position.
            treeView1.SelectedNode = treeView1.GetNodeAt(targetPoint);
        }

        private void treeView1_DragDrop(object sender, DragEventArgs e)
        {
            // Retrieve the client coordinates of the drop location.
            Point targetPoint = treeView1.PointToClient(new Point(e.X, e.Y));

            // Retrieve the node at the drop location.
            TreeNode targetNode = treeView1.GetNodeAt(targetPoint);

            // Retrieve the node that was dragged.
            TreeNode draggedNode = (TreeNode)e.Data.GetData(typeof(TreeNode));

            // Confirm that the node at the drop location is not 
            // the dragged node or a descendant of the dragged node.
            if (!draggedNode.Equals(targetNode) && !ContainsNode(draggedNode, targetNode))
            {
                // If it is a move operation, remove the node from its current 
                // location and add it to the node at the drop location.
                if (e.Effect == DragDropEffects.Move)
                {
                    if (targetNode.Tag == null)
                    {
                        draggedNode.Remove();
                        targetNode.Nodes.Add(draggedNode);
                    }
                    else if (targetNode.Parent != null)
                    {
                        draggedNode.Remove();
                        targetNode.Parent.Nodes.Add(draggedNode);
                    }
                    else
                    {
                        draggedNode.Remove();
                        treeView1.Nodes.Add(draggedNode);
                    }
                }
                // Expand the node at the location 
                // to show the dropped node.
                targetNode.Expand();
            }
        }

        // Determine whether one node is a parent 
        // or ancestor of a second node.
        private bool ContainsNode(TreeNode node1, TreeNode node2)
        {
            // Check the parent node of the second node.
            if (node2.Parent == null) return false;
            if (node2.Parent.Equals(node1)) return true;

            // If the parent node is not null or equal to the first node, 
            // call the ContainsNode method recursively using the parent of 
            // the second node.
            return ContainsNode(node1, node2.Parent);
        }

        private void insertNodeToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (!CanUI()) return;

            if (treeView1.SelectedNode != null)
            {
                treeView1.SelectedNode.Nodes.Add(new TreeNode("New Category"));
            }
            else
            {
                treeView1.Nodes.Add(new TreeNode("New Category"));
            }
        }

        private void newSpriteToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (!CanUI()) return;
            var Spr = new Sprite()
            {
                name = "New Sprite",
                bounds = Selected,
                rotates = cbRotates.Checked,
                sheet = SheetPaths[cbSheetSelector.SelectedIndex]
            };
            var node = new TreeNode("New Sprite") { Tag = Spr };

            if (treeView1.SelectedNode != null && treeView1.SelectedNode.Tag == null)
            {
                treeView1.SelectedNode.Nodes.Add(node);
                treeView1.SelectedNode = node;
            }
            else
            {
                treeView1.Nodes.Add(node);
                treeView1.SelectedNode = node;
            }

            SelectedSprite = Spr;
            if (SelectedNode != null) SelectedNode.Text = SelectedNode.Text.Replace(">> ", "");
            SelectedNode = node;
            SelectedNode.Text = ">> " + SelectedNode.Text.Replace(">> ", "");
            textBox1.Text = Spr.name;
            Selected = Spr.bounds;
            cbSheetSelector.SelectedIndex = SheetPaths.IndexOf(Spr.sheet);
            cbRotates.Checked = Spr.rotates;
            pPreview.Invalidate();
            pDrawing.Invalidate();
        }


        public bool CanUI()
        {
            if (SheetPaths.Count <= 0) return false;
            if (cbSheetSelector.SelectedIndex < 0) cbSheetSelector.SelectedIndex = 0;
            return true;
        }

        private void renameNodeToolStripMenuItem_Click(object sender, EventArgs e)
        {
            treeView1.LabelEdit = true;
            if (treeView1.SelectedNode != null) treeView1.SelectedNode.BeginEdit();
        }

        private void treeView1_AfterLabelEdit(object sender, NodeLabelEditEventArgs e)
        {
            if (e.Node != null)
            {
                if (e.Label == null || e.Label.Length <= 0) e.CancelEdit = true;
                foreach (var n in treeView1.GetAllNodes())
                {
                    if (n != e.Node && n.Tag == null && n.Text.ToLower().Equals(e.Label)) e.CancelEdit = true;
                }
                if (!e.CancelEdit) e.Node.Text = e.Label;
                e.CancelEdit = true;
            }
        }

        private void newToolStripMenuItem_Click(object sender, EventArgs e)
        {
            ResetUi();
        }
        private void Editor_Load(object sender, EventArgs e)
        {
            ResetUi();

        }

        public void ResetUi()
        {
            workingDir = null;
            SheetPaths.Clear();
            treeView1.Nodes.Clear();
            treeView1.Nodes.Add("Root");
            textBox1.Text = "";
            Selected = new Rectangle(-1, -1, 0, 0);
            Dragging = false;
            lbSheets.Items.Clear();
            cbSheetSelector.Items.Clear();

        }

        private void deleteNodeToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (treeView1.SelectedNode != null)
            {
                treeView1.SelectedNode.Remove();
            }
        }
    }
}
