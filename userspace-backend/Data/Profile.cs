using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using userspace_backend.Data.Profiles;

namespace userspace_backend.Data
{
    public class Profile
    {
        public string Name { get; set; }

        public int OutputDPI { get; set; }

        public double YXRatio { get; set; }

        public Acceleration Acceleration { get; set; }

        public Hidden Hidden { get; set; }
    }
}
