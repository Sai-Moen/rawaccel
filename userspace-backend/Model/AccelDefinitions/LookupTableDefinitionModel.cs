using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using userspace_backend.Data.Profiles;
using userspace_backend.Data.Profiles.Accel;
using userspace_backend.Model.EditableSettings;
using static userspace_backend.Data.Profiles.Accel.LookupTableAccel;

namespace userspace_backend.Model.AccelDefinitions
{
    public class LookupTableDefinitionModel : AccelDefinitionModel<LookupTableAccel>
    {
        public LookupTableDefinitionModel(Acceleration dataObject) : base(dataObject)
        {
        }

        public EditableSetting<LookupTableType> ApplyAs { get; set; }

        public EditableSetting<LookupTableData> Data { get; set; }

        public override Acceleration MapToData()
        {
            return new LookupTableAccel()
            {
                ApplyAs = this.ApplyAs.EditableValue,
                Data = this.Data.EditableValue.Data,
            };
        }

        protected override IEnumerable<IEditableSetting> EnumerateEditableSettings()
        {
            return [ApplyAs, Data];
        }

        protected override IEnumerable<IEditableSettingsCollection> EnumerateEditableSettingsCollections()
        {
            return Enumerable.Empty<IEditableSettingsCollection>();
        }

        protected override LookupTableAccel GenerateDefaultDataObject()
        {
            return new LookupTableAccel()
            {
                ApplyAs = LookupTableType.Velocity,
                Data = [],
            };
        }

        protected override void InitSpecificSettingsAndCollections(LookupTableAccel dataObject)
        {
            ApplyAs = new EditableSetting<LookupTableType>(dataObject.ApplyAs, UserInputParsers.LookupTableTypeParser);
            Data = new EditableSetting<LookupTableData>(new LookupTableData(dataObject.Data), UserInputParsers.LookupTableDataParser);
        }
    }

    public class LookupTableData : IComparable
    {
        public LookupTableData(double[]? data = null)
        {
            Data = data ?? [];
        }

        public double[] Data { get; set; }

        public int CompareTo(object? obj)
        {
            if (obj == null)
            {
                return -1;
            }

            double[]? compareTo = obj as double[];

            if (compareTo == null)
            {
                return -1;
            }

            // We are using CompareTo as a stand-in for equality
            return Data.SequenceEqual(compareTo) ? 0 : -1;
        }
    }
}
