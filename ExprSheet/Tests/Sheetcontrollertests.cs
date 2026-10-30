using Core;
using Xunit;

namespace Tests;

public class SheetControllerTests {
    private readonly SheetController _controller = new SheetController();

    public SheetControllerTests() {
        _controller.NewSheet(4, 4);
    }

    private Cell At(string name) => _controller.Sheet.GetCell(name);

    private bool Set(string name, string formula) => _controller.SetCellFormula(At(name), formula);

    private int Number(string name) => (int)At(name).Value.AsNumber();

    // basic input

    [Fact]
    public void NewControllerHasASheet() {
        SheetController fresh = new SheetController();
        Assert.True(fresh.Sheet.Rows > 0);
        Assert.True(fresh.Sheet.Cols > 0);
        Assert.Null(fresh.Selected);
    }

    [Fact]
    public void ConstantIsStoredAndComputed() {
        Assert.True(Set("A1", "2 + 3"));
        Assert.Equal(5, Number("A1"));
        Assert.False(At("A1").HasError());
    }

    [Fact]
    public void FormulaTextIsKeptAsTyped() {
        Set("A1", "2 + 3");
        Assert.Equal("2 + 3", At("A1").Formula);
    }

    [Fact]
    public void BadFormulaIsReportedAndReturnsFalse() {
        Assert.False(Set("A1", "2 +"));
        Assert.True(At("A1").HasError());
        Assert.True(At("A1").ErrorPos >= 0);
    }

    [Fact]
    public void ErrorIsClearedWhenFormulaIsFixed() {
        Set("A1", "2 +");
        Assert.True(Set("A1", "2 + 2"));
        Assert.False(At("A1").HasError());
        Assert.Equal(-1, At("A1").ErrorPos);
    }

    [Fact]
    public void ClearCellEmptiesEverything() {
        Set("A1", "5");
        _controller.ClearCell(At("A1"));

        Assert.Equal("", At("A1").Formula);
        Assert.Equal(ValueKind.Empty, At("A1").Value.Kind);
        Assert.False(At("A1").HasError());
    }

    // recalculation

    [Fact]
    public void DependentCellIsComputed() {
        Set("A1", "2");
        Set("B1", "A1 * 3");
        Assert.Equal(6, Number("B1"));
    }

    [Fact]
    public void DependentCellIsRecomputedWhenSourceChanges() {
        Set("A1", "2");
        Set("B1", "A1 * 3");
        Set("A1", "5");
        Assert.Equal(15, Number("B1"));
    }

    [Fact]
    public void WholeChainIsRecomputed() {
        Set("A1", "1");
        Set("B1", "A1 + 1");
        Set("C1", "B1 + 1");
        Set("D1", "C1 + 1");

        Set("A1", "10");

        Assert.Equal(13, Number("D1"));
    }

    [Fact]
    public void FormulaMayBeTypedBeforeTheCellItNeeds() {
        Set("B1", "A1 * 3");
        Assert.True(At("B1").HasError());

        Set("A1", "2");

        Assert.False(At("B1").HasError());
        Assert.Equal(6, Number("B1"));
    }

    [Fact]
    public void ErrorSpreadsToDependentCells() {
        Set("A1", "1 / 0");
        Set("B1", "A1 + 1");

        Assert.Contains("Ділення на нуль", At("A1").ErrorText);
        Assert.Contains("містить помилку", At("B1").ErrorText);
    }

    [Fact]
    public void ReferenceToEmptyCellIsAnError() {
        Set("B1", "A1 + 1");
        Assert.Contains("порожня", At("B1").ErrorText);
    }

    [Fact]
    public void ReferenceOutsideTheSheetIsAnError() {
        Set("A1", "Z9 + 1");
        Assert.Contains("поза межами", At("A1").ErrorText);
    }

    // cycles

    [Fact]
    public void CellReferencingItselfIsReportedAsCycle() {
        Assert.False(Set("A1", "A1 + 1"));
        Assert.Contains("Циклічне посилання", At("A1").ErrorText);
        Assert.Null(_controller.GetTree(At("A1")));
    }

    [Fact]
    public void MutualReferencesLeaveBothCellsInError() {
        Set("A1", "B1");
        Assert.False(Set("B1", "A1"));

        Assert.True(At("A1").HasError());
        Assert.True(At("B1").HasError());
    }

    [Fact]
    public void BreakingACycleRestoresBothCells() {
        Set("A1", "B1");
        Set("B1", "A1");

        Set("B1", "7");

        Assert.False(At("B1").HasError());
        Assert.False(At("A1").HasError());
        Assert.Equal(7, Number("A1"));
    }

    // length of the formula

