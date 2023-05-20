using Avalonia.Threading;

using LiveChartsCore;
using LiveChartsCore.Defaults;
using LiveChartsCore.SkiaSharpView;

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Documents;

using RawAccel.Models.Settings;

namespace RawAccel.ViewModels
{
    public sealed class ProfilesViewModel : ViewModelBase
    {
        public ProfilesViewModel()
        {
            Profiles = new HashSet<Profile>
            {
                new Profile()
            };
        }

        public ISet<Profile> Profiles { get; set; }
    }
}
