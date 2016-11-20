using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TrackUtility.Model {

    public class MessageException : Exception {

        public MessageException(string message, string title, Exception inner) : base(message, inner) {
            _title = title;
        }

        public string Title => _title;
        private string _title;
    }
}
