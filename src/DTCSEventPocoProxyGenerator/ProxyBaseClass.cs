using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Reflection;

namespace DTCSEventPocoProxyGenerator
{
  public abstract class ProxyBaseClass
  {
    protected void SetFieldsFrom(object o)
    {
      if (o == null) return;
      var sourceProperties = o.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance);
      var targetFields = this.GetType().GetFields(BindingFlags.NonPublic | BindingFlags.Instance);

      List<(PropertyInfo Source, FieldInfo Target)> transferingItems =
          sourceProperties
          .Join(targetFields, s => "_" + s.Name, t => t.Name, (s, t) => (Source: s, Target: t))
          .ToList();

      foreach (var (source, target) in transferingItems)
      {
        target.SetValue(this, source.GetValue(o));
      }
    }


    protected void SetFieldsFrom(object o, List<MapperItem> columnNamePropertyNameMapper)
    {
      if (o == null) return;
      var sourceProperties = o.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance)
        .Select(x=>(Property: x, MapperItem: columnNamePropertyNameMapper.FirstOrDefault(y => y.ColumnName == x.Name)))
        .Where(x => x.MapperItem != null)
        .ToList();
      var targetFields = this.GetType().GetFields(BindingFlags.NonPublic | BindingFlags.Instance);

      List<(PropertyInfo Source, FieldInfo Target)> transferingItems =
          sourceProperties
          .Join(targetFields, s => "_" + s.MapperItem.PropertyName, t => t.Name, (s, t) => (Source: s.Property, Target: t))
          .ToList();

      foreach (var (source, target) in transferingItems)
      {
        target.SetValue(this, source.GetValue(o));
      }
    }

    protected void SetFieldsFrom(DataRow dataRow, List<MapperItem> columnNamePropertyNameMapper)
    {
      if (dataRow == null) return;
      var sourceProperties = dataRow
        .Table
        .Columns
        .Cast<DataColumn>()
        .Select(x => (ColumnName: x.ColumnName, MapperItem: columnNamePropertyNameMapper.FirstOrDefault(y => y.ColumnName == x.ColumnName)))
        .Where(x => x.MapperItem != null)
        .ToList();
      var targetFields = this.GetType().GetFields(BindingFlags.NonPublic | BindingFlags.Instance);

      List<(MapperItem Source, FieldInfo Target)> transferingItems =
          sourceProperties
          .Join(targetFields, s => "_" + s.MapperItem.PropertyName, t => t.Name, (s, t) => (Source: s.MapperItem, Target: t))
          .ToList();

      foreach (var (source, target) in transferingItems)
      {
        target.SetValue(this, dataRow.IsNull(source.ColumnName) ? null : dataRow[source.ColumnName]);
      }
    }
  }
}
