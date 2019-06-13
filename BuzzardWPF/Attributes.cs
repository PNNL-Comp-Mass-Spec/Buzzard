using System;

namespace BuzzardWPF
{
    [AttributeUsage(AttributeTargets.Assembly, AllowMultiple = false)]
    public sealed class AssemblyDateAttribute : Attribute
    {
        public AssemblyDateAttribute(string assemblyDate)
        {
            AssemblyDate = assemblyDate;
        }

        public string AssemblyDate { get; }
    }
}
