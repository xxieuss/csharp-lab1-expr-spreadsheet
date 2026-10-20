namespace Core;

internal sealed class EvalException : Exception {
    public EvalException(string message) : base(message) { }
}