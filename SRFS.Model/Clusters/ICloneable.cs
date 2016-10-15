using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SRFS.Model.Clusters {
    public interface ICloneable<out T> {
        T Clone();
    }
}
