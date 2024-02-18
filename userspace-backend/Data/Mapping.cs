using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace userspace_backend.Data
{
    public class Mapping
    {
        public Dictionary<string, MappingGroups> ProfilesToGroups { get; set; }

        public override bool Equals(object? obj)
        {
            bool isEqual = obj is Mapping mapping &&
                mapping.ProfilesToGroups.Count == ProfilesToGroups.Count && !mapping.ProfilesToGroups.Except(ProfilesToGroups).Any();

            var excepted = ((Mapping)obj).ProfilesToGroups.Except(ProfilesToGroups).ToList();

            return isEqual;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(ProfilesToGroups);
        }
    }
}
