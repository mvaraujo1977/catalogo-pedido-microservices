using Catalog.Domain.Common;

namespace Catalog.UnitTests.Domain;

public class EntityTests
{
    private sealed class FakeEntity : Entity
    {
        public FakeEntity(Guid id) : base(id) { }
    }

    private sealed class OutraEntity : Entity
    {
        public OutraEntity(Guid id) : base(id) { }
    }

    [Fact]
    public void Equals_MesmoTipoEMesmoId_SaoIguais()
    {
        var id = Guid.NewGuid();

        var a = new FakeEntity(id);
        var b = new FakeEntity(id);

        Assert.True(a.Equals(b));
        Assert.True(a == b);
        Assert.False(a != b);
    }

    [Fact]
    public void Equals_MesmaInstancia_EhIgual()
    {
        var entity = new FakeEntity(Guid.NewGuid());

        Assert.True(entity.Equals(entity));
    }

    [Fact]
    public void Equals_IdsDiferentes_NaoSaoIguais()
    {
        var a = new FakeEntity(Guid.NewGuid());
        var b = new FakeEntity(Guid.NewGuid());

        Assert.False(a.Equals(b));
        Assert.True(a != b);
    }

    [Fact]
    public void Equals_TiposDiferentesComMesmoId_NaoSaoIguais()
    {
        var id = Guid.NewGuid();

        var a = new FakeEntity(id);
        var b = new OutraEntity(id);

        Assert.False(a.Equals(b));
    }

    [Fact]
    public void Equals_ComNull_RetornaFalse()
    {
        var entity = new FakeEntity(Guid.NewGuid());

        Assert.False(entity.Equals(null));
    }

    [Fact]
    public void Equals_ComObjetoDeOutroTipo_RetornaFalse()
    {
        var entity = new FakeEntity(Guid.NewGuid());

        Assert.False(entity.Equals("nao é uma entity"));
    }

    [Fact]
    public void OperadorIgualdade_ComAmbosNull_RetornaTrue()
    {
        FakeEntity? a = null;
        FakeEntity? b = null;

        Assert.True(a == b);
        Assert.False(a != b);
    }

    [Fact]
    public void OperadorIgualdade_ComUmLadoNull_RetornaFalse()
    {
        var a = new FakeEntity(Guid.NewGuid());
        FakeEntity? b = null;

        Assert.False(a == b);
        Assert.False(b == a);
        Assert.True(a != b);
    }

    [Fact]
    public void GetHashCode_MesmoId_ProduzMesmoHash()
    {
        var id = Guid.NewGuid();

        var a = new FakeEntity(id);
        var b = new FakeEntity(id);

        Assert.Equal(a.GetHashCode(), b.GetHashCode());
    }

    [Fact]
    public void EntidadesComMesmoId_ColapsamEmHashSet()
    {
        var id = Guid.NewGuid();

        var set = new HashSet<Entity> { new FakeEntity(id), new FakeEntity(id) };

        Assert.Single(set);
    }
}
