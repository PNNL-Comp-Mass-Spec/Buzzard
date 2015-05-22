
namespace Finch.Data
{

    /// <summary>
    /// Interface for querying a system based on it's status
    /// </summary>
    public interface IFinchComponent
    {
        /// <summary>
        /// Retrieves finch aggregate data
        /// </summary>
        /// <returns></returns>
        FinchComponentData GetData();
    }
}
