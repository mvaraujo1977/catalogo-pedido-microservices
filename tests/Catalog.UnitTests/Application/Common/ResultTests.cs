using Catalog.Application.Common;

namespace Catalog.UnitTests.Application.Common;

public class ResultTests
{
    [Fact]
    public void Success_SemValor_IndicaSucessoSemErro()
    {
        var result = Result.Success();

        Assert.True(result.IsSuccess);
        Assert.Null(result.Error);
    }

    [Fact]
    public void Failure_SemValor_IndicaFalhaComErro()
    {
        var result = Result.Failure("deu ruim");

        Assert.False(result.IsSuccess);
        Assert.Equal("deu ruim", result.Error);
    }

    [Fact]
    public void Success_ComValor_CarregaOValor()
    {
        var result = Result<string>.Success("ok");

        Assert.True(result.IsSuccess);
        Assert.Equal("ok", result.Value);
        Assert.Null(result.Error);
    }

    [Fact]
    public void Failure_ComValor_DeixaValueComDefault()
    {
        var result = Result<string>.Failure("deu ruim");

        Assert.False(result.IsSuccess);
        Assert.Null(result.Value);
        Assert.Equal("deu ruim", result.Error);
    }

    [Fact]
    public void Failure_ComTipoValor_DeixaValueComDefaultDoTipo()
    {
        var result = Result<int>.Failure("deu ruim");

        Assert.Equal(0, result.Value);
    }

    [Fact]
    public void ResultGenerico_EhUmResult()
    {
        Result result = Result<string>.Success("ok");

        Assert.True(result.IsSuccess);
    }
}
