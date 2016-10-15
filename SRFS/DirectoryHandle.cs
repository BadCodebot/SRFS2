using SRFS.Model.Data;
using System.Collections.Generic;

namespace SRFS {

    public class DirectoryHandle : IHandle {

        public DirectoryHandle(int id, DokanDirectory directory, DokanNet.FileAccess access) {
            ID = id;
            Directory = directory;
            Access = access;
        }

        public override string ToString() {
            return ID.ToString();
        }

        public bool IsSynchronize { get; set; }

        IDokanFileSystemObject IHandle.FileSystemObject => Directory;
        public DokanDirectory Directory { get; }

        public int ID { get; private set; }

        public void Close() { Directory.Close(ID); }

        public DokanNet.FileAccess Access { get; private set; }

        public List<string> Messages => _messages;
        private List<string> _messages = new List<string>();
    }
}