    [Fact]
    public void VeryLongFormulaIsRejectedBeforeParsing() {
        Assert.False(Set("A1", new string('1', 5001)));
        Assert.Contains("довший за", At("A1").ErrorText);
        Assert.Null(_controller.GetTree(At("A1")));
    }

    [Fact]
    public void FormulaAtTheLimitIsAccepted() {
        Assert.True(Set("A1", new string('1', 5000)));
    }

    // resizing

    [Fact]
    public void RowIsAddedAtTheGivenIndex() {
        Assert.True(_controller.AddRow(1));
        Assert.Equal(5, _controller.Sheet.Rows);
    }

    [Fact]
    public void RowCanBeAppendedAtTheEnd() {
        Assert.True(_controller.AddRow(_controller.Sheet.Rows));
        Assert.Equal(5, _controller.Sheet.Rows);
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(99)]
    public void RowIndexOutsideTheSheetIsRefused(int index) {
        Assert.False(_controller.AddRow(index));
        Assert.Equal(4, _controller.Sheet.Rows);
    }

    [Fact]
    public void ColumnIsAdded() {
        Assert.True(_controller.AddColumn(0));
        Assert.Equal(5, _controller.Sheet.Cols);
    }

    [Fact]
    public void RowIsRemoved() {
        Assert.True(_controller.RemoveRow(0));
        Assert.Equal(3, _controller.Sheet.Rows);
    }

    [Fact]
    public void LastRowCannotBeRemoved() {
        _controller.NewSheet(1, 1);
        Assert.False(_controller.RemoveRow(0));
        Assert.Equal(1, _controller.Sheet.Rows);
    }

    [Fact]
    public void LastColumnCannotBeRemoved() {
        _controller.NewSheet(1, 1);
        Assert.False(_controller.RemoveColumn(0));
        Assert.Equal(1, _controller.Sheet.Cols);
    }

    [Fact]
    public void SheetCannotGrowBeyondTheRowLimit() {
        _controller.NewSheet(Sheet.MaxRows, 2);
        Assert.False(_controller.AddRow(0));
        Assert.Equal(Sheet.MaxRows, _controller.Sheet.Rows);
    }

    [Fact]
    public void SheetCannotGrowBeyondTheColumnLimit() {
        _controller.NewSheet(2, Sheet.MaxCols);
        Assert.False(_controller.AddColumn(0));
        Assert.Equal(Sheet.MaxCols, _controller.Sheet.Cols);
    }

    [Fact]
    public void ReferencesArePositionalSoInsertingARowShiftsTheirMeaning() {
        Cell source = At("A1");
        Cell user = At("B1");
        Set("A1", "1");
        Set("B1", "A1 + 1");
        Assert.Equal(2, (int)user.Value.AsNumber());

        _controller.AddRow(0);

        Assert.Equal("A2", source.Name);
        Assert.True(user.HasError());
    }

    [Fact]
    public void SelectionIsDroppedWhenTheSelectedCellDisappears() {
        _controller.SelectCell("A4");
        Assert.NotNull(_controller.Selected);

        _controller.RemoveRow(3);

        Assert.Null(_controller.Selected);
    }

    // assigning

    [Fact]
    public void AssignmentWritesIntoTheTargetCell() {
        Assert.True(Set("A1", "B1 := 7"));
        Assert.Equal(7, Number("A1"));
        Assert.Equal(7, Number("B1"));
    }

    [Fact]
    public void TargetFollowsTheSource() {
        Set("A1", "B1 := 7");
        Set("A1", "B1 := 9");
        Assert.Equal(9, Number("B1"));
    }

    [Fact]
    public void AssignmentCanUseOtherCells() {
        Set("A1", "3");
        Set("A2", "B2 := A1 * 2");
        Assert.Equal(6, Number("B2"));
    }

    [Fact]
    public void TargetIsFreedWhenTheAssignmentIsRemoved() {
        Set("A1", "B1 := 7");
        _controller.ClearCell(At("A1"));
        Assert.Equal(ValueKind.Empty, At("B1").Value.Kind);
    }

    [Fact]
    public void CellCannotAssignToItself() {
        Assert.False(Set("A1", "A1 := 1"));
        Assert.Contains("сама собі", At("A1").ErrorText);
    }

    [Fact]
    public void TargetWithItsOwnContentIsRefused() {
        Set("B1", "1");
        Assert.False(Set("A1", "B1 := 7"));
        Assert.Contains("власний вміст", At("A1").ErrorText);
    }

    [Fact]
    public void SecondWriterIntoTheSameCellIsRefused() {
        Set("A1", "C1 := 1");
        Assert.False(Set("B1", "C1 := 2"));
        Assert.Contains("вже пише", At("B1").ErrorText);
        Assert.Equal(1, Number("C1"));
    }

