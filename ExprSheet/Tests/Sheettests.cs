using Core;
using Xunit;

namespace Tests;

public class SheetTests {

    // column names

    [Theory]
    [InlineData(0, "A")]
    [InlineData(1, "B")]
    [InlineData(25, "Z")]
    [InlineData(26, "AA")]
    [InlineData(27, "AB")]
    [InlineData(51, "AZ")]
    [InlineData(52, "BA")]
    [InlineData(701, "ZZ")]
    [InlineData(702, "AAA")]
    public void ColumnNameUsesBase26(int index, string expected) {
        Assert.Equal(expected, Sheet.ColumnName(index));
    }

    // creation

    [Fact]
    public void SheetHasRequestedSize() {
        Sheet sheet = new Sheet(3, 2);
        Assert.Equal(3, sheet.Rows);
        Assert.Equal(2, sheet.Cols);
    }

    [Theory]
    [InlineData(0, 0, "A1")]
    [InlineData(0, 1, "B1")]
    [InlineData(2, 1, "B3")]
    public void CellsAreNamedByPosition(int row, int col, string expected) {
        Assert.Equal(expected, new Sheet(3, 2).GetCell(row, col).Name);
    }

    [Fact]
    public void NewCellsAreEmpty() {
        Cell cell = new Sheet(2, 2).GetCell(0, 0);
        Assert.Equal(ValueKind.Empty, cell.Value.Kind);
        Assert.Equal("", cell.Formula);
        Assert.False(cell.HasError());
    }

    // search by name

    [Fact]
    public void CellIsFoundByName() {
        Assert.Equal("B2", new Sheet(3, 3).GetCell("B2").Name);
    }

    [Fact]
    public void NameIsCaseInsensitive() {
        Assert.Equal("B2", new Sheet(3, 3).GetCell("b2").Name);
    }

    [Fact]
    public void GetCellAndGetCellByNameReturnTheSameObject() {
        Sheet sheet = new Sheet(3, 3);
        Assert.Same(sheet.GetCell(1, 1), sheet.GetCell("B2"));
    }

    [Theory]
    [InlineData("D1")]
    [InlineData("A9")]
    public void ReferenceOutsideTheSheetIsRejected(string name) {
        Sheet sheet = new Sheet(3, 3);
        Exception error = Assert.ThrowsAny<Exception>(() => sheet.GetCell(name));
        Assert.Contains("поза межами таблиці", error.Message);
    }

    [Theory]
    [InlineData("1A")]
    [InlineData("A")]
    [InlineData("12")]
    [InlineData("AAAA1")]
    public void MalformedNameIsRejected(string name) {
        Sheet sheet = new Sheet(3, 3);
        Exception error = Assert.ThrowsAny<Exception>(() => sheet.GetCell(name));
        Assert.Contains("Некоректне посилання", error.Message);
    }

    // value

    [Fact]
    public void GetValueReturnsStoredValue() {
        Sheet sheet = new Sheet(2, 2);
        sheet.SetValue(sheet.GetCell("A1"), Value.FromNumber(5));
        Assert.Equal(5, (int)sheet.GetValue("A1").AsNumber());
    }

    [Fact]
    public void GetValueRejectsEmptyCell() {
        Exception error = Assert.ThrowsAny<Exception>(() => new Sheet(2, 2).GetValue("A1"));
        Assert.Contains("порожня", error.Message);
    }

    // resizing

    [Fact]
    public void AddRowGrowsTheSheet() {
        Sheet sheet = new Sheet(2, 2);
        sheet.AddRow(1);
        Assert.Equal(3, sheet.Rows);
        Assert.Equal(2, sheet.Cols);
    }

    [Fact]
    public void AddColumnGrowsEveryRow() {
        Sheet sheet = new Sheet(2, 2);
        sheet.AddColumn(1);
        Assert.Equal(3, sheet.Cols);
        Assert.Equal("C1", sheet.GetCell(0, 2).Name);
    }

    [Fact]
    public void RowCanBeInsertedInTheMiddleNotOnlyAtTheEnd() {
        Sheet sheet = new Sheet(2, 2);
        sheet.SetValue(sheet.GetCell(0, 0), Value.FromNumber(5));

        sheet.AddRow(0);

        Assert.Equal(ValueKind.Empty, sheet.GetCell(0, 0).Value.Kind);
        Assert.Equal(5, (int)sheet.GetCell(1, 0).Value.AsNumber());
    }

