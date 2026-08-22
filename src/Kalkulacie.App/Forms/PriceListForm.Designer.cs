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
        ((System.ComponentModel.ISupportInitialize)(this.grid)).BeginInit();
        this.SuspendLayout();

        // grid
        this.grid.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
        this.grid.Location = new Point(12, 12);
        this.grid.Size = new Size(960, 520);
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

        // PriceListForm
        this.AutoScaleDimensions = new SizeF(7F, 15F);
        this.AutoScaleMode = AutoScaleMode.Font;
        this.ClientSize = new Size(984, 590);
        this.Controls.Add(this.grid);
        this.Controls.Add(this.btnRefresh);
        this.Controls.Add(this.btnPreview);
        this.Controls.Add(this.btnPrint);
        this.MinimumSize = new Size(700, 400);
        this.Name = "PriceListForm";
        this.StartPosition = FormStartPosition.CenterParent;
        this.Text = "Kalkulácie";
        ((System.ComponentModel.ISupportInitialize)(this.grid)).EndInit();
        this.ResumeLayout(false);
    }

    private DataGridView grid;
    private Button btnRefresh;
    private Button btnPreview;
    private Button btnPrint;
}
