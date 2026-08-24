namespace Kalkulacie.App.Forms;

partial class CloneProductForm
{
    private System.ComponentModel.IContainer components = null;

    protected override void Dispose(bool disposing)
    {
        if (disposing && (components != null)) components.Dispose();
        base.Dispose(disposing);
    }

    private void InitializeComponent()
    {
        this.lblName = new Label();
        this.txtName = new TextBox();
        this.btnOk = new Button();
        this.btnCancel = new Button();
        this.SuspendLayout();

        this.lblName.Location = new Point(12, 15);
        this.lblName.Size = new Size(340, 20);
        this.lblName.Text = "Názov nového výrobku:";

        this.txtName.Location = new Point(12, 38);
        this.txtName.Size = new Size(340, 23);

        this.btnOk.Location = new Point(196, 74);
        this.btnOk.Size = new Size(75, 28);
        this.btnOk.Text = "OK";
        this.btnOk.DialogResult = DialogResult.OK;

        this.btnCancel.Location = new Point(277, 74);
        this.btnCancel.Size = new Size(75, 28);
        this.btnCancel.Text = "Zrušiť";
        this.btnCancel.DialogResult = DialogResult.Cancel;

        this.AutoScaleDimensions = new SizeF(7F, 15F);
        this.AutoScaleMode = AutoScaleMode.Font;
        this.ClientSize = new Size(364, 114);
        this.Controls.Add(this.lblName);
        this.Controls.Add(this.txtName);
        this.Controls.Add(this.btnOk);
        this.Controls.Add(this.btnCancel);
        this.FormBorderStyle = FormBorderStyle.FixedDialog;
        this.MaximizeBox = false;
        this.MinimizeBox = false;
        this.Name = "CloneProductForm";
        this.StartPosition = FormStartPosition.CenterParent;
        this.Text = "Kopírovať výrobok";
        this.ResumeLayout(false);
    }

    private Label lblName;
    private TextBox txtName;
    private Button btnOk;
    private Button btnCancel;
}
