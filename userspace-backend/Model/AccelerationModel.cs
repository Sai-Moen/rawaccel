using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using userspace_backend.Data.Profiles;
using userspace_backend.Model.EditableSettings;
using static userspace_backend.Data.Profiles.Acceleration;

namespace userspace_backend.Model
{
    public class AccelerationModel : EditableSettingsCollection<Acceleration>
    {
        public AccelerationModel(Acceleration dataObject) : base(dataObject)
        {
        }

        public EditableSetting<AccelerationDefinitionType> DefinitionType { get; set; }

        protected Dictionary<AccelerationDefinitionType, EditableSettingsCollection<Acceleration>> Definitions { get; set; }

        protected AccelerationDefinitionType CurrentlyActiveDefinitionType { get; set; }

        public override Acceleration MapToData()
        {
            throw new NotImplementedException();
        }

        protected override IEnumerable<IEditableSetting> GetEditableSettings()
        {
            throw new NotImplementedException();
        }

        protected override IEnumerable<IEditableSettingsCollection> GetEditableSettingsCollections()
        {
            throw new NotImplementedException();
        }

        protected override void InitEditableSettingsAndCollections(Acceleration dataObject)
        {
            Definitions = new Dictionary<AccelerationDefinitionType, EditableSettingsCollection<Acceleration>>();
        }
    }
}
