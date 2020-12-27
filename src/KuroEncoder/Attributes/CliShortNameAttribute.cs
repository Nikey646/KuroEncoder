using System;

namespace KuroEncoder.Attributes
{
    public class CliShortNameAttribute : Attribute
    {
        public String Name { get; }

        public CliShortNameAttribute(String name)
        {
            this.Name = name;
        }
    }
}
