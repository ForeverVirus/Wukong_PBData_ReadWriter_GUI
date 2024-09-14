using System.Collections;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using Google.Protobuf;

namespace Wukong_PBData_ReadWriter_GUI.Models;

public class DataPropertyItem
{
    public required string PropertyName { get; init; }
    public required PropertyInfo PropertyInfo { get; init; }
    public required IMessage BelongData { get; init; }
    public required DataItem DataItem { get; init; }

    public string PropertyDesc =>
        DataFileHelper.DescriptionConfig.GetValueOrDefault(
            DataItem.File.FileData?.GetType().Name + "_" + PropertyName, ""
        );

    public double AsDouble
    {
        get => Convert.ToDouble(PropertyInfo.GetValue(BelongData));
        set
        {
            PropertyInfo.SetValue(
                BelongData,
                Convert.ChangeType(value, PropertyInfo.PropertyType)
            );
            DataItem.IsDirty = true;
        }
    }

    public string? AsString
    {
        get => PropertyInfo.GetValue(BelongData) as string;
        set
        {
            PropertyInfo.SetValue(BelongData, value);
            DataItem.IsDirty = true;
        }
    }

    public Array Enums => Enum.GetValues(PropertyInfo.PropertyType);

    public object? EnumValue
    {
        get => PropertyInfo.GetValue(BelongData);
        set
        {
            PropertyInfo.SetValue(BelongData, value);
            DataItem.IsDirty = true;
        }
    }
}

public class PropertyItemTemplateSelector : DataTemplateSelector
{
    public required DataTemplate NumberTemplate { get; set; }
    public required DataTemplate StringTemplate { get; set; }
    public required DataTemplate EnumTemplate { get; set; }
    public required DataTemplate MessageTemplate { get; set; }
    public required DataTemplate ListTemplate { get; set; }

    public override DataTemplate? SelectTemplate(object? item, DependencyObject container)
    {
        if (item is not DataPropertyItem dataPropertyItem) return null;
        var valueType = dataPropertyItem.PropertyInfo.PropertyType;
        if (valueType == typeof(int) || valueType == typeof(float) || valueType == typeof(long) ||
            valueType == typeof(double))
        {
            return NumberTemplate;
        }

        if (valueType == typeof(string))
        {
            return StringTemplate;
        }

        if (valueType.IsEnum)
        {
            return EnumTemplate;
        }

        if (typeof(IMessage).IsAssignableFrom(valueType))
        {
            return MessageTemplate;
        }

        return typeof(IList).IsAssignableFrom(valueType) ? ListTemplate : null;
    }
}