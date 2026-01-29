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
/// DTO for specific object. Class + values for constructor
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

public sealed class SnakeCaseNamingPolicy : JsonNamingPolicy
{
    public override string ConvertName(string name)
    {
        var sb = new StringBuilder();

        for (int i = 0; i < name.Length; i++)
        {
            var c = name[i];

            if (char.IsUpper(c))
            {
                if (i > 0) sb.Append('_');
                sb.Append(char.ToLowerInvariant(c));
            }
            else
            {
                sb.Append(c);
            }
        }

        return sb.ToString();
    }
}
