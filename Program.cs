// Program.cs
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using FluentValidation;

namespace JsonToValidator
{
    class Program
    {
        static void Main(string[] args)
        {
            var inputFile = "endpoint.json";
            var outputFile = "Validation.cs";

            if (!File.Exists(inputFile))
            {
                Console.WriteLine($"File {inputFile} not found.");
                return;
            }

            var json = File.ReadAllText(inputFile);
            var schema = JsonSerializer.Deserialize<Schema>(json, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
            });

            var validatorCode = GenerateValidatorClasses(schema);

            File.WriteAllText(outputFile, validatorCode);
            Console.WriteLine($"Validation code generated in {outputFile}");
        }

        private static string GenerateValidatorClasses(Schema schema)
        {
            var sb = new StringBuilder();

            foreach (var collection in schema.Collections)
            {
                GenerateValidatorForCollection(sb, collection);
            }

            return sb.ToString();
        }

        private static void GenerateValidatorForCollection(StringBuilder sb, Collection collection)
        {
            foreach (var property in collection.Properties)
            {
                GenerateValidatorForProperty(sb, property, collection.CollectionName);
            }
        }

        private static void GenerateValidatorForProperty(StringBuilder sb, Property property, string parentClassName)
        {
            var className = SanitizeClassName($"{parentClassName}_{property.Name}Validator");

            if (property.Properties != null && property.Properties.Any())
            {
                // Nested properties
                sb.AppendLine($"public class {className} : AbstractValidator<{SanitizeClassName(property.Name)}>");
                sb.AppendLine("{");

                foreach (var prop in property.Properties)
                {
                    GenerateValidatorForProperty(sb, prop, $"{parentClassName}_{property.Name}");
                }

                sb.AppendLine("}");
                sb.AppendLine();
            }
            else
            {
                // Basic properties
                var typeName = GetTypeName(property.Type);

                sb.AppendLine($"public class {className} : AbstractValidator<{typeName}>");
                sb.AppendLine("{");

                switch (property.Type)
                {
                    case "string":
                        if (property.Enum != null && property.Enum.Any())
                        {
                            sb.AppendLine($"    RuleFor(x => x.{property.Name}).IsInEnum();");
                        }
                        else
                        {
                            sb.AppendLine($"    RuleFor(x => x.{property.Name}).NotEmpty();");
                        }
                        break;

                    case "numeric":
                        var minValue = property.MinValue ?? 0;
                        var maxValue = property.MaxValue ?? int.MaxValue;
                        sb.AppendLine($"    RuleFor(x => x.{property.Name}).InclusiveBetween({minValue}, {maxValue});");
                        break;

                    case "DateTime":
                        sb.AppendLine($"    RuleFor(x => x.{property.Name}).NotEmpty();");
                        break;

                    default:
                        throw new ArgumentException($"Unsupported type: {property.Type}");
                }

                sb.AppendLine("}");
                sb.AppendLine();
            }
        }

        private static string SanitizeClassName(string className)
        {
            return string.Concat(className
                .Split(new[] { ' ', '-', '_', '.', '/' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(s => char.ToUpper(s[0]) + s.Substring(1).ToLower()));
        }

        private static string GetTypeName(string type)
        {
            return type switch
            {
                "string" => "string",
                "numeric" => "decimal",
                "DateTime" => "DateTime",
                _ => throw new ArgumentException($"Unsupported type: {type}")
            };
        }
    }
}

public class Schema
{
    public List<Collection> Collections { get; set; }
}

public class Collection
{
    public string CollectionName { get; set; }
    public List<Property> Properties { get; set; }
}

public class Property
{
    public string Name { get; set; }
    public List<Property> Properties { get; set; }
    public string Type { get; set; }
    public List<string> Enum { get; set; }
    public string Description { get; set; }
    public bool Required { get; set; }
    public int? MinValue { get; set; }
    public int? MaxValue { get; set; }
}
