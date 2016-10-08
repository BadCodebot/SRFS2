using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SRFS.Model.Clusters;

namespace SRFS.Model {
    public interface IClusterIO {

        void Load(Cluster c);
        void Save(Cluster c);
    }
}
