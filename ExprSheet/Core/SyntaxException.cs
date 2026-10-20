namespace Core;

internal sealed class SyntaxException : Exception {
    public int Position { get; }

    public SyntaxException(string message, int position) : base(message) {
        Position = position;
    }
}