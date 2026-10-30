using System.Numerics;
using Core;
using Xunit;

namespace Tests;

public class ValueTests {
    [Fact]
    public void EmptyHasEmptyKind() {
        Assert.Equal(ValueKind.Empty, Value.Empty.Kind);
    }

    [Fact]
    public void EmptyPrintsAsEmptyString() {
        Assert.Equal("", Value.Empty.ToString());
    }

    [Fact]
    public void NumberKeepsItsValue() {
        Value v = Value.FromNumber(42);
        Assert.Equal(ValueKind.Number, v.Kind);
        Assert.Equal(new BigInteger(42), v.AsNumber());
    }

    [Fact]
    public void BooleanKeepsItsValue() {
        Assert.True(Value.FromBool(true).AsBool());
        Assert.False(Value.FromBool(false).AsBool());
    }

    [Theory]
    [InlineData(true, "Істина")]
    [InlineData(false, "Хиба")]
    public void BooleanPrintsInUkrainian(bool flag, string expected) {
        Assert.Equal(expected, Value.FromBool(flag).ToString());
    }

    [Fact]
    public void NumberIsNotLimitedByLong() {
        BigInteger huge = BigInteger.Pow(10, 40);
        Assert.Equal(huge, Value.FromNumber(huge).AsNumber());
    }

    [Fact]
    public void AsNumberRejectsBoolean() {
        Exception error = Assert.ThrowsAny<Exception>(() => Value.FromBool(true).AsNumber());
        Assert.Contains("Очікувалося число", error.Message);
    }

    [Fact]
    public void AsBoolRejectsNumber() {
        Exception error = Assert.ThrowsAny<Exception>(() => Value.FromNumber(1).AsBool());
        Assert.Contains("Очікувалося логічне значення", error.Message);
    }

    [Fact]
    public void AsNumberRejectsEmpty() {
        Exception error = Assert.ThrowsAny<Exception>(() => Value.Empty.AsNumber());
        Assert.Contains("порожню клітинку", error.Message);
    }
}