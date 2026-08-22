namespace Kalkulacie.App.Forms;

partial class OperationsForm
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
        ((System.ComponentModel.ISupportInitialize)(this.grid)).BeginInit();
        this.SuspendLayout();

        // grid
        this.grid.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
        this.grid.Location = new Point(12, 12);
        this.grid.Size = new Size(560, 380);
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

        // btnSave
        this.btnSave.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
        this.btnSave.Location = new Point(472, 405);
        this.btnSave.Size = new Size(100, 30);
        this.btnSave.Text = "Uložiť";
        this.btnSave.Click += new EventHandler(this.btnSave_Click);

        // OperationsForm
        this.AutoScaleDimensions = new SizeF(7F, 15F);
        this.AutoScaleMode = AutoScaleMode.Font;
        this.ClientSize = new Size(584, 448);
        this.Controls.Add(this.grid);
        this.Controls.Add(this.btnAdd);
        this.Controls.Add(this.btnDelete);
        this.Controls.Add(this.btnSave);
        this.MinimumSize = new Size(400, 350);
        this.Name = "OperationsForm";
        this.StartPosition = FormStartPosition.CenterParent;
        this.Text = "Réžia / Práca";
        ((System.ComponentModel.ISupportInitialize)(this.grid)).EndInit();
        this.ResumeLayout(false);
    }

    private DataGridView grid;
    private Button btnAdd;
    private Button btnDelete;
    private Button btnSave;
}
