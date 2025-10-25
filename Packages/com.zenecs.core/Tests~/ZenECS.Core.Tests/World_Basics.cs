using Xunit;
using ZenECS.Core;

public sealed class World_Basics
{
    private readonly struct Position { public readonly float X, Y; public Position(float x, float y){ X = x; Y = y; } }

    [Fact]
    public void Add_Read_Replace_Remove()
    {
        var w = new World();
        var e = w.CreateEntity();

        w.Add(e, new Position(1, 2));
        Assert.True(w.Has<Position>(e));

        // Read
        ref readonly var p = ref w.Read<Position>(e);
        Assert.Equal(1, p.X);
        Assert.Equal(2, p.Y);

        // Replace
        w.Replace(e, new Position(9, 8));
        ref readonly var p2 = ref w.Read<Position>(e);
        Assert.Equal(9, p2.X);
        Assert.Equal(8, p2.Y);

        // Remove
        w.Remove<Position>(e);
        Assert.False(w.Has<Position>(e));
    }

    [Fact]
    public void TryRead_And_GetOrAdd()
    {
        var w = new World();
        var e = w.CreateEntity();

        Assert.False(w.TryRead<Position>(e, out var missing));

        ref readonly var init = ref w.GetOrAdd(e, new Position(3, 4));
        Assert.Equal(3, init.X);
        Assert.Equal(4, init.Y);
        Assert.True(w.Has<Position>(e));
    }
}