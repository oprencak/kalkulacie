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
        this.btnMaterials = new Button();
        this.btnOperations = new Button();
        this.btnProducts = new Button();
        this.SuspendLayout();

        // btnMaterials
        this.btnMaterials.Location = new Point(40, 40);
        this.btnMaterials.Size = new Size(200, 50);
        this.btnMaterials.Text = "Materiály";
        this.btnMaterials.Font = new Font("Segoe UI", 12F);
        this.btnMaterials.Click += new EventHandler(this.btnMaterials_Click);

        // btnOperations
        this.btnOperations.Location = new Point(40, 110);
        this.btnOperations.Size = new Size(200, 50);
        this.btnOperations.Text = "Réžia / Práca";
        this.btnOperations.Font = new Font("Segoe UI", 12F);
        this.btnOperations.Click += new EventHandler(this.btnOperations_Click);

        // btnProducts
        this.btnProducts.Location = new Point(40, 180);
        this.btnProducts.Size = new Size(200, 50);
        this.btnProducts.Text = "Výrobky";
        this.btnProducts.Font = new Font("Segoe UI", 12F);
        this.btnProducts.Click += new EventHandler(this.btnProducts_Click);

        // MainForm
        this.AutoScaleDimensions = new SizeF(7F, 15F);
        this.AutoScaleMode = AutoScaleMode.Font;
        this.ClientSize = new Size(284, 281);
        this.Controls.Add(this.btnMaterials);
        this.Controls.Add(this.btnOperations);
        this.Controls.Add(this.btnProducts);
        this.FormBorderStyle = FormBorderStyle.FixedSingle;
        this.MaximizeBox = false;
        this.Name = "MainForm";
        this.StartPosition = FormStartPosition.CenterScreen;
        this.Text = "Kalkulácie";
        this.ResumeLayout(false);
    }

    private Button btnMaterials;
    private Button btnOperations;
    private Button btnProducts;
}
