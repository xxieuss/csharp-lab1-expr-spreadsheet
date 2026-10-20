namespace Core;

public class Evaluator {
    public string ErrorText { get; private set; } = "";

    public bool HasError() => ErrorText.Length > 0;

    public Value? Evaluate(Node ast, Sheet sheet) {
        ErrorText = "";
        try {
            return ast.Evaluate(sheet, 0);
        } catch (EvalException ex) {
            ErrorText = ex.Message;
            return null;
        } catch (DivideByZeroException) {
            ErrorText = "Ділення на нуль";
            return null;
        }
    }
}