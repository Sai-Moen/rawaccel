using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Serialization;

namespace userspace_backend.Data
{
    public class Mapping
    {
        [JsonRequired]
        public Dictionary<string, string> GroupsToProfiles { get; set; }

        public override bool Equals(object? obj)
        {
            bool isEqual = obj is Mapping mapping &&
                mapping.GroupsToProfiles.Count == GroupsToProfiles.Count && !mapping.GroupsToProfiles.Except(GroupsToProfiles).Any();

            return isEqual;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(GroupsToProfiles);
        }
    }
}
