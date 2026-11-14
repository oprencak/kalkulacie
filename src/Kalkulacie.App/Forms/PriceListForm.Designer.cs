namespace Kalkulacie.App.Forms;

partial class PriceListForm
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
        this.btnRefresh = new Button();
        this.btnPreview = new Button();
        this.btnPrint = new Button();
        this.btnExport = new Button();
        this.rdAll = new RadioButton();
        this.rdManual = new RadioButton();
        this.rdAutomatic = new RadioButton();
        this.lblSearch = new Label();
        this.txtSearch = new TextBox();
        this.lblDecimals = new Label();
        this.numDecimals = new NumericUpDown();
        this.lblCount = new Label();
        ((System.ComponentModel.ISupportInitialize)(this.grid)).BeginInit();
        ((System.ComponentModel.ISupportInitialize)(this.numDecimals)).BeginInit();
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

        // lblSearch
        this.lblSearch.Location = new Point(270, 14);
        this.lblSearch.Size = new Size(55, 23);
        this.lblSearch.Text = "Hľadať:";
        this.lblSearch.TextAlign = ContentAlignment.MiddleLeft;

        // txtSearch
        this.txtSearch.Location = new Point(328, 14);
        this.txtSearch.Size = new Size(250, 23);
        this.txtSearch.TextChanged += new EventHandler(this.txtSearch_TextChanged);

        // lblDecimals
        this.lblDecimals.Location = new Point(604, 14);
        this.lblDecimals.Size = new Size(110, 23);
        this.lblDecimals.Text = "Desatinné miesta:";
        this.lblDecimals.TextAlign = ContentAlignment.MiddleLeft;

        // numDecimals
        this.numDecimals.Location = new Point(716, 14);
        this.numDecimals.Size = new Size(48, 23);
        this.numDecimals.Minimum = 2;
        this.numDecimals.Maximum = 4;
        this.numDecimals.Value = 4;
        this.numDecimals.ValueChanged += new EventHandler(this.numDecimals_ValueChanged);

        // lblCount
        this.lblCount.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
        this.lblCount.Location = new Point(822, 546);
        this.lblCount.Size = new Size(150, 30);
        this.lblCount.TextAlign = ContentAlignment.MiddleRight;

        // grid
        this.grid.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
        this.grid.Location = new Point(12, 44);
        this.grid.Size = new Size(960, 488);
        this.grid.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
        this.grid.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
        this.grid.ReadOnly = true;
        this.grid.AllowUserToAddRows = false;

        // btnRefresh
        this.btnRefresh.Anchor = AnchorStyles.Bottom | AnchorStyles.Left;
        this.btnRefresh.Location = new Point(12, 546);
        this.btnRefresh.Size = new Size(110, 30);
        this.btnRefresh.Text = "Prepočítať";
        this.btnRefresh.Click += new EventHandler(this.btnRefresh_Click);

        // btnPreview
        this.btnPreview.Anchor = AnchorStyles.Bottom | AnchorStyles.Left;
        this.btnPreview.Location = new Point(130, 546);
        this.btnPreview.Size = new Size(110, 30);
        this.btnPreview.Text = "Náhľad...";
        this.btnPreview.Click += new EventHandler(this.btnPreview_Click);

        // btnPrint
        this.btnPrint.Anchor = AnchorStyles.Bottom | AnchorStyles.Left;
        this.btnPrint.Location = new Point(248, 546);
        this.btnPrint.Size = new Size(110, 30);
        this.btnPrint.Text = "Tlačiť...";
        this.btnPrint.Click += new EventHandler(this.btnPrint_Click);

        // btnExport
        this.btnExport.Anchor = AnchorStyles.Bottom | AnchorStyles.Left;
        this.btnExport.Location = new Point(366, 546);
        this.btnExport.Size = new Size(130, 30);
        this.btnExport.Text = "Export do CSV...";
        this.btnExport.Click += new EventHandler(this.btnExport_Click);

        // PriceListForm
        this.AutoScaleDimensions = new SizeF(7F, 15F);
        this.AutoScaleMode = AutoScaleMode.Font;
        this.ClientSize = new Size(984, 590);
        this.Controls.Add(this.rdAll);
        this.Controls.Add(this.rdManual);
        this.Controls.Add(this.rdAutomatic);
        this.Controls.Add(this.lblSearch);
        this.Controls.Add(this.txtSearch);
        this.Controls.Add(this.lblDecimals);
        this.Controls.Add(this.numDecimals);
        this.Controls.Add(this.grid);
        this.Controls.Add(this.btnRefresh);
        this.Controls.Add(this.btnPreview);
        this.Controls.Add(this.btnPrint);
        this.Controls.Add(this.btnExport);
        this.Controls.Add(this.lblCount);
        this.MinimumSize = new Size(800, 400);
        this.Name = "PriceListForm";
        this.StartPosition = FormStartPosition.CenterParent;
        this.Text = "Kalkulácie";
        ((System.ComponentModel.ISupportInitialize)(this.grid)).EndInit();
        ((System.ComponentModel.ISupportInitialize)(this.numDecimals)).EndInit();
        this.ResumeLayout(false);
        this.PerformLayout();
    }

    private DataGridView grid;
    private Button btnRefresh;
    private Button btnPreview;
    private Button btnPrint;
    private Button btnExport;
    private RadioButton rdAll;
    private RadioButton rdManual;
    private RadioButton rdAutomatic;
    private Label lblSearch;
    private TextBox txtSearch;
    private Label lblDecimals;
    private NumericUpDown numDecimals;
    private Label lblCount;
}
