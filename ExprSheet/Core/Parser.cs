using System.Numerics;

namespace Core;

public class Parser {
    private string _text = "";
    private int _pos;
    private string _lexeme = "";
    private int _lexemeStart;
    private bool _allowAssign;

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
        _depth = 0;
        _allowAssign = true;

        try {
            NextLexeme();
            Node node = ParseStatement();
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
            string lower = _lexeme.ToLowerInvariant();
            if (lower == "not" || lower == "if" || lower == "then") _lexeme = lower;
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

        if (char.IsLetter(_lexeme[0])) {
            string name = _lexeme;
            if (!IsCellName(name)) throw Error($"Невідоме ім'я '{name}'");
            NextLexeme();
            return new CellRefNode(name.ToUpperInvariant());
        }

        if (_lexeme == "(") {
            EnterLevel();
            NextLexeme();
            Node inside = ParseLogical();
            if (_lexeme != ")") throw Error("Очікувалася закривна дужка");
            NextLexeme();
            _depth--;
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
            EnterLevel();
            NextLexeme();
            Node right = ParsePower();
            _depth--;
            return new BinaryNode("^", left, right);
        }
        return left;
    }

    private Node ParseUnary() {
        if (_lexeme == "+" || _lexeme == "-") {
            string op = _lexeme;
            EnterLevel();
            NextLexeme();
            Node operand = ParseUnary();
            _depth--;
            return new UnaryNode(op, operand);
        }
        return ParsePrimary();
    }

    private Node ParseLogical() {
        if (_lexeme == "not") {
            EnterLevel();
            NextLexeme();
            Node operand = ParseLogical();
            _depth--;
            return new UnaryNode("not", operand);
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

    private const int MaxDepth = 500;
    private int _depth;

    private void EnterLevel() {
        if (++_depth > MaxDepth) throw Error("Занадто глибока вкладеність виразу");
    }

    private static bool IsCellName(string s) {
        int i = 0;
        while (i < s.Length && char.IsLetter(s[i])) i++;
        if (i == 0 || i == s.Length) return false;
        for (int j = i; j < s.Length; j++)
            if (!char.IsDigit(s[j])) return false;
        return true;
    }

    private Node ParseStatement() {
        if (_lexeme == "if") {
            EnterLevel();
            NextLexeme();
            Node condition = ParseLogical();
            if (_lexeme != "then") throw Error("Очікувалося 'then' після умови");
            NextLexeme();

            bool saved = _allowAssign;
            _allowAssign = false;
            Node thenPart = ParseStatement();
            _allowAssign = saved;

            _depth--;
            return new IfNode(condition, thenPart);
        }

        int startPos = _lexemeStart;
        Node left = ParseLogical();

        if (_lexeme == ":=") {
            if (!_allowAssign)
                throw new SyntaxException("Присвоєння може бути лише на початку вмісту клітинки", startPos);
            if (left is not CellRefNode target)
                throw new SyntaxException("Ліворуч від ':=' має стояти ім'я клітинки", startPos);
            NextLexeme();
            return new AssignNode(target.Name, ParseLogical());
        }

        return left;
    }
}