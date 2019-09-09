using Shouldly;
using System;
using System.Collections.Generic;
using Xunit;

namespace DTCSEventPocoProxyGenerator.Tests
{
  public class ProxyBaseClassTests
  {
  }

  public class SetFieldsTests
  {
    [Fact]
    public void FromObject_NullTarget_ThrowException()
    {
      var exception = Assert.Throws<ArgumentNullException>(() => SetFields.FromObject(null, null));
      exception.ShouldBeOfType<ArgumentNullException>();
      exception.ShouldNotBeNull();
      exception.ParamName.ShouldBe("target");
    }

    [Fact]
    public void FromObject_NullSource_DoNothing()
    {
      SetFields.FromObject(GetType(), null);
    }

    [Fact]
    public void FromObject_NotNullSourceAndTarget_Success()
    {
      var target = new Demo();
      var id = Guid.Parse("{6E25FEB7-771E-4194-BAA2-4184F54C4953}");
      var source = new { Id = id, Name = "Alper" };

      SetFields.FromObject(target, source);

      target.Id.ShouldBe(id);
      target.Name.ShouldBe("Alper");
    }


    [Fact]
    public void FromObject_NotNullSourceAndTargetWithMissingProperty_Success()
    {
      var target = new Demo();
      var id = Guid.Parse("{6E25FEB7-771E-4194-BAA2-4184F54C4953}");
      var source = new { Id = id };

      SetFields.FromObject(target, source);

      target.Id.ShouldBe(id);
      target.Name.ShouldBeNull();
    }

    [Fact]
    public void FromObject_NotNullSourceAndTargetWithExtraProperty_Success()
    {
      var target = new Demo();
      var id = Guid.Parse("{6E25FEB7-771E-4194-BAA2-4184F54C4953}");
      var source = new { Id = id, Name = "Alper", Code = 1 };

      SetFields.FromObject(target, source);

      target.Id.ShouldBe(id);
      target.Name.ShouldBe("Alper");
    }


    [Fact]
    public void FromObject_NotNullSourceAndTargetAndMapping_Success()
    {
      var target = new Demo();
      var id = Guid.Parse("{6E25FEB7-771E-4194-BAA2-4184F54C4953}");
      var source = new { id = id, name = "Alper" };
      var mapping = new List<MapperItem>() {
        new MapperItem { ColumnName="id", PropertyName="Id" },
        new MapperItem { ColumnName="name", PropertyName="Name" }
      };

      SetFields.FromObjectWithMapping(target, source, mapping);

      target.Id.ShouldBe(id);
      target.Name.ShouldBe("Alper");
    }



    public class Demo
    {
      public Guid Id { get; set; }
      public string Name { get; set; }
    }
  }
}
