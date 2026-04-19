namespace JortPob.Common;

using System;

[AttributeUsage(AttributeTargets.Property)]
public class SettingAttribute : Attribute
{
    public bool IsRequired { get; init; }
    public object DefaultValue { get; init; }

    // No arguments = REQUIRED
    public SettingAttribute(bool IsRequired = false, object DefaultValue = default)
    {
        this.IsRequired = IsRequired;
        this.DefaultValue = DefaultValue;
    }
}