namespace Kalkulacie.App.Forms;

public partial class MainForm : Form
{
    public MainForm()
    {
        InitializeComponent();
    }

    private void btnMaterials_Click(object sender, EventArgs e)
    {
        using var form = new MaterialsForm();
        form.ShowDialog(this);
    }

    private void btnOperations_Click(object sender, EventArgs e)
    {
        using var form = new OperationsForm();
        form.ShowDialog(this);
    }

    private void btnProducts_Click(object sender, EventArgs e)
    {
        using var form = new ProductsForm();
        form.ShowDialog(this);
    }
}
