using SRFS.Model.Data;
using System.Collections.Generic;

namespace SRFS {

    public interface IHandle {

        bool IsSynchronize { get; set; }
        int ID { get; }

        IDokanFileSystemObject FileSystemObject { get; }

        List<string> Messages { get; }

        void Close();
    }
}
