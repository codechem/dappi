using System;

namespace Dappi.Cli.Helpers;

public static class StringExtensions
{
    public static bool IsNullOrWhiteSpace(this string? v)
    {
        return string.IsNullOrWhiteSpace(v);
    }

    public static (string? company, string? project, string? module) NameParse(this string name)
    {
        if (name.IsNullOrWhiteSpace())
        {
            throw new ArgumentNullException(nameof(name));
        }

        if (name.IndexOf('.') <= 0)
        {
            return (string.Empty, name, string.Empty);
        }

        if (name.EndsWith('.'))
        {
            Console.WriteLine(
                "Invalid Format! Please Enter As Format: CompanyName.ProjectName or CompanyName.ProjectName.ModuleName!");
            throw new ArgumentException("Invalid Format", nameof(name));
        }

        string?[] tNames = name.Split('.');
        var company = string.Empty;
        var project = string.Empty;
        var module = string.Empty;
        if (tNames.Length > 1)
        {
            company = tNames[0];
            project = tNames[1];
        }

        if (tNames.Length > 2)
        {
            module = tNames[2];
        }

        return (company, project, module);
    }


    public static string EnsureEndsWith(this string v, char b)
    {
        if (v.EndsWith(b))
        {
            return v;
        }

        return v + b;
    }
}