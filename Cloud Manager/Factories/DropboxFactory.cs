using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Cloud_Manager.Factories
{
    class DropboxFactory : CloudFactory
    {
        public override CloudDrive Create()
        {
            return new DropboxManager();
        }
    }
}
