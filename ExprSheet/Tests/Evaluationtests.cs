using System.Numerics;
using Core;
using Xunit;

namespace Tests;

public class EvaluationTests {

    private static Sheet SheetWith(params (string name, int value)[] cells) {
        Sheet sheet = new Sheet(5, 5);
        foreach ((string name, int value) in cells)
            sheet.SetValue(sheet.GetCell(name), Value.FromNumber(value));
        return sheet;
    }

    // arithmetic

    [Theory]
    [InlineData("2 + 3", 5)]
    [InlineData("10 - 4", 6)]
    [InlineData("6 * 7", 42)]
    [InlineData("12 / 4", 3)]
    [InlineData("2 ^ 10", 1024)]
    [InlineData("2 ^ 0", 1)]
    [InlineData("-5 + 8", 3)]
    [InlineData("2 + 3 * 4", 14)]
    [InlineData("(2 + 3) * 4", 20)]
    public void ArithmeticIsComputed(string source, int expected) {
        Assert.Equal(expected, Expr.Number(source));
    }

    [Theory]
    [InlineData("10 / 3", 3)]
    [InlineData("-7 / 2", -3)]
    public void DivisionIsIntegerAndTruncatesTowardsZero(string source, int expected) {
        Assert.Equal(expected, Expr.Number(source));
    }

    [Fact]
    public void BigNumbersDoNotOverflow() {
        Assert.Equal(BigInteger.Pow(2, 1000), Expr.Eval("2 ^ 1000").AsNumber());
    }

    [Fact]
    public void DivisionByZeroIsReported() {
        Assert.Equal("Ділення на нуль", Expr.EvalError("1 / 0"));
    }

    // how power is protected

    [Fact]
    public void NegativeExponentIsRejected() {
        Assert.Contains("не ціле число", Expr.EvalError("2 ^ -1"));
    }

    [Fact]
    public void HugeExponentIsRejected() {
        Assert.Contains("Завеликий показник", Expr.EvalError("2 ^ 200000"));
    }

    [Fact]
    public void HugeResultIsRejectedEvenWhenExponentIsAllowed() {
        Assert.Contains("Результат піднесення", Expr.EvalError("10 ^ 50000"));
    }

    // comparison and logic

    [Theory]
    [InlineData("1 <= 2", true)]
    [InlineData("2 <= 1", false)]
    [InlineData("2 <= 2", true)]
    [InlineData("2 >= 1", true)]
    [InlineData("1 >= 2", false)]
    [InlineData("1 <> 2", true)]
    [InlineData("2 <> 2", false)]
    public void ComparisonsReturnBooleans(string source, bool expected) {
        Assert.Equal(expected, Expr.Bool(source));
    }

    [Fact]
    public void ComparisonResultIsBooleanKind() {
        Assert.Equal(ValueKind.Boolean, Expr.Eval("1 <= 2").Kind);
    }

    [Theory]
    [InlineData("not (1 <= 2)", false)]
    [InlineData("not (2 <= 1)", true)]
    public void NotInvertsBoolean(string source, bool expected) {
        Assert.Equal(expected, Expr.Bool(source));
    }

    [Fact]
    public void BooleansCanBeComparedForInequality() {
        Assert.True(Expr.Bool("(1 <= 2) <> (2 <= 1)"));
    }

    // strict typification

    [Fact]
    public void NumberIsNotAcceptedAsCondition() {
        Assert.Contains("Очікувалося логічне значення", Expr.EvalError("not 5"));
    }

    [Fact]
    public void BooleanIsNotAcceptedAsNumber() {
        Assert.Contains("Очікувалося число", Expr.EvalError("(1 <= 2) + 1"));
    }

    [Fact]
    public void BooleanCannotBeNegated() {
        Assert.Contains("Очікувалося число", Expr.EvalError("-(1 <= 2)"));
    }

    [Fact]
    public void ValuesOfDifferentKindsCannotBeCompared() {
        Assert.Contains("різних типів", Expr.EvalError("(1 <= 2) <> 3"));
    }

    // cell references

