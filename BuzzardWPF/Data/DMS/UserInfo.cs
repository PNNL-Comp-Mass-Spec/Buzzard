namespace BuzzardWPF.Data.DMS
{
    /// <summary>
    /// Class to hold data about LcmsNet users
    /// </summary>
    public class UserInfo
    {
        /// <summary>
        /// Name of user
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// User ID (network login) of user
        /// </summary>
        public string Id { get; set; }

        public override string ToString()
        {
            var name = string.IsNullOrWhiteSpace(Name) ? "Undefined user" : Name;

            if (string.IsNullOrWhiteSpace(Id))
            {
                return name;
            }

            return Id + ": " + name;
        }
    }
}
