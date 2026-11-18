namespace Kalkulacie.App.Forms;

partial class MainForm
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
        this.btnKalkulacie = new Button();
        this.btnProducts = new Button();
        this.separator = new Label();
        this.btnMaterials = new Button();
        this.btnOperations = new Button();
        this.btnSettings = new Button();
        this.components = new System.ComponentModel.Container();
        this.separator2 = new Label();
        this.lblDb = new Label();
        this.btnChangeDb = new Button();
        this.lblVersion = new Label();
        this.pnlUpdate = new Panel();
        this.lblUpdate = new Label();
        this.btnUpdate = new Button();
        this.toolTip = new ToolTip(this.components);
        this.SuspendLayout();

        int x = 40, w = 220, h = 44, gap = 54, row = 30;

        this.btnKalkulacie.Location = new Point(x, row);
        this.btnKalkulacie.Size = new Size(w, h);
        this.btnKalkulacie.Text = "Kalkulácie";
        this.btnKalkulacie.Font = new Font("Segoe UI", 11F);
        this.btnKalkulacie.Click += new EventHandler(this.btnKalkulacie_Click);
        row += gap;

        this.btnProducts.Location = new Point(x, row);
        this.btnProducts.Size = new Size(w, h);
        this.btnProducts.Text = "Výrobky";
        this.btnProducts.Font = new Font("Segoe UI", 11F);
        this.btnProducts.Click += new EventHandler(this.btnProducts_Click);
        row += gap + 10;

        this.separator.Location = new Point(x, row);
        this.separator.Size = new Size(w, 2);
        this.separator.BorderStyle = BorderStyle.Fixed3D;
        row += 14;

        this.btnMaterials.Location = new Point(x, row);
        this.btnMaterials.Size = new Size(w, h);
        this.btnMaterials.Text = "Materiály";
        this.btnMaterials.Font = new Font("Segoe UI", 11F);
        this.btnMaterials.Click += new EventHandler(this.btnMaterials_Click);
        row += gap;

        this.btnOperations.Location = new Point(x, row);
        this.btnOperations.Size = new Size(w, h);
        this.btnOperations.Text = "Réžia / Práca";
        this.btnOperations.Font = new Font("Segoe UI", 11F);
        this.btnOperations.Click += new EventHandler(this.btnOperations_Click);
        row += gap;

        this.btnSettings.Location = new Point(x, row);
        this.btnSettings.Size = new Size(w, h);
        this.btnSettings.Text = "Koeficienty";
        this.btnSettings.Font = new Font("Segoe UI", 11F);
        this.btnSettings.Click += new EventHandler(this.btnSettings_Click);
        row += gap + 10;

        this.separator2.Location = new Point(x, row);
        this.separator2.Size = new Size(w, 2);
        this.separator2.BorderStyle = BorderStyle.Fixed3D;
        row += 10;

        this.lblDb.Location = new Point(x, row);
        this.lblDb.Size = new Size(w, 20);
        this.lblDb.AutoEllipsis = true;
        this.lblDb.Font = new Font("Segoe UI", 9F);
        row += 22;

        this.btnChangeDb.Location = new Point(x, row);
        this.btnChangeDb.Size = new Size(w, 30);
        this.btnChangeDb.Text = "Zmeniť databázu…";
        this.btnChangeDb.Font = new Font("Segoe UI", 9F);
        this.btnChangeDb.Click += new EventHandler(this.btnChangeDb_Click);
        row += 30 + 8;

        this.lblVersion.Location = new Point(x, row);
        this.lblVersion.Size = new Size(w, 16);
        this.lblVersion.Font = new Font("Segoe UI", 8F);
        this.lblVersion.ForeColor = SystemColors.GrayText;
        this.lblVersion.TextAlign = ContentAlignment.MiddleCenter;
        row += 16;

        // Ponuka novej verzie — skrytá, zobrazí ju MainForm.ShowUpdate (okno sa vtedy zväčší)
        this.pnlUpdate.Location = new Point(x - 10, row + 8);
        this.pnlUpdate.Size = new Size(w + 20, 64);
        this.pnlUpdate.BackColor = Color.FromArgb(255, 244, 206);
        this.pnlUpdate.Visible = false;
        this.pnlUpdate.Controls.Add(this.lblUpdate);
        this.pnlUpdate.Controls.Add(this.btnUpdate);

        this.lblUpdate.Location = new Point(8, 6);
        this.lblUpdate.Size = new Size(w + 4, 20);
        this.lblUpdate.Font = new Font("Segoe UI", 9F, FontStyle.Bold);

        this.btnUpdate.Location = new Point(8, 28);
        this.btnUpdate.Size = new Size(w + 4, 28);
        this.btnUpdate.Text = "Aktualizovať…";
        this.btnUpdate.Font = new Font("Segoe UI", 9F);
        this.btnUpdate.Click += new EventHandler(this.btnUpdate_Click);

        this.AutoScaleDimensions = new SizeF(7F, 15F);
        this.AutoScaleMode = AutoScaleMode.Font;
        this.ClientSize = new Size(300, row + 12);
        this.Controls.Add(this.separator2);
        this.Controls.Add(this.lblDb);
        this.Controls.Add(this.btnChangeDb);
        this.Controls.Add(this.lblVersion);
        this.Controls.Add(this.pnlUpdate);
        this.Controls.Add(this.btnKalkulacie);
        this.Controls.Add(this.btnProducts);
        this.Controls.Add(this.separator);
        this.Controls.Add(this.btnMaterials);
        this.Controls.Add(this.btnOperations);
        this.Controls.Add(this.btnSettings);
        this.FormBorderStyle = FormBorderStyle.FixedSingle;
        this.MaximizeBox = false;
        this.Name = "MainForm";
        this.StartPosition = FormStartPosition.CenterScreen;
        this.Text = "Kalkulácie";
        this.ResumeLayout(false);
    }

    private Button btnKalkulacie;
    private Button btnProducts;
    private Label separator;
    private Button btnMaterials;
    private Button btnOperations;
    private Button btnSettings;
    private Label lblVersion;
    private Panel pnlUpdate;
    private Label lblUpdate;
    private Button btnUpdate;
    private Label separator2;
    private Label lblDb;
    private Button btnChangeDb;
    private ToolTip toolTip;
}
