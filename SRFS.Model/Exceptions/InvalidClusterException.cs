﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SRFS.Model.Exceptions {
    public class InvalidClusterException : Exception {
        public InvalidClusterException(string message) : base(message) { }
    }
}
