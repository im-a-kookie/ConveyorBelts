namespace SpriteSheetHelper
{
    partial class Editor
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            components = new System.ComponentModel.Container();
            pDrawingContainer = new Panel();
            pDrawing = new Panel();
            cbSheetSelector = new ComboBox();
            tabControl1 = new TabControl();
            tabPage1 = new TabPage();
            treeView1 = new TreeView();
            pPreview = new Panel();
            cbRotates = new CheckBox();
            lbSpriteDimensions = new Label();
            textBox1 = new TextBox();
            tabPage2 = new TabPage();
            bRemoveSheet = new Button();
            bAddSheet = new Button();
            lbSheets = new ListBox();
            pMenuBar = new Panel();
            menuStrip1 = new MenuStrip();
            fileToolStripMenuItem = new ToolStripMenuItem();
            newToolStripMenuItem = new ToolStripMenuItem();
            exportToolStripMenuItem = new ToolStripMenuItem();
            importToolStripMenuItem = new ToolStripMenuItem();
            panel1 = new Panel();
            contextMenuStrip1 = new ContextMenuStrip(components);
            newSpriteToolStripMenuItem = new ToolStripMenuItem();
            insertNodeToolStripMenuItem = new ToolStripMenuItem();
            deleteNodeToolStripMenuItem = new ToolStripMenuItem();
            renameNodeToolStripMenuItem = new ToolStripMenuItem();
            pDrawingContainer.SuspendLayout();
            tabControl1.SuspendLayout();
            tabPage1.SuspendLayout();
            tabPage2.SuspendLayout();
            pMenuBar.SuspendLayout();
            menuStrip1.SuspendLayout();
            panel1.SuspendLayout();
            contextMenuStrip1.SuspendLayout();
            SuspendLayout();
            // 
            // pDrawingContainer
            // 
            pDrawingContainer.BackColor = Color.Silver;
            pDrawingContainer.Controls.Add(pDrawing);
            pDrawingContainer.Controls.Add(cbSheetSelector);
            pDrawingContainer.Dock = DockStyle.Fill;
            pDrawingContainer.Location = new Point(0, 28);
            pDrawingContainer.Name = "pDrawingContainer";
            pDrawingContainer.Padding = new Padding(3);
            pDrawingContainer.Size = new Size(647, 610);
            pDrawingContainer.TabIndex = 0;
            // 
            // pDrawing
            // 
            pDrawing.BackColor = SystemColors.Control;
            pDrawing.Dock = DockStyle.Fill;
            pDrawing.Location = new Point(3, 31);
            pDrawing.Name = "pDrawing";
            pDrawing.Padding = new Padding(3);
            pDrawing.Size = new Size(641, 576);
            pDrawing.TabIndex = 1;
            pDrawing.Paint += pDrawing_Paint;
            pDrawing.MouseDown += pDrawing_MouseDown;
            pDrawing.MouseMove += pDrawing_MouseMove;
            pDrawing.MouseUp += pDrawing_MouseUp;
            // 
            // cbSheetSelector
            // 
            cbSheetSelector.Dock = DockStyle.Top;
            cbSheetSelector.DropDownStyle = ComboBoxStyle.DropDownList;
            cbSheetSelector.FormattingEnabled = true;
            cbSheetSelector.Location = new Point(3, 3);
            cbSheetSelector.Name = "cbSheetSelector";
            cbSheetSelector.Size = new Size(641, 28);
            cbSheetSelector.TabIndex = 0;
            cbSheetSelector.SelectedIndexChanged += cbSheetSelector_SelectedIndexChanged;
            // 
            // tabControl1
            // 
            tabControl1.Controls.Add(tabPage1);
            tabControl1.Controls.Add(tabPage2);
            tabControl1.Dock = DockStyle.Fill;
            tabControl1.Location = new Point(3, 3);
            tabControl1.Name = "tabControl1";
            tabControl1.SelectedIndex = 0;
            tabControl1.Size = new Size(372, 604);
            tabControl1.TabIndex = 1;
            // 
            // tabPage1
            // 
            tabPage1.Controls.Add(treeView1);
            tabPage1.Controls.Add(pPreview);
            tabPage1.Controls.Add(cbRotates);
            tabPage1.Controls.Add(lbSpriteDimensions);
            tabPage1.Controls.Add(textBox1);
            tabPage1.Location = new Point(4, 29);
            tabPage1.Name = "tabPage1";
            tabPage1.Padding = new Padding(3);
            tabPage1.Size = new Size(364, 571);
            tabPage1.TabIndex = 0;
            tabPage1.Text = "Sprites";
            tabPage1.UseVisualStyleBackColor = true;
            // 
            // treeView1
            // 
            treeView1.AllowDrop = true;
            treeView1.Location = new Point(6, 0);
            treeView1.Name = "treeView1";
            treeView1.Size = new Size(218, 566);
            treeView1.TabIndex = 7;
            treeView1.AfterLabelEdit += treeView1_AfterLabelEdit;
            treeView1.NodeMouseClick += treeView1_NodeMouseClick;
            treeView1.NodeMouseDoubleClick += treeView1_NodeMouseDoubleClick;
            treeView1.MouseDown += treeView1_MouseClick;
            // 
            // pPreview
            // 
            pPreview.BorderStyle = BorderStyle.FixedSingle;
            pPreview.Location = new Point(230, 39);
            pPreview.Name = "pPreview";
            pPreview.Size = new Size(128, 128);
            pPreview.TabIndex = 6;
            pPreview.Paint += pPreview_Paint;
            // 
            // cbRotates
            // 
            cbRotates.AutoSize = true;
            cbRotates.Location = new Point(230, 173);
            cbRotates.Name = "cbRotates";
            cbRotates.Size = new Size(96, 24);
            cbRotates.TabIndex = 5;
            cbRotates.Text = "Rotatable";
            cbRotates.UseVisualStyleBackColor = true;
            // 
            // lbSpriteDimensions
            // 
            lbSpriteDimensions.AutoSize = true;
            lbSpriteDimensions.Location = new Point(230, 200);
            lbSpriteDimensions.Name = "lbSpriteDimensions";
            lbSpriteDimensions.Size = new Size(50, 20);
            lbSpriteDimensions.TabIndex = 4;
            lbSpriteDimensions.Text = "label1";
            // 
            // textBox1
            // 
            textBox1.Location = new Point(230, 6);
            textBox1.Name = "textBox1";
            textBox1.Size = new Size(128, 27);
            textBox1.TabIndex = 1;
            textBox1.TextChanged += textBox1_TextChanged;
            // 
            // tabPage2
            // 
            tabPage2.Controls.Add(bRemoveSheet);
            tabPage2.Controls.Add(bAddSheet);
            tabPage2.Controls.Add(lbSheets);
            tabPage2.Location = new Point(4, 29);
            tabPage2.Name = "tabPage2";
            tabPage2.Padding = new Padding(3);
            tabPage2.Size = new Size(364, 571);
            tabPage2.TabIndex = 1;
            tabPage2.Text = "Sheets";
            tabPage2.UseVisualStyleBackColor = true;
            // 
            // bRemoveSheet
            // 
            bRemoveSheet.Location = new Point(106, 256);
            bRemoveSheet.Name = "bRemoveSheet";
            bRemoveSheet.Size = new Size(94, 29);
            bRemoveSheet.TabIndex = 2;
            bRemoveSheet.Text = "Remove";
            bRemoveSheet.UseVisualStyleBackColor = true;
            bRemoveSheet.Click += bRemoveSheet_Click;
            // 
            // bAddSheet
            // 
            bAddSheet.Location = new Point(6, 256);
            bAddSheet.Name = "bAddSheet";
            bAddSheet.Size = new Size(94, 29);
            bAddSheet.TabIndex = 1;
            bAddSheet.Text = "Add";
            bAddSheet.UseVisualStyleBackColor = true;
            bAddSheet.Click += bAddSheet_Click;
            // 
            // lbSheets
            // 
            lbSheets.FormattingEnabled = true;
            lbSheets.ItemHeight = 20;
            lbSheets.Location = new Point(6, 6);
            lbSheets.Name = "lbSheets";
            lbSheets.Size = new Size(194, 244);
            lbSheets.TabIndex = 0;
            // 
            // pMenuBar
            // 
            pMenuBar.Controls.Add(menuStrip1);
            pMenuBar.Dock = DockStyle.Top;
            pMenuBar.Location = new Point(0, 0);
            pMenuBar.Name = "pMenuBar";
            pMenuBar.Size = new Size(1025, 28);
            pMenuBar.TabIndex = 3;
            // 
            // menuStrip1
            // 
            menuStrip1.ImageScalingSize = new Size(20, 20);
            menuStrip1.Items.AddRange(new ToolStripItem[] { fileToolStripMenuItem });
            menuStrip1.Location = new Point(0, 0);
            menuStrip1.Name = "menuStrip1";
            menuStrip1.Size = new Size(1025, 28);
            menuStrip1.TabIndex = 0;
            menuStrip1.Text = "menuStrip1";
            // 
            // fileToolStripMenuItem
            // 
            fileToolStripMenuItem.DropDownItems.AddRange(new ToolStripItem[] { newToolStripMenuItem, exportToolStripMenuItem, importToolStripMenuItem });
            fileToolStripMenuItem.Name = "fileToolStripMenuItem";
            fileToolStripMenuItem.Size = new Size(46, 24);
            fileToolStripMenuItem.Text = "File";
            // 
            // newToolStripMenuItem
            // 
            newToolStripMenuItem.Name = "newToolStripMenuItem";
            newToolStripMenuItem.Size = new Size(137, 26);
            newToolStripMenuItem.Text = "New";
            newToolStripMenuItem.Click += newToolStripMenuItem_Click;
            // 
            // exportToolStripMenuItem
            // 
            exportToolStripMenuItem.Name = "exportToolStripMenuItem";
            exportToolStripMenuItem.Size = new Size(137, 26);
            exportToolStripMenuItem.Text = "Export";
            exportToolStripMenuItem.Click += exportToolStripMenuItem_Click;
            // 
            // importToolStripMenuItem
            // 
            importToolStripMenuItem.Name = "importToolStripMenuItem";
            importToolStripMenuItem.Size = new Size(137, 26);
            importToolStripMenuItem.Text = "Import";
            importToolStripMenuItem.Click += importToolStripMenuItem_Click;
            // 
            // panel1
            // 
            panel1.BackColor = Color.Silver;
            panel1.Controls.Add(tabControl1);
            panel1.Dock = DockStyle.Right;
            panel1.Location = new Point(647, 28);
            panel1.Name = "panel1";
            panel1.Padding = new Padding(3);
            panel1.Size = new Size(378, 610);
            panel1.TabIndex = 3;
            // 
            // contextMenuStrip1
            // 
            contextMenuStrip1.ImageScalingSize = new Size(20, 20);
            contextMenuStrip1.Items.AddRange(new ToolStripItem[] { newSpriteToolStripMenuItem, insertNodeToolStripMenuItem, deleteNodeToolStripMenuItem, renameNodeToolStripMenuItem });
            contextMenuStrip1.Name = "contextMenuStrip1";
            contextMenuStrip1.Size = new Size(174, 100);
            // 
            // newSpriteToolStripMenuItem
            // 
            newSpriteToolStripMenuItem.Name = "newSpriteToolStripMenuItem";
            newSpriteToolStripMenuItem.Size = new Size(173, 24);
            newSpriteToolStripMenuItem.Text = "New Sprite";
            newSpriteToolStripMenuItem.Click += newSpriteToolStripMenuItem_Click;
            // 
            // insertNodeToolStripMenuItem
            // 
            insertNodeToolStripMenuItem.Name = "insertNodeToolStripMenuItem";
            insertNodeToolStripMenuItem.Size = new Size(173, 24);
            insertNodeToolStripMenuItem.Text = "Insert Node";
            insertNodeToolStripMenuItem.Click += insertNodeToolStripMenuItem_Click;
            // 
            // deleteNodeToolStripMenuItem
            // 
            deleteNodeToolStripMenuItem.Name = "deleteNodeToolStripMenuItem";
            deleteNodeToolStripMenuItem.Size = new Size(173, 24);
            deleteNodeToolStripMenuItem.Text = "Delete Node";
            // 
            // renameNodeToolStripMenuItem
            // 
            renameNodeToolStripMenuItem.Name = "renameNodeToolStripMenuItem";
            renameNodeToolStripMenuItem.Size = new Size(173, 24);
            renameNodeToolStripMenuItem.Text = "Rename Node";
            renameNodeToolStripMenuItem.Click += renameNodeToolStripMenuItem_Click;
            // 
            // Editor
            // 
            AutoScaleDimensions = new SizeF(8F, 20F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(1025, 638);
            Controls.Add(pDrawingContainer);
            Controls.Add(panel1);
            Controls.Add(pMenuBar);
            MainMenuStrip = menuStrip1;
            Name = "Editor";
            Text = "Editor";
            Load += Editor_Load;
            KeyDown += Editor_KeyDown;
            pDrawingContainer.ResumeLayout(false);
            tabControl1.ResumeLayout(false);
            tabPage1.ResumeLayout(false);
            tabPage1.PerformLayout();
            tabPage2.ResumeLayout(false);
            pMenuBar.ResumeLayout(false);
            pMenuBar.PerformLayout();
            menuStrip1.ResumeLayout(false);
            menuStrip1.PerformLayout();
            panel1.ResumeLayout(false);
            contextMenuStrip1.ResumeLayout(false);
            ResumeLayout(false);
        }

        #endregion

        private Panel pDrawingContainer;
        private ComboBox cbSheetSelector;
        private TabControl tabControl1;
        private TabPage tabPage1;
        private TabPage tabPage2;
        private Button bRemoveSheet;
        private Button bAddSheet;
        private ListBox lbSheets;
        private Panel pMenuBar;
        private Panel pDrawing;
        private TextBox textBox1;
        private Panel panel1;
        private CheckBox cbRotates;
        private Label lbSpriteDimensions;
        private Panel pPreview;
        private MenuStrip menuStrip1;
        private ToolStripMenuItem fileToolStripMenuItem;
        private ToolStripMenuItem exportToolStripMenuItem;
        private ToolStripMenuItem importToolStripMenuItem;
        private TreeView treeView1;
        private ContextMenuStrip contextMenuStrip1;
        private ToolStripMenuItem insertNodeToolStripMenuItem;
        private ToolStripMenuItem deleteNodeToolStripMenuItem;
        private ToolStripMenuItem renameNodeToolStripMenuItem;
        private ToolStripMenuItem newSpriteToolStripMenuItem;
        private ToolStripMenuItem newToolStripMenuItem;
    }
}