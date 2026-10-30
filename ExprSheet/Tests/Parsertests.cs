using Core;
using Xunit;

namespace Tests;

public class ParserTests {

    // lexer

    [Theory]
    [InlineData("1+2", "(1 + 2)")]
    [InlineData("  1  +  2  ", "(1 + 2)")]
    [InlineData("\t1\t+\t2\t", "(1 + 2)")]
    public void WhitespaceIsIgnored(string source, string expected) {
        Assert.Equal(expected, Expr.Text(source));
    }

    [Theory]
    [InlineData("1 <= 2", "(1 <= 2)")]
    [InlineData("1 >= 2", "(1 >= 2)")]
    [InlineData("1 <> 2", "(1 <> 2)")]
    public void TwoCharacterOperatorsAreReadBeforeSingleOnes(string source, string expected) {
        Assert.Equal(expected, Expr.Text(source));
    }

    [Theory]
    [InlineData("not A1")]
    [InlineData("NOT A1")]
    [InlineData("Not A1")]
    public void KeywordsAreCaseInsensitive(string source) {
        Assert.Equal("(not A1)", Expr.Text(source));
    }

    [Fact]
    public void CellNameIsUppercased() {
        Assert.Equal("(A1 + 1)", Expr.Text("a1 + 1"));
    }

    [Fact]
    public void UnknownCharacterIsRejected() {
        Assert.Contains("Неочікуваний символ", Expr.ParseError("1 # 2"));
    }

    // priority and associativity

    [Theory]
    [InlineData("2 + 3 * 4", "(2 + (3 * 4))")]
    [InlineData("2 * 3 + 4", "((2 * 3) + 4)")]
    [InlineData("2 + 3 <= 4", "((2 + 3) <= 4)")]
    [InlineData("2 * 3 ^ 4", "(2 * (3 ^ 4))")]
    [InlineData("not 1 <= 2", "(not (1 <= 2))")]
    public void StrongerOperationsBindFirst(string source, string expected) {
        Assert.Equal(expected, Expr.Text(source));
    }

    [Theory]
    [InlineData("1 - 2 - 3", "((1 - 2) - 3)")]
    [InlineData("1 + 2 + 3", "((1 + 2) + 3)")]
    [InlineData("8 / 4 / 2", "((8 / 4) / 2)")]
    public void AdditionAndMultiplicationAreLeftAssociative(string source, string expected) {
        Assert.Equal(expected, Expr.Text(source));
    }

    [Fact]
    public void PowerIsRightAssociative() {
        Assert.Equal("(2 ^ (3 ^ 2))", Expr.Text("2 ^ 3 ^ 2"));
    }

    [Fact]
    public void ParenthesesOverridePriority() {
        Assert.Equal("((2 + 3) * 4)", Expr.Text("(2 + 3) * 4"));
    }

    [Theory]
    [InlineData("-5", "(-5)")]
    [InlineData("+5", "(+5)")]
    [InlineData("--5", "(-(-5))")]
    [InlineData("-A1 + 2", "((-A1) + 2)")]
    public void UnarySignsBindTighterThanAddition(string source, string expected) {
        Assert.Equal(expected, Expr.Text(source));
    }

    [Fact]
    public void ComparisonCannotBeChained() {
        Assert.Contains("Зайві символи", Expr.ParseError("1 <= 2 <= 3"));
    }

