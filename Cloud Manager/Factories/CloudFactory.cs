using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Cloud_Manager
{
    abstract class CloudFactory
    {
        public abstract CloudDrive Create();
    }
}
