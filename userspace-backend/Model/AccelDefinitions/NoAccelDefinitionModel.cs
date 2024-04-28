using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using userspace_backend.Data.Profiles;
using userspace_backend.Data.Profiles.Accel;
using userspace_backend.Model.EditableSettings;

namespace userspace_backend.Model.AccelDefinitions
{
    public class NoAccelDefinitionModel : AccelDefinitionModel<NoAcceleration>
    {
        public NoAccelDefinitionModel(Acceleration dataObject) : base(dataObject)
        {
        }

        public NoAcceleration NoAcceleration { get; protected set; }

        public override Acceleration MapToData()
        {
            return NoAcceleration;
        }

        protected override IEnumerable<IEditableSetting> EnumerateEditableSettings()
        {
            return Enumerable.Empty<IEditableSetting>();
        }

        protected override IEnumerable<IEditableSettingsCollection> EnumerateEditableSettingsCollections()
        {
            return Enumerable.Empty<IEditableSettingsCollection>();
        }

        protected override NoAcceleration GenerateDefaultDataObject()
        {
            return new NoAcceleration();
        }

        protected override void InitSpecificSettingsAndCollections(NoAcceleration dataObject)
        {
            // Nothing to do here since no acceleration has no settings
        }
    }
}
