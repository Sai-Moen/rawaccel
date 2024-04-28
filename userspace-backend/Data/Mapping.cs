using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Serialization;

namespace userspace_backend.Data
{
    public class Mapping
    {
        [JsonRequired]
        public Dictionary<string, MappingGroups> ProfilesToGroups { get; set; }

        public override bool Equals(object? obj)
        {
            bool isEqual = obj is Mapping mapping &&
                mapping.ProfilesToGroups.Count == ProfilesToGroups.Count && !mapping.ProfilesToGroups.Except(ProfilesToGroups).Any();

            return isEqual;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(ProfilesToGroups);
        }
    }
}
