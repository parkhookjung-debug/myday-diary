using System;
using System.Linq;

namespace MyDay.Windows.Core
{
    public static partial class TemplateCatalog
    {
        public static readonly string[] Categories=DesktopContract.Categories;
        public static readonly string[] CategoryNames=DesktopContract.CategoryNames;
        public static string CategoryName(string id) { int i=Array.IndexOf(Categories,id); return i<0?id:CategoryNames[i]; }
        public static readonly string[] ReferenceIds=DesktopContract.ReferenceIds;
        public static string ReferenceUrl(string id)
        {
            int i=Array.IndexOf(ReferenceIds,id);return DesktopContract.ReferenceUrls[i<0?0:i];
        }
    }
}
