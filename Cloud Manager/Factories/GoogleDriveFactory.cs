using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Cloud_Manager
{
    class GoogleDriveFactory : CloudFactory
    {
        public override CloudDrive Create()
        {
            return new GoogleDriveManager();
        }
    }
}
