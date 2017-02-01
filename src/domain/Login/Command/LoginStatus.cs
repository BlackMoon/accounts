namespace Kit.Dal.Oracle.Domain.Login.Command
{
    public enum LoginStatus
    {
        /// <summary>
        /// Sign in was successful
        /// </summary>
        Success = 0,
        
        /// <summary>
        /// Password expired
        /// </summary>
        Expired = 1,
        
        /// <summary>
        /// Password expiring in {n} days
        /// </summary>
        Expiring = 2,
        
        /// <summary>
        /// Sign in failed
        /// </summary>
        Failure = 3
    }
}