using Core;
using Xunit;

namespace Tests;

public class DependencyGraphTests {
    private readonly DependencyGraph _graph = new DependencyGraph();
    private readonly Cell _a = new Cell();
    private readonly Cell _b = new Cell();
    private readonly Cell _c = new Cell();
    private readonly Cell _d = new Cell();

    private static List<Cell> Of(params Cell[] cells) => new List<Cell>(cells);

    // edges

    [Fact]
    public void EdgeIsVisibleFromBothEnds() {
        _graph.SetDependencies(_a, Of(_b));

        Assert.Equal(Of(_b), _graph.GetPrecedents(_a));
        Assert.Equal(Of(_a), _graph.GetDependents(_b));
    }

    [Fact]
    public void UnknownCellHasNoEdges() {
        Assert.Empty(_graph.GetPrecedents(_a));
        Assert.Empty(_graph.GetDependents(_a));
    }

    [Fact]
    public void RepeatedReferenceIsStoredOnce() {
        _graph.SetDependencies(_a, Of(_b, _b, _b));

        Assert.Single(_graph.GetPrecedents(_a));
        Assert.Single(_graph.GetDependents(_b));
    }

    [Fact]
    public void OneCellCanDependOnSeveral() {
        _graph.SetDependencies(_a, Of(_b, _c));

        Assert.Equal(2, _graph.GetPrecedents(_a).Count);
        Assert.Contains(_a, _graph.GetDependents(_b));
        Assert.Contains(_a, _graph.GetDependents(_c));
    }

    [Fact]
    public void NewDependenciesReplaceTheOldOnes() {
        _graph.SetDependencies(_a, Of(_b));
        _graph.SetDependencies(_a, Of(_c));

        Assert.Equal(Of(_c), _graph.GetPrecedents(_a));
        Assert.Empty(_graph.GetDependents(_b));
        Assert.Equal(Of(_a), _graph.GetDependents(_c));
    }

    [Fact]
    public void ClearingDependenciesRemovesTheBackLink() {
        _graph.SetDependencies(_a, Of(_b));
        _graph.SetDependencies(_a, Of());

        Assert.Empty(_graph.GetPrecedents(_a));
        Assert.Empty(_graph.GetDependents(_b));
    }

    [Fact]
    public void ReturnedListsAreCopies() {
        _graph.SetDependencies(_a, Of(_b));

        _graph.GetPrecedents(_a).Add(_c);
        _graph.GetDependents(_b).Add(_c);

        Assert.Single(_graph.GetPrecedents(_a));
        Assert.Single(_graph.GetDependents(_b));
    }

    // cycle search

    [Fact]
    public void CellThatReferencesItselfIsACycle() {
        _graph.SetDependencies(_a, Of(_a));
        Assert.True(_graph.HasCycle(_a));
    }

    [Fact]
    public void TwoCellsReferencingEachOtherAreACycle() {
        _graph.SetDependencies(_a, Of(_b));
        _graph.SetDependencies(_b, Of(_a));

        Assert.True(_graph.HasCycle(_a));
        Assert.True(_graph.HasCycle(_b));
    }

    [Fact]
    public void LongCycleIsFound() {
        _graph.SetDependencies(_a, Of(_b));
        _graph.SetDependencies(_b, Of(_c));
        _graph.SetDependencies(_c, Of(_d));
        _graph.SetDependencies(_d, Of(_a));

        Assert.True(_graph.HasCycle(_a));
    }

    [Fact]
    public void ChainWithoutCycleIsAccepted() {
        _graph.SetDependencies(_a, Of(_b));
        _graph.SetDependencies(_b, Of(_c));

        Assert.False(_graph.HasCycle(_a));
        Assert.False(_graph.HasCycle(_b));
        Assert.False(_graph.HasCycle(_c));
    }

    [Fact]
    public void DiamondIsNotACycle() {
        _graph.SetDependencies(_d, Of(_b, _c));
        _graph.SetDependencies(_b, Of(_a));
        _graph.SetDependencies(_c, Of(_a));

        Assert.False(_graph.HasCycle(_d));
    }

    [Fact]
    public void CycleSomewhereElseDoesNotAffectThisCell() {
        _graph.SetDependencies(_b, Of(_c));
        _graph.SetDependencies(_c, Of(_b));
        _graph.SetDependencies(_a, Of());

        Assert.False(_graph.HasCycle(_a));
        Assert.True(_graph.HasCycle(_b));
    }

    [Fact]
    public void UnknownCellHasNoCycle() {
        Assert.False(_graph.HasCycle(_a));
    }

    // topological order

    [Fact]
    public void ChainIsOrderedFromSourceToTarget() {
        _graph.SetDependencies(_a, Of());
        _graph.SetDependencies(_b, Of(_a));
        _graph.SetDependencies(_c, Of(_b));

        Assert.Equal(Of(_a, _b, _c), _graph.TopologicalOrder());
    }

    [Fact]
    public void EveryPrecedentComesBeforeItsDependent() {
        _graph.SetDependencies(_a, Of());
        _graph.SetDependencies(_b, Of(_a));
        _graph.SetDependencies(_c, Of(_a));
        _graph.SetDependencies(_d, Of(_b, _c));

        List<Cell> order = _graph.TopologicalOrder();

        Assert.True(order.IndexOf(_a) < order.IndexOf(_b));
        Assert.True(order.IndexOf(_a) < order.IndexOf(_c));
        Assert.True(order.IndexOf(_b) < order.IndexOf(_d));
        Assert.True(order.IndexOf(_c) < order.IndexOf(_d));
    }

    [Fact]
    public void IndependentCellsAreAllIncluded() {
        _graph.SetDependencies(_a, Of());
        _graph.SetDependencies(_b, Of());

        Assert.Equal(2, _graph.TopologicalOrder().Count);
    }

    [Fact]
    public void OnlyRegisteredCellsTakePartInTheOrder() {
        _graph.SetDependencies(_b, Of(_a));

        Assert.Equal(Of(_b), _graph.TopologicalOrder());
    }

    [Fact]
    public void CellsInsideACycleAreLeftOutOfTheOrder() {
        _graph.SetDependencies(_a, Of(_b));
        _graph.SetDependencies(_b, Of(_a));

        Assert.Empty(_graph.TopologicalOrder());
    }

    [Fact]
    public void EmptyGraphGivesEmptyOrder() {
        Assert.Empty(_graph.TopologicalOrder());
    }
}