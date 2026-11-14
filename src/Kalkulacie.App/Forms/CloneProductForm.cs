namespace Kalkulacie.App.Forms;

public partial class CloneProductForm : AppForm
{
    public string NewName => txtName.Text;

    public CloneProductForm(string sourceName)
    {
        InitializeComponent();
        txtName.Text = sourceName + " - kópia";
        txtName.SelectAll();
        AcceptButton = btnOk;
        CancelButton = btnCancel;
    }
}
