using System.Numerics;

namespace Core;

public class Parser {
    private string _text = "";
    private int _pos;
    private string _lexeme = "";
    private int _lexemeStart;

    public string ErrorText { get; private set; } = "";
    public int ErrorPos { get; private set; } = -1;

    public bool HasError() => ErrorText.Length > 0;

    public Node? Parse(string text) {
        _text = text;
        _pos = 0;
        _lexeme = "";
        _lexemeStart = 0;
        ErrorText = "";
        ErrorPos = -1;

        try {
            NextLexeme();
            Node node = ParseLogical();
            if (_lexeme != "") throw Error($"Зайві символи після виразу: '{_lexeme}'");
            return node;
        } catch (SyntaxException ex) {
            ErrorText = ex.Message;
            ErrorPos = ex.Position;
            return null;
        }
    }

    private SyntaxException Error(string message) => new SyntaxException(message, _lexemeStart);

    private void NextLexeme() {
        while (_pos < _text.Length && char.IsWhiteSpace(_text[_pos])) _pos++;

        _lexemeStart = _pos;

        if (_pos >= _text.Length) {
            _lexeme = "";
            return;
        }

        char c = _text[_pos];

        if (char.IsDigit(c)) {
            int start = _pos;
            while (_pos < _text.Length && char.IsDigit(_text[_pos])) _pos++;
            _lexeme = _text.Substring(start, _pos - start);
            return;
        }

        if (char.IsLetter(c)) {
            int start = _pos;
            while (_pos < _text.Length && char.IsLetterOrDigit(_text[_pos])) _pos++;
            _lexeme = _text.Substring(start, _pos - start);
            if (_lexeme.ToLowerInvariant() == "not") _lexeme = "not";
            return;
        }

        if (_pos + 1 < _text.Length) {
            string two = _text.Substring(_pos, 2);
            if (two == "<=" || two == ">=" || two == "<>" || two == ":=") {
                _pos += 2;
                _lexeme = two;
                return;
            }
        }

        if ("+-*/^()".Contains(c)) {
            _pos++;
            _lexeme = c.ToString();
            return;
        }

        throw Error($"Неочікуваний символ '{c}'");
    }

    private Node ParsePrimary() {
        if (_lexeme == "") throw Error("Неочікуваний кінець виразу");

        if (char.IsDigit(_lexeme[0])) {
            BigInteger value = BigInteger.Parse(_lexeme);
            NextLexeme();
            return new NumberNode(value);
        }

        if (_lexeme == "(") {
            NextLexeme();
            Node inside = ParseLogical();
            if (_lexeme != ")") throw Error("Очікувалася закривна дужка");
            NextLexeme();
            return inside;
        }

        throw Error($"Очікувалося число або дужка, а отримано '{_lexeme}'");
    }

    private Node ParseSum() {
        Node left = ParseProduct();
        while (_lexeme == "+" || _lexeme == "-") {
            string op = _lexeme;
            NextLexeme();
            left = new BinaryNode(op, left, ParseProduct());
        }
        return left;
    }

    private Node ParseProduct() {
        Node left = ParsePower();
        while (_lexeme == "*" || _lexeme == "/") {
            string op = _lexeme;
            NextLexeme();
            left = new BinaryNode(op, left, ParsePower());
        }
        return left;
    }

    private Node ParsePower() {
        Node left = ParseUnary();
        if (_lexeme == "^") {
            NextLexeme();
            return new BinaryNode("^", left, ParsePower());
        }
        return left;
    }

    private Node ParseUnary() {
        if (_lexeme == "+" || _lexeme == "-") {
            string op = _lexeme;
            NextLexeme();
            return new UnaryNode(op, ParseUnary());
        }
        return ParsePrimary();
    }

    private Node ParseLogical() {
        if (_lexeme == "not") {
            NextLexeme();
            return new UnaryNode("not", ParseLogical());
        }
        return ParseCompare();
    }

    private Node ParseCompare() {
        Node left = ParseSum();
        if (_lexeme == "<=" || _lexeme == ">=" || _lexeme == "<>") {
            string op = _lexeme;
            NextLexeme();
            return new BinaryNode(op, left, ParseSum());
        }
        return left;
    }
}