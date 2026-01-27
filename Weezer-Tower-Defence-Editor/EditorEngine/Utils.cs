using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

/// <summary>
/// DTO for behaviours like StandardBulletBehavior. Generalized thing
/// </summary>
public sealed class BehaviorConfig
{
    public string Name { get; set; }
    public string FileName { get; set; }
    public string ClassName { get; set; }
    public List<ArgConfig> Args { get; set; }
}

public sealed class ArgConfig
{
    public string Name { get; set; }
    public string Type { get; set; }
}

/// <summary>
/// DTO for specific bullet. Class + values for constructor
/// </summary>
public sealed class TypeSpecification
{
    public string Name { get; set; }
    public string ClassName { get; set; }
    public List<ArgValueSpec> Args { get; set; }
}

public sealed class ArgValueSpec
{
    public string Name { get; set; }
    public JsonElement Value { get; set; }
}
