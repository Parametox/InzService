<<<<<<< HEAD
﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceProcess;
using System.Text;
using System.Threading.Tasks;

namespace InzService
{
    static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        static void Main()
        {
            ServiceBase[] ServicesToRun;
            ServicesToRun = new ServiceBase[]
            {
                new InzService()
            };
            ServiceBase.Run(ServicesToRun);
        }

        // todo: connection string 
        //private static string GetConnectionString()
        //{
        //    return "metadata=res://*/InzDbModel.csdl|res://*/InzDbModel.ssdl|res://*/InzDbModel.msl;" +
        //        "provider=System.Data.SqlClient;" +
        //        "provider connection string=\"data source=DESKTOP-2DH49DG;" +
        //        "initial catalog=InzDatabase;" +
        //        "user id=pci;" +
        //        "password=pass#pass;" +
        //        "MultipleActiveResultSets=True; " +
        //        "App=EntityFramework\"";
        //}
    }
}
=======
﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceProcess;
using System.Text;
using System.Threading.Tasks;

namespace InzService
{
    static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        static void Main()
        {
            ServiceBase[] ServicesToRun;
            ServicesToRun = new ServiceBase[]
            {
                new InzService()
            };
            ServiceBase.Run(ServicesToRun);
        }

        // todo: connection string 
        //private static string GetConnectionString()
        //{
        //    return "metadata=res://*/InzDbModel.csdl|res://*/InzDbModel.ssdl|res://*/InzDbModel.msl;" +
        //        "provider=System.Data.SqlClient;" +
        //        "provider connection string=\"data source=DESKTOP-2DH49DG;" +
        //        "initial catalog=InzDatabase;" +
        //        "user id=pci;" +
        //        "password=pass#pass;" +
        //        "MultipleActiveResultSets=True; " +
        //        "App=EntityFramework\"";
        //}
    }
}
>>>>>>> 7634a5a11bd73e313484bf67d9f5b1588400d1aa
