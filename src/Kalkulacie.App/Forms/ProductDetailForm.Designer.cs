namespace Kalkulacie.App.Forms;

partial class ProductDetailForm
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
        this.txtName = new TextBox();
        this.cmbMode = new ComboBox();
        this.lblNameLabel = new Label();
        this.lblModeLabel = new Label();
        this.gridRows = new DataGridView();
        this.btnSave = new Button();
        this.btnAddMaterial = new Button();
        this.btnAddOperation = new Button();
        this.btnEditRow = new Button();
        this.btnDeleteRow = new Button();
        this.btnMoveUp = new Button();
        this.btnMoveDown = new Button();
        this.panelSummary = new Panel();
        this.lblMaterialTotal = new Label();
        this.lblOperationTotal = new Label();
        this.lblUvn = new Label();
        this.lblBasePrice = new Label();
        ((System.ComponentModel.ISupportInitialize)(this.gridRows)).BeginInit();
        this.panelSummary.SuspendLayout();
        this.SuspendLayout();

        // lblNameLabel
        this.lblNameLabel.Location = new Point(12, 15);
        this.lblNameLabel.Size = new Size(60, 23);
        this.lblNameLabel.Text = "Názov:";

        // txtName
        this.txtName.Location = new Point(76, 12);
        this.txtName.Size = new Size(300, 23);

        // lblModeLabel
        this.lblModeLabel.Location = new Point(390, 15);
        this.lblModeLabel.Size = new Size(40, 23);
        this.lblModeLabel.Text = "Typ:";

        // cmbMode
        this.cmbMode.Location = new Point(434, 12);
        this.cmbMode.Size = new Size(130, 23);
        this.cmbMode.DropDownStyle = ComboBoxStyle.DropDownList;

        // btnSave
        this.btnSave.Location = new Point(580, 11);
        this.btnSave.Size = new Size(110, 25);
        this.btnSave.Text = "Uložiť názov";
        this.btnSave.Click += new EventHandler(this.btnSave_Click);

        // gridRows
        this.gridRows.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
        this.gridRows.Location = new Point(12, 50);
        this.gridRows.Size = new Size(858, 378);
        this.gridRows.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
        this.gridRows.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
        this.gridRows.ReadOnly = true;
        this.gridRows.AllowUserToAddRows = false;
        this.gridRows.DoubleClick += new EventHandler(this.gridRows_DoubleClick);

        // btnAddMaterial
        this.btnAddMaterial.Anchor = AnchorStyles.Bottom | AnchorStyles.Left;
        this.btnAddMaterial.Location = new Point(12, 440);
        this.btnAddMaterial.Size = new Size(110, 28);
        this.btnAddMaterial.Text = "+ Materiál";
        this.btnAddMaterial.Click += new EventHandler(this.btnAddMaterial_Click);

        // btnAddOperation
        this.btnAddOperation.Anchor = AnchorStyles.Bottom | AnchorStyles.Left;
        this.btnAddOperation.Location = new Point(130, 440);
        this.btnAddOperation.Size = new Size(100, 28);
        this.btnAddOperation.Text = "+ Práca";
        this.btnAddOperation.Click += new EventHandler(this.btnAddOperation_Click);

        // btnEditRow
        this.btnEditRow.Anchor = AnchorStyles.Bottom | AnchorStyles.Left;
        this.btnEditRow.Location = new Point(238, 440);
        this.btnEditRow.Size = new Size(90, 28);
        this.btnEditRow.Text = "Upraviť";
        this.btnEditRow.Click += new EventHandler(this.btnEditRow_Click);

        // btnDeleteRow
        this.btnDeleteRow.Anchor = AnchorStyles.Bottom | AnchorStyles.Left;
        this.btnDeleteRow.Location = new Point(336, 440);
        this.btnDeleteRow.Size = new Size(90, 28);
        this.btnDeleteRow.Text = "Vymazať";
        this.btnDeleteRow.Click += new EventHandler(this.btnDeleteRow_Click);

        // btnMoveUp
        this.btnMoveUp.Anchor = AnchorStyles.Bottom | AnchorStyles.Left;
        this.btnMoveUp.Location = new Point(446, 440);
        this.btnMoveUp.Size = new Size(80, 28);
        this.btnMoveUp.Text = "▲ Hore";
        this.btnMoveUp.Click += new EventHandler(this.btnMoveUp_Click);

        // btnMoveDown
        this.btnMoveDown.Anchor = AnchorStyles.Bottom | AnchorStyles.Left;
        this.btnMoveDown.Location = new Point(534, 440);
        this.btnMoveDown.Size = new Size(80, 28);
        this.btnMoveDown.Text = "▼ Dole";
        this.btnMoveDown.Click += new EventHandler(this.btnMoveDown_Click);

        // panelSummary — súhrn ceny: štyri hodnoty vedľa seba, popis nad hodnotou
        this.panelSummary.Anchor = AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
        this.panelSummary.BackColor = Theme.Surface;
        this.panelSummary.Location = new Point(12, 478);
        this.panelSummary.Size = new Size(858, 62);
        this.panelSummary.Paint += new PaintEventHandler(this.panelSummary_Paint);

        var captionFont = new Font("Segoe UI", 8.25F);
        var valueFont = new Font("Segoe UI Semibold", 12F);
        var summary = new (string Caption, Label Value)[]
        {
            ("Materiál", this.lblMaterialTotal), ("Práca", this.lblOperationTotal),
            ("UVN", this.lblUvn), ("Cena", this.lblBasePrice),
        };
        for (int i = 0; i < summary.Length; i++)
        {
            int x = 16 + i * 180;
            this.panelSummary.Controls.Add(new Label
            {
                Location = new Point(x, 9), Size = new Size(170, 16),
                Text = summary[i].Caption, Font = captionFont, ForeColor = Theme.TextMuted,
            });
            summary[i].Value.Location = new Point(x - 2, 26);
            summary[i].Value.Size = new Size(174, 26);
            summary[i].Value.Font = valueFont;
            summary[i].Value.AutoEllipsis = true;
            this.panelSummary.Controls.Add(summary[i].Value);
        }
        this.lblBasePrice.ForeColor = Theme.Accent;

        // ProductDetailForm
        this.AutoScaleDimensions = new SizeF(7F, 15F);
        this.AutoScaleMode = AutoScaleMode.Font;
        this.ClientSize = new Size(882, 552);
        this.Controls.Add(this.lblNameLabel);
        this.Controls.Add(this.txtName);
        this.Controls.Add(this.lblModeLabel);
        this.Controls.Add(this.cmbMode);
        this.Controls.Add(this.btnSave);
        this.Controls.Add(this.gridRows);
        this.Controls.Add(this.btnAddMaterial);
        this.Controls.Add(this.btnAddOperation);
        this.Controls.Add(this.btnEditRow);
        this.Controls.Add(this.btnDeleteRow);
        this.Controls.Add(this.btnMoveUp);
        this.Controls.Add(this.btnMoveDown);
        this.Controls.Add(this.panelSummary);
        this.MinimumSize = new Size(600, 500);
        this.Name = "ProductDetailForm";
        this.StartPosition = FormStartPosition.CenterParent;
        this.Text = "Výrobok";
        ((System.ComponentModel.ISupportInitialize)(this.gridRows)).EndInit();
        this.panelSummary.ResumeLayout(false);
        this.ResumeLayout(false);
        this.PerformLayout();
    }

    private TextBox txtName;
    private ComboBox cmbMode;
    private Label lblNameLabel;
    private Label lblModeLabel;
    private DataGridView gridRows;
    private Button btnSave;
    private Button btnAddMaterial;
    private Button btnAddOperation;
    private Button btnEditRow;
    private Button btnDeleteRow;
    private Button btnMoveUp;
    private Button btnMoveDown;
    private Panel panelSummary;
    private Label lblMaterialTotal;
    private Label lblOperationTotal;
    private Label lblUvn;
    private Label lblBasePrice;
}
