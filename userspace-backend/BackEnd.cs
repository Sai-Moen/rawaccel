using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using userspace_backend.Data;

namespace userspace_backend
{
    public class BackEnd
    {
        public IList<Device> Devices { get; set; }

        public IList<Mapping> Mappings { get; set; }

        public IList<Profile> Profiles { get; set; }

        public void Load(string path)
        {

        }
    }
}
