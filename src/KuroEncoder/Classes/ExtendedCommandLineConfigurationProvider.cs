using System;
using System.Collections.Generic;
using Microsoft.Extensions.Configuration;

namespace KuroEncoder.Classes
{
    public class ExtendedCommandLineConfigurationProvider : ConfigurationProvider
    {
        private Dictionary<String, String> _switchMapping;

        protected IEnumerable<String> Args { get; init; }

        public ExtendedCommandLineConfigurationProvider(IEnumerable<String> args, IDictionary<String, String> switchMapping = null)
        {
            this.Args = args ?? throw new ArgumentNullException(nameof(args));

            if (switchMapping != null)
                this._switchMapping = GetValidatedSwitchMappingsCopy(switchMapping);
        }

        public override void Load()
        {
            var data = new Dictionary<String, String>(StringComparer.OrdinalIgnoreCase);
            String key, value;

            var queue = new Queue<String>(this.Args);

            while (queue.TryDequeue(out var currentArg))
            {
                var keyStartIndex = 0;

                if (currentArg.StartsWith("--"))
                    keyStartIndex = 2;
                else if (currentArg.StartsWith("-"))
                    keyStartIndex = 1;

                if (keyStartIndex == 0)
                {
                    continue; // Ignore invalid keys. Must start with -- or -.
                }

                // If the switch is a key in the switch mappings, use it.
                if (this._switchMapping != null &&
                    this._switchMapping.TryGetValue(currentArg, out var mappedKey))
                {
                    key = mappedKey;
                }
                // If the switch is a key with a single "-", and isn't in the switch mappings, it's invalid. Ignore it.
                else if (keyStartIndex == 1)
                {
                    continue;
                }
                else
                {
                    key = currentArg.Substring(keyStartIndex);
                }

                if (queue.TryPeek(out var nextArg))
                {
                    if (nextArg.StartsWith("-"))
                    {
                        value = "true";
                    }
                    else
                    {
                        value = nextArg;
                        _ = queue.Dequeue();
                    }
                }
                else
                {
                    value = "true";
                }

                data[key] = value;
            }

            this.Data = data;
        }

        private Dictionary<String, String> GetValidatedSwitchMappingsCopy(IDictionary<String, String> switchMappings)
        {
            // The dictionary passed in might be constructed with a case-sensitive comparer
            // However, the keys in configuration providers are all case-insensitive
            // So we check whether the given switch mappings contain duplicated keys with case-insensitive comparer
            var switchMappingsCopy = new Dictionary<String, String>(switchMappings.Count, StringComparer.OrdinalIgnoreCase);
            foreach (var mapping in switchMappings)
            {
                // Only keys start with "--" or "-" are acceptable
                if (!mapping.Key.StartsWith("-") && !mapping.Key.StartsWith("--"))
                {
                    throw new ArgumentException(
                        String.Format("The switch mappings contain an invalid switch '{0}'.", mapping.Key),
                        nameof(switchMappings));
                }

                if (switchMappingsCopy.ContainsKey(mapping.Key))
                {
                    throw new ArgumentException(
                        String.Format("Keys in switch mappings are case-insensitive. A duplicated key '{0}' was found.", mapping.Key),
                        nameof(switchMappings));
                }

                switchMappingsCopy.Add(mapping.Key, mapping.Value);
            }

            return switchMappingsCopy;
        }
    }
}
