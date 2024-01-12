using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Xml.Linq;

namespace SpriteSheetHelper
{
    public partial class Editor : Form
    {

        List<string> SheetPaths = new List<string>();

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

        public Editor()
        {
            InitializeComponent();
            typeof(Panel).InvokeMember("DoubleBuffered", BindingFlags.SetProperty | BindingFlags.Instance | BindingFlags.NonPublic, null, pDrawing, new object[] { true });
            textBox1.KeyDown += Editor_KeyDown;
            // Add event handlers for the required drag events.
            treeView1.ItemDrag += new ItemDragEventHandler(treeView1_ItemDrag);
            treeView1.DragEnter += new DragEventHandler(treeView1_DragEnter);
            treeView1.DragOver += new DragEventHandler(treeView1_DragOver);
            treeView1.DragDrop += new DragEventHandler(treeView1_DragDrop);

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
            e.Graphics.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.NearestNeighbor;
            e.Graphics.PixelOffsetMode = System.Drawing.Drawing2D.PixelOffsetMode.Half;

            int n = cbSheetSelector.SelectedIndex;
            if (n >= 0 && n < SheetPaths.Count)
            {
                if (pDrawing.Tag == null)
                {
                    if (File.Exists(SheetPaths[n]))
                        pDrawing.Tag = Image.FromFile(SheetPaths[n]);
                }

                if (pDrawing.Tag != null && pDrawing.Tag is Image i)
                {


                    e.Graphics.Clear(pDrawing.BackColor);
                    //integral scale
                    int s = Math.Min((int)pDrawing.Width / i.Width, (int)pDrawing.Height / i.Height);
                    int w = (pDrawing.Width - s * i.Width) / 2;
                    int h = (pDrawing.Height - s * i.Height) / 2;
                    Rectangle iBounds = new Rectangle(w, h, s * i.Width, s * i.Height);

                    e.Graphics.DrawImage(i, w, h, s * i.Width, s * i.Height);

                    //now set the transform to the scale yay
                    e.Graphics.Transform = new System.Drawing.Drawing2D.Matrix(s * 16, 0, 0, s * 16, w, h);


                    var iR = new Rectangle(0, 0, i.Width / 16, i.Height / 16);
                    if (iR.Contains(MouseCurrent) && (!Dragging || iR.Contains(ClickPlace)))
                    {
                        if (Dragging)
                        {
                            var p = MouseCurrent;
                            e.Graphics.DrawRectangle(new Pen(Color.FromArgb(128, 255, 0, 0), 2 / (16f * s)),
                                Math.Min(ClickPlace.X, MouseCurrent.X),
                                Math.Min(ClickPlace.Y, MouseCurrent.Y),
                                1 + Math.Abs(ClickPlace.X - MouseCurrent.X),
                                1 + Math.Abs(ClickPlace.Y - MouseCurrent.Y));
                        }
                        else e.Graphics.DrawRectangle(new Pen(Color.FromArgb(128, 255, 0, 0), 1 / (16f * s)), MouseCurrent.X, MouseCurrent.Y, 1, 1); ;
                    }

                    if (iR.Contains(Selected))
                    {
                        e.Graphics.DrawRectangle(new Pen(Color.FromArgb(128, 50, 80, 255), 3 / (16f * s)), Selected.X, Selected.Y, Selected.Width, Selected.Height);
                    }


                }
            }
            else
            {
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
                MouseCurrent = new Point((e.X - iBounds.X) / (16 * s), (e.Y - iBounds.Y) / (16 * s));



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
                MouseCurrent = new Point((e.X - iBounds.X) / (16 * s), (e.Y - iBounds.Y) / (16 * s));
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
                //get the location
                int s = Math.Min((int)pDrawing.Width / i.Width, (int)pDrawing.Height / i.Height);
                int w = (pDrawing.Width - s * i.Width) / 2;
                int h = (pDrawing.Height - s * i.Height) / 2;
                Rectangle iBounds = new Rectangle(w, h, s * i.Width, s * i.Height);
                MouseCurrent = new Point((e.X - iBounds.X) / (16 * s), (e.Y - iBounds.Y) / (16 * s));
                var iR = new Rectangle(0, 0, i.Width / 16, i.Height / 16);
                if (iR.Contains(MouseCurrent) && (!Dragging || iR.Contains(ClickPlace)))
                    Selected = new Rectangle(
                        Math.Min(ClickPlace.X, MouseCurrent.X),
                        Math.Min(ClickPlace.Y, MouseCurrent.Y),
                        1 + Math.Abs(ClickPlace.X - MouseCurrent.X),
                        1 + Math.Abs(ClickPlace.Y - MouseCurrent.Y));
                else Selected = new Rectangle(-1, -1, 0, 0);

                if (SelectedSprite != null && SelectedNode != null)
                {
                    SelectedSprite.bounds = Selected;
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
                //get the location
                int s = Math.Min((int)pDrawing.Width / i.Width, (int)pDrawing.Height / i.Height);
                int w = (pDrawing.Width - s * i.Width) / 2;
                int h = (pDrawing.Height - s * i.Height) / 2;
                Rectangle iBounds = new Rectangle(w, h, s * i.Width, s * i.Height);
                var iR = new Rectangle(0, 0, i.Width / 16, i.Height / 16);
                if (iR.Contains(Selected) && Selected.Width > 0 && Selected.Height > 0)
                {
                    var sR_ = new Rectangle(Selected.X, Selected.Y, Selected.Width, Selected.Height);
                    var sR = new Rectangle(sR_.X * 16, sR_.Y * 16, sR_.Width * 16, sR_.Height * 16);

                    //get the location
                    s = Math.Min((int)pPreview.Width / sR.Width, (int)pPreview.Height / sR.Height);
                    w = (pPreview.Width - s * sR.Width) / 2;
                    h = (pPreview.Height - s * sR.Height) / 2;

                    var ssr = new Rectangle(w, h, sR.Width * s, sR.Height * s);

                    e.Graphics.DrawImage(i,
                        ssr,
                        sR,
                        GraphicsUnit.Pixel);
                }


            }
        }

       
        private void textBox1_TextChanged(object sender, EventArgs e)
        {
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

                    foreach(var n in treeView1.GetAllNodes())
                    {
                        if (n.Tag is Sprite s)
                        {
                            List<Sprite> spriteGroup;
                            if (sprites.ContainsKey(s.sheet)) spriteGroup = sprites[s.sheet];
                            else sprites.Add(s.sheet, spriteGroup = new List<Sprite>());
                            spriteGroup.Add(s);
                            var nn = n;
                            while (nn.Parent != null) { nn = nn.Parent; s.stack = nn.Text + "\\" + s.stack;  }
                            if (s.stack.EndsWith('\\')) s.stack = s.stack.Remove(s.stack.Length - 1);
                        }
                    }

                    StringBuilder sb = new StringBuilder(100 * sprites.Count);
                    foreach (var k in sprites)
                    {
                        if (File.Exists(k.Key))
                        {
                            using (Image i = Image.FromFile(k.Key))
                            {
                                foreach (Sprite s in k.Value)
                                {
                                    Rectangle r_out = new Rectangle(0, 0, s.bounds.Width * 16, s.bounds.Height * 16);
                                    Rectangle r_in = new Rectangle(s.bounds.X * 16, s.bounds.Y * 16, s.bounds.Width * 16, s.bounds.Height * 16);
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
                                    sb.Append(s.stack + "#" + s.name + "#" + s.sheet + "#" + (r_in.X + (r_in.Y << 8) + (r_in.Width << 16) + (r_in.Height << 24)) + "#\n");
                                    s.stack = "";
                                }

                            }
                        }
                    }
                    File.WriteAllText(dir + "\\index.spr", sb.ToString());
                }




            }
        }

