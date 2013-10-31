using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace IronPython.Runtime.Types {
    class PythonTypeNullSlot : PythonTypeSlot {
        public static PythonTypeNullSlot Instance = new PythonTypeNullSlot();

        internal override bool TryGetValue(CodeContext context, object instance, PythonType owner, out object value) {
            if (instance == null) {
                value = this;
            } else {
                value = null;
            }
            return true;
        }
    }
}