    // syntax errors

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("1 +")]
    [InlineData("*")]
    public void IncompleteExpressionIsRejected(string source) {
        Assert.NotEqual("", Expr.ParseError(source));
    }

    [Fact]
    public void MissingClosingBracketIsReported() {
        Assert.Contains("закривна дужка", Expr.ParseError("(1 + 2"));
    }

    [Fact]
    public void ExtraTokensAfterExpressionAreReported() {
        Assert.Contains("Зайві символи", Expr.ParseError("1 + 2 3"));
    }

    [Theory]
    [InlineData("abc")]
    [InlineData("A1B")]
    [InlineData("1A")]
    public void NameThatIsNotACellReferenceIsRejected(string source) {
        Assert.NotEqual("", Expr.ParseError(source));
    }

    [Fact]
    public void ErrorPositionPointsAtTheStartOfTheBadToken() {
        //            0123456
        Assert.Equal(4, Expr.ParseErrorPosition("1 + #"));
    }

    [Fact]
    public void ErrorPositionIsMinusOneWhenThereIsNoError() {
        Parser parser = new Parser();
        parser.Parse("1 + 1");
        Assert.Equal(-1, parser.ErrorPos);
        Assert.False(parser.HasError());
    }

    [Fact]
    public void ParserIsReusableAfterAnError() {
        Parser parser = new Parser();
        Assert.Null(parser.Parse("1 +"));
        Assert.NotNull(parser.Parse("1 + 1"));
        Assert.False(parser.HasError());
        Assert.Equal(-1, parser.ErrorPos);
    }

    [Fact]
    public void TooDeepNestingIsRejectedInsteadOfCrashing() {
        string source = new string('(', 600) + "1" + new string(')', 600);
        Assert.Contains("Занадто глибока вкладеність", Expr.ParseError(source));
    }

    [Fact]
    public void LongFlatExpressionIsStillParsed() {
        string source = string.Join(" + ", Enumerable.Repeat("1", 1000));
        Assert.NotNull(Expr.Parse(source));
    }

    // language operators

    [Fact]
    public void AssignmentIsParsed() {
        Node node = Expr.Parse("B5 := 1 + 1");
        AssignNode assign = Assert.IsType<AssignNode>(node);
        Assert.Equal("B5", assign.TargetName);
        Assert.Equal("B5 := (1 + 1)", assign.ToText());
    }

    [Fact]
    public void AssignmentTargetMustBeACellName() {
        Assert.Contains("Ліворуч", Expr.ParseError("1 + 1 := 5"));
    }

    [Fact]
    public void AssignmentCannotBeChained() {
        Assert.NotEqual("", Expr.ParseError("A1 := B1 := 5"));
    }

    [Fact]
    public void ConditionalIsParsed() {
        Node node = Expr.Parse("if A1 <= 10 then 7");
        Assert.IsType<IfNode>(node);
        Assert.Equal("if (A1 <= 10) then 7", node.ToText());
    }

    [Fact]
    public void ConditionalsCanBeNested() {
        Assert.Equal("if (A1 <= 1) then if (B1 <= 2) then 3", Expr.Text("if A1 <= 1 then if B1 <= 2 then 3"));
    }

    [Fact]
    public void ThenIsRequiredAfterCondition() {
        Assert.Contains("then", Expr.ParseError("if A1 <= 10 7"));
    }

    [Fact]
    public void AssignmentIsForbiddenInsideThenBranch() {
        Assert.Contains("на початку вмісту клітинки", Expr.ParseError("if A1 <= 10 then B5 := 1"));
    }

    [Fact]
    public void AssignmentIsForbiddenInsideNestedThenBranchToo() {
        Assert.Contains("на початку вмісту клітинки", Expr.ParseError("if A1 <= 1 then if B1 <= 2 then C1 := 3"));
    }

    // link collection

    [Fact]
    public void CollectRefsFindsEveryReference() {
        List<string> refs = Expr.Parse("A1 + B2 * (C3 - A1)").CollectRefs();
        Assert.Equal(new[] { "A1", "B2", "C3", "A1" }, refs);
    }

    [Fact]
    public void CollectRefsLooksIntoBothPartsOfConditional() {
        List<string> refs = Expr.Parse("if A1 <= 1 then B2").CollectRefs();
        Assert.Contains("A1", refs);
        Assert.Contains("B2", refs);
    }

    [Fact]
    public void CollectRefsIgnoresAssignmentTarget() {
        List<string> refs = Expr.Parse("B5 := A1 + 1").CollectRefs();
        Assert.Equal(new[] { "A1" }, refs);
    }

    [Fact]
    public void CollectRefsIsEmptyForConstants() {
        Assert.Empty(Expr.Parse("2 + 2").CollectRefs());
    }

    // tree

    [Fact]
    public void TreeIsIndentedByLevel() {
        string tree = Expr.Parse("1 + 2").ToTree(0);
        Assert.Contains("операція +", tree);
        Assert.Contains("   число 1", tree);
        Assert.Contains("   число 2", tree);
    }
}