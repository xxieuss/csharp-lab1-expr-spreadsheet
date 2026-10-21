using Core;
using Cell = Core.Cell;

namespace Ui;

public partial class MainPage : ContentPage {
    private readonly SheetController _controller = new SheetController();

    public MainPage() {
        InitializeComponent();
        _controller.NewSheet(3, 3);
    }

    private void OnCalculate(object? sender, EventArgs e) {
        Cell cell = _controller.SelectCell("A1");
        _controller.SetCellFormula(cell, FormulaEntry.Text ?? "");
        ShowCell(cell);
    }

    private void ShowCell(Cell cell) {
        if (cell.HasError()) {
            ResultLabel.Text = "";
            ErrorLabel.Text = cell.ErrorPos >= 0
                ? $"{cell.ErrorText}  (позиція {cell.ErrorPos + 1})"
                : cell.ErrorText;
        } else {
            ResultLabel.Text = $"Значення: {cell.Value}";
            ErrorLabel.Text = "";
        }

        Node? tree = _controller.GetTree(cell);
        TreeLabel.Text = tree == null ? "" : "Дерево розбору:\n" + tree.ToTree(1);
    }
}