    [Fact]
    public void NamesAreReassignedByPositionAfterInsert() {
        Sheet sheet = new Sheet(2, 2);
        Cell first = sheet.GetCell(0, 0);
        Assert.Equal("A1", first.Name);

        sheet.AddRow(0);

        Assert.Equal("A2", first.Name);
        Assert.Same(first, sheet.GetCell(1, 0));
    }

    [Fact]
    public void RemoveRowShrinksTheSheet() {
        Sheet sheet = new Sheet(3, 2);
        sheet.RemoveRow(0);
        Assert.Equal(2, sheet.Rows);
    }

    [Fact]
    public void RemoveColumnShrinksEveryRow() {
        Sheet sheet = new Sheet(2, 3);
        sheet.RemoveColumn(0);
        Assert.Equal(2, sheet.Cols);
    }

    [Fact]
    public void RemovedColumnShiftsTheRemainingNames() {
        Sheet sheet = new Sheet(1, 3);
        Cell second = sheet.GetCell(0, 1);
        Assert.Equal("B1", second.Name);

        sheet.RemoveColumn(0);

        Assert.Equal("A1", second.Name);
    }

    // JSON

    [Fact]
    public void FromJsonRestoresSizeAndFormulas() {
        Sheet sheet = Sheet.FromJson("""
            {"Rows":2,"Cols":2,"Cells":[{"Name":"A1","Formula":"1 + 1"}]}
            """);

        Assert.Equal(2, sheet.Rows);
        Assert.Equal(2, sheet.Cols);
        Assert.Equal("1 + 1", sheet.GetCell("A1").Formula);
        Assert.Equal("", sheet.GetCell("B2").Formula);
    }

    [Fact]
    public void FromJsonSkipsCellsOutsideTheSavedSize() {
        Sheet sheet = Sheet.FromJson("""
            {"Rows":1,"Cols":1,"Cells":[{"Name":"A1","Formula":"1"},{"Name":"Z9","Formula":"2"}]}
            """);

        Assert.Equal(1, sheet.Rows);
        Assert.Equal("1", sheet.GetCell("A1").Formula);
    }

    [Fact]
    public void FromJsonRejectsBrokenJson() {
        StorageException error = Assert.Throws<StorageException>(() => Sheet.FromJson("це не json"));
        Assert.Contains("коректним JSON", error.Message);
    }

    [Fact]
    public void FromJsonRejectsEmptyDocument() {
        StorageException error = Assert.Throws<StorageException>(() => Sheet.FromJson("null"));
        Assert.Contains("порожній", error.Message);
    }

    [Theory]
    [InlineData("""{"Rows":0,"Cols":5,"Cells":[]}""")]
    [InlineData("""{"Rows":5,"Cols":0,"Cells":[]}""")]
    [InlineData("""{"Rows":-1,"Cols":5,"Cells":[]}""")]
    public void FromJsonRejectsImpossibleSize(string json) {
        StorageException error = Assert.Throws<StorageException>(() => Sheet.FromJson(json));
        Assert.Contains("Некоректний розмір", error.Message);
    }

    [Fact]
    public void FromJsonRejectsSheetBiggerThanTheLimit() {
        string json = $$"""{"Rows":{{Sheet.MaxRows + 1}},"Cols":2,"Cells":[]}""";
        StorageException error = Assert.Throws<StorageException>(() => Sheet.FromJson(json));
        Assert.Contains("завелика", error.Message);
    }

    [Fact]
    public void ToJsonStoresOnlyNonEmptyCells() {
        SheetController controller = new SheetController();
        controller.NewSheet(3, 3);
        controller.SetCellFormula(controller.Sheet.GetCell("A1"), "1 + 1");

        string json = controller.Sheet.ToJson();

        Assert.Contains("A1", json);
        Assert.DoesNotContain("B2", json);
    }

    [Fact]
    public void JsonRoundTripKeepsFormulas() {
        SheetController controller = new SheetController();
        controller.NewSheet(3, 3);
        controller.SetCellFormula(controller.Sheet.GetCell("A1"), "2 + 2");
        controller.SetCellFormula(controller.Sheet.GetCell("B2"), "A1 * 3");

        Sheet restored = Sheet.FromJson(controller.Sheet.ToJson());

        Assert.Equal(3, restored.Rows);
        Assert.Equal("2 + 2", restored.GetCell("A1").Formula);
        Assert.Equal("A1 * 3", restored.GetCell("B2").Formula);
    }

    [Fact]
    public void JsonKeepsUkrainianTextUnescaped() {
        SheetController controller = new SheetController();
        controller.NewSheet(2, 2);
        controller.SetCellFormula(controller.Sheet.GetCell("A1"), "привіт");

        Assert.Contains("привіт", controller.Sheet.ToJson());
    }
}