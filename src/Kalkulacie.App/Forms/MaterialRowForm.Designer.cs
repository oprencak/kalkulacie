namespace Kalkulacie.App.Forms;

partial class MaterialRowForm
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
        this.txtSearch = new TextBox();
        this.lstMaterial = new ListBox();
        this.rdArea = new RadioButton();
        this.rdPiece = new RadioButton();
        this.lblA = new Label();
        this.numA = new NumericUpDown();
        this.lblB = new Label();
        this.numB = new NumericUpDown();
        this.lblQty = new Label();
        this.numQty = new NumericUpDown();
        this.btnOk = new Button();
        this.btnCancel = new Button();
        this.lblMaterial = new Label();
        ((System.ComponentModel.ISupportInitialize)(this.numA)).BeginInit();
        ((System.ComponentModel.ISupportInitialize)(this.numB)).BeginInit();
        ((System.ComponentModel.ISupportInitialize)(this.numQty)).BeginInit();
        this.SuspendLayout();

        int col1 = 12, col2 = 100, w2 = 300, row = 14, step = 34;

        // lblMaterial
        this.lblMaterial.Location = new Point(col1, row);
        this.lblMaterial.Size = new Size(80, 23);
        this.lblMaterial.Text = "Materiál:";
        this.lblMaterial.TextAlign = ContentAlignment.MiddleLeft;

        // txtSearch — filtruje zoznam pod sebou
        this.txtSearch.Location = new Point(col2, row);
        this.txtSearch.Size = new Size(w2, 23);
        this.txtSearch.PlaceholderText = "Hľadať...";
        this.txtSearch.TextChanged += new EventHandler(this.txtSearch_TextChanged);
        this.txtSearch.KeyDown += new KeyEventHandler(this.txtSearch_KeyDown);
        row += 28;

        this.lstMaterial.Location = new Point(col2, row);
        this.lstMaterial.Size = new Size(w2, 154);
        this.lstMaterial.IntegralHeight = false;
        this.lstMaterial.DoubleClick += new EventHandler(this.btnOk_Click);
        row += 154 + 11;

        // rdPiece / rdArea
        this.rdPiece.Location = new Point(col2, row);
        this.rdPiece.Size = new Size(90, 23);
        this.rdPiece.Text = "Kusy";
        this.rdPiece.CheckedChanged += new EventHandler(this.rdArea_CheckedChanged);

        this.rdArea.Location = new Point(col2 + 100, row);
        this.rdArea.Size = new Size(90, 23);
        this.rdArea.Text = "Plocha";
        this.rdArea.CheckedChanged += new EventHandler(this.rdArea_CheckedChanged);
        row += step;

        // lblA / numA
        this.lblA.Location = new Point(col1, row);
        this.lblA.Size = new Size(80, 23);
        this.lblA.Text = "Rozmer A:";
        this.lblA.TextAlign = ContentAlignment.MiddleLeft;

        this.numA.Location = new Point(col2, row);
        this.numA.Size = new Size(w2, 23);
        this.numA.DecimalPlaces = 4;
        this.numA.Maximum = 999999;
        row += step;

        // lblB / numB
        this.lblB.Location = new Point(col1, row);
        this.lblB.Size = new Size(80, 23);
        this.lblB.Text = "Rozmer B:";
        this.lblB.TextAlign = ContentAlignment.MiddleLeft;

        this.numB.Location = new Point(col2, row);
        this.numB.Size = new Size(w2, 23);
        this.numB.DecimalPlaces = 4;
        this.numB.Maximum = 999999;
        row += step;

        // lblQty / numQty
        this.lblQty.Location = new Point(col1, row);
        this.lblQty.Size = new Size(80, 23);
        this.lblQty.Text = "Množstvo:";
        this.lblQty.TextAlign = ContentAlignment.MiddleLeft;

        this.numQty.Location = new Point(col2, row);
        this.numQty.Size = new Size(w2, 23);
        this.numQty.DecimalPlaces = 4;
        this.numQty.Maximum = 999999;
        this.numQty.Minimum = 0;
        this.numQty.Value = 1;
        row += step + 10;

        // btnOk
        this.btnOk.Location = new Point(col2, row);
        this.btnOk.Size = new Size(90, 28);
        this.btnOk.Text = "OK";
        this.btnOk.Click += new EventHandler(this.btnOk_Click);

        // btnCancel
        this.btnCancel.Location = new Point(col2 + 100, row);
        this.btnCancel.Size = new Size(90, 28);
        this.btnCancel.Text = "Zrušiť";
        this.btnCancel.DialogResult = DialogResult.Cancel;

        // Form
        this.AcceptButton = this.btnOk;
        this.CancelButton = this.btnCancel;
        this.AutoScaleDimensions = new SizeF(7F, 15F);
        this.AutoScaleMode = AutoScaleMode.Font;
        this.ClientSize = new Size(420, row + 50);
        this.Controls.AddRange(new Control[] {
            lblMaterial, txtSearch, lstMaterial, rdPiece, rdArea,
            lblA, numA, lblB, numB, lblQty, numQty,
            btnOk, btnCancel
        });
        this.FormBorderStyle = FormBorderStyle.FixedDialog;
        this.MaximizeBox = false;
        this.MinimizeBox = false;
        this.Name = "MaterialRowForm";
        this.StartPosition = FormStartPosition.CenterParent;
        this.Text = "Pridať materiál";
        ((System.ComponentModel.ISupportInitialize)(this.numA)).EndInit();
        ((System.ComponentModel.ISupportInitialize)(this.numB)).EndInit();
        ((System.ComponentModel.ISupportInitialize)(this.numQty)).EndInit();
        this.ResumeLayout(false);
    }

    private TextBox txtSearch;
    private ListBox lstMaterial;
    private RadioButton rdArea;
    private RadioButton rdPiece;
    private Label lblA, lblB, lblQty, lblMaterial;
    private NumericUpDown numA, numB, numQty;
    private Button btnOk, btnCancel;
}
