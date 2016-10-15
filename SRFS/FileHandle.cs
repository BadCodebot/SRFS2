using SRFS.Model.Data;
using System.Collections.Generic;

namespace SRFS {

    public class FileHandle : IHandle {

        public FileHandle(int id, DokanFile file) {
            ID = id;
            File = file;
        }

        public override string ToString() {
            return ID.ToString();
        }

        public bool IsSynchronize { get; set; }

        IDokanFileSystemObject IHandle.FileSystemObject => File;
        public DokanFile File { get; }

        public int ID { get; private set; }

        public void Close() { File.Close(); }

        public List<string> Messages => _messages;
        private List<string> _messages = new List<string>();

    }
}
