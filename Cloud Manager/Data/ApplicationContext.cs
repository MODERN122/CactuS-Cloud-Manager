using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Cloud_Manager.Data
{
    class ApplicationContext : DbContext
    {
        public DbSet<Cloud> Clouds { get; set; }

        public ApplicationContext() : base("DefaultConnection")
        {
            
        }


    }
}
