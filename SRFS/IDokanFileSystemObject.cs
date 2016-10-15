using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DokanNet;
using System.Security.AccessControl;
using SRFS.Model.Data;
using FileAttributes = System.IO.FileAttributes;

namespace SRFS {
    public interface IDokanFileSystemObject {

        void Delete();
        FileInformation GetInformation();
        FileSystemSecurity GetSecurity(AccessControlSections sections);
        void Move(Directory newDirectory, string newName);
        int ParentID { get; }
        void SetAttributes(FileAttributes attributes);
        void SetSecurity(FileSystemSecurity security, AccessControlSections sections);
        void SetTime(DateTime? creationTime, DateTime? lastAccessTime, DateTime? lastWriteTime);
    }
}
