using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;

namespace DTCSEventPocoProxyGenerator
{
  public class InterfaceProxyBuilder
  {
    private readonly ConcurrentDictionary<Type, Type> _interfaceTypeMapper = new ConcurrentDictionary<Type, Type>();

    public InterfaceProxyBuilder()
    {
    }

    public void Generate(params Type[] interfaceTypes)
    {
      var a = interfaceTypes.Where(x => !_interfaceTypeMapper.ContainsKey(x)).AsEnumerable();

      if (a.Any())
      {
        ModuleBuilder moduleBuilder =
            CreateAssemblyAndModule($"__{a.First().Name}__Proxy__");
        foreach (var interfaceType in a)
        {
          var proxyType = CreateType(moduleBuilder, interfaceType);
          _interfaceTypeMapper.TryAdd(interfaceType, proxyType);
        }
      }
    }


    public Type GetProxyType(Type interfaceType)
    {
      if (interfaceType == null) throw new ArgumentNullException(nameof(interfaceType));
      return _interfaceTypeMapper.GetOrAdd(interfaceType, ProxyFactory);
    }

    public Type GetProxyType<TInterface>()
    {
      return this.GetProxyType(typeof(TInterface));
    }

    public object GetProxyInstance(Type interfaceType)
    {
      var type = GetProxyType(interfaceType);
      return Activator.CreateInstance(type);
    }

    public object GetProxyInstance(Type interfaceType, object initialValues)
    {
      return Activator.CreateInstance(GetProxyType(interfaceType), initialValues);
    }

    public object GetProxyInstance(Type interfaceType, object initialValues, List<MapperItem> columnNamePropertyNameMapper)
    {
      return Activator.CreateInstance(GetProxyType(interfaceType), initialValues, columnNamePropertyNameMapper);
    }

    public IEnumerable<object> GetProxyInstances(Type interfaceType, IEnumerable<object> initialValues)
    {
      var proxyType = GetProxyType(interfaceType);
      return initialValues.Select(x => Activator.CreateInstance(proxyType, x)).AsEnumerable();
    }

    public IEnumerable<object> GetProxyInstances(Type interfaceType, IEnumerable<object> initialValues, List<MapperItem> columnNamePropertyNameMapper)
    {
      var proxyType = GetProxyType(interfaceType);
      return initialValues.Select(x => Activator.CreateInstance(proxyType, x, columnNamePropertyNameMapper)).AsEnumerable();
    }

    private static readonly object ProxyFactoryLock = new object();
    private Type ProxyFactory(Type interfaceType)
    {
      lock (ProxyFactoryLock)
      {
        ModuleBuilder moduleBuilder =
            CreateAssemblyAndModule($"__{interfaceType.Name}__Proxy__");
        return CreateType(moduleBuilder, interfaceType);
      }
    }

    private ModuleBuilder CreateAssemblyAndModule(string typeSignature)
    {
      var now = DateTime.Now;
      var assemblyName = new AssemblyName(typeSignature)
      {
        Version = new Version(
          1,
          (now.Year - 2000) * 100 + now.Month,
          now.Day * 100 + now.Hour,
          now.Minute * 100 + now.Second)
      };

      AssemblyBuilder assemblyBuilder = AssemblyBuilder.DefineDynamicAssembly(assemblyName, AssemblyBuilderAccess.RunAndCollect);

      return assemblyBuilder.DefineDynamicModule($"{typeSignature}Module__");
    }

    private TypeBuilder GetTypeBuilder(ModuleBuilder moduleBuilder, Type interfaceType)
    {
      TypeBuilder tb = moduleBuilder.DefineType($"__{interfaceType.Name}__Proxy__",
              TypeAttributes.Public |
              TypeAttributes.Class |
              TypeAttributes.AutoClass |
              TypeAttributes.AnsiClass |
              TypeAttributes.BeforeFieldInit |
              TypeAttributes.AutoLayout,
              typeof(ProxyBaseClass));

      tb.AddInterfaceImplementation(interfaceType);

      return tb;
    }

    private Type CreateType(ModuleBuilder moduleBuilder, Type interfaceType)
    {
      TypeBuilder tb = GetTypeBuilder(moduleBuilder, interfaceType);
      CreateConstructors(tb);

      // NOTE: assuming your list contains Field objects with fields FieldName(string) and FieldType(Type)
      foreach (var property in interfaceType.GetProperties(BindingFlags.Public | BindingFlags.Instance))
        CreateProperty(tb, property);

      Type objectType = tb.CreateTypeInfo().AsType();
      return objectType;
    }

    private void CreateConstructors(TypeBuilder tb)
    {
      tb.DefineDefaultConstructor(MethodAttributes.Public | MethodAttributes.SpecialName | MethodAttributes.RTSpecialName | MethodAttributes.HideBySig);

      var baseDefaultConstructor = typeof(ProxyBaseClass)
                .GetConstructors(BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.FlattenHierarchy)
                .First(x => x.GetParameters().Length == 0);

      CreateObjectParameterConstructor(tb, baseDefaultConstructor);
      CreateObjectWithMappingParameterConstructor(tb, baseDefaultConstructor);
      CreateDataRowParameterConstructor(tb, baseDefaultConstructor);
    }

