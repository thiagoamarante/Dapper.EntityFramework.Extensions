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
    
    public partial class BlogPost
    {
        public int Id { get; set; }
        public int BlogId { get; set; }
        public string Body { get; set; }
        public System.DateTime DatePublication { get; set; }
    
        public virtual Blog Blog { get; set; }
    }
}
