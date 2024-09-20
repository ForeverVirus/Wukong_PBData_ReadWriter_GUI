using Google.Protobuf;
using System.Collections;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;

namespace Wukong_PBData_ReadWriter_GUI.Models;

public class DataProperty(PropertyInfo propertyInfo, IMessage belongData, Action callBack)
{
    public string PropertyName => propertyInfo.Name;
    public Type PropertyType => propertyInfo.PropertyType;
    public bool CanWrite => propertyInfo.CanWrite;

    public double AsDouble
    {
        get => Convert.ToDouble(propertyInfo.GetValue(belongData));
        set => ChangeValue(value);
    }

    public string AsString
    {
        get => Convert.ToString(propertyInfo.GetValue(belongData)) ?? "";
        set => ChangeValue(value);
    }

    public Array EnumType => Enum.GetValues(PropertyType);
    public object AsEnum
    {
        get => propertyInfo.GetValue(belongData)!;
        set => ChangeValue(value);
    }

    public DataProperty[] AsObject
    {
        get
        {
            var obj = (IMessage)propertyInfo.GetValue(belongData)!;
            return obj.GetType().GetProperties()
                 .Where(p => p.Name != "Parser")
                 .Select(p => new DataProperty(p, obj, callBack))
                 .ToArray();
        }
    }

    public List<DataProperty[]> AsList
    {
        get
        {
            var list = (IList)propertyInfo.GetValue(belongData)!;
            var res = new List<DataProperty[]>();
            foreach (IMessage obj in list)
            {
                res.Add(obj.GetType().GetProperties()
                 .Where(p => p.Name != "Parser")
                 .Select(p => new DataProperty(p, obj, callBack))
                 .ToArray());
            }
            return res;
        }
    }

    private void ChangeValue(object value)
    {
        propertyInfo.SetValue(belongData, Convert.ChangeType(value, PropertyType));
        callBack();
    }

    // public string PropertyDesc =>
    //     DataFileHelper.DescriptionConfig.GetValueOrDefault(
    //         DataItem.File.FileData?.GetType().Name + "_" + PropertyName, ""
    //     );
}

public class PropertyTemplateSelector : DataTemplateSelector
{
    public required DataTemplate NumberTemplate { get; init; }
    public required DataTemplate StringTemplate { get; init; }
    public required DataTemplate EnumTemplate { get; init; }
    public required DataTemplate ObjectTemplate { get; init; }
    public required DataTemplate ListTemplate { get; init; }

    public override DataTemplate? SelectTemplate(object item, DependencyObject container)
    {
        if (item is not DataProperty dataProperty) return null;
        var type = dataProperty.PropertyType;
        if (type == typeof(int) || type == typeof(float))
        {
            return NumberTemplate;
        }
        if (type == typeof(string))
        {
            return StringTemplate;
        }
        if (type.IsEnum)
        {
            return EnumTemplate;
        }
        if (typeof(IMessage).IsAssignableFrom(type))
        {
            return ObjectTemplate;
        }
        if (typeof(IList).IsAssignableFrom(type))
        {
            return ListTemplate;
        }

        return base.SelectTemplate(item, container);
    }
}