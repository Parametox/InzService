//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated from a template.
//
//     Manual changes to this file may cause unexpected behavior in your application.
//     Manual changes to this file will be overwritten if the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

namespace InzService
{
    using System;
    using System.Collections.Generic;
    
    public partial class Logs
    {
        public long LogId { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public string Date { get; set; }
        public long RefTempId { get; set; }
    
        public virtual Logs Logs1 { get; set; }
        public virtual Logs Log1 { get; set; }
    }
}
