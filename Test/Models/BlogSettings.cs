//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated from a template.
//
//     Manual changes to this file may cause unexpected behavior in your application.
//     Manual changes to this file will be overwritten if the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

namespace Test.Models
{
    using System;
    using System.Collections.Generic;
    
    public partial class BlogSettings
    {
        public int BlogId { get; set; }
        public bool AutoSave { get; set; }
        public bool AutoPost { get; set; }
    
        public virtual Blog Blog { get; set; }
    }
}