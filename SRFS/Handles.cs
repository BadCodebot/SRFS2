using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SRFS.Model.Data;

namespace SRFS {

    public static class Handles {

        public static FileHandle CreateNewFileHandle(DokanFile file) {
            lock (_handles) {
                for (int i = 0; i < _handles.Count; i++) {
                    if (_handles[i] == null) {
                        FileHandle reusedHandle = new FileHandle(i, file);
                        _handles[i] = reusedHandle;
                        return reusedHandle;
                    }
                }
                FileHandle newHandle = new FileHandle(_handles.Count, file);
                _handles.Add(newHandle);
                return newHandle;
            }
        }

        public static DirectoryHandle CreateNewDirectoryHandle(DokanDirectory directory, DokanNet.FileAccess access) {
            lock (_handles) {
                for (int i = 0; i < _handles.Count; i++) {
                    if (_handles[i] == null) {
                        DirectoryHandle reusedHandle = new DirectoryHandle(i, directory, access);
                        _handles[i] = reusedHandle;
                        return reusedHandle;
                    }
                }

                DirectoryHandle newHandle = new DirectoryHandle(_handles.Count, directory, access);
                _handles.Add(newHandle);
                return newHandle;
            }
        }

        public static void DeleteHandle(IHandle h) {
            lock (_handles) {
                _handles[h.ID] = null;
            }
        }

        private static List<IHandle> _handles = new List<IHandle>();
    }
}
