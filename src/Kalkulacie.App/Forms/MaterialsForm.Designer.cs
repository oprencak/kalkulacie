namespace Kalkulacie.App.Forms;

partial class MaterialsForm
{
    private System.ComponentModel.IContainer components = null;

    protected override void Dispose(bool disposing)
    {
        if (disposing && (components != null))
            components.Dispose();
        base.Dispose(disposing);
    }

    private void InitializeComponent()
    {
        this.grid = new DataGridView();
        this.btnAdd = new Button();
        this.btnDelete = new Button();
        this.btnSave = new Button();
        this.btnUsage = new Button();
        this.lblSearch = new Label();
        this.txtSearch = new TextBox();
        ((System.ComponentModel.ISupportInitialize)(this.grid)).BeginInit();
        this.SuspendLayout();

        // lblSearch
        this.lblSearch.Location = new Point(12, 12);
        this.lblSearch.Size = new Size(55, 23);
        this.lblSearch.Text = "Hľadať:";
        this.lblSearch.TextAlign = ContentAlignment.MiddleLeft;

        // txtSearch
        this.txtSearch.Location = new Point(70, 12);
        this.txtSearch.Size = new Size(250, 23);
        this.txtSearch.TextChanged += new EventHandler(this.txtSearch_TextChanged);

        // grid
        this.grid.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
        this.grid.Location = new Point(12, 44);
        this.grid.Size = new Size(660, 348);
        this.grid.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
        this.grid.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
        this.grid.AllowUserToAddRows = false;

        // btnAdd
        this.btnAdd.Anchor = AnchorStyles.Bottom | AnchorStyles.Left;
        this.btnAdd.Location = new Point(12, 405);
        this.btnAdd.Size = new Size(100, 30);
        this.btnAdd.Text = "Pridať";
        this.btnAdd.Click += new EventHandler(this.btnAdd_Click);

        // btnDelete
        this.btnDelete.Anchor = AnchorStyles.Bottom | AnchorStyles.Left;
        this.btnDelete.Location = new Point(120, 405);
        this.btnDelete.Size = new Size(100, 30);
        this.btnDelete.Text = "Vymazať";
        this.btnDelete.Click += new EventHandler(this.btnDelete_Click);

        // btnUsage
        this.btnUsage.Anchor = AnchorStyles.Bottom | AnchorStyles.Left;
        this.btnUsage.Location = new Point(228, 405);
        this.btnUsage.Size = new Size(130, 30);
        this.btnUsage.Text = "Kde sa používa";
        this.btnUsage.Click += new EventHandler(this.btnUsage_Click);

        // btnSave
        this.btnSave.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
        this.btnSave.Location = new Point(572, 405);
        this.btnSave.Size = new Size(100, 30);
        this.btnSave.Text = "Uložiť";
        this.btnSave.Click += new EventHandler(this.btnSave_Click);

        // MaterialsForm
        this.AutoScaleDimensions = new SizeF(7F, 15F);
        this.AutoScaleMode = AutoScaleMode.Font;
        this.ClientSize = new Size(684, 448);
        this.Controls.Add(this.lblSearch);
        this.Controls.Add(this.txtSearch);
        this.Controls.Add(this.grid);
        this.Controls.Add(this.btnAdd);
        this.Controls.Add(this.btnDelete);
        this.Controls.Add(this.btnUsage);
        this.Controls.Add(this.btnSave);
        this.MinimumSize = new Size(500, 350);
        this.Name = "MaterialsForm";
        this.StartPosition = FormStartPosition.CenterParent;
        this.Text = "Materiály";
        ((System.ComponentModel.ISupportInitialize)(this.grid)).EndInit();
        this.ResumeLayout(false);
        this.PerformLayout();
    }

    private DataGridView grid;
    private Button btnAdd;
    private Button btnDelete;
    private Button btnSave;
    private Button btnUsage;
    private Label lblSearch;
    private TextBox txtSearch;
}
