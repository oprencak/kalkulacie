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
        this.components = new System.ComponentModel.Container();
        this.pnlHeader = new Panel();
        this.lblSection1 = new Label();
        this.btnKalkulacie = new NavButton();
        this.btnEditions = new NavButton();
        this.btnProducts = new NavButton();
        this.lblSection2 = new Label();
        this.btnMaterials = new NavButton();
        this.btnOperations = new NavButton();
        this.btnSettings = new NavButton();
        this.lblDb = new Label();
        this.btnChangeDb = new Button();
        this.pnlUpdate = new Panel();
        this.lblUpdate = new Label();
        this.btnUpdate = new Button();
        this.toolTip = new ToolTip(this.components);
        this.SuspendLayout();

        int width = 360, x = 20, w = width - 2 * x, h = 52, gap = 58, row = 0;

        // Hlavička s logom, názvom a verziou — kreslí ju MainForm.PaintHeader
        this.pnlHeader.Location = new Point(0, row);
        this.pnlHeader.Size = new Size(width, 76);
        this.pnlHeader.BackColor = Theme.Accent;
        this.pnlHeader.Paint += new PaintEventHandler(this.PaintHeader);
        row += 76 + 14;

        this.lblSection1.Location = new Point(x, row);
        this.lblSection1.Size = new Size(w, 18);
        this.lblSection1.Text = "KALKULÁCIE";
        this.lblSection1.Font = new Font("Segoe UI Semibold", 8F);
        this.lblSection1.ForeColor = Theme.TextMuted;
        row += 22;

        this.btnKalkulacie.Location = new Point(x, row);
        this.btnKalkulacie.Size = new Size(w, h);
        this.btnKalkulacie.Text = "Kalkulácie";
        this.btnKalkulacie.Description = "Aktuálny cenník — tlač, náhľad, export";
        this.btnKalkulacie.Glyph = "";
        this.btnKalkulacie.Click += new EventHandler(this.btnKalkulacie_Click);
        row += gap;

        this.btnEditions.Location = new Point(x, row);
        this.btnEditions.Size = new Size(w, h);
        this.btnEditions.Text = "Uložené kalkulácie";
        this.btnEditions.Description = "Cenníky uchované k určitému dátumu";
        this.btnEditions.Glyph = "";
        this.btnEditions.Click += new EventHandler(this.btnEditions_Click);
        row += gap;

        this.btnProducts.Location = new Point(x, row);
        this.btnProducts.Size = new Size(w, h);
        this.btnProducts.Text = "Výrobky";
        this.btnProducts.Description = "Zoznam výrobkov a ich zloženie";
        this.btnProducts.Glyph = "";
        this.btnProducts.Click += new EventHandler(this.btnProducts_Click);
        row += gap + 8;

        this.lblSection2.Location = new Point(x, row);
        this.lblSection2.Size = new Size(w, 18);
        this.lblSection2.Text = "ČÍSELNÍKY A NASTAVENIA";
        this.lblSection2.Font = new Font("Segoe UI Semibold", 8F);
        this.lblSection2.ForeColor = Theme.TextMuted;
        row += 22;

        this.btnMaterials.Location = new Point(x, row);
        this.btnMaterials.Size = new Size(w, h);
        this.btnMaterials.Text = "Materiály";
        this.btnMaterials.Description = "Vstupy a suroviny s jednotkovými cenami";
        this.btnMaterials.Glyph = "";
        this.btnMaterials.Click += new EventHandler(this.btnMaterials_Click);
        row += gap;

        this.btnOperations.Location = new Point(x, row);
        this.btnOperations.Size = new Size(w, h);
        this.btnOperations.Text = "Réžia / Práca";
        this.btnOperations.Description = "Operácie a ich sadzby";
        this.btnOperations.Glyph = "";
        this.btnOperations.Click += new EventHandler(this.btnOperations_Click);
        row += gap;

        this.btnSettings.Location = new Point(x, row);
        this.btnSettings.Size = new Size(w, h);
        this.btnSettings.Text = "Koeficienty";
        this.btnSettings.Description = "Prirážky a zaokrúhľovanie cenových vzorcov";
        this.btnSettings.Glyph = "";
        this.btnSettings.Click += new EventHandler(this.btnSettings_Click);
        row += gap + 10;

        // Aktívna databáza a jej zmena v jednom riadku
        this.lblDb.Location = new Point(x, row);
        this.lblDb.Size = new Size(w - 138, 30);
        this.lblDb.AutoEllipsis = true;
        this.lblDb.TextAlign = ContentAlignment.MiddleLeft;
        this.lblDb.ForeColor = Theme.TextMuted;

        this.btnChangeDb.Location = new Point(x + w - 130, row);
        this.btnChangeDb.Size = new Size(130, 30);
        this.btnChangeDb.Text = "Zmeniť databázu…";
        this.btnChangeDb.Click += new EventHandler(this.btnChangeDb_Click);
        row += 30;

        // Ponuka novej verzie — skrytá, zobrazí ju MainForm.CheckForUpdateAsync (okno sa vtedy zväčší)
        this.pnlUpdate.Location = new Point(x, row + 14);
        this.pnlUpdate.Size = new Size(w, 68);
        this.pnlUpdate.BackColor = Theme.WarningBack;
        this.pnlUpdate.Visible = false;
        this.pnlUpdate.Paint += new PaintEventHandler(this.PaintUpdatePanel);
        this.pnlUpdate.Controls.Add(this.lblUpdate);
        this.pnlUpdate.Controls.Add(this.btnUpdate);

        this.lblUpdate.Location = new Point(10, 8);
        this.lblUpdate.Size = new Size(w - 20, 20);
        this.lblUpdate.Font = new Font("Segoe UI Semibold", 9F);

        this.btnUpdate.Location = new Point(10, 31);
        this.btnUpdate.Size = new Size(w - 20, 28);
        this.btnUpdate.Text = "Aktualizovať…";
        this.btnUpdate.Click += new EventHandler(this.btnUpdate_Click);

        this.AutoScaleDimensions = new SizeF(7F, 15F);
        this.AutoScaleMode = AutoScaleMode.Font;
        this.ClientSize = new Size(width, row + 16);
        this.Controls.Add(this.pnlHeader);
        this.Controls.Add(this.lblSection1);
        this.Controls.Add(this.btnKalkulacie);
        this.Controls.Add(this.btnEditions);
        this.Controls.Add(this.btnProducts);
        this.Controls.Add(this.lblSection2);
        this.Controls.Add(this.btnMaterials);
        this.Controls.Add(this.btnOperations);
        this.Controls.Add(this.btnSettings);
        this.Controls.Add(this.lblDb);
        this.Controls.Add(this.btnChangeDb);
        this.Controls.Add(this.pnlUpdate);
        this.FormBorderStyle = FormBorderStyle.FixedSingle;
        this.MaximizeBox = false;
        this.Name = "MainForm";
        this.StartPosition = FormStartPosition.CenterScreen;
        this.Text = "Kalkulácie";
        this.ResumeLayout(false);
    }

    private Panel pnlHeader;
    private Label lblSection1;
    private NavButton btnKalkulacie;
    private NavButton btnEditions;
    private NavButton btnProducts;
    private Label lblSection2;
    private NavButton btnMaterials;
    private NavButton btnOperations;
    private NavButton btnSettings;
    private Panel pnlUpdate;
    private Label lblUpdate;
    private Button btnUpdate;
    private Label lblDb;
    private Button btnChangeDb;
    private ToolTip toolTip;
}
