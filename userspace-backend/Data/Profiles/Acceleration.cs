namespace userspace_backend.Data.Profiles
{
    public class Acceleration
    {
        public enum AccelerationDefinitionType
        {
            None,
            Formula,
            LookupTable,
        }

        public virtual AccelerationDefinitionType Type { get; init; }

        public Anisotropy Anisotropy { get; init; }
    }
}
