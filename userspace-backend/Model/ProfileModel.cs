using System.Collections.Generic;
using DATA = userspace_backend.Data;
using userspace_backend.Model.AccelDefinitions;
using userspace_backend.Model.EditableSettings;
using userspace_backend.Model.ProfileComponents;

namespace userspace_backend.Model
{
    public class ProfileModel : EditableSettingsCollection<DATA.Profile>
    {
        public ProfileModel(DATA.Profile dataObject, IModelValueValidator<string> nameValidator) : base(dataObject)
        {
            NameValidator = nameValidator;
            UpdateCurrentValidatedDriverProfile();
        }

        public string CurrentNameForDisplay => Name.CurrentValidatedValue;

        public EditableSetting<string> Name { get; set; }

        public EditableSetting<int> OutputDPI { get; set; }

        public EditableSetting<double> YXRatio { get; set; }

        public AccelerationModel Acceleration { get; set; }

        public HiddenModel Hidden { get; set; }

        public Profile CurrentValidatedDriverProfile { get; protected set; }

        protected IModelValueValidator<string> NameValidator { get; }

        public override DATA.Profile MapToData()
        {
            return new DATA.Profile()
            {
                Name = Name.ModelValue,
                OutputDPI = OutputDPI.ModelValue,
                YXRatio = YXRatio.ModelValue,
                Acceleration = Acceleration.MapToData(),
                Hidden = Hidden.MapToData(),
            };
        }

        public Profile MapToDriver()
        {
            return new Profile()
            {
                outputDPI = OutputDPI.ModelValue,
                yxOutputDPIRatio = YXRatio.ModelValue,
                argsX = Acceleration.MapToDriver(),
                domainXY = new Vec2<double>
                {
                    x = Acceleration.Anisotropy.DomainX.ModelValue,
                    y = Acceleration.Anisotropy.DomainY.ModelValue,
                },
                rangeXY = new Vec2<double>
                {
                    x = Acceleration.Anisotropy.RangeX.ModelValue,
                    y = Acceleration.Anisotropy.RangeY.ModelValue,
                },
                rotation = Hidden.RotationDegrees.ModelValue,
                lrOutputDPIRatio = Hidden.LeftRightRatio.ModelValue,
                udOutputDPIRatio = Hidden.UpDownRatio.ModelValue,
                snap = Hidden.AngleSnappingDegrees.ModelValue,
                maximumSpeed = Hidden.SpeedCap.ModelValue,
                minimumSpeed = 0,
                inputSpeedArgs = new SpeedArgs
                {
                    combineMagnitudes = Acceleration.Anisotropy.CombineXYComponents.ModelValue,
                    lpNorm = Acceleration.Anisotropy.LPNorm.ModelValue,
                    outputSmoothHalflife = Hidden.OutputSmoothingHalfLife.ModelValue,
                    inputSmoothHalflife = Acceleration.Coalescion.InputSmoothingHalfLife.ModelValue,
                    scaleSmoothHalflife = Acceleration.Coalescion.ScaleSmoothingHalfLife.ModelValue,
                }
            };
        }

        // TODO: check if driver profile can be made disposable to avoid garbage collection
        protected void UpdateCurrentValidatedDriverProfile()
        {
            CurrentValidatedDriverProfile = MapToDriver();
        }

        protected void UpdateCurvePreview()
        {
            UpdateCurrentValidatedDriverProfile();
        }

        protected override IEnumerable<IEditableSetting> EnumerateEditableSettings()
        {
            return [Name, OutputDPI, YXRatio];
        }

        protected override IEnumerable<IEditableSettingsCollection> EnumerateEditableSettingsCollections()
        {
            return [Acceleration, Hidden];
        }

        protected override void InitEditableSettingsAndCollections(DATA.Profile dataObject)
        {
            Name = new EditableSetting<string>(
                displayName: "Name",
                initialValue: dataObject.Name,
                parser: UserInputParsers.StringParser,
                validator: NameValidator);
            OutputDPI = new EditableSetting<int>(
                displayName: "Output DPI",
                initialValue: dataObject.OutputDPI,
                parser: UserInputParsers.IntParser,
                validator: ModelValueValidators.DefaultIntValidator);
            YXRatio = new EditableSetting<double>(
                displayName: "Y/X Ratio",
                initialValue: dataObject.YXRatio,
                parser: UserInputParsers.DoubleParser,
                validator: ModelValueValidators.DefaultDoubleValidator);
            Acceleration = new AccelerationModel(dataObject.Acceleration);
            Hidden = new HiddenModel(dataObject.Hidden);
        }
    }
}
