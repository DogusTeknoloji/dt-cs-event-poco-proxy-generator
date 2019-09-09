using Shouldly;
using System;
using Xunit;

namespace DTCSEventPocoProxyGenerator.Tests
{
  public class InterfaceProxyBuilderTests
  {
    protected static readonly InterfaceProxyBuilder CommonInstance = new InterfaceProxyBuilder();

    [Fact]
    public void GetProxyType_NullInterfaceTypeParameter_ThrowException()
    {
      var exception = Assert.Throws<ArgumentNullException>(() => CommonInstance.GetProxyType(null));
      exception.ShouldBeOfType<ArgumentNullException>();
      exception.ParamName.ShouldBe("interfaceType");
    }

    [Fact]
    public void GetProxyType_NotNullInterfaceTypeParameter_Success()
    {
      var type = CommonInstance.GetProxyType(typeof(IDemo1));

      type.ShouldNotBeNull();
      typeof(IDemo1).IsAssignableFrom(type).ShouldBeTrue();
    }

    [Fact]
    public void GetProxyInstance_NotNullInterfaceTypeParameter_Success()
    {
      var result = CommonInstance.GetProxyInstance(typeof(IDemo1));
      var typedResult = result as IDemo1;

      result.ShouldNotBeNull();
      (result is IDemo1).ShouldBeTrue();
      typedResult.Id.ShouldBe(Guid.Empty);
      typedResult.Name.ShouldBeNull();
    }


    public interface IDemo1
    {
      Guid Id { get; set; }
      string Name { get; set; }
    }

    public interface IDemo2
    {
      Guid Id { get; }
      string Name { get; }
    }

  }
}
