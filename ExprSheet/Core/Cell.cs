namespace Core;

public class Cell {
    public string Name { get; internal set; } = "";
    public string Formula { get; internal set; } = "";
    public Value Value { get; internal set; } = Core.Value.Empty;
    public string ErrorText { get; internal set; } = "";
    public int ErrorPos { get; internal set; } = -1;
    public Node? Ast { get; internal set; }
    internal Cell? WritesTo { get; set; }
    internal Cell? WrittenBy { get; set; }

    public bool HasError() => ErrorText.Length > 0;

    public void Clear() {
        Formula = "";
        Value = Core.Value.Empty;
        ErrorText = "";
        ErrorPos = -1;
        Ast = null;
    }

    public string Display(DisplayMode mode) {
        if (mode == DisplayMode.Expression) return Formula;
        if (HasError()) return "ПОМИЛКА";
        return Value.ToString();
    }
}