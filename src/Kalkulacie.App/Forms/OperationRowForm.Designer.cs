namespace Kalkulacie.App.Forms;

partial class OperationRowForm
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
        this.lstOperation = new ListBox();
        this.numQty = new NumericUpDown();
        this.btnOk = new Button();
        this.btnCancel = new Button();
        this.lblOperation = new Label();
        this.lblQty = new Label();
        this.lblCost = new Label();
        ((System.ComponentModel.ISupportInitialize)(this.numQty)).BeginInit();
        this.SuspendLayout();

        int col1 = 12, col2 = 100, w2 = 300, row = 14;

        this.lblOperation.Location = new Point(col1, row);
        this.lblOperation.Size = new Size(80, 23);
        this.lblOperation.Text = "Réžia:";
        this.lblOperation.TextAlign = ContentAlignment.MiddleLeft;

        // txtSearch — filtruje zoznam pod sebou
        this.txtSearch.Location = new Point(col2, row);
        this.txtSearch.Size = new Size(w2, 23);
        this.txtSearch.PlaceholderText = "Hľadať...";
        this.txtSearch.TextChanged += new EventHandler(this.txtSearch_TextChanged);
        this.txtSearch.KeyDown += new KeyEventHandler(this.txtSearch_KeyDown);
        row += 28;

        this.lstOperation.Location = new Point(col2, row);
        this.lstOperation.Size = new Size(w2, 154);
        this.lstOperation.IntegralHeight = false;
        this.lstOperation.DoubleClick += new EventHandler(this.btnOk_Click);
        row += 154 + 11;

        this.lblQty.Location = new Point(col1, row);
        this.lblQty.Size = new Size(80, 23);
        this.lblQty.Text = "Počet kusov:";
        this.lblQty.TextAlign = ContentAlignment.MiddleLeft;

        this.numQty.Location = new Point(col2, row);
        this.numQty.Size = new Size(w2, 23);
        this.numQty.DecimalPlaces = 4;
        this.numQty.Maximum = 999999;
        this.numQty.Minimum = 0;
        this.numQty.Value = 1;
        row += 28;

        // lblCost — vysvetlenie výpočtu: sadzba sa delí počtom kusov
        this.lblCost.Location = new Point(col2, row);
        this.lblCost.Size = new Size(w2, 36);
        this.lblCost.ForeColor = SystemColors.GrayText;
        row += 36 + 10;

        this.btnOk.Location = new Point(col2, row);
        this.btnOk.Size = new Size(90, 28);
        this.btnOk.Text = "OK";
        this.btnOk.Click += new EventHandler(this.btnOk_Click);

        this.btnCancel.Location = new Point(col2 + 100, row);
        this.btnCancel.Size = new Size(90, 28);
        this.btnCancel.Text = "Zrušiť";
        this.btnCancel.DialogResult = DialogResult.Cancel;

        this.AcceptButton = this.btnOk;
        this.CancelButton = this.btnCancel;
        this.AutoScaleDimensions = new SizeF(7F, 15F);
        this.AutoScaleMode = AutoScaleMode.Font;
        this.ClientSize = new Size(420, row + 50);
        this.Controls.AddRange(new Control[] {
            lblOperation, txtSearch, lstOperation, lblQty, numQty, lblCost, btnOk, btnCancel
        });
        this.FormBorderStyle = FormBorderStyle.FixedDialog;
        this.MaximizeBox = false;
        this.MinimizeBox = false;
        this.Name = "OperationRowForm";
        this.StartPosition = FormStartPosition.CenterParent;
        this.Text = "Pridať réžiu / prácu";
        ((System.ComponentModel.ISupportInitialize)(this.numQty)).EndInit();
        this.ResumeLayout(false);
    }

    private TextBox txtSearch;
    private ListBox lstOperation;
    private NumericUpDown numQty;
    private Button btnOk, btnCancel;
    private Label lblOperation, lblQty, lblCost;
}
