using Xunit;
using ZenECS.Core;

public sealed class World_EntityLifecycle
{
    [Fact]
    public void Destroy_Recycle_Generation_Increments()
    {
        var w = new World();
        var e1 = w.CreateEntity();
        int id  = e1.Id;
        int gen1 = e1.Gen;

        Assert.True(w.IsAlive(e1));

        w.DestroyEntity(e1);
        Assert.False(w.IsAlive(e1));

        // Recreate; the world may reuse the id, but generation must increase.
        var e2 = w.CreateEntity(id); // ask same fixed id to stress the generation rule
        Assert.True(w.IsAlive(e2));
        Assert.Equal(id, e2.Id);
        Assert.True(e2.Gen > gen1);

        // Old handle must not be valid
        Assert.False(w.IsAlive(e1));
    }
}