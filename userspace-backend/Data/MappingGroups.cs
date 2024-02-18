using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace userspace_backend.Data
{
    public class MappingGroups
    {
        public IList<string> Groups { get; set; }

        public override bool Equals(object? obj)
        {
            return obj is MappingGroups groups &&
                    groups.Groups.Count == Groups.Count && !groups.Groups.Except(Groups).Any();
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(Groups);
        }
    }
}
