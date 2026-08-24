namespace Kalkulacie.App.Forms;

partial class ProductsForm
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
        this.btnDetail = new Button();
        this.btnDelete = new Button();
        this.btnClone = new Button();
        this.rdAll = new RadioButton();
        this.rdManual = new RadioButton();
        this.rdAutomatic = new RadioButton();
        ((System.ComponentModel.ISupportInitialize)(this.grid)).BeginInit();
        this.SuspendLayout();

        // rdAll
        this.rdAll.Location = new Point(12, 14);
        this.rdAll.Size = new Size(70, 23);
        this.rdAll.Text = "Všetky";
        this.rdAll.Checked = true;
        this.rdAll.CheckedChanged += new EventHandler(this.rdFilter_CheckedChanged);

        // rdManual
        this.rdManual.Location = new Point(88, 14);
        this.rdManual.Size = new Size(70, 23);
        this.rdManual.Text = "Ručné";
        this.rdManual.CheckedChanged += new EventHandler(this.rdFilter_CheckedChanged);

        // rdAutomatic
        this.rdAutomatic.Location = new Point(164, 14);
        this.rdAutomatic.Size = new Size(80, 23);
        this.rdAutomatic.Text = "Automat";
        this.rdAutomatic.CheckedChanged += new EventHandler(this.rdFilter_CheckedChanged);

        // grid
        this.grid.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
        this.grid.Location = new Point(12, 44);
        this.grid.Size = new Size(660, 368);
        this.grid.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
        this.grid.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
        this.grid.ReadOnly = true;
        this.grid.AllowUserToAddRows = false;
        this.grid.DoubleClick += new EventHandler(this.grid_DoubleClick);

        // btnAdd
        this.btnAdd.Anchor = AnchorStyles.Bottom | AnchorStyles.Left;
        this.btnAdd.Location = new Point(12, 425);
        this.btnAdd.Size = new Size(100, 30);
        this.btnAdd.Text = "Nový";
        this.btnAdd.Click += new EventHandler(this.btnAdd_Click);

        // btnDetail
        this.btnDetail.Anchor = AnchorStyles.Bottom | AnchorStyles.Left;
        this.btnDetail.Location = new Point(120, 425);
        this.btnDetail.Size = new Size(100, 30);
        this.btnDetail.Text = "Otvoriť";
        this.btnDetail.Click += new EventHandler(this.btnDetail_Click);

        // btnDelete
        this.btnDelete.Anchor = AnchorStyles.Bottom | AnchorStyles.Left;
        this.btnDelete.Location = new Point(228, 425);
        this.btnDelete.Size = new Size(100, 30);
        this.btnDelete.Text = "Vymazať";
        this.btnDelete.Click += new EventHandler(this.btnDelete_Click);

        // btnClone
        this.btnClone.Anchor = AnchorStyles.Bottom | AnchorStyles.Left;
        this.btnClone.Location = new Point(336, 425);
        this.btnClone.Size = new Size(100, 30);
        this.btnClone.Text = "Kopírovať";
        this.btnClone.Click += new EventHandler(this.btnClone_Click);

        // ProductsForm
        this.AutoScaleDimensions = new SizeF(7F, 15F);
        this.AutoScaleMode = AutoScaleMode.Font;
        this.ClientSize = new Size(684, 468);
        this.Controls.Add(this.rdAll);
        this.Controls.Add(this.rdManual);
        this.Controls.Add(this.rdAutomatic);
        this.Controls.Add(this.grid);
        this.Controls.Add(this.btnAdd);
        this.Controls.Add(this.btnDetail);
        this.Controls.Add(this.btnDelete);
        this.Controls.Add(this.btnClone);
        this.MinimumSize = new Size(500, 400);
        this.Name = "ProductsForm";
        this.StartPosition = FormStartPosition.CenterParent;
        this.Text = "Výrobky";
        ((System.ComponentModel.ISupportInitialize)(this.grid)).EndInit();
        this.ResumeLayout(false);
    }

    private DataGridView grid;
    private Button btnAdd;
    private Button btnDetail;
    private Button btnDelete;
    private Button btnClone;
    private RadioButton rdAll;
    private RadioButton rdManual;
    private RadioButton rdAutomatic;
}