    [Fact]
    public void TargetOutsideTheSheetIsRefused() {
        Assert.False(Set("A1", "Z9 := 1"));
        Assert.Contains("поза межами", At("A1").ErrorText);
    }

    [Fact]
    public void ErrorOfTheSourceReachesTheTarget() {
        Set("A1", "B1 := 1 / 0");
        Assert.True(At("B1").HasError());
    }

    // conditional operator

    [Fact]
    public void ConditionalFillsTheCellWhenConditionHolds() {
        Set("A1", "5");
        Set("B1", "if A1 <= 10 then 777");
        Assert.Equal(777, Number("B1"));
    }

    [Fact]
    public void ConditionalLeavesTheCellEmptyWhenConditionFails() {
        Set("A1", "50");
        Set("B1", "if A1 <= 10 then 777");
        Assert.Equal(ValueKind.Empty, At("B1").Value.Kind);
        Assert.False(At("B1").HasError());
    }

    // display modes

    [Fact]
    public void ValueModeIsTheDefault() {
        Assert.Equal(DisplayMode.Value, _controller.Mode);
    }

    [Fact]
    public void ModeIsSwitched() {
        _controller.SetDisplayMode(DisplayMode.Expression);
        Assert.Equal(DisplayMode.Expression, _controller.Mode);
    }

    [Fact]
    public void CellDecidesWhatToShowItself() {
        Set("A1", "2 + 3");
        Assert.Equal("5", At("A1").Display(DisplayMode.Value));
        Assert.Equal("2 + 3", At("A1").Display(DisplayMode.Expression));
    }

    [Fact]
    public void BrokenCellShowsErrorMarkerInValueMode() {
        Set("A1", "2 +");
        Assert.Equal("ПОМИЛКА", At("A1").Display(DisplayMode.Value));
    }

    // cell selection

    [Fact]
    public void SelectCellRemembersTheCell() {
        Cell selected = _controller.SelectCell("B2");
        Assert.Same(selected, _controller.Selected);
        Assert.Equal("B2", selected.Name);
    }

    [Fact]
    public void NewSheetDropsTheSelection() {
        _controller.SelectCell("B2");
        _controller.NewSheet(2, 2);
        Assert.Null(_controller.Selected);
    }

    // tree, tracing, dependencies

    [Fact]
    public void TreeIsAvailableForAValidFormula() {
        Set("A1", "2 + 3");
        Node? tree = _controller.GetTree(At("A1"));
        Assert.NotNull(tree);
        Assert.Contains("операція +", tree!.ToTree(0));
    }

    [Fact]
    public void ThereIsNoTreeForAnEmptyCell() {
        Assert.Null(_controller.GetTree(At("A1")));
    }

    [Fact]
    public void ThereIsNoTreeForABrokenFormula() {
        Set("A1", "2 +");
        Assert.Null(_controller.GetTree(At("A1")));
    }

    [Fact]
    public void TraceEndsWithTheResult() {
        Set("A1", "2 + 3");
        Assert.Equal("результат: 5", _controller.GetTrace(At("A1"))[^1]);
    }

    [Fact]
    public void TraceIsEmptyForAnEmptyCell() {
        Assert.Empty(_controller.GetTrace(At("A1")));
    }

    [Fact]
    public void DependenciesAreVisibleInBothDirections() {
        Set("A1", "1");
        Set("B1", "A1 + 1");

        Assert.Contains(At("A1"), _controller.GetDependencies(At("B1")));
        Assert.Contains(At("B1"), _controller.GetDependents(At("A1")));
    }

    // saving through the interface

    [Fact]
    public void SheetIsSavedThroughTheInterface() {
        MemoryStorage storage = new MemoryStorage();
        Set("A1", "2 + 2");

        _controller.Save(storage, "таблиця.json");

        Assert.Equal(1, storage.SaveCount);
    }

    [Fact]
    public void LoadedSheetIsRecalculatedFromFormulas() {
        MemoryStorage storage = new MemoryStorage();
        Set("A1", "2 + 2");
        Set("B1", "A1 * 3");
        _controller.Save(storage, "таблиця.json");

        SheetController other = new SheetController();
        other.Load(storage, "таблиця.json");

        Assert.Equal("2 + 2", other.Sheet.GetCell("A1").Formula);
        Assert.Equal(12, (int)other.Sheet.GetCell("B1").Value.AsNumber());
    }

    [Fact]
    public void LoadingAMissingFileRaisesStorageException() {
        MemoryStorage storage = new MemoryStorage();
        Assert.Throws<StorageException>(() => _controller.Load(storage, "немає.json"));
    }
}