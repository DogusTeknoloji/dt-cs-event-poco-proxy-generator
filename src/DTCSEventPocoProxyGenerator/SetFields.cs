using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Reflection;

namespace DTCSEventPocoProxyGenerator
{
  internal static class SetFields
  {
    public static void FromObject(object target, object source)
    {
      if (target == null) throw new ArgumentNullException(nameof(target));
      if (source == null || target == null) return;
      var sourceProperties = source.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance);
      var targetProperties = target.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance);

      List<(PropertyInfo Source, PropertyInfo Target)> transferingItems =
          sourceProperties
          .Join(targetProperties, s => s.Name, t => t.Name, (s, t) => (Source: s, Target: t))
          .ToList();

      foreach (var (sourceProperty, targetField) in transferingItems)
      {
        targetField.SetValue(target, sourceProperty.GetValue(source));
      }
    }

    public static void FromObjectWithMapping(object target, object source, List<MapperItem> columnNamePropertyNameMapper)
    {
      if (source == null || target == null) return;
      var sourceProperties = source.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance)
        .Select(x => (Property: x, MapperItem: columnNamePropertyNameMapper.FirstOrDefault(y => y.ColumnName == x.Name)))
        .Where(x => x.MapperItem != null)
        .ToList();
      var targetFields = target.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance);

      List<(PropertyInfo Source, PropertyInfo Target)> transferingItems =
          sourceProperties
          .Join(targetFields, s => s.MapperItem.PropertyName, t => t.Name, (s, t) => (Source: s.Property, Target: t))
          .ToList();

      foreach (var (sourceProperty, targetField) in transferingItems)
      {
        targetField.SetValue(target, sourceProperty.GetValue(source));
      }
    }

    public static void FromDataRow(object target, DataRow dataRow, List<MapperItem> columnNamePropertyNameMapper)
    {
      if (dataRow == null || target == null) return;
      var sourceProperties = dataRow
        .Table
        .Columns
        .Cast<DataColumn>()
        .Select(x => (ColumnName: x.ColumnName, MapperItem: columnNamePropertyNameMapper.FirstOrDefault(y => y.ColumnName == x.ColumnName)))
        .Where(x => x.MapperItem != null)
        .ToList();
      var targetFields = target.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance);

      List<(MapperItem Source, PropertyInfo Target)> transferingItems =
          sourceProperties
          .Join(targetFields, s => s.MapperItem.PropertyName, t => t.Name, (s, t) => (Source: s.MapperItem, Target: t))
          .ToList();

      foreach (var (sourceProperty, targetField) in transferingItems)
      {
        targetField.SetValue(target, dataRow.IsNull(sourceProperty.ColumnName) ? null : dataRow[sourceProperty.ColumnName]);
      }
    }
  }
}