    [Fact]
    public void CellValueIsTakenFromTheSheet() {
        Assert.Equal(7, Expr.Number("A1 + 2", SheetWith(("A1", 5))));
    }

    [Fact]
    public void SeveralCellsCanBeUsedAtOnce() {
        Assert.Equal(30, Expr.Number("A1 * B2", SheetWith(("A1", 5), ("B2", 6))));
    }

    [Fact]
    public void EmptyCellInExpressionIsAnError() {
        Assert.Contains("порожня", Expr.EvalError("A1 + 1", new Sheet(3, 3)));
    }

    [Fact]
    public void ReferenceOutsideTheSheetIsAnError() {
        Assert.Contains("поза межами", Expr.EvalError("Z9 + 1", new Sheet(3, 3)));
    }

    // conditional operator

    [Fact]
    public void ThenBranchIsReturnedWhenConditionIsTrue() {
        Assert.Equal(7, Expr.Number("if A1 <= 10 then 7", SheetWith(("A1", 5))));
    }

    [Fact]
    public void EmptyValueIsReturnedWhenConditionIsFalse() {
        Assert.Equal(ValueKind.Empty, Expr.Eval("if A1 <= 1 then 7", SheetWith(("A1", 5))).Kind);
    }

    [Fact]
    public void ThenBranchIsNotEvaluatedWhenConditionIsFalse() {
        Assert.Equal(ValueKind.Empty, Expr.Eval("if A1 <= 1 then 1 / 0", SheetWith(("A1", 5))).Kind);
    }

    [Fact]
    public void ConditionMustBeBoolean() {
        Assert.Contains("Очікувалося логічне значення", Expr.EvalError("if 5 then 7", new Sheet(3, 3)));
    }

    // assignment

    [Fact]
    public void AssignmentReturnsTheValueOfItsRightSide() {
        Assert.Equal(7, Expr.Number("B2 := 3 + 4", new Sheet(3, 3)));
    }

    [Fact]
    public void AssignmentNodeDoesNotWriteIntoTheSheetByItself() {
        Sheet sheet = new Sheet(3, 3);
        Expr.Eval("B2 := 7", sheet);
        Assert.Equal(ValueKind.Empty, sheet.GetCell("B2").Value.Kind);
    }

    // stack overflow protection

    [Fact]
    public void TooDeepTreeIsRejectedDuringEvaluation() {
        string source = string.Join(" + ", Enumerable.Repeat("1", 600));
        Assert.Contains("Занадто глибока", Expr.EvalError(source));
    }

    // step-by-step calculation

    [Fact]
    public void TraceEndsWithTheResult() {
        Evaluator evaluator = new Evaluator();
        List<string> trace = evaluator.EvaluateWithTrace(Expr.Parse("1 + 2"), new Sheet(2, 2));
        Assert.Equal("результат: 3", trace[^1]);
    }

    [Fact]
    public void TraceShowsIntermediateSteps() {
        Evaluator evaluator = new Evaluator();
        List<string> trace = evaluator.EvaluateWithTrace(Expr.Parse("(1 + 2) * 3"), new Sheet(2, 2));
        Assert.Contains("(1 + 2) = 3", trace);
        Assert.Contains("((1 + 2) * 3) = 9", trace);
    }

    [Fact]
    public void TraceEndsWithTheErrorWhenEvaluationFails() {
        Evaluator evaluator = new Evaluator();
        List<string> trace = evaluator.EvaluateWithTrace(Expr.Parse("1 / 0"), new Sheet(2, 2));
        Assert.Equal("помилка: Ділення на нуль", trace[^1]);
        Assert.True(evaluator.HasError());
    }

    [Fact]
    public void EvaluatorClearsItsErrorBetweenRuns() {
        Evaluator evaluator = new Evaluator();
        Sheet sheet = new Sheet(2, 2);

        Assert.Null(evaluator.Evaluate(Expr.Parse("1 / 0"), sheet));
        Assert.True(evaluator.HasError());

        Assert.NotNull(evaluator.Evaluate(Expr.Parse("1 + 1"), sheet));
        Assert.False(evaluator.HasError());
    }
}