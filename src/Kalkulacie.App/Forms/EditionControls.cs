using Kalkulacie.Core.Enums;

namespace Kalkulacie.App.Forms;

internal static class EditionControls
{
    public static DataGridView Grid(params (string Property, string Header, int Weight, string? Format)[] columns)
    {
        var grid = new DataGridView
        {
            Dock = DockStyle.Fill, AutoGenerateColumns = false, ReadOnly = true,
            AllowUserToAddRows = false, AllowUserToDeleteRows = false,
            AllowUserToResizeRows = false, RowHeadersVisible = false,
            SelectionMode = DataGridViewSelectionMode.FullRowSelect, MultiSelect = false,
            AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
        };
        foreach (var (property, header, weight, format) in columns)
        {
            var column = new DataGridViewTextBoxColumn
            {
                DataPropertyName = property, HeaderText = header,
                FillWeight = weight, SortMode = DataGridViewColumnSortMode.NotSortable,
            };
            if (format is not null)
            {
                column.DefaultCellStyle.Format = format;
                column.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;
            }
            grid.Columns.Add(column);
        }
        return grid;
    }

    public static FlowLayoutPanel Bar(params Control[] controls)
    {
        var panel = new FlowLayoutPanel { Dock = DockStyle.Fill, AutoSize = true, WrapContents = false };
        panel.Controls.AddRange(controls);
        return panel;
    }

    public static Button Button(string text, Action action)
    {
        var button = new Button { Text = text, AutoSize = true };
        button.Click += (_, _) => action();
        return button;
    }

    public static string ModeLabel(CalculationMode mode) => mode == CalculationMode.Manual ? "Ručná" : "Strojová";
    public const string DecimalFormat = "0.############################";
}
