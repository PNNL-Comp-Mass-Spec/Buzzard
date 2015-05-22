using System.Collections.Generic;
using System;
using Finch.Data;


namespace Finch
{
    /// <summary>
    /// Writes data to a stream for health monitoring reasons.
    /// </summary>
    public interface IFinchWriter
    {
        void WriteAggregates(List<FinchAggregateData> aggregates, string path);        
    }
}
