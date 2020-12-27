using System;

namespace KuroEncoder.Attributes
{
    public class CliAliasAttribute : Attribute
    {
        public String Alias { get; }

        public CliAliasAttribute(String alias)
        {
            this.Alias = alias;
        }
    }
}
