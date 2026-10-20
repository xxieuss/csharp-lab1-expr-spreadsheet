using System.Numerics;
using Core;

Sheet sheet = new Sheet();

// -(2 + 3) * 4
Node tree = new BinaryNode("*", new UnaryNode("-", new BinaryNode("+", new NumberNode(2), new NumberNode(3))), 
new NumberNode(4));

Console.WriteLine($"Вираз:    {tree.ToText()}");
Console.WriteLine($"Значення: {tree.Evaluate(sheet, 0)}");

// 2 ^ 100
Node big = new BinaryNode("^", new NumberNode(2), new NumberNode(100));
Console.WriteLine($"{big.ToText()} = {big.Evaluate(sheet, 0)}");

// 7 / 2
Node div = new BinaryNode("/", new NumberNode(7), new NumberNode(2));
Console.WriteLine($"{div.ToText()} = {div.Evaluate(sheet, 0)}");

try {
    new BinaryNode("/", new NumberNode(1), new NumberNode(0)).Evaluate(sheet, 0);
} catch (InvalidOperationException ex) {
    Console.WriteLine($"Помилка: {ex.Message}");
}