    private static void CreateObjectParameterConstructor(TypeBuilder tb, ConstructorInfo baseDefaultConstructor)
    {
      var setFieldsFromMethod = typeof(SetFields).GetMethod("FromObject");

      var constructor1 = tb.DefineConstructor(
          MethodAttributes.Public | MethodAttributes.SpecialName | MethodAttributes.RTSpecialName | MethodAttributes.HideBySig,
          CallingConventions.Standard,
          new[] { typeof(object) });

      ILGenerator getIl = constructor1.GetILGenerator();

      getIl.Emit(OpCodes.Ldarg_0);
      getIl.Emit(OpCodes.Call, baseDefaultConstructor);
      getIl.Emit(OpCodes.Nop);
      getIl.Emit(OpCodes.Nop);

      getIl.Emit(OpCodes.Ldarg_0);
      getIl.Emit(OpCodes.Ldarg_1);
      getIl.Emit(OpCodes.Call, setFieldsFromMethod);
      getIl.Emit(OpCodes.Nop);
      getIl.Emit(OpCodes.Ret);
    }

    private static void CreateObjectWithMappingParameterConstructor(TypeBuilder tb, ConstructorInfo baseDefaultConstructor)
    {
      var setFieldsFromMethod = typeof(SetFields).GetMethod("FromObjectWithMapping");

      var constructor1 = tb.DefineConstructor(
          MethodAttributes.Public | MethodAttributes.SpecialName | MethodAttributes.RTSpecialName | MethodAttributes.HideBySig,
          CallingConventions.Standard,
          new[] { typeof(object), typeof(List<MapperItem>) });

      ILGenerator getIl = constructor1.GetILGenerator();

      getIl.Emit(OpCodes.Ldarg_0);
      getIl.Emit(OpCodes.Call, baseDefaultConstructor);
      getIl.Emit(OpCodes.Nop);
      getIl.Emit(OpCodes.Nop);

      getIl.Emit(OpCodes.Ldarg_0);
      getIl.Emit(OpCodes.Ldarg_1);
      getIl.Emit(OpCodes.Ldarg_2);
      getIl.Emit(OpCodes.Call, setFieldsFromMethod);
      getIl.Emit(OpCodes.Nop);
      getIl.Emit(OpCodes.Ret);
    }

    private static void CreateDataRowParameterConstructor(TypeBuilder tb, ConstructorInfo baseDefaultConstructor)
    {
      var setFieldsFromMethod = typeof(SetFields).GetMethod("FromDataRow");

      var constructor1 = tb.DefineConstructor(
          MethodAttributes.Public | MethodAttributes.SpecialName | MethodAttributes.RTSpecialName | MethodAttributes.HideBySig,
          CallingConventions.Standard,
          new[] { typeof(DataRow), typeof(List<MapperItem>) });

      ILGenerator getIl = constructor1.GetILGenerator();

      getIl.Emit(OpCodes.Ldarg_0);
      getIl.Emit(OpCodes.Call, baseDefaultConstructor);
      getIl.Emit(OpCodes.Nop);
      getIl.Emit(OpCodes.Nop);

      getIl.Emit(OpCodes.Ldarg_0);
      getIl.Emit(OpCodes.Ldarg_1);
      getIl.Emit(OpCodes.Ldarg_2);
      getIl.Emit(OpCodes.Call, setFieldsFromMethod);
      getIl.Emit(OpCodes.Nop);
      getIl.Emit(OpCodes.Ret);
    }

    private void CreateProperty(TypeBuilder tb, PropertyInfo property)
    {
      // The property "set" and property "get" methods require a special
      // set of attributes.
      MethodAttributes getSetAttr = MethodAttributes.Public |
          MethodAttributes.SpecialName | MethodAttributes.HideBySig | MethodAttributes.Virtual | MethodAttributes.Final | MethodAttributes.NewSlot;

      FieldBuilder fieldBuilder = tb.DefineField("_" + property.Name, property.PropertyType, FieldAttributes.Private);

      PropertyBuilder propertyBuilder = tb.DefineProperty(property.Name, System.Reflection.PropertyAttributes.HasDefault, property.PropertyType, null);

      MethodBuilder getPropMthdBldr = tb.DefineMethod(
          "get_" + property.Name,
          getSetAttr,
          property.PropertyType,
          Type.EmptyTypes);

      ILGenerator getIl = getPropMthdBldr.GetILGenerator();

      getIl.Emit(OpCodes.Ldarg_0);
      getIl.Emit(OpCodes.Ldfld, fieldBuilder);
      getIl.Emit(OpCodes.Ret);

      MethodBuilder setPropMthdBldr =
          tb.DefineMethod("set_" + property.Name,
            getSetAttr,
            null
            , new[] { property.PropertyType });

      ILGenerator setIl = setPropMthdBldr.GetILGenerator();

      setIl.Emit(OpCodes.Ldarg_0);
      setIl.Emit(OpCodes.Ldarg_1);
      setIl.Emit(OpCodes.Stfld, fieldBuilder);
      setIl.Emit(OpCodes.Ret);

      propertyBuilder.SetGetMethod(getPropMthdBldr);
      propertyBuilder.SetSetMethod(setPropMthdBldr);
    }
  }
}
