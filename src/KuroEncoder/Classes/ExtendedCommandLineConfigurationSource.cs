using System;
using System.Collections.Generic;
using Microsoft.Extensions.Configuration;

namespace KuroEncoder.Classes
{
    public class ExtendedCommandLineConfigurationSource : IConfigurationSource
    {
        public IDictionary<String, String> SwitchMappings { get; set; }

        public IEnumerable<String> Args { get; set; }

        public IConfigurationProvider Build(IConfigurationBuilder builder)
        {
            return new ExtendedCommandLineConfigurationProvider(this.Args, this.SwitchMappings);
        }
    }
}
