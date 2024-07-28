using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using userspace_backend.Data.Profiles.Accel;
using userspace_backend.Data.Profiles;
using userspace_backend.Model.AccelDefinitions;

namespace userspace_backend.Model.EditableSettings
{
    public interface IModelValueValidator<T>
    {
        bool Validate(T value);
    }

    public static class ModelValueValidators
    {
        public static DefaultModelValueValidator<int> DefaultIntValidator = new DefaultModelValueValidator<int>();
        public static DefaultModelValueValidator<double> DefaultDoubleValidator = new DefaultModelValueValidator<double>();
        public static DefaultModelValueValidator<string> DefaultStringValidator = new DefaultModelValueValidator<string>();
        public static DefaultModelValueValidator<bool> DefaultBoolValidator = new DefaultModelValueValidator<bool>();
        public static DefaultModelValueValidator<Acceleration.AccelerationDefinitionType> DefaultAccelerationTypeValidator = new DefaultModelValueValidator<Acceleration.AccelerationDefinitionType>();
        public static DefaultModelValueValidator<LookupTableAccel.LookupTableType> DefaultLookupTableTypeValidator = new DefaultModelValueValidator<LookupTableAccel.LookupTableType>();
        public static DefaultModelValueValidator<LookupTableData> DefaultLookupTableDataValidator = new DefaultModelValueValidator<LookupTableData>();
        public static DefaultModelValueValidator<FormulaAccel.AccelerationFormulaType> DefaultAccelerationFormulaTypeValidator = new DefaultModelValueValidator<FormulaAccel.AccelerationFormulaType>();
    }

    public class DefaultModelValueValidator<T> : IModelValueValidator<T>
    {
        public bool Validate(T value)
        {
            return true;
        }
    }
}
