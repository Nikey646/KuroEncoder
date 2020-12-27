using System;
using System.Collections.Generic;
using System.Reflection;
using KuroEncoder.Attributes;
using KuroEncoder.Classes;
using Microsoft.Extensions.Configuration;

namespace KuroEncoder.Extensions
{
    public static class CliConfigurationExtensions
    {

        public static IConfigurationBuilder AddCommandLine<TFor>(this IConfigurationBuilder configurationBuilder, String[] args)
        {
            var map = new Dictionary<String, String>();
            var type = typeof(TFor).GetTypeInfo();

            foreach (var prop in type.DeclaredProperties)
            {
                var aliases = prop.GetCustomAttributes<CliAliasAttribute>();
                foreach (var attribute in aliases)
                {
                    map.Add("--" + attribute.Alias, prop.Name);
                }

                var shortName = prop.GetCustomAttribute<CliShortNameAttribute>();
                if (shortName != null)
                    map.Add("-" + shortName.Name, prop.Name);
            }

            return configurationBuilder.Add(new ExtendedCommandLineConfigurationSource { Args = args, SwitchMappings = map});
        }

    }
}
