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

    public List<string> EvaluateWithTrace(Node ast, Sheet sheet) {
        List<string> trace = new List<string>();
        ErrorText = "";

        try {
            Value result = ast.Evaluate(sheet, 0, trace);
            trace.Add($"результат: {result}");
        } catch (EvalException ex) {
            ErrorText = ex.Message;
            trace.Add($"помилка: {ex.Message}");
        }

        return trace;
    }
}