        private void importToolStripMenuItem_Click(object sender, EventArgs e)
        {
            using (OpenFileDialog ofd = new OpenFileDialog() { Filter = "Sprite Definitions | *.spr" })
            {
                if (ofd.ShowDialog() == DialogResult.OK)
                {
                    var s = File.ReadAllText(ofd.FileName).Split("\n").Select(x => x.Trim());
                    ResetUi();

                    foreach (string ss in s)
                    {

                        string[] sss = ss.Split("#").Where(x => x.Trim().Length > 1).ToArray();
                        RectangleConverter r = new RectangleConverter();

                        int x = 0;
                        if (int.TryParse(sss[3], out x))
                        {
                            Sprite spr = new Sprite() { stack = sss[0], name = sss[1], sheet = sss[2], bounds = new Rectangle(x & 0xFF, (x >> 8) & 0xFF, (x >> 16) & 0xFF, (x >> 24) & 0xFF) };
                            if (!SheetPaths.Contains(spr.sheet)) SheetPaths.Add(spr.sheet);

                            TreeNode n = null;
                            foreach(TreeNode nn in treeView1.Nodes) { n = nn; break; }
                            
                            string[] stackparts = spr.stack.Split('\\');
                            bool FoundOwner = false;
                            for(int i = 1; i < stackparts.Length; i++)
                            {
                                if (stackparts[i] != null && stackparts[i].Length > 0)
                                {
                                    bool has = false;
                                    //see if we can find the node
                                    foreach(TreeNode nn in n.Nodes)
                                    {
                                        if (nn.Text == stackparts[i])
                                        {
                                            n = nn;
                                            has = true;
                                            break;
                                        }
                                    }

                                    if (!has)
                                    {
                                        TreeNode nn = new TreeNode(stackparts[i]);
                                        n.Nodes.Add(nn);
                                        n = nn;
                                    }
                                }
                            }

                            if (n != null) n.Nodes.Add(new TreeNode(spr.name) { Tag = spr });

                        }


                    }

                    lbSheets.Items.Clear();
                    cbSheetSelector.Items.Clear();
                    foreach (string path in SheetPaths)
                    {
                        lbSheets.Items.Add(s);
                        cbSheetSelector.Items.Add(Path.GetFileNameWithoutExtension(path));
                    }

                }
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
                        lbSpriteDimensions.Text = Selected.ToString();
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
                foreach(var n in treeView1.GetAllNodes())
                {
                    if (n.Tag == null && n.Text.ToLower().Equals(e.Label)) e.CancelEdit = true;
                }
                if (e.CancelEdit) e.Node.Text = e.Label;
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
            SheetPaths.Clear();
            treeView1.Nodes.Clear();
            treeView1.Nodes.Add("Root");
            textBox1.Text = "";
            Selected = new Rectangle(-1, -1, 0, 0);
            Dragging = false;
            lbSheets.Items.Clear();
            cbSheetSelector.Items.Clear();

        }


    }
}
